using UnityEngine;
using Unity.MLAgents;
using Assets.Scripts.New;
using System;

// Robot blu => #0
// Robot giallo => #1

public class AgentModeSelector : MonoBehaviour
{
    public int TeamID = 0; //segna in giallo così
    public int TeamRole = 0;
    public FieldUtilities fieldUtilities;
    public Material material_team_0;
    public Material material_team_1;
    public Material material_team_0_pretrained;
    public Material material_team_1_pretrained;
    public MeshRenderer MeshRendererComp;
    public MeshCollider MeshColliderComp;

    public void BeginningStart_Coordinated(object sender, EventArgs ea)
    {
        ContestantConfigLoader configLoader = fieldUtilities.contestantConfigLoader;
        GameManager gameManager = fieldUtilities.gameManager;

        // NormalContestantAC va aggiunto PRIMA di DecisionRequester:
        // DR ha [RequireComponent(typeof(Agent))], quindi se aggiunto per primo Unity
        // crea un plain Agent base sul GO. Aggiungendo prima la sottoclasse, DR trova
        // già un Agent valido e non ne crea uno duplicato.
        switch (fieldUtilities.contestantConfigLoader.PlayMode)
        {
            case E_Mode.Competition1v1:
                NormalContestantAC cac = gameObject.AddComponent<NormalContestantAC>();
                cac.GeneralSetup_And_GatherReferences(fieldUtilities, gameManager, TeamID, TeamRole, configLoader);
                fieldUtilities.agentsScripts[TeamID].Add(cac);

                DecisionRequester dr = gameObject.AddComponent<DecisionRequester>();
                dr.DecisionPeriod = configLoader.DR_DecisionPeriod;
                dr.DecisionStep = configLoader.DR_DecisionStep;
                dr.TakeActionsBetweenDecisions = configLoader.DR_TakeActionsBetweenDecisions;
                break;

            default:
                Debug.LogError("AgentModeSelector: Modalità sconosciuta nel ConfigLoader!");
                break;
        }

        //Debug.Log($"Agente spawnato correttamente, Modalità caricata: {fieldUtilities.contestantConfigLoader.PlayMode}");
    }
}
