using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//[requirecomponent(typeof(meshfilter))]
//public class newbehaviourscript : monobehaviour {

//    void start () {
//        meshfilter filter = getcomponent(typeof (meshfilter)) as meshfilter;
//        if (filter != null) {
//            mesh mesh = filter.mesh;

//            vector3[] normals = mesh.normals;
//            for (int i = 0; i < normals.length; i++)
//                normals[i] = -normals[i];
//            mesh.normals = normals;

//            for (int m = 0; m < mesh.submeshcount; m++) {
//                int[] triangles = mesh.gettriangles(m);
//                for (int i = 0; i < triangles.length-2; i += 3) {
//                    int temp = triangles[i + 0];
//                    triangles[i + 0] = triangles[i + 1];
//                    triangles[i + 1] = temp;
//                }
//                mesh.settriangles(triangles, m);
//            }
//        }
//    }
//}

public class NewBehaviourScript : MonoBehaviour {
    [SerializeField]
    public GameObject gocorrect;
    [SerializeField]
    public GameObject goflip;

    struct DataVector {
        public Vector3 vectorNormal;
        public int countSum;
    }
    Dictionary<int,DataVector> listVertexNormal = new Dictionary<int, DataVector>();

    void Reset () {
        Mesh m1 = gocorrect.GetComponent<MeshFilter>().mesh;
        Mesh m2 = goflip.GetComponent<MeshFilter>().mesh;
    }


    // Start is called before the first frame update
    void Start () {
        Mesh m1 = gocorrect.GetComponent<MeshFilter>().mesh;
        Mesh m2 = goflip.GetComponent<MeshFilter>().mesh;


        for (int i = 0; i < m2.triangles.Length - 2; i += 3) {
            Vector3 ver1 = m2.vertices[m2.triangles[i]];
            Vector3 ver2 = m2.vertices[m2.triangles[i + 1]];
            Vector3 ver3 = m2.vertices[m2.triangles[i + 2]];

            Vector3 U1 = ver2 - ver1;
            Vector3 V1 = ver3 - ver1;
            Vector3 normalv1 = Vector3.Cross(U1, V1);
            if (!listVertexNormal.ContainsKey(m2.triangles[i])) {
                DataVector data;
                data.vectorNormal = normalv1;
                data.countSum = 1;
                listVertexNormal.Add(m2.triangles[i], data);
            }
            else {
                DataVector data = listVertexNormal[m2.triangles[i]];
                data.vectorNormal += normalv1;
                data.countSum++;
                listVertexNormal[m2.triangles[i]] = data;
            }

            if (!listVertexNormal.ContainsKey(m2.triangles[i + 1])) {
                DataVector data;
                data.vectorNormal = normalv1;
                data.countSum = 1;
                listVertexNormal.Add(m2.triangles[i + 1], data);
            }
            else {
                DataVector data = listVertexNormal[m2.triangles[i+1]];
                data.vectorNormal += normalv1;
                data.countSum++;
                listVertexNormal[m2.triangles[i + 1]] = data;
            }

            if (!listVertexNormal.ContainsKey(m2.triangles[i + 2])) {
                DataVector data;
                data.vectorNormal = normalv1;
                data.countSum = 1;
                listVertexNormal.Add(m2.triangles[i + 2], data);
            }
            else {
                DataVector data = listVertexNormal[m2.triangles[i+2]];
                data.vectorNormal += normalv1;
                data.countSum++;
                listVertexNormal[m2.triangles[i + 2]] = data;
            }
        }
        //Vector3[] norm = m2.normals;
        //int[] triangles = m2.GetTriangles(0);
        for (int i = 0; i < m2.triangles.Length - 2; i += 3) {
            Vector3 ver1 = m2.vertices[m2.triangles[i]];
            Vector3 ver2 = m2.vertices[m2.triangles[i+1]];
            Vector3 ver3 = m2.vertices[m2.triangles[i+2]];

            Vector3 U1 = ver2 - ver1;
            Vector3 V1 = ver3 - ver1;

            Vector3 normalv1 = Vector3.Cross(U1, V1);
            normalv1.Normalize();
            DataVector dv = listVertexNormal[m2.triangles[i]];
            DataVector dv2 = listVertexNormal[m2.triangles[i+1]];
            DataVector dv3 = listVertexNormal[m2.triangles[i+2]];

            Vector3 normalVector = dv.vectorNormal / dv.countSum;
            Vector3 normalVector2 = dv2.vectorNormal / dv2.countSum;
            Vector3 normalVector3 = dv3.vectorNormal / dv3.countSum;
            Vector3 sum = normalVector + normalVector2 + normalVector3;
            sum.Normalize();

            if (Vector3.Dot(normalv1, sum) > 0) Debug.Log("printstuff");
            else {
                Debug.Log("ËRROR");
                //norm[triangles[i]] = norm[triangles[i]] * -1;
                //norm[triangles[i+1]] = norm[triangles[i+1]] * -1;
                //norm[triangles[i+2]] = norm[triangles[i+2]] * -1;

                //int tmp = triangles[i];
                //triangles[i] = m2.triangles[i + 2];
                //triangles[i + 2] = tmp;
                //m2.normals[m2.triangles[i]] *= -1;
                //m2.normals[m2.triangles[i+1]] *= -1;
                //m2.normals[m2.triangles[i+2]] *= -1;
            }
        }
        //m2.normals = norm;
        //m2.SetTriangles(triangles, 0);
        Vector3[] norm = m2.normals;
        for (int i = 0; i < norm.Length; i++) {
            norm[i] = norm[i] * -1;
        }
        m2.normals = norm;

        //        for (int m = 0; m < mesh.submeshcount; m++) {
        //                int[] triangles = mesh.gettriangles(m);
        //                for (int i = 0; i < triangles.length-2; i += 3) {
        //                    int temp = triangles[i + 0];
        //                    triangles[i + 0] = triangles[i + 1];
        //                    triangles[i + 1] = temp;
        //                }
        //                mesh.settriangles(triangles, m);
        for (int m = 0; m < m2.subMeshCount; m++) {
            int[] triangles = m2.GetTriangles(m);
            for (int i = 0; i < triangles.Length - 2; i += 3) {
                int tmp = triangles[i];
                triangles[i] = triangles[i + 2];
                triangles[i + 2] = tmp;
            }
            m2.SetTriangles(triangles, m);
        }
        //m2.RecalculateNormals();
        //m2.RecalculateNormals();
        //goflip.GetComponent<MeshFilter>().mesh.RecalculateNormals();
        //Vector3[] listTriangles = new Vector3[m2.triangles.Length / 3];
        //int count = 0;
        //for (int i = 0; i < m2.triangles.Length - 2; i += 3, count++) {
        //    listTriangles[count][0] = m2.triangles[i];
        //    listTriangles[count][1] = m2.triangles[i + 1];
        //    listTriangles[count][2] = m2.triangles[i + 2];
        //}

        //Vector3[] test = RecalculateNormal.recalculateTriangles(listTriangles);
        //int[] newTriangles = new int[m2.triangles.Length];

        ////count = 0;
        ////for (int i = 0; i < m2.triangles.Length - 2; i += 3, count++) {
        ////    newTriangles[i] = (int)listTriangles[count][2]; 
        ////    newTriangles[i + 1] = (int)listTriangles[count][1];
        ////    newTriangles[i + 2] = (int)listTriangles[count][0];
        ////}

        ////m2.triangles = newTriangles;
        //Vector3[] newNormals = new Vector3[m2.normals.Length];
        //for (int i = 0; i < m1.normals.Length; i++)
        //    newNormals[i] = m2.normals[i] * -1;
        //    //for (int i = 0; i < m1.triangles.Length - 2; i += 3) {
        //    //    Vector3 U = m1.vertices[m1.triangles[i + 1]] - m1.vertices[m1.triangles[i]];
        //    //    Vector3 V = m1.vertices[m1.triangles[i + 2]] - m1.vertices[m1.triangles[i]];

        //    //    Vector3 normal = Vector3.Cross(U, V).normalized;
        //    //    newNormals[m1.triangles[i]] = normal;
        //    //    newNormals[m1.triangles[i + 1]] = normal;
        //    //    newNormals[m1.triangles[i + 2]] = normal;
        //    //}
        //m2.normals = newNormals;
        ////m2.RecalculateTangents();
        //m2.RecalculateNormals();
        //goflip.GetComponent<MeshFilter>().mesh = m2;
        //Recalculate normal for test
        //Vector3[] newNormal = new Vector3[m2.vertices.Length];
        //for(int i=0; i< newNormal.Length; i++) {

        //}

    }


