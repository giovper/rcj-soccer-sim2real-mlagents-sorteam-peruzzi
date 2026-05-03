using System;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;
using UnityEngineInternal;

public static class InitAgentUtilities
{
    public static GameObject GetFieldParent (GameObject target)
    {
        if (target == null)
        {
            Debug.LogError("Target nullo in GetFieldParent.");
            return null;
        }
        Transform parent = target.transform.parent;
        for (int i = 0; i < 6; i++)
        {
            if (parent.gameObject.CompareTag("SoccerEnv"))
                return parent.gameObject;
            parent = parent.parent;
        }
        return null;
    }

    // 🔹 Trova il GameObject della palla
    public static GameObject GetBall(GameObject field)
    {
        return FindChildWithTag(field, "Target");
    }

    // 🔹 Trova il GameObject dell'agente (robot)
    public static GameObject GetAgent(GameObject field)
    {
        return FindChildWithTag(field, "AgentObject");
    }

    // 🔹 Trova la porta gialla
    public static GameObject GetYellowGoal(GameObject field)
    {
        return FindGoal(field, "GoalObject", true);
    }

    // 🔹 Trova la porta blu
    public static GameObject GetBlueGoal(GameObject field)
    {
        return FindGoal(field, "GoalObject", false);
    }

    // 🔹 Metodo generico per cercare figlio con tag
    public static GameObject FindChildWithTag(GameObject parent, string tag)
    {
        if (parent == null)
        {
            Debug.LogError("Parent nullo in FindChildWithTag.");
            return null;
        }

        foreach (Transform child in parent.transform)
        {
            if (child.CompareTag(tag))
                return child.gameObject;
        }

        Debug.LogWarning($"Nessun figlio con tag {tag} trovato sotto {parent.name}");
        return null;
    }

    // 🔹 Metodo generico per cercare goal specifico (gialla/blu)
    private static GameObject FindGoal(GameObject parent, string tag, bool yellow)
    {
        if (parent == null)
        {
            Debug.LogError("Parent nullo in FindGoal.");
            return null;
        }

        foreach (Transform child in parent.transform)
        {
            if (child.CompareTag(tag))
            {
                ScoreUtilities su = child.GetComponent<ScoreUtilities>();
                if (su != null && su.isYellowGoal == yellow)
                    return child.gameObject;
            }
        }

        //Debug.LogWarning($"Nessuna porta {(yellow ? "gialla" : "blu")} trovata sotto {parent.name}");
        return null;
    }

