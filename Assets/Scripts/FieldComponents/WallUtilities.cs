using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallUtilities : MonoBehaviour
{
    [SerializeField] public int id;

    public void SetFieldSizeWall(GameObject prefab, Vector2 size)
    {
        Vector3 pos = transform.localPosition;
        Vector3 scale = transform.localScale;

        if (id <= 1) //long barriers
        {
            pos.z = size.y/20 * (id==0 ? -1 : 1) + (transform.localScale.z/2 * (id==0 ? -1 : 1));
            scale.x = size.x/10 + 2*transform.localScale.z;
        }
        else
        {
            pos.x = size.x/20 * (id==3 ? -1 : 1) + (transform.localScale.z/2 * (id==3 ? -1 : 1));
            scale.x = size.y/10 + 2*transform.localScale.z;
        }

        transform.localPosition = pos;
        transform.localScale = scale;
    }
}
