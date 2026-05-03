using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BehaviorParameters))]
//[RequireComponent(typeof(SetFieldSizeUtility))]
public class SoccerAgentController : Agent
{
    [SerializeField] bool AUTO_CONFIGURATION = true;
    [SerializeField] string logPath = "";
    [SerializeField] float MOVE_SPEED = 0.85f;
    [SerializeField] float ROTATE_SPEED = 200f;
    [SerializeField] float FREE_MOVE_SPEED = 0.8f;
    [SerializeField] E_Mode PlayMode;
    [SerializeField] float REACH_BALL_TIME = 10f;
    [SerializeField] float THROW_BALL_TIME = 10f;
    [SerializeField] bool LOG_REWARDS = true;

    string inputs;
    string prevLine;
    bool allDirMode = true;
    Rigidbody rb;
    GameObject Field;
    Transform target;
    GameObject Ball;
    SetFieldSizeUtility SetFieldSize;
    FieldUtilities fieldUtilities;
    private Coroutine currentRoutine;
    bool isAutonomous;
    BehaviorParameters behaviorParameters;
    bool ignoreImprovementEvaluationFrame = true;
    float TotReward = 0;

    private void Initialization()
    {
        rb = GetComponent<Rigidbody>();
        if (transform.parent == null)
        {
            Debug.LogError("Nessun parent trovato!");
            return;
        }
        
        if (!transform.parent.parent.CompareTag("SoccerEnv"))
        {
            Debug.LogError("Il parent non ha il tag SoccerEnv!");
            return;
        }
        
        Field = transform.parent.parent.gameObject;

        if (Field.GetComponent<FieldUtilities>() == null) 
        {
            Debug.LogError("fieldUtilities non trovato!");
            return;
        }

        fieldUtilities = Field.GetComponent<FieldUtilities>();

        fieldUtilities.SetReferences();

        if (fieldUtilities.Ball == null)
        {
            Debug.LogError("Ball non trovato!");
            return;
        }
        
        target = fieldUtilities.Ball?.transform;
        Ball = fieldUtilities.Ball;
        SetFieldSize = GetComponent<SetFieldSizeUtility>();

        if (SetFieldSize == null)
        {
            Debug.LogError("SetFieldSizeUtility non trovato!");
        }
    }

    private new void Awake()
    {
        if (!AUTO_CONFIGURATION)
            return;

        // Configuration And Enable Setup
        isAutonomous = !gameObject.transform.root.gameObject.CompareTag("Envs");
        GameObject configLoaderObject = FindFirstRootWithTag("ConfigLoader");

        if (!configLoaderObject)
        {
            Debug.LogError("No ConfigLoaderManager Found");
            return;
        }

        behaviorParameters = GetComponent<BehaviorParameters>();

        ConfigLoader configLoader = configLoaderObject.GetComponent<ConfigLoader>();   

        if (!configLoader)
        {
            Debug.LogError("No ConfigLoader Script Found");
            return;
        }  

        if (configLoader.AUTO_CONFIGURATION)
            GetConfig(configLoader);
            
    }

    private void Start()
    {
        //Debug.LogError($"START ORAAA {gameObject.transform.parent.parent.gameObject.name}");

        Initialization();

        //Debug.LogError();
        
        // Initialize vars used to track getting better in time
        
    }

    public override void OnEpisodeBegin()
    {
        //Debug.Log("New Episode");

        if (target == null)
            Debug.Log("No Ball target");

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        // Randomize
        
        var ballScript = Ball?.GetComponent<BallBehavior>();
        switch (PlayMode)
        {
            case E_Mode.Testing:
                ballScript.PlaceBall("free1");
                SetRandomLocationAuto();
                break;
            case E_Mode.ThrowBallTraining:
                RandomizeFieldBallAndRobot();
                currentRoutine = StartCoroutine(ThrowBallFromRandomPositionRoutine());
                break;
            case E_Mode.ReachBall:
                RandomizeFieldBallAndRobot();
                currentRoutine = StartCoroutine(ReachBallRoutine());
                break;
        }
    }
    