    public static GameObject FindFirstRootWithTag(string tag)
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject obj in objs)
        {
            if (obj.transform.parent == null) // deve essere in root
                return obj;
        }
        return null;
    }
    
    //ELIMINARE
    public static bool IsSphereColliding(SphereCollider sphere, bool showAny = false, bool showCorrect = false, LayerMask mask = default)
    {
        if (sphere == null)
            return false;

        if (mask == default)
            mask = ~0;

        // Centro in coordinate globali
        Vector3 worldCenter = sphere.transform.TransformPoint(sphere.center);

        // Calcolo del raggio in base alla scala globale (prendiamo la componente massima)
        float maxScale = Mathf.Max(
            sphere.transform.lossyScale.x,
            sphere.transform.lossyScale.y,
            sphere.transform.lossyScale.z
        );
        float worldRadius = sphere.radius * maxScale;

        Collider[] hits = Physics.OverlapSphere(worldCenter, worldRadius, mask, QueryTriggerInteraction.Ignore);

        // DEBUG
        if (showAny)
            DebugUtils.DrawSphereRuntime(worldCenter, worldRadius, Color.red, 4);

        foreach (var hit in hits)
            if (hit.transform != sphere.transform)
            {
                Debug.LogError(hit.name);
                Vector3 v = hit.transform.position;
                v.y += 10;
                DebugUtils.DrawLineRuntime(hit.transform.position, v, Color.green);
                hit.transform.position = v;
                return true;
            }

        if (showCorrect)
            DebugUtils.DrawSphereRuntime(worldCenter, worldRadius, Color.green, 4);

        return false;
    }

    //ELIMINARE
    public static bool IsBoxColliding(BoxCollider box, bool showAny = false, bool showCorrect = false, LayerMask mask = default) //non usato
    {
        if (box == null)
            return false;

        if (mask == default)
            mask = ~0;

        Vector3 worldCenter = box.transform.TransformPoint(box.center);
        Vector3 worldSize = Vector3.Scale(box.size, box.transform.lossyScale);
        Vector3 halfExtents = worldSize * 0.5f;
        Quaternion worldRot = box.transform.rotation;

        Collider[] hits = Physics.OverlapBox(worldCenter, halfExtents, worldRot, mask, QueryTriggerInteraction.Ignore);

        // DEBUG
        if (showAny)
            DebugUtils.DrawParallelepipedRuntime(worldCenter, worldRot, halfExtents * 2, Color.red, 4);

        foreach (var hit in hits)
            if (hit.transform != box.transform)
            {
                hit.transform.localPosition += new Vector3(0f, 100f, 0f);
                return true;
            }

        if (showCorrect)
            DebugUtils.DrawParallelepipedRuntime(worldCenter, worldRot, halfExtents * 2, Color.green, 4);

        return false;
    }

    //ELIMINARE
    public static bool IsCapsuleColliding(CapsuleCollider capsule, bool showAny = false, bool showCorrect = false, LayerMask mask = default)
    {
        if (capsule == null)
            return false;

        if (mask == default)
            mask = ~0;

        Transform t = capsule.transform;

        // Direzione dell'asse della capsula (0=x, 1=y, 2=z)
        Vector3 dir = Vector3.up;
        if (capsule.direction == 0) dir = Vector3.right;
        else if (capsule.direction == 2) dir = Vector3.forward;

        // Centro globale
        Vector3 center = t.TransformPoint(capsule.center);

        // Calcolo scala massima per il raggio
        Vector3 lossy = t.lossyScale;
        float radiusScale = 1f;
        float heightScale = 1f;

        switch (capsule.direction)
        {
            case 0: // x
                radiusScale = Mathf.Max(lossy.y, lossy.z);
                heightScale = lossy.x;
                break;
            case 1: // y
                radiusScale = Mathf.Max(lossy.x, lossy.z);
                heightScale = lossy.y;
                break;
            case 2: // z
                radiusScale = Mathf.Max(lossy.x, lossy.y);
                heightScale = lossy.z;
                break;
        }

        float radius = capsule.radius * radiusScale;
        float height = Mathf.Max(capsule.height * heightScale, 2 * radius); // altezza totale, almeno 2*radius

        // Punti A e B dei poli della capsula
        Vector3 offset = dir * ((height / 2) - radius);
        Vector3 pointA = center + offset;
        Vector3 pointB = center - offset;

        Collider[] hits = Physics.OverlapCapsule(pointA, pointB, radius, mask, QueryTriggerInteraction.Ignore);

        // DEBUG
        if (showAny)
            DebugUtils.DrawCapsuleRuntime(center, radius, height, capsule.direction, Color.red, 4);

        foreach (var hit in hits)
            if (hit.transform != capsule.transform)
                return true;

        if (showCorrect)
            DebugUtils.DrawCapsuleRuntime(center, radius, height, capsule.direction, Color.green, 4);

        return false;
    }

    //ELIMINARE
    public static bool IsColliderColliding(Collider collider, List<Collider> toIgnoreColliders, string ignoreCollidersWithGameObjectsTag, bool showAny = false, bool showCorrect = false, LayerMask mask = default)
    {
        if (!collider.enabled)
        {
            throw new Exception("COllider disabled");
        }
        toIgnoreColliders.Add(collider);

        if (collider == null)
            return false;

        if (mask == default)
            mask = ~0;

        // Prendiamo il bounds globale
        Bounds b = collider.bounds; 
        Vector3 center = collider.transform.position; //questo è sbagliato
        Vector3 Extents = b.extents; // metà dimensione
        Extents.y *= 2;
        Quaternion rot = Quaternion.identity; // bounds sono sempre axis-aligned

        Extents = Vector3.Scale(Extents, collider.transform.lossyScale);

        //Debug.LogWarning("diocanennnn" + halfExtents);

        Collider[] hits = Physics.OverlapBox(center, new Vector3(0.02f, 0.02f, 0.02f), rot, mask, QueryTriggerInteraction.Ignore);

        // DEBUG
        if (showAny)
        {
            DebugUtils.DrawParallelepipedRuntime(center, rot, Extents, Color.red, 4);
            DebugUtils.DrawParallelepipedRuntime(center, Quaternion.identity, new Vector3(0.02f, 0.02f, 0.02f), Color.black, 3);
        }

        foreach (var hit in hits)
            if (!toIgnoreColliders.Contains(hit) && !hit.gameObject.CompareTag(ignoreCollidersWithGameObjectsTag))
            {
                var p = hit.gameObject.transform.position;
                p.y += 3f;
                hit.gameObject.transform.position = p;

                /*var xp = collider.gameObject.transform.position;
                xp.y -= 10;
                collider.gameObject.transform.position = xp;*/

                Debug.LogWarning(hit.gameObject.name);
                
                return true;
            }

        if (showCorrect)
            DebugUtils.DrawParallelepipedRuntime(center, rot, Extents, Color.green, 4);

        return false;
    }
    public static Vector3 ZeroY(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }
}