using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Classe che descrive un oggetto di tipo Free Form Deformation
/// </summary>

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FFD : MonoBehaviour
{
    // corrispondono numero piani in cui è suddiviso il cubo, di base l = z, m = y, n = x;
    [SerializeField]
    public int l = 2;
    [SerializeField]
    public int n = 2;
    [SerializeField]
    public int m = 2;

    [SerializeField]
    public Transform deformObject;

    Vector3[] originalVerticies;
    Vector3[] FFDTransformedVerticies;
    Dictionary<Vector3, int> mapVertex = new Dictionary<Vector3,int>();

    Vector3[] controlPointsOrignal;
    Vector3[] controlPoints;
    //List<Vector3> controlPoints = new List<Vector3>();

    Vector3 min, max;

    float[] FactorialCoefficentL;
    float[] FactorialCoefficentM;
    float[] FactorialCoefficentN;

    Mesh mesh;

    // Metodo chiamato quando si crea la griglia
    public void CreateFFDGrill () {
        mesh = GetComponent<MeshFilter>().mesh;
        mesh.Clear();
        controlPoints = null;
        controlPointsOrignal = null;
        mesh.name = "FFD";
        
        mapVertex.Clear();
        FFDTransformedVerticies = null;
        CreateVerticesGrid();
        RecalculateBounds();
        Debug.Log("Size" + mesh.bounds.size);
        //CreateVertices();
        //CreateTriangles();
        controlPointsOrignal = new Vector3[controlPoints.Length];
        for(int i=0; i< controlPoints.Length; i++) {
            controlPointsOrignal[i] = controlPoints[i];
        }
        
        if (deformObject != null) {
            Mesh deformMesh = deformObject.GetComponent<MeshFilter>().mesh;
            originalVerticies = new Vector3[deformMesh.vertexCount];
            for (int i=0; i< deformMesh.vertexCount; i++) {
                originalVerticies[i] = deformMesh.vertices[i];
            }
            TransformObjectVerticies();
        }
    }

    public void ResetControlPoints () {
        for (int i = 0; i < controlPoints.Length; i++)
        {
            controlPoints[i] = controlPointsOrignal[i];
        }

        mesh.vertices = controlPoints;
        mesh.RecalculateBounds();
        if (deformObject == null)
             deformObject = GameObject.Find("Cube").transform;
        Mesh deformedMesh = deformObject.GetComponent<MeshFilter>().mesh;
        deformedMesh.vertices = originalVerticies;
        
    }

    private void CreateVerticesGrid ()
    {
        //controlPoints = new Vector3[(l + 1) * (m + 1) * (n + 1)];
        
        int v=0;
        /* for (int i = 0; i <= l; i++) {
             for (int j = 0; j <= m; j++) {
                 for (int k = 0; k <=  n; k++) {
                     vecto
                     //mapVertex.Add(new Vector3(i, j, k), v); 
                     //controlPoints[v++] = new Vector3(i, j, k);
                 }
             }
         }*/
        for (float i = 0.0f; i < 1.0f; i += i/l) {
            for (float j = 0.0f; j < 1.0f; j += j/m) {
                for (float k = 0.0f; k < 1.0f; k += k/n)
                {
                    Vector3 vect = new Vector3( min.x + i*l, min.y + j*m, min.z + k*n);
                    mapVertex.Add(new Vector3(i, j, k), v);
                    controlPoints[v++] = vect;
                }
            }
        }
        mesh.vertices = controlPoints;
    }
    /*
    private void CreateVertices () {
        // Calcolo numero di vertici di un cubo (diviso in sottocubi di dimensioni di un'unità)
        int cornercontrolPoints = 8;
        int edgecontrolPoints = (l + m + n - 3) * 4;
        int facecontrolPoints = ((l - 1) * (m - 1) + (l - 1) * (n - 1) + (m - 1) * (n - 1)) * 2;
        controlPoints = new Vector3[cornercontrolPoints + edgecontrolPoints + facecontrolPoints];

        // Assegno valore ai vari vertici
        int v = 0; // Indice dei vettori

        // Calcolo vertici per ogni valore di y
        for (int y = 0; y <= m; y++)
        {
            // Calcolo vertici asse X da 0 - l
            // z
            // ^ 
            // └─> x
            // . . . .
            // .     .
            // .     .
            // x x x x
            //
            for (int x = 0; x <= l; x++) {
                mapVertex.Add( new Vector3(x, y, 0), v);
                controlPoints[v++] = new Vector3(x, y, 0);
            }

            // Calcolo vertici asse Z da 1 - n
            // z
            // ^ 
            // └─> x
            // . . . z
            // .     z
            // .     z
            // x x x x
            //
            for (int z = 1; z <= n; z++) {
                mapVertex.Add(new Vector3 ( l, y, z), v);
                controlPoints[v++] = new Vector3(l, y, z);
            }
            // Calcolo vertici asse X da l - 0
            // z
            // ^ 
            // └─> x
            // x x x z
            // .     z
            // .     z
            // x x x x
            //
            for (int x = l - 1; x >= 0; x--) {
                mapVertex.Add(new Vector3(x, y, n), v);
                controlPoints[v++] = new Vector3(x, y, n);
            }
            // Calcolo vertici asse Z da n - 0
            // z
            // ^ 
            // └─> x
            // x x x z
            // z     z
            // z     z
            // x x x x
            //
            for (int z = n - 1; z > 0; z--) {
                mapVertex.Add(new Vector3(0, y, z), v);
                controlPoints[v++] = new Vector3(0, y, z);
            }
        }

        // Calcolo vertici lati top e bottom
        for (int z = 1; z < n; z++)
        {
            for (int x = 1; x < l; x++)
            {
                mapVertex.Add(new Vector3(x, m, z), v);
                controlPoints[v++] = new Vector3(x, m, z);
            }
        }
        for (int z = 1; z < n; z++)
        {
            for (int x = 1; x < l; x++)
            {
                mapVertex.Add(new Vector3(x, 0, z), v);
                controlPoints[v++] = new Vector3(x, 0, z);
            }
        }

        mesh.vertices = controlPoints;
    }

    private void CreateTriangles ()
    {
        int quads = (l * m + l * n + m * n) * 2;
        int[] triangles = new int[quads * 6];

        int ring = (l + n) * 2; // Offset per la prossima riga == anello vertici

        int t = 0, v = 0; // Indici per i vertici ed i triangoli

        // Loop per la creazione dei quadrati sui lati del cubo ad eccezione di top e bottom
        for (int y = 0; y < m; y++, v++)
        {
            for (int q = 0; q < ring - 1; q++, v++)
                t = SetQuad(triangles, t, v, v + 1, v + ring, v + ring + 1);

            // L'ultimo quadrato deve essere calcolato a parte in modo tale che venga calcolato
            // per la riga corretta
            t = SetQuad(triangles, t, v, v - ring + 1, v + ring, v + 1);
        }

        t = CreateTopFace(triangles, t, ring);
        t = CreateBottomFace(triangles, t, ring);

        mesh.triangles = triangles;
    }

    private int CreateTopFace ( int[] triangles, int t, int ring ) {
        int v = ring * m;
        for (int x = 0; x < l - 1; x++, v++)
        {
            t = SetQuad(triangles, t, v, v + 1, v + ring - 1, v + ring);
        }
        t = SetQuad(triangles, t, v, v + 1, v + ring - 1, v + 2);

        // Da qui le cose sono più complicate, la prima riga della facciata è stata inserita
        // subito dopo che gli altri lati laterali erano stati inseriti
        // Per semplicità si tiene conto dei vertici laterali e dell'indice minimo inserito successivamente
        int vMin = ring * (m + 1) - 1;
        int vMid = vMin + 1;
        int vMax = v + 2;

        // Righe in mezzo ad eccezione dell'ultima
        for (int z = 1; z < n - 1; z++, vMin--, vMax++, vMid++)
        {
            t = SetQuad(triangles, t, vMin, vMid, vMin - 1, vMid + l - 1);

            for (int x = 1; x < l - 1; x++, vMid++)
                t = SetQuad(triangles, t, vMid, vMid + 1, vMid + l - 1, vMid + l);

            t = SetQuad(triangles, t, vMid, vMax, vMid + l - 1, vMax + 1);
        }

        int vTop = vMin - 2;
        t = SetQuad(triangles, t, vMin, vMid, vTop + 1, vTop);
        for (int x = 1; x < l - 1; x++, vTop--, vMid++)
            t = SetQuad(triangles, t, vMid, vMid + 1, vTop, vTop - 1);
        t = SetQuad(triangles, t, vMid, vTop - 2, vTop, vTop - 1);
        return t;
    }

    private int CreateBottomFace ( int[] triangles, int t, int ring ) {
        int v = 1;
        int vMid = controlPoints.Length - (l - 1) * (n - 1); // In modo tale da avere accesso al primo indice

        // Prima riga
        t = SetQuad(triangles, t, ring - 1, vMid, 0, 1);
        for (int x = 1; x < l - 1; x++, v++, vMid++)
            t = SetQuad(triangles, t, vMid, vMid + 1, v, v + 1);
        t = SetQuad(triangles, t, vMid, v + 2, v, v + 1);

        // Parte centrale
        int vMin = ring - 2;
        vMid -= l - 2;
        int vMax = v + 2;

        for (int z = 1; z < n - 1; z++, vMin--, vMid++, vMax++)
        {
            t = SetQuad(triangles, t, vMin, vMid + l - 1, vMin + 1, vMid);
            for (int x = 1; x < l - 1; x++, vMid++)
                t = SetQuad(triangles, t, vMid + l - 1, vMid + l, vMid, vMid + 1);
            t = SetQuad(triangles, t, vMid + l - 1, vMax + 1, vMid, vMax);
        }

        // Ultima riga
        int vTop = vMin - 1;
        t = SetQuad(triangles, t, vTop + 1, vTop, vTop + 2, vMid);
        for (int x = 1; x < l - 1; x++, vTop--, vMid++)
            t = SetQuad(triangles, t, vTop, vTop - 1, vMid, vMid + 1);
        t = SetQuad(triangles, t, vTop, vTop - 1, vMid, vTop - 2);

        return t;
    }
    */
    /**
     * Metodo che permette la creazione di un singolo quadrato formato da due triangoli
     * I numeri sui vertici rappresentano l'ordine di lettura dei vertici (antiorario per ogni triangolo)
     * 1T e 2T sono gli indicativi dei triangoli
     * 
     * 1/4 * * * * * * 5
     *   *  *          *
     *   *    *    2T  * 
     *   *      *      *
     *   *  1T    *    *
     *   *          *  *
     *   0 * * * * * * 2/3
     *
     * Come parametri riceve:
     *  -Indici dei rispettivi vertci (v00,v01,v10,v11)
     *  -i = indice di lettura dei triangoli, corrisponde all'indice '0' del disegno soprastante
     */
    private static int SetQuad ( int[] triangles, int i, int v00, int v10, int v01, int v11 )
    {
        triangles[i] = v00;
        triangles[i + 1] = triangles[i + 4] = v01;
        triangles[i + 2] = triangles[i + 3] = v10;
        triangles[i + 5] = v11;
        return i + 6; // Ritorno l'indice successivo dove inizia il quadrato
    }


    private void Reset ()
    {
        if (deformObject == null)
            deformObject = GameObject.Find("Cube").transform;
        CreateFFDGrill();
        CalculateFactorialCoefficent();
    }


    // Ricalcolo limiti mesh e aggiorno punto massimo e minimo dell'oggetto
    private void RecalculateBounds () {
        mesh.RecalculateBounds();
        min = mesh.bounds.min;
        max = mesh.bounds.max;
    }

    private void TransformObjectVerticies () {
        if (deformObject == null)
            deformObject = GameObject.Find("Cube").transform;

        Mesh deformMesh = deformObject.GetComponent<MeshFilter>().mesh;

        FFDTransformedVerticies = new Vector3[deformMesh.vertexCount];
        for (int i=0; i< deformMesh.vertexCount; i++) {
            Vector3 vertex =deformMesh.vertices[i];// transform.InverseTransformPoint(deformObject.TransformPoint(deformMesh.vertices[i]));
            float s = (vertex.x - min.x) / (max.x - min.x);
            float t = (vertex.y - min.y) / (max.y - min.y);
            float u = (vertex.z - min.z) / (max.z - min.z);
            FFDTransformedVerticies[i] = new Vector3(s, t, u);
        }
    }


    private void CalculateFactorialCoefficent () {
        FactorialCoefficentL = new float[l+1];
        FactorialCoefficentM = new float[m+1];
        FactorialCoefficentN = new float[n+1];
        
        uint numerator = Factorial.CalculateFactorial(Convert.ToUInt32(l));
        for (int x=0; x<=l; x++) {
            uint denominatorX = Factorial.CalculateFactorial(Convert.ToUInt32(x));
            uint denominatorDifference = Factorial.CalculateFactorial(Convert.ToUInt32(l-x));
            FactorialCoefficentL[x] = (float)numerator / (float)(denominatorX * denominatorDifference);
        }

        numerator = Factorial.CalculateFactorial(Convert.ToUInt32(m));
        for (int y = 0; y <= m; y++)
        {
            uint denominatorY = Factorial.CalculateFactorial(Convert.ToUInt32(y));
            uint denominatorDifference = Factorial.CalculateFactorial(Convert.ToUInt32(m-y));
            FactorialCoefficentM[y] = (float)numerator / (float)(denominatorY * denominatorDifference);
        }

        numerator = Factorial.CalculateFactorial(Convert.ToUInt32(n));
        for (int z = 0; z <= n; z++)
        {
            uint denominatorZ = Factorial.CalculateFactorial(Convert.ToUInt32(z));
            uint denominatorDifference = Factorial.CalculateFactorial(Convert.ToUInt32(n-z));
            FactorialCoefficentN[z] = (float)numerator / (float)(denominatorZ * denominatorDifference);
        }
    }


    public Vector3 GetControlPoint(int index ) {
        return controlPoints[index];
    }

    public void deformTargetObject () {
        Vector3[] verticies = new Vector3[originalVerticies.Length]; 
        for(int v=0; v< FFDTransformedVerticies.Length; v++) {
            //Vector3 point = deformObject.transform.TransformPoint(FFDTransformedVerticies[v]);
            // point = transform.InverseTransformPoint(point);
            //float u,s,t;
            //u = point.z / n;
            //s = point.x / l;
            //t = point.y / m;

            //X = X + binom(l, i) ∗ pow(1 − s, l − i) ∗ pow(s, i)∗
            //binom(m − 1, j) ∗ pow(1 − t, m − j) ∗ pow(t, j)∗
            //binom(n, k) ∗ pow(1 − u, n − k) ∗ pow(u, k) ∗ P ijk;
            Vector3 vec = FFDTransformedVerticies[v];
            Vector3 deformedPoint = new Vector3(0,0,0);
            for (int i = 0; i < l; i++) {
                for (int j = 0; j < m; j++) {
                    for (int k = 0; k < n; k++) {
                        Vector3 calcualateValue = controlPoints[mapVertex[new Vector3(i, j, k)]] *
                                         Mathf.Pow(vec.z, k) * Mathf.Pow(1 - vec.z, n - k) * FactorialCoefficentN[k] *
                                         Mathf.Pow(vec.y, j) * Mathf.Pow(1 - vec.y, m - j) * FactorialCoefficentM[j] *
                                         Mathf.Pow(vec.x, i) * Mathf.Pow(1 - vec.x, l - i) * FactorialCoefficentL[i];
                        deformedPoint += calcualateValue;
                        }
                }
            }
            //deformedPoint *= 3;
            //modifiedVerticies[v] += deformedPoint;

            //deformedPoint = transform.TransformPoint(deformedPoint);
            //deformedPoint = deformObject.InverseTransformPoint(deformedPoint);
            //FFDTransformedVerticies[v] += deformedPoint; 
            verticies[v] = deformedPoint;

        }

        Mesh deformedMesh = deformObject.GetComponent<MeshFilter>().mesh;
        deformedMesh.vertices = verticies;
    }


    public void SetControlPoint ( int index , Vector3 point)
    {
        controlPoints[index] = point;
        mesh.vertices = controlPoints;
        mesh.RecalculateBounds();
        Debug.Log(mesh.bounds.min);
        Debug.Log(mesh.bounds.max);

        deformTargetObject();
    }

    public int ControlPointCount {
        get{
            return controlPoints.Length;
        }
    }

    private void OnDrawGizmos ()
    {
        if (controlPoints == null)
        {
            return;
        }
        // Test disegno linee
        //Gizmos.color = Color.white;
        //int count =0;
        //for (int y = 0; y <= n; y++) {
            /*if (y == 0 || y == m)
            {
                if (y == 0) {
                    for (int i = 0; i < (l + n) * 2; i++)
                        Gizmos.DrawLine(controlPoints[count + i], controlPoints[count + i + (l + n) * 2 * m]);
                }
                Gizmos.DrawLine(controlPoints[count + 1], controlPoints[count + 2 + m + l]);
                Gizmos.DrawLine(controlPoints[count + 2], controlPoints[count + 1 + m + l]);
                Gizmos.DrawLine(controlPoints[count + 4], controlPoints[count + 2 + m + l*2]);
                Gizmos.DrawLine(controlPoints[count + 5], controlPoints[count + 1 + m + l*2]);
            }*/
            /*for (int x = 0; x < l; x++)
                Gizmos.DrawLine(transform.TransformPoint(controlPoints[count + x]), transform.TransformPoint(controlPoints[count + x + 1]));
            count += l;
            for (int x = 0; x < n; x++)
                Gizmos.DrawLine(transform.TransformPoint(controlPoints[count + x]), transform.TransformPoint(controlPoints[count + x + 1]));
            count += n;
            for (int x = 0; x < l; x++)
                Gizmos.DrawLine(transform.TransformPoint(controlPoints[count + x]), transform.TransformPoint(controlPoints[count + x + 1]));
            count += l;
            for (int x = 0; x < n-1; x++)
                Gizmos.DrawLine(transform.TransformPoint(controlPoints[count + x]), transform.TransformPoint(controlPoints[count + x + 1]));
            Gizmos.DrawLine(transform.TransformPoint(controlPoints[count + n-1]), transform.TransformPoint(controlPoints[count - 2*l - n]));
            count += n;*/
        //}
    }
}
