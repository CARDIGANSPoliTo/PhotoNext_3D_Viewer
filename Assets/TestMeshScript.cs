using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Material))]
[RequireComponent(typeof(MeshCollider))]
public class TestMeshScript : MonoBehaviour
{
    [SerializeField, Range(0,10)]
    public float Amplitude = 0.0f;

    [SerializeField, Range(-90, 90)]
    public float Angle = 0.0f;

    [SerializeField, Range(0,10)]
    public float Offset = 0.0f;

    float oldOffset, oldAmp, oldAngle;

    MeshFilter meshF;
    Bounds bound;
    Mesh mesh;
    Vector3[] verticesOriginal;

    Vector3 mPrevPos = Vector3.zero;
    Vector3 mPosDelta = Vector3.zero;

    Vector3[] updateVector;
    volatile bool end = false;

    public void initData() {
        // Creation of the final mesh
        //MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        //List<CombineInstance> combineInstances = new List<CombineInstance>();
        //for(int i=0; i<meshFilters.Length; i++) {
        //    if (!meshFilters[i].gameObject.name.Equals(gameObject.name))
        //    {
        //        meshFilters[i].mesh.normals = new Vector3[meshFilters[i].mesh.vertices.Length];
        //        //meshFilters[i].mesh.RecalculateNormals();
        //        CombineInstance ci = new CombineInstance();
        //        ci.mesh = meshFilters[i].mesh;
        //        ci.transform = meshFilters[i].transform.localToWorldMatrix;
        //        combineInstances.Add(ci);
        //        meshFilters[i].gameObject.SetActive(false);
        //    }
        //}
        //transform.GetComponent<MeshFilter>().mesh = new Mesh();
        //transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combineInstances.ToArray());
        //transform.GetComponent<MeshFilter>().mesh.RecalculateNormals();
        //transform.GetComponent<MeshCollider>().sharedMesh = transform.GetComponent<MeshFilter>().mesh;

        meshF = transform.GetComponent<MeshFilter>();
        mesh = meshF.mesh;
        //MeshHelper.Subdivide(mesh, 8);
        //meshF.mesh = mesh;
        //meshF.mesh = mesh;
        bound = mesh.bounds;
        verticesOriginal = mesh.vertices;
        updateVector = mesh.vertices;
        //mesh.vertices = vertices;
        //mesh.RecalculateNormals();
        //mesh.RecalculateBounds();
        //bound = mesh.bounds;

        //Debug.Log(bound.extents.x + " " + bound.extents.y + " " + bound.extents.z);
        //oldOffset = Offset;
        oldOffset = Offset;
        oldAmp = Amplitude;
        oldAngle = Angle;
        //Task.Run(() => CalculateBending());
    }

    
    void CalculateBending(){
        while (!end) {
            Task.Delay(1000);
            if (GameManager.instance.statusGame == Status.MONITORING) {
                Vector3[] vertices = new Vector3[verticesOriginal.Length];

                //if (GameManager.instance.changeProperty) {
                    for (int i = 0; i < vertices.Length; i++) {
                        vertices[i] = verticesOriginal[i];
                        float normalizedValue = vertices[i].z / bound.extents.z;
                        Debug.Log("Normalized value" + normalizedValue);
                        //float ampl = (float)((GameManager.instance.CurrentSensorWavelength[0] - GameManager.instance.SensorsFromNetwork[0].WavelenghtIdle) / 40f) * (0.26f);
                        //Debug.Log(ampl);
                        vertices[i].y += normalizedValue * normalizedValue * (-0.25f); //Mathf.Sin(Offset + (3.14f/2 * normalizedValue )) * Amplitude;
                    }

                    lock (updateVector) {
                        updateVector = vertices;
                    }
                //}
            }
        }
    }
    
    //Rou
    IEnumerator UpdateWingBending() {
        while (true) {
            //if (oldOffset != Offset || oldAmp != Amplitude || oldAngle != Angle) {
            if(GameManager.instance.statusGame == Status.MONITORING) {
                //    Vector3[] vertices = new Vector3[verticesOriginal.Length];
                //    //if (GameManager.instance.changeProperty) {
                //    for (int i = 0; i < vertices.Length; i++) {
                //        vertices[i] = verticesOriginal[i];
                //        float normalizedValue = vertices[i].z / bound.extents.z;
                //        //Debug.Log("Normalized value" + normalizedValue);
                //        float ampl = (float)((GameManager.instance.CurrentSensorWavelength[0] - GameManager.instance.SensorsFromNetwork[0].WavelenghtIdle) / 40f) * (0.26f);
                //        Debug.Log(ampl);
                //        vertices[i].y += normalizedValue * normalizedValue * (-0.25f); //Mathf.Sin(Offset + (3.14f/2 * normalizedValue )) * Amplitude;
                //    }
                //    mesh.vertices = vertices;
                //    mesh.RecalculateNormals();
                //    mesh.RecalculateBounds();
                //}
                lock (updateVector) {
                    mesh.vertices = updateVector;
                    mesh.RecalculateNormals();
                    mesh.RecalculateBounds();
                }
            yield return new WaitForSeconds(1.5f);
            //oldOffset = Offset;
            //oldAmp = Amplitude;
            //oldAngle = Angle;
            //}
            }
        }
    }

