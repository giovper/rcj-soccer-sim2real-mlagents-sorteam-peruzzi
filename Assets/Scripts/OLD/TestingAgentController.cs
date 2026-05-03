using System.Collections;
using UnityEngine;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;

public class TestingAgentController : BaseSoccerAgentController
{
    private GameObject yellowGoal;
    private GameObject blueGoal;
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

        ballBehavior.PlaceBall("free1");

        goalSize = fieldUtilities.GoalSize;

        SetRandomLocationAuto();
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
        yield return null;
    }
}