    private IEnumerator ReachBallRoutine()
    {
        float start = Time.time;
        float prevDist = 0;
        Vector3 prevPos = new Vector3();

        bool shouldEvaluateImprovement = false;

        while (Time.time - start < REACH_BALL_TIME) // Evaluate improvement in time
        {
            float currentDist = Vector3.Distance(target.localPosition, transform.localPosition);
            Vector3 currentPos = transform.localPosition;
            if (!ignoreImprovementEvaluationFrame && shouldEvaluateImprovement)
            {
                
                AgentAddReward(0.1f);
                if (currentDist < prevDist)
                {
                    AgentAddReward(0.5f);
                }                    
                else
                {
                    AgentAddReward(-0.6f);
                }
                if (Vector3.Distance(prevPos, currentPos) > 0.1)
                    AgentAddReward(0.2f);
                
            }
            else
            {
                ignoreImprovementEvaluationFrame = false;
            }
            prevDist = currentDist;
            prevPos = currentPos;
            
            yield return new WaitForSeconds(0.2f);
        }

        EndEpisode();
    }

    private IEnumerator ThrowBallFromRandomPositionRoutine()
    {
        float start = Time.time;

        bool shouldEvaluateImprovement = true;

        while (Time.time - start < THROW_BALL_TIME) // Evaluate improvement in time
        {
            float currentDist = Vector3.Distance(target.localPosition, transform.localPosition);
            Vector3 currentPos = transform.localPosition;
            if (!ignoreImprovementEvaluationFrame && shouldEvaluateImprovement)
            {
                float velocity_len = Ball.GetComponent<Rigidbody>().velocity.magnitude;
                float reward = Mathf.Clamp(velocity_len, 0f, 0.6f) * 0.02f;
                AgentAddReward(reward);
            }
            else
            {
                ignoreImprovementEvaluationFrame = false;
            }
            
            yield return new WaitForSeconds(0.2f);
        }

        EndEpisode();
    }

    private void OnHitBall ()
    {
        switch (PlayMode)
        {
            case E_Mode.ThrowBallTraining:
                AgentAddReward(0f);
                break;
            case E_Mode.ReachBall:
                AgentAddReward(8f);
                ignoreImprovementEvaluationFrame = true;
                RandomizeBallPositionWhileNearRobot();
                break;
        }
    }

    private void OnHitWall(GameObject wall)
    {
        switch (PlayMode)
        {
            case E_Mode.ThrowBallTraining:
                AgentAddReward(-0.5f);
                break;
            case E_Mode.ReachBall:
                AgentAddReward(-30f);
                EndEpisode();
                break;
        }
    }

    public void Goal(GameObject scoreArea)
    {
        switch (PlayMode)
        {
            case E_Mode.ThrowBallTraining:
                Debug.Log("Punto! La palla è entrata nella score area.");
                AgentAddReward(50f);
                break;
            case E_Mode.ReachBall:
            
                break;
        }
    }

    #region ML_Agents_Functions

