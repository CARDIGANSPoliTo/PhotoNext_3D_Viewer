using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangePivot : MonoBehaviour
{


    // Start is called before the first frame update
    void Start()
    {
        Vector3 pivot = transform.localPosition;
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.mesh;
        Bounds bound = mesh.bounds;
        Vector3 center = bound.center;
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] -= center;
            vertices[i] = Quaternion.Euler(0, 90, 90) * vertices[i];   
        }
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        Debug.Log(meshFilter.mesh.bounds);
        BoxCollider collider = gameObject.GetComponent<BoxCollider>();
        Vector3 newSize = collider.size;

        collider.center -= center;
        // New vector size(y,z,x)
        collider.size = new Vector3(newSize.z, newSize.x, newSize.y);
        
        Debug.Log(collider.bounds);
        
        //collider.bounds ;

        // Vector3 offset = bound.center *-1;
        // Vector3 position = new Vector3(offset.x / bound.x)

        //Debug.Log(meshFilter.mesh.bounds);
    }
}
