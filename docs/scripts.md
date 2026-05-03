# Descrizione degli Script

Gli script OLD (`Assets/Scripts/OLD/`) non sono documentati — sono versioni precedenti non più usate.

---

## Gameplay

### `ContestantAgentController.cs`
Classe astratta base per tutti gli agenti. Estende `Agent` di ML-Agents.

Responsabilità:
- Gestione del ciclo di vita dell'episodio (`OnEpisodeBegin`, `EndEpisodeFunction`)
- Applicazione delle reward tramite `AddRewardContestantAgent()` (con logging e cache)
- Configurazione di `BehaviorParameters` a runtime (`GetConfig`, `GetDynamicPhasedBehaviorTypeAndModel`)
- Posizionamento fisico del robot (`SetRandomLocationRange`, `SetPhysicsEnabled`)
- Raccolta osservazioni e azioni (implementate nelle sottoclassi)
- Gestione collisioni (`OnCollisionEnter` → dispatching verso metodi astratti)
- Coroutine `EvalRoutine` (loop di reward continuo) e `PrintRoutine` (log periodico)

Campi importanti: `TeamID`, `TeamRole`, `rb`, `fieldUtilities`, `gameManager`, `IsTransformLocked`

### `NormalContestantAC.cs`
Implementazione concreta dell'agente per la modalità `Competition1v1`. Estende `ContestantAgentController`.

Responsabilità:
- `CollectObservations`: posizione agente + palla (con noise adattivo), sensori di linea, velocità agente + palla (10 obs totali)
- `OnActionReceived`: applica forze di movimento orientate in base al `TeamID`
- `EvalRoutine`: calcola reward continue (proximity, ballLikelyToScore)
- `OnHitBall`: reward per tiro in base a forza e precisione verso la porta
- `OnHitWall`: penalità + sollevamento temporaneo del robot + riposizionamento asincrono
- `PlaceRobot`: posizionamento in varie modalità ("starting_normal", "random", "raised", ecc.)

### `GameManager.cs`
Controlla il loop di gioco per un singolo campo. Creato da `FieldUtilities` a runtime.

Responsabilità:
- Avvio e fine episodio (`StartGame`, `EndGame`)
- Timer episodio e applicazione di `constantNegative` reward ogni `DELTA_TIME_EVAL`
- Rilevamento palla fuori campo e gestione throw-in (`ThrowInRoutine`)
- Segnalazione goal (`BallScored`) con reward a tutta la squadra
- Cambio fase curriculum (`PhaseManager.CheckAndHandlePhaseChange`)
- Cambia materiale della palla quando esce dal campo (visuale)

Eventi esposti: `EndEpisodeEvent`, `ReciveActionEvent`

### `RewardConfigCollection.cs`
ScriptableObject che contiene le configurazioni di reward per fase.

Struttura:
- `RewardConfig` (struct): tutti i valori numerici di reward (proximity, hitBall, score, ecc.)
- `RewardConfigCollection`: array `perPhase` + metodi `GetForPhase(int)` e `GetForPhase(ContestantAgentController)`

Campi di `RewardConfig`: `proximityVeryClose/Close/Medium/Far/VeryFar`, `ballLikelyToScore`, `hitWall`, `scorePositive`, `scoreNegative`, `hitBallDirectionAlpha`, `hitBallForceAlpha`, `hitBallPrecisionOffset`, `constantNegative`

---

## GenerationAndSetup

### `ContestantConfigLoader.cs`
Componente principale della scena. Punto di ingresso dell'intera simulazione.

Responsabilità:
- Lettura parametri dall'Inspector (NUM_FIELDS, ExecutionMode, speed, tempi, ecc.)
- Istanziazione di tutti i campi (`GenerateFields`) tramite prefab
- Copia file di log nella cartella della run (per tracciabilità)
- Inizializzazione del `PhaseManager`
- Fornisce `BehaviorParameters` globali agli agenti per la copia della configurazione

Vedi anche: [Generazione del campo](generation.md)

