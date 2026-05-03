using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BehaviorParameters))]
public abstract class ContestantAgentController : Agent
{
    
    protected RewardConfig RewardConf
    {
        get
        {
            return fieldUtilities.contestantConfigLoader.RewCC.GetForPhase(this);
        }
    }

    /* =========================================================================
       CONFIG (dal padre)
       ========================================================================= */
    [Header("Config")]
    private string logPath = "";
    private bool LOG_REWARDS = true;

    [Header("Movement")]
    protected float MOVE_SPEED;
    protected float ROTATE_SPEED;

    protected GameObject Ball;
    protected BallBehavior ballBehavior;
    public GameObject Field;
    public FieldUtilities fieldUtilities {get; private set;}
    protected SetFieldSizeUtility setFieldSizeUtility;

    public Rigidbody rb {get; private set;}
    public MeshRenderer MeshRendererComp;
    public MeshCollider MeshColliderComp;
    public SensorsReader sr;
    protected BehaviorParameters behaviorParameters;

    protected bool allDirMode = true;
    protected float TotReward = 0f;
    protected Coroutine routine;
    protected Coroutine printRoutine;


    /* =========================================================================
       VARIABILI DELLA EX CLASSE FIGLIA
       ========================================================================= */
    protected bool ignoreImprovementEvaluationFrame = true;

    public GameManager gameManager;   // era public nella figlia

    public int TeamID;     // 0 = SEGNA IN PORTA GIALLA | segno della x della pos di partenza = TeamID == 0 ? -1f : 1f;
    public int TeamRole;

    public AgentModeSelector agentModeSelector {get; protected set;}

    protected SensorsReader sensorsReader;

    public bool IsTransformLocked;

    private Dictionary<string, (int num, double value)> rewardCache = new();


    /* =========================================================================
       AWAKE + START
       ========================================================================= */

    // ??? tenevo virtual dal padre?
    protected override void Awake()
    {
        base.Awake();
        //Debug.LogWarning("|" + transform.parent.gameObject.transform.position + TeamID + TeamRole + " Awake chiamato");
        
        //gather immediate references
        sensorsReader = GetComponent<SensorsReader>();
        sensorsReader.contestantAgentController = this;  
        sr = GetComponent<SensorsReader>();
        rb = GetComponent<Rigidbody>();

        behaviorParameters = GetComponent<BehaviorParameters>();

        agentModeSelector = GetComponent<AgentModeSelector>();
        MeshRendererComp = agentModeSelector.MeshRendererComp;
        MeshColliderComp = agentModeSelector.MeshColliderComp;
    }

    public void Start()
    {
        //Debug.LogWarning("|" + transform.parent.gameObject.transform.position + TeamID + TeamRole + " Start chiamato");
    }

    public virtual void GeneralSetup_And_GatherReferences(FieldUtilities fu, GameManager gm, int teamID, int teamRole, ContestantConfigLoader ccl)
    {
        //Debug.LogWarning("|" + transform.parent.gameObject.transform.position + TeamID + TeamRole + " GeneralSetup_And_GatherReferences chiamato");
        fieldUtilities = fu;
        gameManager = gm;
        TeamRole = teamRole;
        TeamID = teamID;

        Field = fieldUtilities.gameObject;
        setFieldSizeUtility = Field.GetComponent<SetFieldSizeUtility>();

        rb.centerOfMass = new Vector3(0f, (float)(-GetShapeHeight()/2.0*0.99), 0f);

        MeshRendererComp.material = teamID == 0 ? agentModeSelector.material_team_0 : agentModeSelector.material_team_1;

        Ball = fieldUtilities.Ball;
        ballBehavior = Ball.GetComponent<BallBehavior>();

        GetConfig(ccl);

        gameManager.EndEpisodeEvent += EndEpisodeFunction;
        gameManager.ReciveActionEvent += ReactToManagerFunction;

    }

    protected virtual void EndEpisodeFunction(object sender, EventArgs ea)
    {
        EndEpisode();
        if (routine != null)
        {
            StopCoroutine(routine);
            StopCoroutine(printRoutine);
        }

        throw new Exception("Devi completare o fare altro? No? Ricopia sta funzione");
    }

    public abstract void PlaceRobot(string position);

    /* =========================================================================
       CONFIG
       ========================================================================= */

