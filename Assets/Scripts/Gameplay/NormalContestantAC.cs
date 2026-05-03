using Assets.Scripts.New;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEngine;

public class NormalContestantAC : ContestantAgentController
{
    Coroutine RePlace_Robot_Coroutine;
    protected override IEnumerator EvalRoutine()
    {
        if (RePlace_Robot_Coroutine != null) StopCoroutine(RePlace_Robot_Coroutine);

        float start = Time.time;
        bool evalBallProximity = true;
        bool evalBallLikelyToScore = true;

        Vector3 prevBallPos = new Vector3();

        while (true)
        {
            Vector3 currentBallPos = fieldUtilities.Ball.transform.localPosition;
            Vector2 logicalBallPos = fieldUtilities.GetLogicalPositionFromLocalPos(currentBallPos);
            float currentDistance = Vector2.Distance(new Vector2(transform.localPosition.x, transform.localPosition.z), new Vector2(currentBallPos.x, currentBallPos.z));

            if (!ignoreImprovementEvaluationFrame)
            {

                if (evalBallProximity)
                {
                    if (currentDistance < 0.2f)
                        AddRewardContestantAgent(RewardConf.proximityVeryClose, "eval proximity 0");
                    else if (currentDistance < 0.3f)
                        AddRewardContestantAgent(RewardConf.proximityClose, "eval proximity 1");
                    else if (currentDistance < 0.6f)
                        AddRewardContestantAgent(RewardConf.proximityMedium, "eval proximity 2");
                    else if (currentDistance < 1f)
                        AddRewardContestantAgent(RewardConf.proximityFar, "eval proximity 3");
                    else
                        AddRewardContestantAgent(RewardConf.proximityVeryFar, "eval proximity 4");
                }

                if (evalBallLikelyToScore)
                {
                    float alphaY = (0.4f - Math.Abs(logicalBallPos.y)) * 2f; // -1 -> 1
                    float alphaX = logicalBallPos.x * (TeamID == 0 ? 1f : -1f); // -1 -> 1

                    // Moltiplicativa: X reward azzerata se la palla è fuori dalla fascia centrale Y (alphaY<=0)
                    float alpha = alphaX * Mathf.Max(0f, alphaY); // da 1 a -1 in base alla x, ma solo se |Y|<0.4
                    if (alphaY < 0)
                        alpha -= 0.4f;

                    AddRewardContestantAgent(alpha * RewardConf.ballLikelyToScore, "eval ball likely to score");
                }
            }
            else
            {
                ignoreImprovementEvaluationFrame = false;
            }

            prevBallPos = currentBallPos;

            yield return new WaitForSeconds(fieldUtilities.contestantConfigLoader.DELTA_TIME_EVAL);
        }
    }

