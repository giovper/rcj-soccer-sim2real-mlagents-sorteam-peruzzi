# Soccer AI — Unity ML-Agents (RoboCup Junior Soccer Open)

Progetto di reinforcement learning per addestrare agenti artificiali a giocare a calcio in una simulazione Unity, con l'obiettivo di trasferire il modello addestrato su robot fisici per il **RoboCup Junior Soccer Open**.

---

## Competizione

Questo progetto è stato creato come parte inegrante del robot del nostro team "SOR", che ha partecipato alle _gare nazionali_ della Robocup JR, la competizione nella quale la nostra scuola da anni partecipa col progetto extrascolastico di robotica.

_La RoboCup Junior Soccer Open è una competizione internazionale di robotica educativa in cui squadre di studenti progettano, costruiscono e programmano robot autonomi per giocare partite di calcio.
Nella categoria “Open”, i partecipanti possono usare soluzioni hardware e software più avanzate rispetto alle categorie base, puntando su strategia, sensori e intelligenza artificiale.
I robot devono riconoscere la palla, muoversi sul campo in autonomia e collaborare con i compagni di squadra per segnare goal.
L’obiettivo della competizione è sviluppare competenze STEM, problem solving, programmazione e lavoro di squadra in un contesto pratico e creativo._

Io, Giovanni Peruzzi, mi sono occupato principalmente del lato software, in particolare di parte della Computer Vision (effettuta attraverso uno specchio iperbolico - vedi docs/GeogebraSpecchio.png - e la necessaria de-distorsione e riconoscimento palla e porte, usate per triangolare la posizione) e soprattutto del comportamento del robot: abbiamo fin da subito deciso di utilizzare una IA.

_Il nostro robot sarà programmato tramite agenti di intelligenza artificiale allenati in simulazione virtuale con Unity ML-Agents.
Questo approccio gli consente di adattarsi dinamicamente alle situazioni di gioco, migliorando strategia e reattività._

Tuttavia, a causa di problemi organizzativi col lavoro e con l'azienda sponsor, purtoppo, l'hardware del robot non è stato pronto dunque alle gare non abbiamo utilizzato alcun modello IA. Siamo però desiderosi di ritentarci l'anno prossimo, alla RobocupJR 2027

**NOTA**: Mentre qui i robot si sfidano in un 1v1, nella RoboCup JR categoria Soccer Open si sfidano 2 robot contro 2 robot in una partita. L'idea iniziale quando questo progetto è inziato era di allenare 2 agenti IA per i 2 ruoli dei 2 robot, oppure di allenarne solo uno che potesse fare entrambe le cose, quindi lo stesso modello per ambe i robot. Per difficoltà nel progetto - mancanza di tempo e per il concentrarsi sulle difficoltà dell'hardware, che non è stato pronto durante le gare, abbiamo deciso di riservare l'approccio IA solamente al robot attaccante, più complesso da ideare, e il ruolo del difensore, definito e separato, sarebbe stato implementato con un codice che prevedeva la traiettoria della palla e si posizionava in modo tale da bloccarla, qualora andasse nella porta da difendere.

Sono disponibili altre info negli allegati nella cartella docs/

---

## Descrizione

Due robot (uno per squadra) imparano a giocare in un campo da calcio in miniatura tramite **PPO** (Proximal Policy Optimization). Il training avviene con un curriculum a fasi progressive:

- **Fase 0 — AvvicinatiPalla**: l'agente impara ad avvicinarsi alla palla e spingerla verso la porta
- **Fase 1 — ImparaTirare**: l'agente impara a tirare con precisione e forza
- **Fase 2 — CombattiControAvversari**: l'agente affronta modelli pre-allenati di difficoltà crescente

Ogni episodio termina quando viene segnato un goal o allo scadere del tempo. La posizione di partenza di robot e palla è randomizzata a ogni episodio.

---

## Dipendenze

### Unity
- **Unity 2022.3.62f2**
- Package: `com.unity.ml-agents` 3.0.0
- Package: `com.unity.sentis` 2.1.0

### Python
Crea un virtual environment e installa le dipendenze:

```bash
python3 -m venv venv
source venv/bin/activate          # macOS/Linux
# oppure: venv\Scripts\activate   # Windows
pip install -r requirements_MacPythonVENV.txt
```

Pacchetti principali: `mlagents==0.30.0`, `torch==1.11.0`, `onnx==1.15.0`

---

## Come avviare il training

1. Apri il progetto in Unity 2022.3.62f2
2. Apri la scena principale (es. `Assets/Scenes/MainScene`)
3. Configura i parametri nel componente `ContestantConfigLoader` nell'Inspector:
   - `NUM_FIELDS`: numero di campi paralleli (es. 30)
   - `ExecutionMode`: `Default` per il training
   - `RewCC`: assegna il ScriptableObject `RewardConfigCollection`
4. Attiva il virtual environment Python
5. Avvia il training:

```bash
mlagents-learn configs/config_simpler_curriculum_2_edit.yaml --run-id=NomeRun --force
```

6. Avvia la simulazione in Unity (Play)

I risultati vengono salvati in `results/<NomeRun>/`.

---

## Come eseguire in inferenza

1. Imposta `ExecutionMode` su `Inference` nel `ContestantConfigLoader`
2. Assegna il modello `.onnx` nel campo `Model` del `BehaviorParameters`
3. Avvia la scena in Unity

### Modalità Utente vs AI
Imposta `ExecutionMode` su `UserVsAgent` e configura `PlayerTeamID` (0 = blu).  
Controlli: `W/A/S/D` per muoversi, `Q/E` per ruotare.

---

## Struttura del progetto

```
Assets/Scripts/
├── FieldComponents/     # Componenti del campo (palla, porte, muri)
├── Gameplay/            # Logica di gioco (agenti, reward, GameManager)
├── GenerationAndSetup/  # Setup del campo e degli agenti a runtime
├── Sensors/             # Sensori di linea (simulazione sensori reali)
└── Utilities/           # Utility varie

configs/                 # File YAML per il training ML-Agents
results/                 # Output del training (checkpoints, log TensorBoard)
docs/                    # Documentazione tecnica del progetto
```

---

## Visualizzare i risultati con TensorBoard

```bash
tensorboard --logdir results/
```

---

## Docs

- [Progressi del progetto](docs/progress.md)
- [Descrizione degli script](docs/scripts.md)
- [Generazione del campo e degli agenti](docs/generation.md)