    public void GetConfig(ContestantConfigLoader cl)
    {
        logPath = cl.logPath;
        MOVE_SPEED = cl.MOVE_SPEED;
        ROTATE_SPEED = cl.ROTATE_SPEED;
        LOG_REWARDS = cl.LOG_REWARDS;

        
        if (behaviorParameters == null)
        {
            Debug.LogError("BehaviorParameters non trovato sull'agente!");
            return;
        }
        
        if (cl.Parameters != null)
        {
            var sourceParams = cl.Parameters;
            behaviorParameters.BehaviorName = sourceParams.BehaviorName;
            behaviorParameters.InferenceDevice = sourceParams.InferenceDevice;
            behaviorParameters.TeamId = sourceParams.TeamId;
            behaviorParameters.DeterministicInference = sourceParams.DeterministicInference;
            /*
             * Please note that if you choose a Model that has different shape than the expected one, Unity crashes even if you are not doing inference X(
            */
            switch (cl.ExecutionMode)
            {
                case E_ExecutionMode.Inference:
                    behaviorParameters.Model = sourceParams.Model;
                    behaviorParameters.BehaviorType = BehaviorType.InferenceOnly;
                    break;

                case E_ExecutionMode.Heuristic:
                    behaviorParameters.Model = null;
                    behaviorParameters.BehaviorType = BehaviorType.HeuristicOnly;
                    break;

                case E_ExecutionMode.UserVsAgent:
                    if (TeamID == cl.PlayerTeamID)
                    {
                        behaviorParameters.Model = null;
                        behaviorParameters.BehaviorType = BehaviorType.HeuristicOnly;
                    }
                    else
                    {
                        behaviorParameters.Model = sourceParams.Model;
                        behaviorParameters.BehaviorType = BehaviorType.InferenceOnly;
                    }
                    break;

                case E_ExecutionMode.Default:
                default:
                    if (cl.phaseManager.UseDynamicBehaviorTypeAndModel)
                        GetDynamicPhasedBehaviorTypeAndModel();
                    else
                    {
                        behaviorParameters.Model = sourceParams.Model;
                        behaviorParameters.BehaviorType = BehaviorType.Default;
                    }
                    break;
            }
        }
    }

    public void GetDynamicPhasedBehaviorTypeAndModel()
    {
        var (_, model, behaviorType) = fieldUtilities.contestantConfigLoader.phaseManager.ChooseDynamicBehaviorTypeAndModel(this);

        /*if (behaviorType == BehaviorType.InferenceOnly)
        {
            Debug.LogWarning("Inference w/ " + model.name);
        }*/
        //behaviorParameters.BehaviorType = BehaviorType.Default;
        behaviorParameters.Model = model;
        behaviorParameters.BehaviorType = behaviorType;
        
        if (TeamID == 0)
        {
            if (behaviorType == BehaviorType.InferenceOnly)
            {
                MeshRendererComp.material = agentModeSelector.material_team_0_pretrained;
            }
            else
            {
                MeshRendererComp.material = agentModeSelector.material_team_0;
            }
        }
        else if (TeamID == 1)
        {
            if (behaviorType == BehaviorType.InferenceOnly)
            {
                MeshRendererComp.material = agentModeSelector.material_team_1_pretrained;
            }
            else
            {
                MeshRendererComp.material = agentModeSelector.material_team_1;
            }
        }

    }


    /* =========================================================================
       EPISODIO
       ========================================================================= */

    /// ??? era virtual + override separato. Qui uniamo:
    /** SCELTA POSSIBILE:
     * - mantenere override (superfluo in classe unica)
     * - togliere override
     * - mantenere virtual se prevedi altre classi derivate
     */
    public override void OnEpisodeBegin()
    {
        if (fieldUtilities == null) return;
        
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        SetPhysicsEnabled(true);
        PlaceRobot("starting_auto_phase");
        ignoreImprovementEvaluationFrame = true;
        TotReward = 0;
        rewardCache = new();

        routine = StartCoroutine(EvalRoutine());
        printRoutine = StartCoroutine(PrintRoutine());
    }

    protected abstract IEnumerator EvalRoutine();


    /* =========================================================================
       OBSERVATIONS
       ========================================================================= */

    /**
     * ⚠️ Questo era ABSTRACT nel padre.
     * Ora che è un'unica classe, non serve abstract.
     * TODO: decidere se tenerlo virtual o normale.
     */
    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 ballPos = Ball.transform.localPosition;
        Vector3 agentPos = transform.localPosition;
        Vector3 yellowGoalPos = fieldUtilities.YellowGoal.transform.localPosition;
        Vector3 blueGoalPos = fieldUtilities.BlueGoal.transform.localPosition;

        sensor.AddObservation(agentPos.x);
        sensor.AddObservation(agentPos.z);
        sensor.AddObservation(ballPos.x);
        sensor.AddObservation(ballPos.z);

        Debug.LogError("Ma diomera");

        //angoli porte, robot avversari e o alleati