    private void OnDrawGizmos () {
        Mesh m2 = goflip.GetComponent<MeshFilter>().mesh;

        for (int i = 0; i < m2.vertices.Length; i++) {
            var vert = m2.vertices[i];
            var normal = m2.normals[i]/100;
            Gizmos.DrawSphere(vert, 0.001f);
            Gizmos.DrawLine(vert, vert + normal);
        }
    }
}

public static class RecalculateNormal {
    public static Vector3[] recalculateTriangles(Vector3[] original_triangles) {
        int triangles_to_exam = original_triangles.Length;
        List<Vector3> triangle_corrected = new List<Vector3>();
        for(int i=0; i<triangles_to_exam; i++) {
            Vector3 triangle = original_triangles[i];
            List<Vector3> shared_edges = original_triangles.Where(e => {
                int commond_edge = 0;
                for (int j = 0; j < 3; j++)
                    for (int k = 0; k < 3; k++)
                        commond_edge += (triangle[j] == e[k]) ? 1 : 0;
                return commond_edge > 0 ? true : false;
            }).ToList();

            if(shared_edges.Count > 0) {
                Vector3 triangle_c = getCorrectOrientation(triangle, shared_edges);
                triangle_corrected.Add(triangle_c);
            }

            if (i == 0) {
                triangle_corrected.Add(triangle);
            }
                
        }
        
        return triangle_corrected.ToArray();
    }

    static bool ExistsWindingConflict (Vector3 tris1, Vector3 tris2) {
        for(int i = 0; i < 3; i++) {
            int i_1 = (i + 1) % 3;
            for (int j = 0; j < 3; j++) {
                int j_1 = (j + 1) % 3;

                if (tris1[i] == tris2[j] && tris1[i_1] == tris2[j_1])
                    return true;     
            }
        }
        return false;
    }


    static Vector3 FlipTriangle(Vector3 v) {
        float tmp = v[1];
        v[1] = v[2];
        v[2] = tmp;
        return v;
    }

    static Vector3 getCorrectOrientation (Vector3 tris, List<Vector3> subs) {
        foreach(Vector3 t in subs) {
            if(ExistsWindingConflict(tris, t)) {
                return FlipTriangle(tris);
            }
        }
        return tris;
    }

    

}
