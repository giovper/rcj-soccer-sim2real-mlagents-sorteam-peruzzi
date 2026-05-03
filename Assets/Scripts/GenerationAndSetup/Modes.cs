using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum E_Mode
{
    Testing,
    ThrowBallTraining,
    ReachBall,
    Competition1v1
}

public enum E_ExecutionMode
{
    Default,       // training con mlagents-learn
    Inference,     // inferenza pura (un campo, modello fisso)
    Heuristic,     // controllo da tastiera (un campo)
    UserVsAgent    // giocatore (tastiera) vs modello (un campo)
}