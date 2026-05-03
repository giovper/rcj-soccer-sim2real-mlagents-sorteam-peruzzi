# Generazione del campo e degli agenti

Descrive come, dall'avvio della scena Unity, vengono creati tutti i campi e gli agenti a runtime.

---

## Schema generale

```
[Scena] ContestantConfigLoader.Awake()
    └── PhaseManager.Initialize()
    └── GenerateFields()
            └── Instantiate(fieldPrefab) × NUM_FIELDS
                    └── FieldUtilities.AllFieldUtilitiesCoordinatedStart()  [evento coordinato]
                            └── SetFieldSizeUtility.Setup()
                            └── BallBehavior.Setup()
                            └── AgentModeSelector.BeginningStart_Coordinated() × 2 squadre
                                    └── AddComponent<NormalContestantAC>()
                                    └── AddComponent<DecisionRequester>()
                                    └── GeneralSetup_And_GatherReferences()
                                            └── GetConfig()  ← configura BehaviorParameters
                            └── GameManager.Beginning_Setup_And_Start()
                                    └── StartGame()  ← inizia il primo episodio
```

---

## 1. ContestantConfigLoader (`Awake`)

Il `ContestantConfigLoader` è il componente radice nella scena. Al suo `Awake`:

1. Ottiene il riferimento al `PhaseManager` (stesso GameObject)
2. Se `ExecutionMode != Default`, forza `NUM_FIELDS = 1` e disabilita il sistema dinamico di modelli
3. Chiama `phaseManager.Initialize()` — legge la fase corrente dall'Academy e carica i modelli
4. Chiama `GenerateFields()`

**Campi configurabili nell'Inspector:**

| Campo | Descrizione |
|-------|-------------|
| `NUM_FIELDS` | Numero di campi paralleli (es. 30 per training) |
| `ExecutionMode` | Default / Inference / Heuristic / UserVsAgent |
| `PlayerTeamID` | Squadra del giocatore umano (solo in UserVsAgent) |
| `MOVE_SPEED` / `ROTATE_SPEED` | Forze applicate al robot |
| `EPISODE_TIME` | Durata massima episodio in secondi |
| `DELTA_TIME_EVAL` | Intervallo del loop di reward continuo |
| `PRINT_TIME_EVAL` | Intervallo del log periodico reward |
| `LOG_REWARDS` | Abilita/disabilita log reward in console |
| `DR_DecisionPeriod` | Ogni quanti step fissi l'agente prende una decisione |
| `fieldPrefab` | Prefab del campo da istanziare |
| `robotPrefab` | Prefab del robot (usato da FieldUtilities) |
| `RewCC` | ScriptableObject `RewardConfigCollection` |

---

## 2. GenerateFields

Istanzia `NUM_FIELDS` campi su una griglia 2D con spacing di 3 unità:

```csharp
int numSw = (int)Math.Sqrt(NUM_FIELDS);
for (int i = 0; i < NUM_FIELDS; i++)
{
    Vector3 pos = new Vector3(i % numSw * delta.x, 0, i / numSw * delta.y);
    GameObject field = Instantiate(fieldPrefab, pos, Quaternion.identity);
    // ...
}
coordinated_fieldUtilities_start.Invoke(...); // evento coordinato a tutti i campi
```

L'evento coordinato garantisce che tutti i campi siano istanziati prima che uno qualsiasi inizi il suo setup.

---

## 3. FieldUtilities — Setup coordinato

`AllFieldUtilitiesCoordinatedStart` viene chiamato su ogni `FieldUtilities` dopo che tutti i campi sono stati istanziati. Esegue in ordine:

1. **`SetReferences()`** — valida i riferimenti a Ball, Goals, Walls
2. **`SetFieldSizeUtility.Setup()`** — ridimensiona fisicamente il campo
3. Per ogni team (0 e 1), per ogni ruolo (solo ruolo 0 nel 1v1):
   - Trova il `AgentModeSelector` nel prefab robot corrispondente
   - Chiama `BeginningStart_Coordinated(fieldUtilities, gameManager, teamID, teamRole)`
4. Crea il `GameManager` e chiama `Beginning_Setup_And_Start()`

---

## 4. AgentModeSelector — Creazione agente

`BeginningStart_Coordinated` è il punto in cui il robot viene "attivato":

1. **Aggiunge `NormalContestantAC`** come componente (PRIMA di DecisionRequester)
2. **Aggiunge `DecisionRequester`** e lo configura con `DR_DecisionPeriod`, `DR_DecisionStep`, `DR_TakeActionsBetweenDecisions`
3. Chiama `agent.GeneralSetup_And_GatherReferences(fu, gm, teamID, teamRole, ccl)`

> **Nota critica sull'ordine**: `DecisionRequester` ha `[RequireComponent(typeof(Agent))]`. Se aggiunto prima della sottoclasse, Unity crea automaticamente un componente `Agent` base spuro. Aggiungere sempre prima `NormalContestantAC`.

---

## 5. GeneralSetup_And_GatherReferences

Chiamato su `ContestantAgentController`, raccoglie i riferimenti necessari all'agente:

- `fieldUtilities`, `gameManager`, `TeamID`, `TeamRole`
- Riferimento a `Ball`, `BallBehavior`
- Calcola il centro di massa del Rigidbody
- Assegna il materiale corretto in base al team
- Registra i listener agli eventi `EndEpisodeEvent` e `ReciveActionEvent`
- Chiama `GetConfig(ccl)`

---

## 6. GetConfig

Configura il `BehaviorParameters` dell'agente in base all'`ExecutionMode`:

| ExecutionMode | Comportamento |
|---------------|---------------|
| `Default` | Training. Se `UseDynamicBehaviorTypeAndModel`: assegna modello/tipo dalla fase corrente. Altrimenti usa `BehaviorType.Default`. |
| `Inference` | Forza `InferenceOnly` con il modello dal CCL |
| `Heuristic` | Forza `HeuristicOnly` (controllo da tastiera) |
| `UserVsAgent` | Team del giocatore → `HeuristicOnly`; team AI → `InferenceOnly` con modello |

---

## 7. Primo episodio

Dopo il setup, `GameManager.Beginning_Setup_And_Start()` chiama `StartGame()` che:
1. Posiziona la palla (`BallBehavior.PlaceBall`)
2. Avvia la coroutine `EvalRoutine` — il loop principale del gioco
3. Segnala l'inizio episodio agli agenti (tramite ML-Agents → `OnEpisodeBegin`)

Da `OnEpisodeBegin`, ogni agente:
1. Ferma le coroutine attive
2. Riabilita la fisica
3. Chiama `PlaceRobot("starting_auto_phase")` per il posizionamento iniziale
4. Avvia `EvalRoutine` (reward continue) e `PrintRoutine` (log)

---

## Cambio fase

Quando ML-Agents incrementa la fase del curriculum, `PhaseManager.CheckAndHandlePhaseChange()` (chiamato da `GameManager.EndGame`) lo rileva e:
1. Aggiorna `CurrentPhase`
2. Carica i nuovi modelli dalla `DummyCollection`
3. Chiama `ContestantConfigLoader.RegenerateFieldsForNextPhase()` che distrugge e ricrea tutti i campi con la nuova configurazione