        throw new Exception("Devi completare questo");
    }


    /* =========================================================================
       ACTIONS
       ========================================================================= */

    /**
     * ⚠️ Anche questo era abstract nel padre.
     * TODO: puoi togliere abstract e andare così.
     */
    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveY = actions.ContinuousActions[1];
        float moveRotate = actions.ContinuousActions[2];

        if (!IsTransformLocked)
        {
            rb.AddForce((Vector3.forward * moveX + Vector3.right * moveY) * MOVE_SPEED);
            rb.AddTorque(new Vector3(0f, moveRotate * ROTATE_SPEED, 0f));
        }

        throw new Exception("Devi completare questo");
    }


    /* =========================================================================
       HEURISTIC, UTILS ecc. (dal padre)
       ========================================================================= */

    public override void Heuristic(in ActionBuffers actionsOut)
    {
       var c = actionsOut.ContinuousActions;
        c[1] = Input.GetAxisRaw("Horizontal");
        c[0] = Input.GetAxisRaw("Vertical");
        c[2] = (Input.GetKey(KeyCode.Q) ? 1 : 0) + (Input.GetKey(KeyCode.E) ? -1 : 0);

    }

    public void AddRewardContestantAgent(float r, string cause)
    {
        float delta = (r);

        TotReward += delta;

        if (Input.GetKey(KeyCode.O) ? fieldUtilities.fieldNum == 0  : LOG_REWARDS)
        {
            Debug.Log($"[Reward] [F{fieldUtilities.fieldNum}:T{TeamID}:R{TeamRole}]: {delta} because of '{cause}' | Total: {TotReward}");
        }

        if (rewardCache.ContainsKey(cause))
        {
            (int, double) t = rewardCache[cause];
            t.Item1 += 1;
            t.Item2 += delta;
            rewardCache[cause] = t;
        }
        else
        {
            rewardCache.Add(cause, (1, delta));
        }

        AddReward(delta);
    }

    /* =========================================================================
       COLLISIONI ecc. (padre)
       ========================================================================= */

    public abstract void ReactToManagerFunction(object sender, string eventName);
    protected abstract void OnHitBall();
    protected abstract void OnHitWall(GameObject wall);
    protected abstract void OnHitObject(GameObject obj);

    protected abstract void OnTriggerEnter(Collider other);

    protected void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Target"))
            OnHitBall();

        else if (collision.gameObject.CompareTag("Wall"))
            OnHitWall(collision.gameObject);

        else
            OnHitObject(collision.gameObject);
    }


    /* =========================================================================
       POSIZIONAMENTO ROBOT (padre)
       ========================================================================= */

    private bool SetRandomSingleLocationAlpha(float X, float Y)
    {
        if (!IsTransformLocked)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        rb.rotation = Quaternion.identity;

        Vector3 location = fieldUtilities.GetLocalPositionFromLogicalPos(X, Y); //new Vector3((fieldSize.x) / 20 * X, 0, (fieldSize.y) / 20 * Y);
        transform.localPosition = location;

        bool isColliding = !BoundsCollisionChecker.CheckBoundsCollision(
            MeshColliderComp,
            null,    // Lista degli oggetti da escludere
            Physics.AllLayers,     // <-- Controlla TUTTI i layer possibili
            true,                 // Disegna il parallelepipedo (Rosso/Verde)
            true,                     // Disegna il box (Azzurro) attorno a chi viene toccato
            true,
            3f
        ).Any();

        if (isColliding)
        {
            return true;
        }

        return false;
    }

    protected bool SetRandomLocationRange(Vector2 rangeX, Vector2 rangeY, int maxAttempts = 30)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            if (SetRandomSingleLocationAlpha(UnityEngine.Random.Range(rangeX.x, rangeX.y), UnityEngine.Random.Range(rangeY.x, rangeY.y)))
                return true;
        }
        Debug.LogError("Impossible to place robot");
        Debug.Break();
        return false;
    }

    public double GetShapeRadius()
    {
        return fieldUtilities.robotSizeMeters.x / 2f;
    }

    public double GetShapeHeight()
    {
        return fieldUtilities.robotSizeMeters.y;
    }

    protected void SetPhysicsEnabled(bool enabled)
    {
        rb.isKinematic = !enabled;
        MeshColliderComp.enabled = enabled;
        IsTransformLocked = !enabled;
        rb.useGravity = enabled;
    }

    protected GameObject myGoalToScore () => TeamID == 0 ? fieldUtilities.YellowGoal : fieldUtilities.BlueGoal;

    protected IEnumerator PrintRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(fieldUtilities.contestantConfigLoader.PRINT_TIME_EVAL);

            if (Input.GetKey(KeyCode.I) ? fieldUtilities.fieldNum == 0 : LOG_REWARDS)
            {
                string msg = $"Tot rew: {TotReward} | ";

                foreach (var cause in rewardCache.Keys)
                {
                    msg += $"{cause}={rewardCache[cause]}, ";
                }

                Debug.Log($"[Print] [F{fieldUtilities.fieldNum}:T{TeamID}:R{TeamRole}]: " + msg);
            }
        }
    }
}
