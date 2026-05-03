using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace Assets.Scripts.New
{
    public abstract class OldContestantAgentController : BaseSoccerAgentController
    {
        private GameObject yellowGoal;
        private GameObject blueGoal;
        //private bool ignoreImprovementFrame = true;
        protected Vector3 goalSize;
        public GameManager gameManager; //impostato dall'esterno, da AgentModeSelector, quando creato
        protected float EVAL_DELTA_TIME = 0.2f;

        public bool FirstTeam = true; //first deve segnare nel giallo //impostato dall'esterno, da AgentModeSelector

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
            ballBehavior.PlaceBall("free1");
            SetRandomLocationAuto();
        }


        public override void CollectObservations(VectorSensor sensor) //standard
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

        public override void OnActionReceived(ActionBuffers actions) //standard
        {
            float moveMagnitude = actions.ContinuousActions[0];
            float moveRotate = actions.ContinuousActions[1];

            rb.AddForce(transform.forward * moveMagnitude * MOVE_SPEED);
            transform.Rotate(0f, moveRotate * ROTATE_SPEED, 0f, Space.Self);
        }

        protected void StartTimer(float interval)
        {
            // Se c'è già un timer attivo, lo fermiamo
            if (routine != null)
                StopCoroutine(routine);

            routine = StartCoroutine(EvalRoutine());
        }

        // Ferma il timer
        protected void StopTimer()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
        }

        public virtual void OnEpisodeEnded()
        {
            StopTimer();
        }

        
        /*protected override IEnumerator EvalRoutine()
        {
            float start = Time.time;
            float prevDist = 0;
            Vector3 prevPos = Vector3.zero;
            bool shouldEvaluate = true;

            while (true)
            {
                float currentDist = Vector3.Distance(Ball.transform.localPosition, transform.localPosition);
                Vector3 currentPos = transform.localPosition;

                if (!ignoreImprovementFrame && shouldEvaluate)
                {
                    //..
                }
                else
                    ignoreImprovementFrame = false;

                prevDist = currentDist;
                prevPos = currentPos;
                yield return new WaitForSeconds(EVAL_DELTA_TIME);
            }
        }*/

    }
}
