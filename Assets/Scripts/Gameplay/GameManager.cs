using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.Sentis;

public class GameManager : MonoBehaviour
{
    public static Vector2 OUT_OF_FIELD = new Vector2(0.9f, 0.9f);

    RewardConfig RewardConf
    {
        get
        {
            return fieldUtilities.contestantConfigLoader.RewCC.GetForPhase(fieldUtilities.contestantConfigLoader.phaseManager.CurrentPhase);
        }
    }

    public FieldUtilities fieldUtilities;
    public EventHandler EndEpisodeEvent;
    public EventHandler<string> ReciveActionEvent;
    protected Coroutine routine;
    protected SetFieldSizeUtility setFieldSizeUtility;
    protected BallBehavior ballBehavior;
    protected Renderer ballRenderer;

    [SerializeField] public Material M_BallNormale;
    [SerializeField] public Material M_BallFuori;

    bool ignoreImprovementEvaluationFrame = true;

    bool throwInPending = false;
    Coroutine throwInCoroutine;

    public void Beginning_Setup_And_Start()
    {
        GameObject Field = fieldUtilities.gameObject;
        setFieldSizeUtility = Field.GetComponent<SetFieldSizeUtility>();
        ballBehavior = fieldUtilities.Ball.GetComponent<BallBehavior>();
        ballRenderer = fieldUtilities.Ball.GetComponent<Renderer>();

        PreStartGame();
        StartGame();
    }


    void PreStartGame()
    {
        setFieldSizeUtility.SetAutoFieldSizeStandard();
    }


    void StartGame()
    {
        if (throwInCoroutine != null) { StopCoroutine(throwInCoroutine); throwInCoroutine = null; }
        throwInPending = false;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(EvalRoutine());

        ignoreImprovementEvaluationFrame = true;

        //ha già chiamato OnEpisodeBegin negli agenti

        PlaceStartBallForCurrentPhase();

    }

    void PlaceStartBallForCurrentPhase()
    {
        ballBehavior.PlaceBall("center0.7");
    }

    protected IEnumerator EvalRoutine()
    {
        float start = Time.time;

        //Vector3 prevBallPos = new Vector3();

        while (Time.time - start < fieldUtilities.contestantConfigLoader.EPISODE_TIME)
        {
            AddRewardToAllTeam(0, RewardConf.constantNegative, "constant_negative_rew");
            AddRewardToAllTeam(1, RewardConf.constantNegative, "constant_negative_rew");

            Vector3 currentBallPos = fieldUtilities.Ball.transform.localPosition;
            Vector2 logicalBallPos = fieldUtilities.GetLogicalPositionFromLocalPos(currentBallPos);

            bool outOfBounds = IsOutOfBounds(logicalBallPos);
            if (ballRenderer != null)
                ballRenderer.material = outOfBounds ? M_BallFuori : M_BallNormale;

            if (!throwInPending && outOfBounds)
            {
                throwInCoroutine = StartCoroutine(ThrowInRoutine(logicalBallPos));
            }                                                                                                                                                            
            if (throwInPending && !outOfBounds)                                                                                                                            
            {                                                                                                                                                              
                StopCoroutine(throwInCoroutine);
                throwInCoroutine = null;                                                                                                                  
                throwInPending = false;          
            }

            if (!ignoreImprovementEvaluationFrame)
            {
            }
            else
            {
                ignoreImprovementEvaluationFrame = false;
            }

            yield return new WaitForSeconds(fieldUtilities.contestantConfigLoader.DELTA_TIME_EVAL);
        }
        Debug.Log("GameManager Ended Game");
        EndGame();
    }

    void EndGame()
    {
        // Controlla cambio fase in un momento sicuro (nessun agente in creazione)
        if (fieldUtilities.contestantConfigLoader.phaseManager.CheckAndHandlePhaseChange())
        {
            fieldUtilities.contestantConfigLoader.RegenerateFieldsForNextPhase();
            return; // i nuovi campi partiranno da soli

            //oppure volendo si può fare che viene distrutto e rigenerato solo questo campo...
        }

        PreStartGame();

        //Debug.Log("Episode ended");
        EndEpisodeEvent.Invoke(this, EventArgs.Empty);

        StartGame();
    }


    bool IsOutOfBounds(Vector2 logicalPos)
    {
        return Mathf.Abs(logicalPos.x) > OUT_OF_FIELD.x || Mathf.Abs(logicalPos.y) > OUT_OF_FIELD.y;
    }

    IEnumerator ThrowInRoutine(Vector2 exitPos)
    {
        throwInPending = true;
        yield return new WaitForSeconds(UnityEngine.Random.Range(3f, 5f));

        bool outX = Mathf.Abs(exitPos.x) > OUT_OF_FIELD.x; // uscita bordo lato stretto (fondo porta)
        bool outZ = Mathf.Abs(exitPos.y) > OUT_OF_FIELD.y; // uscita bordo laterale

        float newX, newZ;

        if (outX && outZ) // angolo
        {
            newX = Mathf.Sign(exitPos.x) * 0.75f;
            newZ = Mathf.Sign(exitPos.y) * 0.75f;
        }
        else if (outX) // rimessa dal fondo (bordo lato stretto)
        {
            newX = Mathf.Sign(exitPos.x) * 0.75f;
            newZ = 0f;
        }
        else // rimessa laterale
        {
            newX = Mathf.Clamp(exitPos.x, -0.9f, 0.9f);
            newZ = exitPos.y * 0.4f; // stessa Z ma riportata verso il centro
        }

        ballBehavior.SetRandomSingleLocationAlpha(newX, newZ);
        throwInCoroutine = null;
        throwInPending = false;
    }

    public void BallScored(bool isYellow)
    {
        float rewardGoalPositive = RewardConf.scorePositive;
        float rewardGoalNegative = RewardConf.scoreNegative;
        Debug.LogWarning("Ball scored!!!");
        if (isYellow)
        {
            AddRewardToAllTeam(0, rewardGoalPositive, "ball scored");
            AddRewardToAllTeam(1, rewardGoalNegative, "ball scored");
        }
        else
        {
            AddRewardToAllTeam(0, rewardGoalNegative, "ball scored");
            AddRewardToAllTeam(1, rewardGoalPositive, "ball scored");
        }

        // per quando l'episodio non era terminato al goal 
        //ReciveActionEvent?.Invoke(this, "end-action");
        //ballBehavior.PlaceBall("center0.7");

        EndGame();
    }

    public void AgentHitBall(ContestantAgentController cag)
    {
        
    }

    public void AgentHitWall(ContestantAgentController cag)
    {
        
    }

    public void AddRewardToAllTeam(int teamID, float reward, string cause)
    {
        foreach (var s in fieldUtilities.agentsScripts[teamID])
            s.AddRewardContestantAgent(reward, cause);
    }
}