    protected override void EndEpisodeFunction(object sender, EventArgs ea)
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
        if (printRoutine != null) { StopCoroutine(printRoutine); printRoutine = null; }
        EndEpisode(); // questa va sempre per ultima
    }

    public override void PlaceRobot(string position)
    {
        bool hasPhysics;
        if ( !(hasPhysics = !IsTransformLocked) )
        {
            SetPhysicsEnabled(true);
        }

        //corpo

        if (position == "starting_auto_phase")
        {
            //int phase = fieldUtilities.contestantConfigLoader.phaseManager.CurrentPhase;

            PlaceRobot("starting_normal");
        }
        else if (position == "starting_normal")
        {
            SetRandomLocationRange(new Vector2(-0.8f, 0.8f), new Vector2(-0.8f, 0.8f));
        }
        else if (position == "starting_lateral")
        {
            float sign = TeamID == 0 ? -1f : 1f;

            // Agente: metà campo sua, vicino alla palla
            SetRandomLocationRange(
                new Vector2(0.3f * sign, 0.7f * sign),
                new Vector2(-0.4f, 0.4f)
            );
            
        }
        else if (position == "raised")
        {
            Vector3 pos = transform.position;
            pos.y += 0.3f;
            transform.position = pos;
        }
        if (position == "random")
        {
            SetRandomLocationRange(new Vector2(-0.9f, 0.9f), new Vector2(-0.9f, 0.9f));   
        }

        //fine
        if (!hasPhysics)
            SetPhysicsEnabled(false);
    }

    const float AGENT_NOISE  = 0.04f;
    const float MIN_BALL_NOISE = 0.01f;
    const float MAX_BALL_NOISE = 0.06f;
    const float MAX_NOISE_MAX_DISTANCE = 2f;

    public override void CollectObservations(VectorSensor sensor)
    {
        bool naturalDir = TeamID == 0;
        Vector2 trueAgentPos = fieldUtilities.GetLogicalPositionFromLocalPos(transform.localPosition, naturalDir);
        Vector2 trueBallPos  = fieldUtilities.GetLogicalPositionFromLocalPos(Ball.transform.localPosition, naturalDir);
        Vector2 delta = trueBallPos - trueAgentPos;

        float trueDist = delta.magnitude;
        float adaptiveNoise = Mathf.Lerp(MIN_BALL_NOISE, MAX_BALL_NOISE, Mathf.Clamp01(trueDist / MAX_NOISE_MAX_DISTANCE));

        Vector2 agentPos = trueAgentPos.GetNoise(AGENT_NOISE);
        Vector2 ballPos  = (agentPos + delta).GetNoise(adaptiveNoise);

        float center = sr.GetCenterSensorValueFloat();
        float dist = sr.GetDistanceValue(sr.GetMaxDistanceBetweenLinesFelt());

        sensor.AddObservation(agentPos.x);
        sensor.AddObservation(agentPos.y);
        sensor.AddObservation(ballPos.x);
        sensor.AddObservation(ballPos.y);

        sensor.AddObservation(dist);
        sensor.AddObservation(center);

        // 6.5 - Robot velocity (oriented)
        sensor.AddObservation(rb.velocity.z.DirectFloat(naturalDir));
        sensor.AddObservation(rb.velocity.x.DirectFloat(naturalDir));

        // 6.6 - Ball velocity (oriented)
        sensor.AddObservation(ballBehavior.rb.velocity.z.DirectFloat(naturalDir));
        sensor.AddObservation(ballBehavior.rb.velocity.x.DirectFloat(naturalDir));


        /*
        if (Input.GetKey(KeyCode.I) ? fieldUtilities.fieldNum == 0 : fieldUtilities.contestantConfigLoader.LOG_REWARDS)
        {
            if (TeamID == 1)
                Debug.Log($"[Agent Obs] [F{fieldUtilities.fieldNum}:T{TeamID}:R{TeamRole}] >> " + $"{agentPos.x} | {agentPos.y} | Ball: {ballPos.x} | {ballPos.y} | Enemy: {enemyPos.x} | {enemyPos.y} | Sensors: {dist} | {center}");
        }
        */
        
        /*
        sensor.AddObservation(MyGoalAngles.Item1.x);
        sensor.AddObservation(MyGoalAngles.Item1.z);
        sensor.AddObservation(MyGoalAngles.Item2.z);
        */

        //Vector2 trueEnemyPos = fieldUtilities.GetLogicalPositionFromLocalPos((fieldUtilities.Agents.FirstOrDefault((agent) => agent.GetComponent<AgentModeSelector>().TeamID != TeamID)).transform.localPosition, naturalDir);
        
        //Vector3 yellowGoalPos = fieldUtilities.YellowGoal.transform.localPosition;
        //Vector3 blueGoalPos = fieldUtilities.BlueGoal.transform.localPosition;
        //(Vector3, Vector3) MyGoalAngles = (TeamID == 0 ? fieldUtilities.BlueGoal : fieldUtilities.YellowGoal).GetComponent<ScoreUtilities>().LocalAngles;


        //sensor.AddObservation(enemyPos.x);
        //sensor.AddObservation(enemyPos.y);

        //angoli porte, robot avversari e o alleati

    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (IsTransformLocked)
            return;

        bool naturalDir = TeamID == 0;
        
        float moveX = actions.ContinuousActions[0].DirectFloat(naturalDir);
        float moveY = actions.ContinuousActions[1].DirectFloat(naturalDir);
        float moveRotate = actions.ContinuousActions[2]; //non so se è da direzionare

        float vectorLen = (float)Math.Sqrt(moveX*moveX + moveY*moveY);
        if (vectorLen > 1.0f)
        {
            moveX /= vectorLen;
            moveY /= vectorLen;
        }
        
        rb.AddForce((Vector3.forward * moveX + Vector3.right * moveY) * MOVE_SPEED);
        rb.AddTorque(new Vector3(0f, moveRotate * ROTATE_SPEED, 0f));

        //coreggere movimento

        //Debug.Log($"Action recived: {moveX}, {moveY}, {moveRotate}");
    }

    protected override void OnHitBall()
    {
        // 1. Calcolo Forza
        float force = ballBehavior.rb.velocity.magnitude;
        force = Mathf.Clamp(force, 0f, 1.6f);
        float val = (0.1f + Mathf.Pow(force * 2f, 1.2f)*0.4f) * 10f;

        // 2. Calcolo della precisione (Verso la porta)
        Vector3 direzionePalla = ballBehavior.rb.velocity.normalized;
        Vector3 direzionePorta = (myGoalToScore().transform.position - ballBehavior.transform.position).normalized;

        // Il Dot Product restituisce 1 se le direzioni sono identiche, 0 se sono perpendicolari
        float precisione = Vector3.Dot(direzionePalla, direzionePorta);

        // 3. Normalizziamo la precisione (ignoriamo i tiri all'indietro)
        float precisionePremio = Mathf.Max(0, RewardConf.hitBallPrecisionOffset + precisione * 1.2f);

        // 4. Calcolo Finale: Moltiplichiamo la forza per la precisione
        // In questo modo, un tiro fortissimo ma nella direzione sbagliata vale poco.
        val *= RewardConf.hitBallForceAlpha;
        precisionePremio *= RewardConf.hitBallDirectionAlpha;

        float rewardFinale = Math.Clamp(val * precisionePremio, 0f, 4.5f * RewardConf.hitBallDirectionAlpha * RewardConf.hitBallForceAlpha);

        AddRewardContestantAgent(rewardFinale, "hit ball toward goal");

        gameManager.AgentHitBall(this);
    }

    protected override void OnHitWall(GameObject wall)
    {
        //gemini dice di abbassarlo dopo la run da 10M 10h del 2/3/26
        AddRewardContestantAgent(RewardConf.hitWall, "hit wall");
        PlaceRobot("raised");
        SetPhysicsEnabled(false); // Disabilita fisica e collider mentre è sollevato

        RePlace_Robot_Coroutine = StartCoroutine(RePlaceRobot(3f)); // Cambia 5f al numero di secondi desiderato

        gameManager.AgentHitWall(this);
    }

    protected IEnumerator RePlaceRobot(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);

        SetPhysicsEnabled(true); // Riabilita fisica e collider
        PlaceRobot("random"); // Posiziona il robot in una posizione casuale sul campo
    }

    protected override void OnHitObject(GameObject obj)
    {

    }

    protected override void OnTriggerEnter(Collider other)
    {

    }

    public override void ReactToManagerFunction(object sender, string eventName)
    {
        // prima, quando l'episodio non era terminato al goal, riportava il robot in posizione al goal
    }

}
