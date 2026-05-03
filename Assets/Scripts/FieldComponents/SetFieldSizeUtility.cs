using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetFieldSizeUtility : MonoBehaviour
{
    /// <summary>
    /// Sizes in cm.
    /// </summary>
    public void SetFieldSize (Vector2 size, Vector3 goalSize)
    {
        size /= 10;
        goalSize /= 10;

        gameObject.GetComponent<FieldUtilities>().FieldSize = size;
        gameObject.GetComponent<FieldUtilities>().GoalSize = goalSize;

        DebugUtils.DrawParallelepipedRuntime(transform.position, transform.rotation, new Vector3(size.x/10, 0.1f, size.y/10), Color.green, 4);

        Transform ground = GetChildWithTag(gameObject, "Ground")[0];
        ground.localScale = new Vector3(size.x/100, ground.localScale.y, size.y/100);

        foreach (Transform child in GetChildWithTag(gameObject, "Wall"))
        {
            child.gameObject.GetComponent<WallUtilities>().SetFieldSizeWall(gameObject, size);
        }

        foreach (Transform child in GetChildWithTag(gameObject, "GoalObject"))
        {
            child.gameObject.GetComponent<ScoreUtilities>().SetGoalSize(size, goalSize);
        }
    }

    List<Transform> GetChildWithTag(GameObject parent, string tag)
    {
        List<Transform> children = new();
        foreach (Transform child in parent.transform)
        {
            if (child.CompareTag(tag))
                children.Add(child);
        }
        return children;
    }

    public void SetAutoFieldSizeStandard ()
    {
        if (false)
            #pragma warning disable CS0162 // Unreachable code detected
            SetFieldSize(
                new Vector2(Random.Range(180f, 225f), Random.Range(110f, 160f)),
                new Vector3(Random.Range(45f, 60f), Random.Range(10f, 14f), Random.Range(6f, 10f))
            );
            #pragma warning restore CS0162 // Unreachable code detected
        else
            SetFieldSize(
                    new Vector2(243f, 182f),
                    new Vector3(62f, 14f, 10f)
                );
    }

    public void SetAutoFieldSizeLearnThrow()
    {
        SetFieldSize(
            new Vector2(Random.Range(250f, 270f), Random.Range(250f, 270f)),
            new Vector3(Random.Range(45f, 60f), Random.Range(10f, 14f), Random.Range(6f, 10f))
        );
    }
}
