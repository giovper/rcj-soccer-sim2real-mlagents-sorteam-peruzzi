using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

[Serializable]
public struct RewardConfig
{
    public float proximityVeryClose;   // < 0.2m
    public float proximityClose;       // < 0.3m
    public float proximityMedium;      // < 0.6m
    public float proximityFar;         // < 1m
    public float proximityVeryFar;     // >= 1m
    public float ballLikelyToScore;
    public float hitWall;
    public float scorePositive;
    public float scoreNegative;
    public float hitBallDirectionAlpha;
    public float hitBallForceAlpha;
    public float hitBallPrecisionOffset;
    public float constantNegative;
}

[CreateAssetMenu]
public class RewardConfigCollection : ScriptableObject
{
    public RewardConfig[] perPhase;

    public RewardConfig GetForPhase(int phase)
    {
        if (phase < 0 || phase >= perPhase.Length) return perPhase[0];
        return perPhase[phase];
    }

    public RewardConfig GetForPhase(ContestantAgentController cac)
    {
        return GetForPhase(cac.fieldUtilities.contestantConfigLoader.phaseManager.CurrentPhase);
    }
}