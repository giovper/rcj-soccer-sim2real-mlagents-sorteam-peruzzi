# Progressi del progetto

## Prima della Nuova Architettura

Le prime run (fino a `_Prova_7Mar26`) usavano un'architettura diversa:
- Gli episodi **non terminavano al goal** — duravano sempre fino allo scadere del tempo
- La posizione iniziale del robot non era randomizzata a ogni episodio
- Non era presente il sistema di fasi curriculum
- Non c'era il meccanismo di agenti dummy (il robot sfidava solo altri agenti in training)

Problemi riscontrati:
- Dopo ~80M step il mean reward calava drasticamente
- Il robot imparava a colpire la palla verso la porta ma non a fare goal con continuità
- Forte paura dei muri (reward negativa troppo alta per la collisione)

---

## Nuova Architettura

A partire da `ProvaNuovaArchitettura2`, il sistema è stato riscritto:
- **Ogni episodio termina al goal** (o allo scadere del tempo)
- **Posizione randomizzata** a ogni episodio (robot e palla)
- **Curriculum a fasi** tramite `PhaseManager` e ML-Agents `environment_parameters`
- **Agenti dummy** (modelli pre-allenati) come avversari in alcune fasi
- Introdotto `RewardConfigCollection` (ScriptableObject con reward per fase)

---

## ProvaNuovaArchitettura2
Prima run con la nuova architettura. Problema: in fase 2 il robot non randomizzava la posizione → corretto.

## ProvaNuovaArchitettura3 / 3_2
- Uso di `config_simpler_curriculum_1.yaml` poi `config_simpler_curriculum_2.yaml`
- Base solida della nuova architettura

## ProvaNuovaArchitettura4 — RientroPalla
- **Novità principale**: meccanismo di throw-in — se la palla esce dal campo per >3-5s, il GameManager la riporta in una posizione di gioco
- Prima del fix, il robot inseguiva la palla fuori dal campo sprecando step
- Risultati molto migliorati già da 5M step

## ProvaNuovaArchitettura5 — RobotStaticoFase2
- Aggiunto robot statico (dummy fermo) anche nell'ultima fase del curriculum
- Risultati simili alla run precedente ma la modifica è stata mantenuta

## ProvaNuovaArchitettura5_2 / 5_3 — Noise ridotto a ±0.05
- Esperimento: riduzione del noise sulle osservazioni da ±0.1 a ±0.05
- Risultati: nessun cambiamento significativo

## ProvaNuovaArchitettura5_4 — Noise a zero
- Esperimento: noise completamente rimosso
- Risultati: miglioramento solo marginale

## ProvaNuovaArchitettura5_5 — Noise Dinamico
- **Novità**: noise proporzionale alla distanza dalla palla (più lontana = più rumore)
- Formula: `adaptiveNoise = Lerp(MIN, MAX, dist / MAX_DIST)`
- Risultati: maggiore precisione nel movimento e nell'approccio
- Problema rimasto: quando la palla è vicina e il robot quasi fermo, oscillazione avanti/indietro indeciso

## ProvaNuovaArchitettura6 / 6_2 — Osservazioni Velocità
- **Novità**: aggiunte 4 nuove osservazioni (velocità robot vx/vz + velocità palla vx/vz)
- `VectorObservationSize`: 6 → 10
- Il problema dell'oscillazione sembra risolto grazie alle obs di velocità
- **Nuovo problema**: il robot tende a spingere la palla nell'angolo in alto a sinistra contro il muro sopra la porta, segnando solo per rimbalzo fortuito dallo spigolo
- **Causa identificata**: con palla in alto a sinistra, il netto di reward era positivo: `proximityVeryClose(+0.11) + ballLikelyToScoreX(+0.20) + ballLikelyToScoreY(-0.10) + constantNeg(-0.12) = +0.09/step`

## ProvaNuovaArchitettura7 — Fix Exploit Angolo (parziale)
- `ballLikelyToScoreX` e `ballLikelyToScoreY` unite in `ballLikelyToScore` con formula moltiplicativa
- Prima versione: `alpha = alphaX * ((alphaY+1)/2)` — troppo morbida, angolo ancora positivo
- **Fix parziale**: problema ridotto ma non eliminato

## ProvaNuovaArchitettura7_3 — Fix Aggressivo + hitBallPrecisionOffset
- Formula finale: `alpha = alphaX * Max(0, alphaY)` con soglia Y a 0.4 (ampiezza porta)
- Aggiunta penalità fissa `-0.4 * ballLikelyToScore` quando palla è fuori fascia Y
- **Novità**: `hitBallPrecisionOffset` nell'RCC per fase — offset nella formula di precisione del tiro
  - Fase 0: 0.6, Fase 1: 0.35, Fase 2: 0.1 (solo tiri abbastanza precisi premiati)
- Ridotte proximity rewards in fase 2
- Miglioramento marginale della direzione; problema del posizionamento ancora presente quando il robot è lontano dalla porta

---

## Problema aperto
Il robot non apprende a **posizionarsi dietro la palla** prima di colpirla. Quando è lontano dalla porta, si avvicina alla palla da qualsiasi direzione e la spinge in modo non mirato. La prossima modifica prevista è l'aggiunta di una **positioning reward** che premia l'agente per essere sul lato corretto della palla rispetto alla porta.
