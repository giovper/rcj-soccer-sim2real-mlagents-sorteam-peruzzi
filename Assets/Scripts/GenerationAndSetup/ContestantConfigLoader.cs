using System;
using System.Collections.Generic;
using System.IO;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.Sentis;
using Unity.VisualScripting;
using UnityEngine;

public class ContestantConfigLoader : MonoBehaviour
{

    [SerializeField, TextArea] private string infoText = "BLUE=0\nSee BehaviorParameters for:\nBehaviorType\tBehaviorName\tModel\nInference Device\tDeterministic Inference\nDecision Requester parameters\n\nVector Observations And Actions must be set in each scene Agent's BehaviorParameters\n\nPlease note that if you choose a Model that has different shape than the expected one, Unity crashes even if you are not doing inference X(";

    public GameObject fieldPrefab;
    public GameObject robotPrefab;

    [SerializeField] public int NUM_FIELDS = 30;

    [SerializeField] public E_Mode PlayMode;
    [SerializeField] public E_ExecutionMode ExecutionMode = E_ExecutionMode.Default;
    [SerializeField] public int PlayerTeamID = 0;
    [SerializeField] public string logPath = "";
    [SerializeField] public bool LOG_REWARDS = true;
    [SerializeField] public int DR_DecisionPeriod = 5;
    [SerializeField] public int DR_DecisionStep = 0;
    [SerializeField] public bool DR_TakeActionsBetweenDecisions = true;
    [SerializeField] public float MOVE_SPEED = 150f;
    [SerializeField] public float ROTATE_SPEED = 200f;
    [SerializeField] public float EPISODE_TIME = 20; //secs
    [SerializeField] public float DELTA_TIME_EVAL = 0.2f;
    [SerializeField] public float PRINT_TIME_EVAL = 3f;
    //[SerializeField] public int VectorObservationSize = 6;
    public AnimationCurve EPISODE_TIME_CURVE;
    public PhaseManager phaseManager {get; private set;}

    private List<FieldUtilities> fieldUtilitiesList;

    public RewardConfigCollection RewCC;

    private BehaviorParameters parameters;

    private void Awake ()
    {
        phaseManager = GetComponent<PhaseManager>();
        if (phaseManager == null) Debug.LogError("Non c'è phase manager");

        CopyFiles();
        //Debug.Log("STARTED AWAKE CONFIG LOADER");

        if (ExecutionMode != E_ExecutionMode.Default)
        {
            NUM_FIELDS = 1;
            phaseManager.UseDynamicBehaviorTypeAndModel = false;
        }

        phaseManager.Initialize();
        GenerateFields();

        Debug.Log("All fields have been spawned");
    }

    private void GenerateFields()
    {
        fieldUtilitiesList = new();

        EventHandler coordinated_fieldUtilities_start = null;

        Vector2 delta = new Vector2(3, 3);
        int numSw = (int)Math.Sqrt(NUM_FIELDS);
        for (int i = 0; i<NUM_FIELDS; i++)
        {
            GameObject field = Instantiate(fieldPrefab, new Vector3(i%numSw * delta.x, 0, i/ numSw * delta.y), Quaternion.identity);
            fieldUtilitiesList.Add(field.GetComponent<FieldUtilities>());
            fieldUtilitiesList[^1].contestantConfigLoader = this;
            fieldUtilitiesList[^1].fieldNum = i;
            coordinated_fieldUtilities_start += fieldUtilitiesList[^1].AllFieldUtilitiesCoordinatedStart;
        }

        //Debug.LogError("## " + fieldUtilitiesList.Count);
        coordinated_fieldUtilities_start.Invoke(this, EventArgs.Empty);
    }

    public void RegenerateFieldsForNextPhase()
    {
        //Debug.LogError(fieldUtilitiesList.Count); // printa, tra le varie fasi, il num tot dei campi correttamente
        foreach (FieldUtilities fu in fieldUtilitiesList)
        {
            Destroy(fu.gameObject);
        }

        GenerateFields();
    }

    private void Update()
    {
        //float bottleneck = Time.unscaledDeltaTime - 1/Time.timeScale *0.02f; Debug.Log($"Bottleneck: {bottleneck*100:F2}x"); // 1.0 = perfetto, >1 = in ritardo
    }

    public BehaviorParameters Parameters
    {
        get
        {
            if (parameters == null)
                parameters = GetComponent<BehaviorParameters>();
            return parameters;
        }
    }


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

    public float GetEpisodeDuration()
    {
        if (EPISODE_TIME_CURVE == null)
        {
            return EPISODE_TIME;
        }
        throw new Exception();
        /*
        else
        {
            return EPISODE_TIME_CURVE.Evaluate(Academy.Instance.ste)
        }
        */
    }

}
