using System.Collections;
using UnityEngine;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class ReachBallAgentController : BaseSoccerAgentController
{
    private GameObject yellowGoal;
    private GameObject blueGoal;
    private float REACH_BALL_TIME = 10f;
    private bool ignoreImprovementFrame = true;
    protected Vector3 goalSize;

    protected override void Start()
    {
        base.Start();

        yellowGoal = fieldUtilities.YellowGoal;
        blueGoal = fieldUtilities.BlueGoal;
    }
    
    public override void OnEpisodeBegin()
    {
        setFieldSizeUtility.SetAutoFieldSizeStandard();

        base.OnEpisodeBegin();
        goalSize = fieldUtilities.GoalSize;

        RandomizeFieldBallAndRobot();
    }
    

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 ballPos = Ball.transform.localPosition;
        Vector3 agentPos = transform.localPosition;
        Vector3 yellowGoalPos = yellowGoal.transform.localPosition;
        Vector3 blueGoalPos = blueGoal.transform.localPosition;

        sensor.AddObservation(agentPos.x);
        sensor.AddObservation(agentPos.z);
        sensor.AddObservation(ballPos.x);
        sensor.AddObservation(ballPos.z);
        sensor.AddObservation(yellowGoalPos.x);
        sensor.AddObservation(blueGoalPos.x);
        sensor.AddObservation(goalSize.x);
        sensor.AddObservation(goalSize.z);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveMagnitude = actions.ContinuousActions[0];
        float moveRotate = actions.ContinuousActions[1];

        rb.AddForce(transform.forward * moveMagnitude * MOVE_SPEED);
        transform.Rotate(0f, moveRotate * ROTATE_SPEED, 0f, Space.Self);
    }

    protected override IEnumerator EvalRoutine()
    {
        float start = Time.time;
        float prevDist = 0;
        Vector3 prevPos = Vector3.zero;
        bool shouldEvaluate = false;

        while (Time.time - start < REACH_BALL_TIME)
        {
            float currentDist = Vector3.Distance(Ball.transform.localPosition, transform.localPosition);
            Vector3 currentPos = transform.localPosition;

            if (!ignoreImprovementFrame && shouldEvaluate)
            {
                AgentAddReward(0.1f);
                if (currentDist < prevDist) AgentAddReward(0.5f);
                else AgentAddReward(-0.6f);

                if (Vector3.Distance(prevPos, currentPos) > 0.1f)
                    AgentAddReward(0.2f);
            }
            else ignoreImprovementFrame = false;

            prevDist = currentDist;
            prevPos = currentPos;
            yield return new WaitForSeconds(0.2f);
        }
        EndEpisode();
    }

    protected override void OnHitBall()
    {
        AgentAddReward(8f);
        ignoreImprovementFrame = true;
        RandomizeBallPositionWhileNearRobot();
    }

    protected override void OnHitWall(GameObject wall)
    {
        AgentAddReward(-30f);
        EndEpisode();
    }

    private void RandomizeFieldBallAndRobot()
    {
        Ball.GetComponent<BallBehavior>().PlaceBall("free1");
        RandomizeRobotPositionWhileNearBall();
    }
}
