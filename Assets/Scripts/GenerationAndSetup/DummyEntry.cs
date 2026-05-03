using Unity.Sentis;
using UnityEngine;

[CreateAssetMenu(fileName = "DummyEntry", menuName = "Custom/DummyEntry")]
public class DummyEntry : ScriptableObject
{
    public string DummyDescription = "";

    [Header("IsAgent = true solo per l'entry che rappresenta l'avversario che è un altro agente imparante (ModelAsset ignorato)")]
    public bool IsAgent = false;

    public ModelAsset ModelAsset;
}
