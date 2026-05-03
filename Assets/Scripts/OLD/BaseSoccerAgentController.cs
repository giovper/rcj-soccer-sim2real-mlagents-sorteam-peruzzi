using System.Collections;
using System.IO;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BehaviorParameters))]
public abstract class BaseSoccerAgentController : Agent
{
    [Header("Config")]
    [SerializeField] protected bool AUTO_CONFIGURATION = true;
    [SerializeField] protected string logPath = "";
    [SerializeField] protected bool LOG_REWARDS = true;

    [Header("Movement")] //defaults in configloader
    [SerializeField] protected float MOVE_SPEED = 200f;
    [SerializeField] protected float ROTATE_SPEED = 200f;
    [SerializeField] protected float FREE_MOVE_SPEED = 0.8f;

    protected GameObject Ball;
    protected BallBehavior ballBehavior;
    protected GameObject Field;
    protected FieldUtilities fieldUtilities;
    protected SetFieldSizeUtility setFieldSizeUtility;

    protected Rigidbody rb;
    protected BehaviorParameters targetParams;

    protected bool allDirMode = true;
    protected bool isAutonomous;
    protected float TotReward = 0f;
    protected Coroutine routine;

    protected Vector2 fieldSize;

    protected override void Awake()
    {
        OnAwake();
    }