    private void OnApplicationQuit ()
    {
        end = true;
    }

    float timer = 0.0f;
    public void Update() {
        if (timer > 0.5) {
            timer = 0.0f;
            if (GameManager.instance.statusGame == Status.MONITORING) {
                //    Vector3[] vertices = new Vector3[verticesOriginal.Length];
                //    //if (GameManager.instance.changeProperty) {
                //    for (int i = 0; i < vertices.Length; i++) {
                //        vertices[i] = verticesOriginal[i];
                //        float normalizedValue = vertices[i].z / bound.extents.z;
                //        //Debug.Log("Normalized value" + normalizedValue);
                //        float ampl = (float)((GameManager.instance.CurrentSensorWavelength[0] - GameManager.instance.SensorsFromNetwork[0].WavelenghtIdle) / 40f) * (0.26f);
                //        Debug.Log(ampl);
                //        vertices[i].y += normalizedValue * normalizedValue * (-0.25f); //Mathf.Sin(Offset + (3.14f/2 * normalizedValue )) * Amplitude;
                //    }
                //    mesh.vertices = vertices;
                //    mesh.RecalculateNormals();
                //    mesh.RecalculateBounds();
                //}
                lock (updateVector) {
                    GetComponent<MeshFilter>().mesh.SetVertices(updateVector.ToList());
                    GetComponent<MeshFilter>().mesh.RecalculateNormals();
                    GetComponent<MeshFilter>().mesh.RecalculateBounds();
                }
                //Debug.Log(timer);
            }
        }
        //Debug.Log(timer);
        timer += Time.deltaTime;
            //if (name.Equals(" 1")) {
            //if (oldOffset != Offset || oldAmp != Amplitude || oldAngle != Angle) {
            //    Vector3[] vertices = new Vector3[verticesOriginal.Length];
            //    //if (GameManager.instance.changeProperty) {
            //        for (int i = 0; i < vertices.Length; i++) {
            //            vertices[i] = verticesOriginal[i];
            //            float normalizedValue = vertices[i].z / bound.extents.z;
            //            //Debug.Log("Normalized value" + normalizedValue);
            //            //float ampl = (float) ((GameManager.instance.CurrentSensorWavelength[0]- GameManager.instance.SensorsFromNetwork[0].WavelenghtIdle) / 40f) * (0.26f);
            //            //Debug.Log(ampl);
            //            vertices[i].y += normalizedValue * normalizedValue * (-Amplitude); //Mathf.Sin(Offset + (3.14f/2 * normalizedValue )) * Amplitude;
            //        }
            //        mesh.vertices = vertices;
            //        mesh.RecalculateNormals();
            //        mesh.RecalculateBounds();

            //        oldOffset = Offset;
            //        oldAmp = Amplitude;
            //        oldAngle = Angle;
            //    //}
            //}
            //}

            //if (Input.GetMouseButtonDown(0))
            //{
            //    mPosDelta = Input.mousePosition - mPrevPos;
            //    Debug.Log(Vector3.Dot(transform.right, Camera.main.transform.right));
            //    if (Vector3.Dot(transform.up, Vector3.up) >= 0)
            //        transform.Rotate(transform.up, -Vector3.Dot(mPosDelta, Camera.main.transform.right), Space.World);
            //    else
            //        transform.Rotate(transform.up, Vector3.Dot(mPosDelta, Camera.main.transform.right), Space.World);
            //    transform.Rotate(Camera.main.transform.right, Vector3.Dot(mPosDelta, Camera.main.transform.up), Space.World);
            //    if (name.Equals(" 1"))
            //    {
            //        if (oldOffset != Offset || oldAmp != Amplitude || oldAngle != Angle)
            //        {
            //            Vector3[] vertices = new Vector3[verticesOriginal.Length];
            //            for (int i = 0; i < vertices.Length; i++)
            //            {
            //                vertices[i] = verticesOriginal[i];
            //                float normalizedValue = vertices[i].z / bound.extents.z;
            //                //Debug.Log("Normalized value" + normalizedValue);
            //                vertices[i].y += normalizedValue * normalizedValue * Amplitude; //Mathf.Sin(Offset + (3.14f/2 * normalizedValue )) * Amplitude;
            //            }

            //            mesh.vertices = vertices;
            //            mesh.RecalculateNormals();
            //            mesh.RecalculateBounds();

            //            oldOffset = Offset;
            //            oldAmp = Amplitude;
            //            oldAngle = Angle;
            //        }
            //    }
            //    mPrevPos = Input.mousePosition;
            //}

        }


}
