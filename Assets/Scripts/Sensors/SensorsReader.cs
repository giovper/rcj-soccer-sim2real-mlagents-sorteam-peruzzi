using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SensorsReader : MonoBehaviour
{
    public int outerRadiusSensors = 10;
    public float outerRadius = 0.7f; //cm
    public int innerRadiusSensors = 1;
    public float innerRadius = 0f; // cm
    public ContestantAgentController contestantAgentController;

    LayerMask linesLayerMask;
    bool[] outer; bool[] inner;

    void Awake()
    {
        linesLayerMask = LayerMask.GetMask("FieldLines");
        outer = new bool[outerRadiusSensors];
        inner = new bool[innerRadiusSensors];
    }

    void Start()
    {
        StartCoroutine(SensorRoutine());
    }

    IEnumerator SensorRoutine()
    {
        while (true)
        {
            (outer, inner) = ReadLinesSensors();
            yield return new WaitForSeconds(0.1f); // ogni 100ms simulati
        }
    }
    
    
    /*
    void Update()
    {
        (outer, inner) = ReadLinesSensors();
    }
    */

    public bool GetCenterSensorValue()
    {
        return inner.Count( s => s ) > 0;
    }
    public float GetCenterSensorValueFloat()
    {
        return GetCenterSensorValue() ? 1f : 0f;
    }


    public (int n, float dist) GetMaxDistanceBetweenLinesFelt()
    {
        return GetMaxDistanceBetweenLinesFelt(outer);
    }

    protected (int n, float dist) GetMaxDistanceBetweenLinesFelt(bool[] outer)
    {
        int count = outer.Count(s => s);
        if (count == 0)
        {
            return (0, -1);
        }
        if (count == 1)
        {
            return (1, 0);
        }
        
        //2 o + sensori che hanno percepito la linea

        //devo ottenere gli indici dei due sensori i quali hanno tra di loro la maggiore distnza (le loro posizioni sono definite dalle funzioni Get..PointCoordinate)
        float maxDist = 0f;
        for (int i = 0; i < outer.Length; i++)
        {
            if (outer[i])
            {
                Vector3 posI = GetGlobalPointCoordinate(GetAngle(i, outerRadiusSensors), outerRadius);
                for (int j = i + 1; j < outer.Length; j++)
                {
                    if (outer[j])
                    {
                        Vector3 posJ = GetGlobalPointCoordinate(GetAngle(j, outerRadiusSensors), outerRadius);
                        float dist = Vector3.Distance(posI, posJ)/(outerRadius*2);
                        if (dist > maxDist)
                        {
                            maxDist = dist;
                        }
                    }
                }
            }
        }
        return (count, maxDist);
    }

    public float GetDistanceValue((int n, float dist) v)
    {
        if (v.n == 0)
            return 2f;
        else if (v.n == 1)
            return 0.6f;
        else
            return Mathf.Sqrt((float)(0.5f * 0.5f  +  Math.Pow(v.dist/2, 2))); //valore di distanza centro circ -> fino al centro della corda che si forma. E' espresso in un valore che è 0 laddova la corda passa x il centro, viene 0.5 se la corda è tangente alla circ
    }

    protected float? GetHitAngles (bool[] bools)
    {
        List<float> angles = new();

        for (int i = 0; i<bools.Length; i++)
        {
            if (bools[i])
            {
                angles.Add(GetAngle(i, outerRadiusSensors));
            }
        }

        if (angles.Count == 0)
            return null;

        float angle_min_global = angles.Min();
        float angle_max_global = angles.Max();

        //float mid_angle_global = Mathf.LerpAngle(angle_min_global, angle_max_global, 0.5f);

        float mid_angle_global = MediaAngoli(angles.ToArray());

        Vector3 globalCoord = GetLocalAndGlobalPointCoordinate(mid_angle_global, outerRadius).global;

        DebugUtils.DrawSphereRuntime(globalCoord, 0.01f, Color.black, Time.deltaTime);

        return mid_angle_global;
    }

    public static float MediaAngoli(float[] angoli)
    {
        if (angoli == null || angoli.Length == 0) return 0f;

        float sommaX = 0f;
        float sommaY = 0f;

        foreach (float gradi in angoli)
        {
            // 1. Converti l'angolo in radianti
            float rad = gradi * Mathf.Deg2Rad;

            // 2. Trasforma l'angolo in coordinate X e Y (Vettore Unitario)
            sommaX += Mathf.Cos(rad);
            sommaY += Mathf.Sin(rad);
        }

        // 3. Calcola l'angolo della media dei vettori usando Atan2
        // Atan2 gestisce automaticamente tutti i quadranti e il segno
        float mediaRad = Mathf.Atan2(sommaY, sommaX);

        // 4. Riconverti in gradi (risultato tra -180 e 180)
        float risultatoGradi = mediaRad * Mathf.Rad2Deg;

        // 5. Opzionale: Normalizza tra 0 e 360
        if (risultatoGradi < 0) risultatoGradi += 360f;

        return risultatoGradi;
    }

    protected float GetAngle(int i, int numSensors)
    {
        return -transform.rotation.eulerAngles.y * Mathf.Deg2Rad - (360f / numSensors * i) * Mathf.Deg2Rad;
    }

    protected (Vector2 local, Vector3 global) GetLocalAndGlobalPointCoordinate (float angle, float radius)
    {
        return (GetLocalPointCoordinate(angle, radius), GetGlobalPointCoordinate(angle, radius));
    }

    protected Vector2 GetLocalPointCoordinate(float angle, float radius)
    {
        return new Vector2((float)Mathf.Cos(angle) * radius, (float)Mathf.Sin(angle) * radius);
    }

    protected Vector3 GetGlobalPointCoordinate(float angle, float radius)
    {
        Vector2 coord = new Vector2((float)Mathf.Cos(angle) * radius, (float)Mathf.Sin(angle) * radius);
        Vector3 globalCoord = gameObject.transform.position;
        globalCoord.y -= (float)contestantAgentController.GetShapeHeight()/2f;
        globalCoord.x += coord.x;
        globalCoord.z += coord.y;
        globalCoord.y += 0.01f;

        return globalCoord;
    }

    protected (bool[] outer, bool[] inner) ReadLinesSensors()
    {
        bool[] readOuter = new bool[outerRadiusSensors];
        bool[] readInner = new bool[innerRadiusSensors];

        for (int i = 0; i<outerRadiusSensors; i++)
        {
            float angle = GetAngle(i, outerRadiusSensors);

            Vector2 coord; Vector3 globalCoord;

            (coord, globalCoord) = GetLocalAndGlobalPointCoordinate(angle, outerRadius);

            readOuter[i] = ReadColorSensor(globalCoord, true);
        }

        for (int i = 0; i<innerRadiusSensors; i++)
        {
            float angle = GetAngle(i, innerRadiusSensors);
            Vector2 coord; Vector3 globalCoord;

            (coord, globalCoord) = GetLocalAndGlobalPointCoordinate(angle, innerRadius);
            readInner[i] = ReadColorSensor(globalCoord, true);
        }

        GetHitAngles(readOuter);

        return (readOuter, readInner);
    }

    public bool ReadColorSensor(Vector3 globalCoord, bool draw=false)
    {
        Vector3 downCoord = globalCoord;
        downCoord.y -= 0.03f;

        RaycastHit hit;

        if (Physics.Raycast(globalCoord, Vector3.down, out hit, 0.03f, linesLayerMask))
        {
            if (draw) DebugUtils.DrawLineRuntime(globalCoord, downCoord, Color.red, Time.deltaTime); 
            return true;  
        }
        else
        {
            if (draw) DebugUtils.DrawLineRuntime(globalCoord, downCoord, Color.green, Time.deltaTime);
            return false;
        }
    }
}
