using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Policies;
using Unity.Sentis;
using UnityEngine.EventSystems;
using UnityEngine.SocialPlatforms;
using Unity.MLAgents;
using System;

public class PhaseManager : MonoBehaviour
{
    [SerializeField] public bool UseDynamicBehaviorTypeAndModel = true; //viene messo false se è Inferenza o Heuristic
    [SerializeField] public DummyCollection dummyCollection;
    private Dictionary<(int fieldNum, int teamID, int teamRole), ModelAsset> agents_models;

    public int CurrentPhase { get; private set; }
    // Se UseDynamic... è messo a false, la fase è -1
    // Se UseDyamic... è messo true, ma il config.yaml non contine fase, è messo 0
    // Se UseDyamic... è messo true, e il config.yaml contiene fase, è messo quella
    // Se UseDynamic... è messo true, ma è Inferenza o Heuristic, è messo a -1, e UseDynamic.. viene messo a false

    private ContestantConfigLoader contestantConfigLoader;

    public void Awake()
    {
        contestantConfigLoader = GetComponent<ContestantConfigLoader>();
        if (contestantConfigLoader == null) Debug.LogError("Field Utilities non trovato da phaseManager");

        CurrentPhase = -1;
    }

    // Chiamato da CCL prima di GenerateFields(), carica i modelli per la fase corrente
    public void Initialize()
    {
        if (!UseDynamicBehaviorTypeAndModel) return;

        int phase = ReadPhase();
        if (phase < 0)
        {
            Debug.LogWarning("Phase not specified in yaml with UseDynamicBehaviorTypeAndModel=true. Using phase 0 for testing.");
            phase = 0;
        }

        CurrentPhase = phase;
        LoadAgentsModels(phase);
    }

    // Chiamato da GameManager.EndGame() in un momento sicuro (fuori dalla creazione agenti)
    // Ritorna true se la fase è cambiata e i campi devono essere rigenerati
    public bool CheckAndHandlePhaseChange()
    {
        if (!UseDynamicBehaviorTypeAndModel) return false;

        int nowPhase = ReadPhase();
        if (nowPhase < 0 || nowPhase == CurrentPhase) return false;

        Debug.LogWarning("FASE CAMBIATA ora è " + nowPhase + ", prima era " + CurrentPhase);
        CurrentPhase = nowPhase;
        LoadAgentsModels(nowPhase);
        return true;
    }

    private int ReadPhase()
    {
        float raw = Academy.Instance.EnvironmentParameters.GetWithDefault("phase", -1f);
        if (raw < 0f) return -1;
        return Mathf.RoundToInt(raw);
    }

    private void LoadAgentsModels(int phase) // pura: non chiama RegenerateFieldsForNextPhase
    {
        agents_models = new();

        var phaseEntries = dummyCollection.GetPhaseEntries(phase);

        float totalWeight = 0f;
        foreach (var ew in phaseEntries)
        {
            if (ew.entry == null) continue;
            totalWeight += ew.weight;
        }
        if (phaseEntries.Count == 0 || totalWeight <= 0f)
            throw new Exception($"No valid DummyEntry for phase={phase}");

        // Suddivisione proporzionale: calcola quanti campi spettano a ogni entry
        List<DummyEntry> slots = new();
        foreach (var ew in phaseEntries)
        {
            if (ew.entry == null) continue;
            int count = Mathf.RoundToInt(ew.weight / totalWeight * contestantConfigLoader.NUM_FIELDS);
            for (int i = 0; i < count; i++) slots.Add(ew.entry);
        }
        DummyEntry last = phaseEntries[phaseEntries.Count - 1].entry;
        while (slots.Count < contestantConfigLoader.NUM_FIELDS) slots.Add(last);
        while (slots.Count > contestantConfigLoader.NUM_FIELDS) slots.RemoveAt(slots.Count - 1);

        // Shuffle
        for (int i = slots.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (slots[i], slots[j]) = (slots[j], slots[i]);
        }

        int[] roles = GameModeData.GetModeInfo(contestantConfigLoader.PlayMode).PlayerRolesInTeam;
        for (int fieldN = 0; fieldN < contestantConfigLoader.NUM_FIELDS; fieldN++)
        {
            int agentTeam = UnityEngine.Random.Range(0, 2);
            DummyEntry dummyEntry = slots[fieldN];

            foreach (int role in roles)
                agents_models[(fieldN, agentTeam, role)] = null;

            foreach (int role in roles)
                agents_models[(fieldN, 1 - agentTeam, role)] = dummyEntry.ModelAsset;
        }
    }

    // Lookup puro: nessuna rilevazione di cambio fase, nessun side effect
    public (bool skip, ModelAsset, BehaviorType) ChooseDynamicBehaviorTypeAndModel(ContestantAgentController cac)
    {
        if (agents_models == null || !agents_models.ContainsKey((cac.fieldUtilities.fieldNum, cac.TeamID, cac.TeamRole)))
        {
            Debug.LogError($"agents_models non inizializzato o chiave mancante per field={cac.fieldUtilities.fieldNum} team={cac.TeamID} role={cac.TeamRole}");
            return (false, null, BehaviorType.Default);
        }

        ModelAsset ma = agents_models[(cac.fieldUtilities.fieldNum, cac.TeamID, cac.TeamRole)];
        return (false, ma, ma == null ? BehaviorType.Default : BehaviorType.InferenceOnly);
    }


    [Obsolete] public DummyEntry GetRandomDummyEntry(int phase = -1)
    {
        if (phase == int.MinValue)
            throw new Exception("Get Random Dummy called when dynamic model was disabled");

        var phaseEntries = dummyCollection.GetPhaseEntries(phase);

        float totalWeight = 0f;
        foreach (var ew in phaseEntries)
        {
            if (ew.entry == null) continue;
            totalWeight += ew.weight;
        }
        if (totalWeight <= 0f)
            throw new Exception("Dummy collection has no choices for a phase");

        float rnd = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (var ew in phaseEntries)
        {
            if (ew.entry == null) continue;
            cumulative += ew.weight;
            if (rnd <= cumulative) return ew.entry;
        }

        return phaseEntries[phaseEntries.Count - 1].entry;
    }
}