    public override void CollectObservations(VectorSensor sensor)
    {
        //Debug.LogError($"COLLECTOBS {gameObject.transform.parent.parent.gameObject.name}");
        Vector3 ballPos = target.localPosition;
        Vector3 agentPos = transform.localPosition;
        Vector3 yellowGoalPos = fieldUtilities.YellowGoal.transform.localPosition;
        Vector3 blueGoalPos = fieldUtilities.BlueGoal.transform.localPosition;
        //Vector3 enemyAgentPos;
        sensor.AddObservation(agentPos.x);
        sensor.AddObservation(agentPos.z);
        sensor.AddObservation(ballPos.x);
        sensor.AddObservation(ballPos.z);
        sensor.AddObservation(yellowGoalPos.x);
        sensor.AddObservation(blueGoalPos.x);
        //sensor.AddObservation(fieldUtilities.FieldSize.x);
        //sensor.AddObservation(fieldUtilities.FieldSize.y);
        sensor.AddObservation(fieldUtilities.GoalSize.x);
        sensor.AddObservation(fieldUtilities.GoalSize.z);

        inputs = $"{transform.localPosition.ToString()} , {target.localPosition.ToString()}";
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (logPath != ""){

            string line = $""; //here

            if (line != prevLine)
            {
                if (logPath == "debug")
                    Debug.Log(line);
                else
                    using (StreamWriter sw = File.AppendText(logPath))
                        sw.WriteLine(line);

                prevLine = line;
            }
        }

        if (PlayMode != E_Mode.Testing)
        {
            if (allDirMode)
            {
                float moveY = actions.ContinuousActions[0];
                float moveX = actions.ContinuousActions[1];

                Vector2 delta = new Vector2(moveX, moveY);
                Vector2 velocity = delta.normalized * FREE_MOVE_SPEED;

                rb.AddForce(transform.forward * velocity.y + transform.right * velocity.x);
                
            }
            else
            {
                float moveMagnitude = actions.ContinuousActions[0];
                float moveRotate = actions.ContinuousActions[1];

                rb.AddForce(transform.forward * moveMagnitude * MOVE_SPEED);
                transform.Rotate(0f, moveRotate * ROTATE_SPEED, 0f, Space.Self);
            }
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;

        if (PlayMode == E_Mode.Testing)
        {
            continuousActions[0] = 0f;
            continuousActions[1] = 0f;
            return;
        }

        continuousActions[1] = Input.GetAxisRaw("Horizontal");
        continuousActions[0] = Input.GetAxisRaw("Vertical");
    }

    #endregion

    #region VariousFunctions

    private void RandomizeFieldBallAndRobot()
    {
        SetFieldSize.SetAutoFieldSizeStandard();
        Ball.GetComponent<BallBehavior>().PlaceBall("free1");

        RandomizeRobotPositionWhileNearBall();
    }

    private void RandomizeRobotPositionWhileNearBall()
    {
        do
        {
            SetRandomLocationAuto();
        } while (Vector3.Distance(transform.position, target.position) < 0.2f);
    }

    private void RandomizeBallPositionWhileNearRobot()
    {
        do
        {
            Ball.GetComponent<BallBehavior>().PlaceBall("free1");
        } while (Vector3.Distance(transform.position, target.position) < 0.2f);
    }

    public void SetRandomLocationAlpha (float X, float Y)
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.rotation = Quaternion.Euler(0, 0, 0);

        Vector2 fieldSize = Field.GetComponent<FieldUtilities>().FieldSize;
        transform.localPosition = new Vector3((fieldSize.x-10)/20 * X, 0, (fieldSize.y-10)/20 * Y);
    }

    public void SetRandomLocationAuto ()
    {
        SetRandomLocationAlpha(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
    }


    private void OnTriggerEnter(Collider other)
    {
        // Overlap
    }

    void OnCollisionEnter(Collision collision)
    {
        // Hit

        //Debug.Log("Collisione con: " + collision.gameObject.name);

        if (collision.gameObject.tag == "Target")
        {
            OnHitBall();
        }
        
        
        if (collision.gameObject.tag == "Wall")
        {
            OnHitWall(collision.gameObject);
        }
    }
    GameObject FindFirstRootWithTag(string tag)
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject obj in objs)
        {
            if (obj.transform.parent == null) // è nella root della scena
            {
                return obj;
            }
        }
        return null; // nessun oggetto con quel tag in root
    }

    private void GetConfig(ConfigLoader cl)
    {
        logPath = cl.logPath;
        MOVE_SPEED = cl.MOVE_SPEED;
        ROTATE_SPEED = cl.ROTATE_SPEED;
        FREE_MOVE_SPEED = cl.FREE_MOVE_SPEED;
        PlayMode = cl.PlayMode;
        REACH_BALL_TIME = cl.REACH_BALL_TIME;
        THROW_BALL_TIME = cl.THROW_BALL_TIME;
        LOG_REWARDS = cl.LOG_REWARDS;

        if (behaviorParameters == null)
        {
            Debug.LogError("BehaviorParameters non trovato sull'agente!");
            return;
        }

        if (cl.Parameters != null)
        {
            behaviorParameters.BehaviorType = cl.Parameters.BehaviorType;
            behaviorParameters.BehaviorName = cl.Parameters.BehaviorName;
            behaviorParameters.Model = cl.Parameters.Model;   // <--- assegni direttamente il NNModel o ModelAsset
            behaviorParameters.InferenceDevice = cl.Parameters.InferenceDevice;
            behaviorParameters.DeterministicInference = cl.Parameters.DeterministicInference;
        }

        Debug.Log($"{PlayMode} | {behaviorParameters.BehaviorType} | {(behaviorParameters.Model ? behaviorParameters.Model.name : "NO MODEL")}");
    }

    private void AgentAddReward (float r)
    {
        TotReward += r;
        if (LOG_REWARDS)
            Debug.Log("Reward Gained: " + r + " | Total reward: " + TotReward);
        AddReward(r);
    }

    #endregion
}
