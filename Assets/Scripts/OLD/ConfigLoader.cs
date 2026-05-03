using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Policies;
using System;
using System.IO;

public class ConfigLoader : MonoBehaviour
{
    [SerializeField, TextArea] private string infoText = "See BehaviorParameters for:\nBehaviorType\tBehaviorName\tModel\nInference Device\tDeterministic Inference\nDecision Requester parameters\n\nVector Observations And Actions must be set in each scene Agent's BehaviorParameters";

    [SerializeField] public bool AUTO_CONFIGURATION = true;
    [SerializeField] bool Play_In_All_Fields_With_Inference = true;
    [SerializeField] public E_Mode PlayMode;
    [SerializeField] public string logPath = "";
    [SerializeField] public float MOVE_SPEED = 150f;
    [SerializeField] public float ROTATE_SPEED = 200f;
    [SerializeField] public float FREE_MOVE_SPEED = 5f;
    [SerializeField] public float REACH_BALL_TIME = 30f;
    [SerializeField] public float THROW_BALL_TIME = 10f;
    [SerializeField] public bool LOG_REWARDS = true;
    [SerializeField] public int DR_DecisionPeriod = 5;
    [SerializeField] public int DR_DecisionStep = 0;
    [SerializeField] public bool DR_TakeActionsBetweenDecisions = true;

    private bool? enableEnvs, enableSoccerEnv;

    private BehaviorParameters parameters;

    public BehaviorParameters Parameters
    {
        get 
        {
            if (parameters == null)
                parameters = GetComponent<BehaviorParameters>();
            return parameters;
        }
    }

    public bool EnableEnvs
    {
        get
        {
            if (enableEnvs == null)
                enableEnvs = (Parameters.BehaviorType == BehaviorType.Default || Parameters.BehaviorType == BehaviorType.InferenceOnly) && Play_In_All_Fields_With_Inference;
            return enableEnvs.Value;
        }
    }

    public bool EnableSoccerEnv
    {
        get
        {
            if (enableSoccerEnv == null)
                enableSoccerEnv = Parameters.BehaviorType != BehaviorType.Default;
            
            return enableSoccerEnv.Value;
        }
    }

    /*private void Awake ()
    {
        CopyFiles();
        //Debug.LogError("STARTED AWAKE LOADER");

        FindRootEnvs()[0].SetActive(EnableEnvs);
        FindRootSoccerEnvs()[0].SetActive(EnableSoccerEnv);

        //Debug.LogError("FINISHED AWAKE LOADER");
    }*/

    public static List<GameObject> FindRootEnvs()
    {
        List<GameObject> roots = new List<GameObject>();
        GameObject[] sceneRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        foreach (GameObject go in sceneRoots)
        {
            if (go.CompareTag("Envs"))
            {
                roots.Add(go);
            }
        }

        return roots;
    }

    public static List<GameObject> FindRootSoccerEnvs()
    {
        List<GameObject> roots = new List<GameObject>();
        GameObject[] sceneRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        foreach (GameObject go in sceneRoots)
        {
            if (go.CompareTag("SoccerEnv"))
            {
                roots.Add(go);
            }
        }

        return roots;
    }

    private void CopyFiles()
    {
        // Percorso del file runinfo.txt
        string runInfoPath = Path.Combine(Application.dataPath, "..", "VE", "bin", "runinfo.txt");

        if (!File.Exists(runInfoPath))
        {
            Debug.LogWarning("runinfo.txt non trovato! Assicurati di eseguire tramite mlal.py o che il file sia nella cartella corrente.");
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(runInfoPath);

            if (lines.Length > 0)
            {
                string runId = lines[0]; // prima riga = nome della run
                List<string> copiedFiles = new();

                string resultsPath = Path.Combine(Application.dataPath, "..", "VE", "bin", "results", runId);

                for (int i = 1; i < lines.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(lines[i]))
                    {
                        copiedFiles.Add(lines[i]); // righe successive = nomi dei file copiati
                        CopyAdditionalFile(lines[i], resultsPath);
                    }
                }

                Debug.Log($"Run ID: {runId}");
                Debug.Log($"File da copiare: {string.Join(", ", copiedFiles)}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Errore lettura runinfo.txt: {e}");
        }
    }

    void CopyAdditionalFile(string sourcePath, string resultsPath)
    {
        string destFileName = "RUN " + DateTime.Now.ToString("HH-mm-ss dd-MM-yyyy") + " " + sourcePath.Split("/")[^1];

        if (!File.Exists(sourcePath))
        {
            Debug.LogWarning($"File da copiare non trovato: {sourcePath}");
            return;
        }

        string destPath = Path.Combine(resultsPath, destFileName);

        try
        {
            File.Copy(sourcePath, destPath, true); // true = overwrite
            Debug.Log($"File copiato: {sourcePath} -> {destPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Errore copiando file: {e}");
        }
    }
}
