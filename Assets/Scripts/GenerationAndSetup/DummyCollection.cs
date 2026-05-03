using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct DummyEntryWeight
{
    public DummyEntry entry;
    [Range(0f, 1000f)]
    public float weight;
}

[System.Serializable]
public class PhaseConfig
{
    public List<DummyEntryWeight> entries;
}

[CreateAssetMenu(fileName = "DummyCollection", menuName = "Custom/DummyCollection")]
public class DummyCollection : ScriptableObject
{
    public List<PhaseConfig> perPhase;

    public List<DummyEntryWeight> GetPhaseEntries(int phase)
    {
        if (perPhase == null || perPhase.Count == 0)
            throw new System.Exception("DummyCollection: nessuna fase configurata");
        int idx = Mathf.Clamp(phase, 0, perPhase.Count - 1);
        return perPhase[idx].entries;
    }
}
