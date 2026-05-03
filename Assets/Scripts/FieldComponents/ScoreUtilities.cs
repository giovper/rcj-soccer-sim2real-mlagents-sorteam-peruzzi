using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreUtilities : MonoBehaviour
{

    public bool isYellowGoal = false;

    GameObject Field;
    FieldUtilities fieldUtilities;

    public (Vector3, Vector3) WorldAngles;
    public (Vector3, Vector3) LocalAngles;

    void Start()
    {
        Field = InitAgentUtilities.GetFieldParent(this.gameObject);
        fieldUtilities = Field.GetComponent<FieldUtilities>();
    }

    public void SetGoalSize(Vector2 fieldSize, Vector3 scoreSize)
    {

        Vector3 pos = transform.localPosition;
        Vector3 scale = transform.localScale;

        scale.y = 1f / 14 * scoreSize.y;
        scale.z = 1f / 60 * scoreSize.x;

        scale.x = 1f / 7.4f * scoreSize.z;


        pos.x = fieldSize.x/20 * (isYellowGoal ? 1 : -1) + scale.x/2 * (isYellowGoal ? -1 : 1);
        pos.z = 0;

        transform.localPosition = pos;
        transform.localScale = scale;

        GameObject area = InitAgentUtilities.FindChildWithTag(gameObject, "ScoreArea");
        GameObject areaSheild = InitAgentUtilities.FindChildWithTag(gameObject, "ScoreAreaSheild");

        Vector3 areaPos = area.transform.localPosition;
        areaPos.x = /*area.transform.localScale.z*/ 0.185f /2.0f * (isYellowGoal ? 1 : -1);
        area.transform.localPosition = areaPos;

        Vector3 areaSheildPos = areaSheild.transform.localPosition;
        areaSheildPos.x = (0.37f - areaSheild.transform.localScale.z / 2.0f) * (isYellowGoal ? 1 : -1);
        areaSheild.transform.localPosition = areaSheildPos;

        WorldAngles = GetBottomWorldVerticesFromBounds(area.GetComponent<MeshFilter>(), isYellowGoal);
        LocalAngles = GetLocalBottomWorldVerticesFromBounds(area.GetComponent<MeshFilter>(), isYellowGoal);

    }

    
    //ELIMINARE
    public bool SetRandomSingleLocationAlpha(float X, float Y, float angle) //non usato
    {
        Vector2 fieldSize = fieldUtilities.FieldSize;

        Vector3 localPosition = new Vector3((fieldSize.x) / 20 * X, -0.05f, (fieldSize.y) / 20 * Y);
        Quaternion localRotation = Quaternion.Euler(new Vector3(0, Random.Range(0f, 360f), 0));

        transform.localPosition = localPosition;
        transform.localRotation = localRotation;

        if (!InitAgentUtilities.IsBoxColliding(gameObject.GetComponent<BoxCollider>(), true, true))
        {
            return true;
        }

        return false;
    }
    
    
    //ELIMINARE
    public bool SetRandomLocationLearnThrow(int maxAttempts = 30) //non usato
    {
        for (int i = 0; i< maxAttempts; i++)
        {
            if (SetRandomSingleLocationAlpha(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(0, 360)))
                return true;
        }
        Debug.LogError("Impossible to place score");
        Time.timeScale = 0;
        return false;
    }


    public static (Vector3 sx, Vector3 dx) GetBottomWorldVerticesFromBounds(MeshFilter meshFilter, bool zLow)
    {
        Mesh mesh = meshFilter.sharedMesh;
        Bounds b = mesh.bounds;

        // Otteniamo min e max (in local space)
        Vector3 min = b.min;
        Vector3 max = b.max;

        // Gli 8 vertici del cubo in local space
        Vector3[] localVerts = new Vector3[]
        {
            new Vector3(min.x, min.y, zLow ? min.z : max.z),
            new Vector3(max.x, min.y, zLow ? min.z : max.z),
        };

        // Converte in world space
        Matrix4x4 localToWorld = meshFilter.transform.localToWorldMatrix;
        Vector3[] worldVerts = new Vector3[2];

        for (int i = 0; i < 2; i++)
            worldVerts[i] = localToWorld.MultiplyPoint3x4(localVerts[i]);

        //DebugUtils.DrawParallelepipedRuntime(worldVerts[zLow ? 0 : 1], Quaternion.identity, new Vector3(0.02f, 0.02f, 0.02f), Color.black);
        //DebugUtils.DrawParallelepipedRuntime(worldVerts[zLow ? 1 : 0], Quaternion.identity, new Vector3(0.02f, 0.02f, 0.02f), Color.gray);

        return ((worldVerts[zLow ? 0 : 1], worldVerts[zLow ? 1 : 0]));
    }

    public static (Vector3 sx, Vector3 dx) GetLocalBottomWorldVerticesFromBounds(MeshFilter meshFilter, bool zLow)
    {
        Mesh mesh = meshFilter.sharedMesh;
        Bounds b = mesh.bounds;

        // Otteniamo min e max (in local space)
        Vector3 min = b.min;
        Vector3 max = b.max;

        // Gli 8 vertici del cubo in local space
        Vector3[] localVerts = new Vector3[]
        {
            new Vector3(min.x, min.y, zLow ? min.z : max.z),
            new Vector3(max.x, min.y, zLow ? min.z : max.z),
        };
        return ((localVerts[zLow ? 0 : 1], localVerts[zLow ? 1 : 0]));
    }

}