    protected virtual void OnAwake()
    {
        if (!AUTO_CONFIGURATION) return;

        rb = GetComponent<Rigidbody>();
        targetParams = GetComponent<BehaviorParameters>();
        isAutonomous = !transform.root.CompareTag("Envs");
        Field = InitAgentUtilities.GetFieldParent(this.gameObject);
        setFieldSizeUtility = Field.GetComponent<SetFieldSizeUtility>();
        fieldUtilities = Field.GetComponent<FieldUtilities>();
        Ball = fieldUtilities.Ball;
        ballBehavior = Ball.GetComponent<BallBehavior>();

        GameObject cfg = FindFirstRootWithTag("ConfigLoader");
        if (cfg)
        {
            ConfigLoader cl = cfg.GetComponent<ConfigLoader>();
            if (cl != null && cl.AUTO_CONFIGURATION)
                GetConfig(cl);
        }
    }
    /// <summary>
    /// In the Start() method in child classes, call base.Start() as the start: it sets up co-routines
    /// Then, in Start() method of child classes, you can set references that always exist and always are the same (like goals, unlike sizes)
    /// </summary>
    /// 
    protected virtual void Start()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(EvalRoutine());
    }

    protected abstract IEnumerator EvalRoutine();

    protected void GetConfig(ConfigLoader cl)
    {
        logPath = cl.logPath;
        MOVE_SPEED = cl.MOVE_SPEED;
        ROTATE_SPEED = cl.ROTATE_SPEED;
        FREE_MOVE_SPEED = cl.FREE_MOVE_SPEED;
        LOG_REWARDS = cl.LOG_REWARDS;

        if (targetParams == null)
        {
            Debug.LogError("BehaviorParameters non trovato sull'agente!");
            return;
        }

        if (cl.Parameters != null)
        {
            var sourceParams = cl.Parameters;
            targetParams.BehaviorType = sourceParams.BehaviorType;
            targetParams.BehaviorName = sourceParams.BehaviorName;
            targetParams.Model = sourceParams.Model;
            targetParams.InferenceDevice = sourceParams.InferenceDevice;
            targetParams.DeterministicInference = sourceParams.DeterministicInference;

            targetParams.TeamId = sourceParams.TeamId;
        }
    }

    // 🔹 Da implementare nelle classi figlie

    /// <summary>
    /// base.OnEpisodeBegin() imposta semplicemente la fieldSize alla dimensione del campo fieldUtilities.FieldSize
    /// È perciò necessario che nelle classi figlie OnEpisodeBegin() prima imposti la dimensione del campo vouta (setFieldSizeUtility.SetAutoFieldSizeTYPE)
    /// successivamente chiami base.OnEpisodeBegin() e successivamente imposti altre referenze dinamiche/variabili o randomizzi posizioni e altro
    /// </summary>
    public override void OnEpisodeBegin()
    {
        fieldSize = fieldUtilities.FieldSize;
    }

    public abstract override void CollectObservations(VectorSensor sensor);
    public abstract override void OnActionReceived(ActionBuffers actions);

    // 🔹 Heuristic comune, ma può essere sovrascritta
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if (allDirMode)
        {
            var c = actionsOut.ContinuousActions;

            float dx_raw = Input.GetAxisRaw("Horizontal");
            float dy_raw = Input.GetAxisRaw("Vertical");

            float lunghezza = Mathf.Sqrt(dx_raw * dx_raw + dy_raw * dy_raw);
            float dx = 0f;
            float dy = 0f;

            if (lunghezza > 0.001f)
            {
                dx = dx_raw / lunghezza;
                dy = dy_raw / lunghezza;
            }

            c[0] = dy;
            c[1] = dx;
        }
        else
        {
            var c = actionsOut.ContinuousActions;
            c[1] = Input.GetAxisRaw("Horizontal");
            c[0] = Input.GetAxisRaw("Vertical");
        }
    }

    // 🔹 Utility per aggiungere reward con log
    protected void AgentAddReward(float r)
    {
        TotReward += r;
        if (LOG_REWARDS)
            Debug.Log($"Reward: {r} | Total: {TotReward}");
        AddReward(r);
    }

    protected GameObject FindFirstRootWithTag(string tag)
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject obj in objs)
        {
            if (obj.transform.parent == null)
                return obj;
        }
        return null;
    }

    // 🔹 Virtuali per personalizzazioni
    protected virtual void OnHitBall() { }
    protected virtual void OnHitWall(GameObject wall) { }
    protected virtual void OnTriggerEnter(Collider other) { }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Target"))
            OnHitBall();

        if (collision.gameObject.CompareTag("Wall"))
            OnHitWall(collision.gameObject);
    }

    // 🔹 Posizionamento robot
    protected virtual bool SetRandomSingleLocationAlpha(float X, float Y)
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.rotation = Quaternion.identity;

        Vector2 fieldSize = fieldUtilities.FieldSize;

        Vector3 location = new Vector3( (fieldSize.x) / 20 * X, 0, (fieldSize.y) / 20 * Y);
        transform.localPosition = location;

        if (!InitAgentUtilities.IsColliderColliding(gameObject.GetComponent<MeshCollider>(), new(), "", true, true))
        {
            return true;
        }

        return false;
    }

    protected virtual bool SetRandomLocationAuto(int maxAttempts = 30)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            if (SetRandomSingleLocationAlpha(Random.Range(-1f, 1f), Random.Range(-1f, 1f)))
                return true;
        }
        Debug.LogError("Impossible to place robot");
        Time.timeScale = 0;
        return false;
    }

    public double GetShapeRadius()
    {
        return (transform.lossyScale / 2).x;
    }

    protected void RandomizeRobotPositionWhileNearBall(int cm = 4)
    {
        do { SetRandomLocationAuto(); }
        while (
            Vector3.Distance(
                InitAgentUtilities.ZeroY(transform.position),
                InitAgentUtilities.ZeroY(Ball.transform.position)
            ) < (ballBehavior.GetShapeRadius() + GetShapeRadius() + cm / 100.0)
        );
    }

    protected void RandomizeBallPositionWhileNearRobot(int cm = 4)
    {
        do { ballBehavior.PlaceBall("free1"); }
        while (
            Vector3.Distance(
                InitAgentUtilities.ZeroY(transform.position),
                InitAgentUtilities.ZeroY(Ball.transform.position)
            ) < (ballBehavior.GetShapeRadius() + GetShapeRadius() + cm / 100.0)
        );
    }
}
