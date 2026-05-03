using UnityEngine;

public static class DebugUtils
{
    /*public static void DrawParallelepiped(Vector3 position, Quaternion rotation, Vector3 size, Color color)
    {
        Gizmos.color = color;
        Gizmos.matrix = Matrix4x4.TRS(position, rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, size);
    }*/

    public static void DrawParallelepipedRuntime(Vector3 pos, Quaternion rot, Vector3 size, Color color, float duration=8f)
    {
        Vector3 half = size * 0.5f;
        Vector3[] p = new Vector3[8];

        p[0] = pos + rot * new Vector3(-half.x, -half.y, -half.z);
        p[1] = pos + rot * new Vector3(half.x, -half.y, -half.z);
        p[2] = pos + rot * new Vector3(half.x, half.y, -half.z);
        p[3] = pos + rot * new Vector3(-half.x, half.y, -half.z);
        p[4] = pos + rot * new Vector3(-half.x, -half.y, half.z);
        p[5] = pos + rot * new Vector3(half.x, -half.y, half.z);
        p[6] = pos + rot * new Vector3(half.x, half.y, half.z);
        p[7] = pos + rot * new Vector3(-half.x, half.y, half.z);

        void L(int a, int b) => Debug.DrawLine(p[a], p[b], color, duration);

        L(0, 1); L(1, 2); L(2, 3); L(3, 0);
        L(4, 5); L(5, 6); L(6, 7); L(7, 4);
        L(0, 4); L(1, 5); L(2, 6); L(3, 7);
    }

    public static void DrawLineRuntime(Vector3 pos1, Vector3 pos2, Color color, float duration=8f)
    {

        Debug.DrawLine(pos1, pos2, color, duration);
    }

    public static void DrawSphereRuntime(Vector3 center, float radius, Color color, float duration = 8f)
    {
        int meridians = 8;    // numero di "fette" della sfera
        int segmentsPerMeridian = 4; // segmenti per ogni meridiano (2 sopra, 2 sotto)

        for (int m = 0; m < meridians; m++)
        {
            float angle = (360f / meridians) * m;
            Quaternion rot = Quaternion.Euler(0, angle, 0);

            Vector3 prevPoint = center + rot * new Vector3(0, radius, 0);

            for (int s = 1; s <= segmentsPerMeridian; s++)
            {
                float t = s / (float)segmentsPerMeridian;
                float y = Mathf.Cos(t * Mathf.PI) * radius;            // altezza
                float r = Mathf.Sin(t * Mathf.PI) * radius;            // raggio alla quota y
                Vector3 point = center + rot * new Vector3(r, y, 0);

                Debug.DrawLine(prevPoint, point, color, duration);
                prevPoint = point;
            }
        }
    }

    public static void DrawCapsuleRuntime(Vector3 center, float radius, float height, int direction = 1, Color color = default, float duration = 8f)
    {
        if (color == default) color = Color.green;

        Vector3 dir = Vector3.up;
        Vector3 up = Vector3.up;
        Vector3 right = Vector3.right;
        Vector3 forward = Vector3.forward;

        switch (direction)
        {
            case 0: dir = Vector3.right; up = Vector3.up; forward = Vector3.forward; break;
            case 1: dir = Vector3.up; up = Vector3.forward; forward = Vector3.right; break;
            case 2: dir = Vector3.forward; up = Vector3.up; forward = Vector3.right; break;
        }

        float cylinderHeight = Mathf.Max(height - 2 * radius, 0);
        Vector3 topCenter = center + dir * (cylinderHeight / 2);
        Vector3 bottomCenter = center - dir * (cylinderHeight / 2);

        int meridians = 8;
        int segmentsPerHemisphere = 2;

        for (int m = 0; m < meridians; m++)
        {
            float angle = (360f / meridians) * m;
            Quaternion rot = Quaternion.AngleAxis(angle, dir);

            // Parte superiore
            Vector3 prev = topCenter + rot * (up * radius);
            for (int s = 1; s <= segmentsPerHemisphere; s++)
            {
                float t = s / (float)segmentsPerHemisphere * Mathf.PI / 2; // da polo all�equatore
                float y = Mathf.Cos(t) * radius;
                float r = Mathf.Sin(t) * radius;
                Vector3 p = topCenter + rot * (up * y + forward * r);
                Debug.DrawLine(prev, p, color, duration);
                prev = p;
            }

            // Parte inferiore
            prev = bottomCenter + rot * (-up * radius);
            for (int s = 1; s <= segmentsPerHemisphere; s++)
            {
                float t = s / (float)segmentsPerHemisphere * Mathf.PI / 2;
                float y = -Mathf.Cos(t) * radius;
                float r = Mathf.Sin(t) * radius;
                Vector3 p = bottomCenter + rot * (up * y + forward * r);
                Debug.DrawLine(prev, p, color, duration);
                prev = p;
            }

            // Linea verticale cilindro
            Debug.DrawLine(topCenter + rot * (forward * radius), bottomCenter + rot * (forward * radius), color, duration);
        }
    }

}
