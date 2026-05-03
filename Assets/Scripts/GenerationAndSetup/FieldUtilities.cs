using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SocialPlatforms;

public class FieldUtilities : MonoBehaviour
{
    public GameObject Ball;
    public List<GameObject> Agents;
    public GameObject YellowGoal;
    public GameObject BlueGoal;
    public ContestantConfigLoader contestantConfigLoader;
    public GameManager gameManager;
    public List<ContestantAgentController>[] agentsScripts;
    public int fieldNum;
    private Vector2 fieldSize;
    private Vector3 goalSize;
    public readonly Vector3 robotSizeMeters = new Vector3(0.18f, 0.18f, 0.18f); //USATO x le fn tipo GetRobotHeight / Radius
    private PhaseManager phaseManager;


    public void AllFieldUtilitiesCoordinatedStart(object sender, EventArgs ea)
    {
        SetReferences();

        gameManager = gameObject.AddComponent<GameManager>();
        gameManager.fieldUtilities = this;

        Agents = new();
        agentsScripts = new List<ContestantAgentController>[2];
        agentsScripts[0] = new(); agentsScripts[1] = new();

        if (contestantConfigLoader == null)
        {
            Debug.LogError("contestantConfigLoader is null in FieldUtilities");
            return;
        }

        gameManager.Beginning_Setup_And_Start();

        //EventHandler beginning_start_coordinated = null;

        //crea i robot
        for (int i = 0; i<2; i++)
        {
            foreach (var intRole in GameModeData.GetModeInfo(contestantConfigLoader.PlayMode).PlayerRolesInTeam)
            {
                GameObject robot = Instantiate(contestantConfigLoader.robotPrefab, transform);
                //robot.transform.localScale = new Vector3(robotSizeMeters.x, robotSizeMeters.y/2f, robotSizeMeters.z);
                robot.transform.position = transform.position;
                robot.transform.rotation = transform.rotation;
                AgentModeSelector ams = robot.GetComponent<AgentModeSelector>();
                ams.TeamID = i;
                ams.TeamRole = intRole;
                ams.fieldUtilities = this;
                //beginning_start_coordinated += ams.BeginningStart_Coordinated;
                ams.BeginningStart_Coordinated(this, EventArgs.Empty);

                Agents.Add(robot);
                // nello script AgentModeSelector allo Start() viene aggiunto alla lista agentScripts il ContestantAgentController
            }
        }

        //beginning_start_coordinated.Invoke(this, EventArgs.Empty);

        Debug.Log($"Field at {transform.position} has been set up for {Agents.Count} agents");
    }

    public Vector2 FieldSize
    {
        get
        {
            if (fieldSize.x == 0 || fieldSize.y == 0)
            {
                Debug.LogError("Tried to get FieldSize when it was not set");
                //Debug.Break();
            }
            return fieldSize;
        }
        set
        {
            fieldSize = value;
        }
    }

    public Vector3 GoalSize
    {
        get
        {
            if (goalSize.x == 0 || goalSize.y == 0 || goalSize.z == 0)
            {
                Debug.LogError("Tried to get GoalSize when it was not set");
                //Debug.Break();
            }
            return goalSize;
        }
        set
        {
            goalSize = value;
        }
    }

    public void SetReferences ()
    {
        if (Ball == null)
            Debug.LogError("Important objects not avaiable (FieldUtilities)");
    }

    public Vector3 GetLocalPositionFromLogicalPos (float X, float Y) // entrambi vanno da -1 a +1
    {
        return new Vector3((fieldSize.x) / 20 * X, 0, (fieldSize.y) / 20 * Y);
    }

    public Vector2 GetLogicalPositionFromLocalPos (Vector3 localPos, bool natural=true)
    {
        if (!natural)
        {
            localPos = new Vector3(-localPos.x, localPos.y, -localPos.z);
        }
        return new Vector2(localPos.x / (fieldSize.x / 20), localPos.z / (fieldSize.y / 20));
    }
}

public static class Extension
{
    public static Vector2 GetNoise(this Vector2 starting_value, float radius, bool enabled=true)
    {
        if (!enabled)
            return starting_value;
            
        return new Vector2(starting_value.x + UnityEngine.Random.Range(-radius, radius), starting_value.y + UnityEngine.Random.Range(-radius, radius));
    }

    public static float DirectFloat(this float f, bool naturalDir)
    {
        if (naturalDir)
            return f;
        else
            return -f;
    }

    public static float Remap (this float value, float from1, float to1, float from2, float to2) {
	    return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

}
