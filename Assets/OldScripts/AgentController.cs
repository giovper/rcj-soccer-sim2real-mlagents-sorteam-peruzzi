using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class AgentController : Agent
{
    [SerializeField] private Transform target;
    [SerializeField] private string logPath = "";
    [SerializeField] private bool isTestingMode = false;

    float prevDistance = 0;
    string inputs;
    string prevLine;

    private void Start()
    {
        StartCoroutine(TimedRoutine());
        prevDistance = Vector3.Distance(transform.position, target.position);
    }

    private IEnumerator TimedRoutine()
    {
        while (true)
        {
            float distance = Vector3.Distance(transform.position, target.position);

            if (distance < prevDistance)
            {
                AddReward(0.1f);
            }

            prevDistance = distance;
            yield return new WaitForSeconds(0.4f);
        }
    }

    public override void OnEpisodeBegin()
    {
        target.localPosition = new Vector3(Random.Range(-4f, 4f), 0.3f, 0f);

        transform.localPosition = new Vector3(0, 0.3f, 0f);
        
        if (isTestingMode)
        {
            transform.localPosition = new Vector3(1.43f, 0.3f, 0f);
        }
        else
        {
            do
            {
                transform.localPosition = new Vector3(Random.Range(-4, 4), 0.3f, 0f);
            } while (Vector3.Distance(transform.position, target.position) < 2f);  
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition.x);
        sensor.AddObservation(transform.localPosition.y);
        sensor.AddObservation(transform.localPosition.z);
        sensor.AddObservation(target.localPosition.x);
        sensor.AddObservation(target.localPosition.y);
        sensor.AddObservation(target.localPosition.z);

        inputs = $"{transform.localPosition.ToString()} , {target.localPosition.ToString()}";
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float move = actions.ContinuousActions[0];
        float moveSpeed = 2f;

        if (logPath != ""){
            string line = $"{inputs} => {move}";
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

        if (!isTestingMode)
            transform.localPosition += new Vector3(move, 0f) * Time.deltaTime * moveSpeed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Target")
        {
            AddReward(10f);
            EndEpisode();
        }
        if (other.gameObject.tag == "Wall")
        {
            AddReward(-5f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if (isTestingMode) return;

        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Horizontal");
    }
}
