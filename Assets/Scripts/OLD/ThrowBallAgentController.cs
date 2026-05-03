using System.Collections;
using UnityEngine;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class ThrowBallAgentController : BaseSoccerAgentController
{
    private GameObject yellowGoal;
    private float THROW_BALL_TIME = 10f;
    private bool ignoreImprovementFrame = true;
    protected Vector3 goalSize;

    protected override void Start()
    {
        base.Start();

        yellowGoal = fieldUtilities.YellowGoal;
        goalSize = fieldUtilities.GoalSize;

        if (yellowGoal == null)
        {
            Debug.LogError("Yellow Goal not found in FieldUtilities.");
        }
    }

    public override void OnEpisodeBegin()
    {
        setFieldSizeUtility.SetAutoFieldSizeLearnThrow();

        base.OnEpisodeBegin();
        goalSize = fieldUtilities.GoalSize;

        RandomizeGoal();

        ballBehavior.PlaceBall("free1");
        SetRandomLocationAuto();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 ballPos = Ball.transform.localPosition;
        Vector3 agentPos = transform.localPosition;
        Vector3 yellowGoalPos = yellowGoal.transform.localPosition;

        sensor.AddObservation(agentPos.x);
        sensor.AddObservation(agentPos.z);
        sensor.AddObservation(ballPos.x);
        sensor.AddObservation(ballPos.z);
        sensor.AddObservation(yellowGoalPos.x);
        sensor.AddObservation(yellowGoalPos.z);
        sensor.AddObservation(goalSize.x);
        sensor.AddObservation(goalSize.z);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveY = actions.ContinuousActions[0];
        float moveX = actions.ContinuousActions[1];

        Vector2 delta = new Vector2(moveX, moveY);
        Vector2 velocity = delta.normalized * FREE_MOVE_SPEED;

        rb.AddForce(transform.forward * velocity.y + transform.right * velocity.x);
    }

    protected override IEnumerator EvalRoutine()
    {
        float start = Time.time;
        while (Time.time - start < THROW_BALL_TIME)
        {
            if (!ignoreImprovementFrame)
            {
                float velocity_len = Ball.GetComponent<Rigidbody>().velocity.magnitude;
                AgentAddReward(Mathf.Clamp(velocity_len, 0f, 0.6f) * 0.02f);
            }
            else ignoreImprovementFrame = false;

            yield return new WaitForSeconds(0.2f);
        }
        EndEpisode();
    }

    protected override void OnHitWall(GameObject wall)
    {
        AgentAddReward(-0.5f);
    }

    public void Goal(GameObject scoreArea)
    {
        AgentAddReward(50f);
        Debug.Log("Punto! La palla � entrata nella score area.");
        
    }

    private void RandomizeGoal()
    {
        yellowGoal.GetComponent<ScoreUtilities>().SetRandomLocationLearnThrow();
    }
}