### `FieldUtilities.cs`
Hub centrale di un singolo campo. Gestisce riferimenti e coordinate.

Responsabilità:
- Inizializzazione coordinata del campo (`AllFieldUtilitiesCoordinatedStart`)
- Creazione di `GameManager` e spawn degli agenti
- Conversione coordinate: logiche ↔ locali Unity (`GetLocalPositionFromLogicalPos`, `GetLogicalPositionFromLocalPos`)
- Riferimenti a Ball, Goals, Agents, field size

Coordinate logiche: il campo va da -1 a +1 su entrambi gli assi. L'asse X è la direzione di gioco (segnare), Y è laterale.

### `AgentModeSelector.cs`
Componente sul prefab robot. Configura il tipo di agente a runtime.

Responsabilità:
- Aggiunge `NormalContestantAC` al GameObject (prima di `DecisionRequester`, importante per l'ordine)
- Configura `DecisionRequester` con i parametri del CCL
- Chiama `GeneralSetup_And_GatherReferences` sull'agente creato
- Gestisce i materiali per squadra (normale vs pre-allenato)

Nota: `DecisionRequester` ha `[RequireComponent(typeof(Agent))]` — aggiungere prima la sottoclasse evita la creazione di un `Agent` spuro.

### `PhaseManager.cs`
Gestisce il curriculum di fasi e il caricamento dei modelli.

Responsabilità:
- Legge la fase corrente dall'Academy ML-Agents (`ReadPhase`)
- Al cambio fase: distrugge e ricrea tutti i campi (`RegenerateFieldsForNextPhase`)
- Distribuisce i modelli agli agenti in base alla fase e ai pesi della `DummyCollection`
- `ChooseDynamicBehaviorTypeAndModel`: ritorna modello e BehaviorType per un dato agente

### `DummyCollection.cs` / `DummyEntry.cs`
ScriptableObject che definisce gli avversari per fase.

- `DummyEntry`: un avversario (può essere un agente in training o un modello `.onnx` pre-allenato)
- `DummyEntryWeight`: coppia `(DummyEntry, float weight)` per selezione pesata
- `PhaseConfig`: lista di `DummyEntryWeight` per una fase
- `DummyCollection`: lista di `PhaseConfig`, uno per fase

### `Modes.cs`
Definisce gli enum usati nel progetto:
- `E_Mode`: modalità di gioco (`Competition1v1`, `ReachBall`, ecc.)
- `E_ExecutionMode`: modalità di esecuzione (`Default`, `Inference`, `Heuristic`, `UserVsAgent`)

---

## FieldComponents

### `BallBehavior.cs`
Gestisce il posizionamento della palla a inizio episodio e dopo throw-in.

Metodi principali: `PlaceBall(string position)` con modalità "center0.7", "free1", "penalty". Usa `BoundsCollisionChecker` per evitare sovrapposizioni con i robot.

### `ScoreUtilities.cs`
Collegato alle porte. Rileva quando la palla entra in porta e notifica il `GameManager` tramite `BallScored()`.

### `SetFieldSizeUtility.cs`
Scala le dimensioni fisiche del campo in base ai parametri configurati nel CCL.

### `WallUtilities.cs`
Gestisce tag e collider dei muri del campo.

---

## Sensors

### `SensorsReader.cs`
Simula i sensori di linea presenti sui robot fisici RoboCup.

Funzionamento: array di raycast circolari verso il basso. Rileva le linee del campo (layer "FieldLines") e calcola:
- `GetCenterSensorValueFloat()`: 0/1 se il sensore centrale è su una linea
- `GetDistanceValue()`: distanza normalizzata tra le linee rilevate (indicatore di posizione sul campo)

I due valori sono usati come osservazioni dall'agente.

---

## Utilities

### `BoundsCollisionChecker.cs`
Utility statica per verificare se un collider è in collisione con altri oggetti. Usata durante il posizionamento di robot e palla per evitare spawn sovrapposti.

### `InitAgentUtilities.cs`
Utility per l'inizializzazione degli agenti (helper methods usati durante il setup).
