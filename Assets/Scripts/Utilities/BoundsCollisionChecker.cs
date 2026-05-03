using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public static class BoundsCollisionChecker
{
    /// <summary>
    /// Verifica collisioni nel parallelepipedo definito dai bounds di un Collider.
    /// </summary>
    /// <param name="source">Collider sorgente di cui usare i bounds</param>
    /// <param name="ignoreList">GameObjects da ignorare (può essere null)</param>
    /// <param name="mask">LayerMask (default = Everything)</param>
    /// <param name="drawOnCollision">Se true, disegna in rosso il box quando collide</param>
    /// <param name="drawHitColliders">Se true, disegna in azzurro i bounds dei collider colpiti</param>
    /// <param name="drawOnClear">Se true, disegna in verde il box quando NON collide</param>
    /// <param name="drawDuration">Durata in secondi dei disegni debug</param>
    /// <returns>Array di Collider trovati (esclusi quelli nella ignoreList)</returns>
    public static Collider[] CheckBoundsCollision(
        Collider source,
        List<GameObject> ignoreList = null,
        int mask = ~0,
        bool drawOnCollision = false,
        bool drawHitColliders = false,
        bool drawOnClear = false,
        float drawDuration = 1f)
    {
        if (source == null)
        {
            Debug.LogError("BoundsCollisionChecker: source Collider è null.");
            return System.Array.Empty<Collider>();
        }

        Bounds bounds = source.bounds;

        // Calcola centro e half-extents in world space rispettando la rotazione
        Vector3 center;
        Vector3 halfExtents;
        Quaternion orientation;
        GetColliderWorldParams(source, out center, out halfExtents, out orientation);

        Collider[] rawHits = Physics.OverlapBox(center, halfExtents, orientation, mask);

        // Filtra: rimuovi il collider sorgente e quelli nella ignoreList
        var filtered = new List<Collider>();
        HashSet<GameObject> ignoreSet = ignoreList != null
            ? new HashSet<GameObject>(ignoreList)
            : null;

        foreach (var hit in rawHits)
        {
            if (hit == source) continue;
            if (hit.gameObject == source.gameObject) continue;
            if (ignoreSet != null && ignoreSet.Contains(hit.gameObject)) continue;
            filtered.Add(hit);
        }

        bool hasCollisions = filtered.Count > 0;

        // --- DEBUG DRAW ---
        if (hasCollisions && drawOnCollision)
        {
            DrawOrientedBox(center, halfExtents, orientation, Color.red, drawDuration);
            //Debug.Log($"[Universal Hit] {center}");
        }

        if (hasCollisions && drawHitColliders)
        {
            foreach (var hit in filtered)
            {
                Vector3 hc, hhe;
                Quaternion ho;
                GetColliderWorldParams(hit, out hc, out hhe, out ho);
                DrawOrientedBox(hc, hhe, ho, Color.cyan, drawDuration);
                //Debug.Log($"[Universal Hit] {hit.name}");
            }
        }

        if (!hasCollisions && drawOnClear)
        {
            DrawOrientedBox(center, halfExtents, orientation, Color.green, drawDuration);
        }

        return filtered.ToArray();
    }

    /// <summary>
    /// Estrae centro world-space, half-extents locali e rotazione da qualsiasi tipo di Collider.
    /// </summary>
    private static void GetColliderWorldParams(Collider col, out Vector3 center, out Vector3 halfExtents, out Quaternion orientation)
    {
        Transform t = col.transform;
        orientation = t.rotation;
        Vector3 scale = t.lossyScale;

        if (col is BoxCollider box)
        {
            center = t.TransformPoint(box.center);
            halfExtents = Vector3.Scale(box.size * 0.5f, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        }
        else if (col is SphereCollider sphere)
        {
            center = t.TransformPoint(sphere.center);
            float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            float r = sphere.radius * maxScale;
            halfExtents = new Vector3(r, r, r);
        }
        else if (col is CapsuleCollider capsule)
        {
            center = t.TransformPoint(capsule.center);
            float r = capsule.radius;
            float h = capsule.height * 0.5f;
            // direction: 0=X, 1=Y, 2=Z
            Vector3 localHalf;
            switch (capsule.direction)
            {
                case 0:  localHalf = new Vector3(h, r, r); break;
                case 2:  localHalf = new Vector3(r, r, h); break;
                default: localHalf = new Vector3(r, h, r); break;
            }
            halfExtents = Vector3.Scale(localHalf, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        }
        else
        {
            MeshCollider mc = col as MeshCollider;
            if (mc != null && mc.sharedMesh != null)
            {
                Bounds localBounds = mc.sharedMesh.bounds;
                center = t.TransformPoint(localBounds.center);
                Vector3 ls = t.lossyScale;
                halfExtents = Vector3.Scale(
                    localBounds.extents,
                    new Vector3(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z))
                );
                orientation = Quaternion.identity;
            }
            else
            {
                center = t.position;
                halfExtents = Vector3.zero;
                orientation = Quaternion.identity;
                Debug.LogWarning($"MeshCollider su {col.gameObject.name} non ha mesh assegnata");
            }
        }
    }

    /// <summary>
    /// Disegna un wireframe box orientato (OBB) usando Debug.DrawLine.
    /// </summary>
    private static void DrawOrientedBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, Color color, float duration)
    {
        Vector3 right   = orientation * new Vector3(halfExtents.x, 0, 0);
        Vector3 up      = orientation * new Vector3(0, halfExtents.y, 0);
        Vector3 forward = orientation * new Vector3(0, 0, halfExtents.z);

        // 8 vertici dell'OBB
        Vector3 v0 = center - right - up - forward;
        Vector3 v1 = center + right - up - forward;
        Vector3 v2 = center + right - up + forward;
        Vector3 v3 = center - right - up + forward;
        Vector3 v4 = center - right + up - forward;
        Vector3 v5 = center + right + up - forward;
        Vector3 v6 = center + right + up + forward;
        Vector3 v7 = center - right + up + forward;

        // Faccia inferiore
        Debug.DrawLine(v0, v1, color, duration);
        Debug.DrawLine(v1, v2, color, duration);
        Debug.DrawLine(v2, v3, color, duration);
        Debug.DrawLine(v3, v0, color, duration);

        // Faccia superiore
        Debug.DrawLine(v4, v5, color, duration);
        Debug.DrawLine(v5, v6, color, duration);
        Debug.DrawLine(v6, v7, color, duration);
        Debug.DrawLine(v7, v4, color, duration);

        // Pilastri verticali
        Debug.DrawLine(v0, v4, color, duration);
        Debug.DrawLine(v1, v5, color, duration);
        Debug.DrawLine(v2, v6, color, duration);
        Debug.DrawLine(v3, v7, color, duration);
    }


    /// <summary>
    /// Disegna il box orientato (OBB) di un Collider specifico usando la logica interna della classe.
    /// </summary>
    /// <param name="col">Il collider da visualizzare</param>
    /// <param name="color">Colore delle linee</param>
    /// <param name="duration">Durata del disegno in secondi</param>
    public static void DrawCollider(Collider col, Color color, float duration = 1f)
    {
        if (col == null)
        {
            Debug.LogWarning("DrawCollider: Il collider fornito è null.");
            return;
        }

        Vector3 center;
        Vector3 halfExtents;
        Quaternion orientation;

        // Recupera i parametri world-space (posizione, rotazione e scala corretta)
        GetColliderWorldParams(col, out center, out halfExtents, out orientation);

        // Disegna esattamente come fa la funzione principale del checker
        DrawOrientedBox(center, halfExtents, orientation, color, duration);
    }

}