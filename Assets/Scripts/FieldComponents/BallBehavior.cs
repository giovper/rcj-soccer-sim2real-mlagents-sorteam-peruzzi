using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BallBehavior : MonoBehaviour
{
    public Rigidbody rb {get; private set;}
    GameObject Field;
    ContestantConfigLoader contestantConfigLoader;
    FieldUtilities fieldUtilities;


    void Awake()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        Field = InitAgentUtilities.GetFieldParent(this.gameObject);
        fieldUtilities = Field.GetComponent<FieldUtilities>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ScoreArea"))
        {
            fieldUtilities.gameManager.BallScored(other.transform.parent.gameObject.GetComponent<ScoreUtilities>().isYellowGoal);
        }
    }

    public bool SetRandomSingleLocationAlpha (float X, float Y)
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector2 fieldSize = fieldUtilities.FieldSize;

        Vector3 location = fieldUtilities.GetLocalPositionFromLogicalPos(X, Y); // new Vector3((fieldSize.x) / 20 * X, 0, (fieldSize.y) / 20 * Y);

        transform.localPosition = location;

        bool ciSonoCollisioni = BoundsCollisionChecker.CheckBoundsCollision(
            gameObject.GetComponent<SphereCollider>(),
            null,    // Lista degli oggetti da escludere
            Physics.AllLayers,     // <-- Controlla TUTTI i layer possibili
            true,                 // Disegna il parallelepipedo (Rosso/Verde)
            true,                     // Disegna il box (Azzurro) attorno a chi viene toccato
            true,
            3f
        ).Any();

        if (!ciSonoCollisioni)
        {
            return true;
        }
        
        return false;
    }

    private bool SetRandomLocationRange(Vector2 rangeX, Vector2 rangeY, int maxAttempts = 30)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            if (SetRandomSingleLocationAlpha(UnityEngine.Random.Range(rangeX.x, rangeX.y), UnityEngine.Random.Range(rangeY.x, rangeY.y)))
                return true;
        }
        Debug.LogError("Impossible to place ball");
        Debug.Break();
        return false;
    }

    public double GetShapeRadius()
    {
        return (transform.lossyScale / 2).x;
    }

    public void PlaceBall(string position)
    {
        if (position == "center0.7")
        {
            SetRandomLocationRange(new Vector2(-0.7f, 0.7f), new Vector2(-0.7f, 0.7f));
        }
        else if (position == "free1")
        {
            SetRandomLocationRange(new Vector2(-1f, 1f), new Vector2(-1f, 1f));
        }
        else if (position == "penalty")
        {
            // Palla vicino alla porta gialla (x positivo) o blu (x negativo),
            // scelta casuale del lato così entrambi i team si allenano.
            // X: 0.55 → 0.85 (abbastanza vicino alla porta ma non attaccato)
            // Z: -0.35 → 0.35 (centrato sul campo)
            float side = UnityEngine.Random.value > 0.5f ? 1f : -1f;
            SetRandomLocationRange(
                new Vector2(0.55f * side, 0.85f * side),
                new Vector2(-0.35f, 0.35f),
                maxAttempts: 2000
            );
        }
    }
}
