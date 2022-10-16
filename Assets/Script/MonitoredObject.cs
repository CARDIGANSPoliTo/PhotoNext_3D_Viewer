using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Class attached to the GameObject to monitor. It has as children Sensor's object.
/// </summary>
[RequireComponent(typeof(MeshCollider), typeof(MeshRenderer), typeof(MeshFilter))]
[RequireComponent(typeof(Material))]
public class MonitoredObject : MonoBehaviour {
    // Prefab sensor's object
    //------------------------------------------------
    public Transform  prefabSphere = null;
    public Transform  prefabCube   = null;
    public GameObject panelPrefab  = null;
    
    // GUI's panel's object
    //------------------------------------------------
    public GameObject generalPanel;
    public GameObject infoPanel;
    
    // Material's reference
    //------------------------------------------------
    public Material material;

    // Sensor utility variables
    //------------------------------------------------
    Dictionary<string,GameObject> sensors = new Dictionary<string, GameObject>();
    bool selected = false;
    bool showAll  = false;

    GameObject     selectedSensor = null;
    SphereCollider colliderSphere = null;
    BoxCollider    colliderBox    = null;
    GameObject     newLoadedModel = null;
    
    // Object properties variables
    //------------------------------------------------
    Vector3    currentPosition;
    Vector3    currentScale;
    Quaternion currentRotation;

    Vector3 rotationVector = Vector3.zero;
    Vector3 mPrevPos       = Vector3.zero;
    Vector3 mPosDelta      = Vector3.zero;

    // Mantain data for the point network data
    Vector3 originalCenter = Vector3.zero;

    float maxDimension = 1.0f;
    float scaleFloat = 1.0f;

    UInt64 ts = 0;
    
    const string pattern = @"(sensor)[0-9]+(_)[0-9]";
    

    void Awake () {
        // Get all the object's meshes
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

        // If at least one exists, merge them
        if (meshFilters.Where(g => !g.gameObject.name.Equals(gameObject.name)).ToArray().Length <= 0) {
            GameManager.instance.SetModelExist(false);
            return;
        }
        else
            GameManager.instance.SetModelExist(true);

        List<CombineInstance> combine = new List<CombineInstance>();
        // Create an unique mesh based on the children's mesh
        for (uint i = 0; i < meshFilters.Length; i++) {
            Match result = Regex.Match(meshFilters[i].gameObject.name.ToLower(), pattern);

            // Merge only the meshes that don't represent sensors
            if (!meshFilters[i].gameObject.name.Equals(name) && !result.Success) {
                for (int j = 0; j < meshFilters[i].mesh.subMeshCount; j++) {
                    CombineInstance ci = new CombineInstance();
                    ci.mesh = meshFilters[i].mesh;
                    ci.subMeshIndex = j;
                    ci.transform = meshFilters[i].transform.localToWorldMatrix;
                    combine.Add(ci);
                }
                meshFilters[i].gameObject.SetActive(false);
            }
        }

        // Combine the meshes
        CombineMeshes(combine, gameObject);
        Vector3? center = CenterMesh(gameObject);
        if (center == null) {
            GameManager.instance.SetErrorMessage("Problem when importing the 3D model");
            CancelImport();
            return;
        }
        originalCenter = center.Value;

        // Normalize the meshes
        float? dimension = NormalizeMesh(gameObject);
        if (dimension == null) {
            GameManager.instance.SetErrorMessage("Problem when importing the 3D model");
            CancelImport();
            return;
        }
        maxDimension = dimension.Value;

        // Generate the sensors as separate object in the hierarchy
        List<MeshFilter> listSensor = meshFilters.Where(s => {
            Match result = Regex.Match(s.name.ToLower(), pattern);
            return result.Success;
        }).ToList();

        listSensor.ForEach(e => {
            GameObject sensor = Instantiate(prefabSphere, new Vector3(0, 0, 0), Quaternion.identity).gameObject;
            sensor.name = e.name;

            string name = sensor.name;
            name = name.ToLower().Replace("sensor", "");
            string[] pieces = name.Split('_');
            int index = Convert.ToInt32(pieces[1]) * 16 + Convert.ToInt32(pieces[0]);

            e.gameObject.transform.position -= center.Value - e.GetComponent<MeshFilter>().mesh.bounds.center;
            e.gameObject.transform.position = 1.5f * e.gameObject.transform.position / maxDimension;
            sensor.transform.position = e.gameObject.transform.position;
            sensor.transform.rotation = e.gameObject.transform.rotation;
            Destroy(e.gameObject);
            sensor.transform.parent = transform;
            sensors.Add(sensor.name, sensor);

            GameObject display = Instantiate(panelPrefab);
            display.name = sensor.name;
            display.transform.parent = generalPanel.transform;

            Sensor s = new Sensor(Convert.ToInt32(pieces[0]), 0.0f, 0.0f, true); //GameManager.instance.defaultVarTemp
            s.Channel = Convert.ToInt32(pieces[1]);
            s.Position = sensor.transform.position;

            GameManager.instance.UpdateSensorInfo(s);
        });

        // Update the mesh collider
        MeshCollider collider = GetComponent<MeshCollider>();
        collider.sharedMesh = gameObject.GetComponent<MeshFilter>().mesh;
    }

    void Start () {
        currentPosition = transform.localPosition;
        currentRotation = transform.localRotation;
        currentScale = transform.localScale;
    }
    
    void Update ()
    {
        switch (GameManager.instance.statusGame) 
        {
            case Status.MONITORING:
                #region MONITORNG_CASE
                // Update rotation object
                if (Input.GetMouseButtonDown(0)) {
                    mPosDelta = Input.mousePosition - mPrevPos;
                    if (Vector3.Dot(transform.up, Vector3.up) >= 0)
                        transform.Rotate(transform.up, -Vector3.Dot(mPosDelta, Camera.main.transform.right), Space.World);
                    else
                        transform.Rotate(transform.up, Vector3.Dot(mPosDelta, Camera.main.transform.right), Space.World);
                    transform.Rotate(Camera.main.transform.right, Vector3.Dot(mPosDelta, Camera.main.transform.up), Space.World);
                }
                mPrevPos = Input.mousePosition;

                // Update postion panel if near sensor
                //if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity)) {
                //    if(!showAll)
                //        ShowText(hit.point, hit.collider.name);
                //}

                //Update sensor' properies on material
                //if (GameManager.instance.changeProperty)
                //{
                //Vector4[] newProperties = GameManager.instance.SensorPropertyNew;// instance.GetNewSensorProperty();
                //    material.SetVectorArray("_Properties", newProperties);
                    //if (GameManager.instance.measurementType == Measurement.DISPLACEMENT)
                    //{
                    //    List<KeyValuePair<UInt64, float>> wav = GameManager.instance.GetCurrentSensorWavelength();
                    //    UInt64 tm = GameManager.instance.GetCurrentSensorWavelength().Where(e => e.Value != 0.0f).First().Key;

                    //    if ((tm - ts) > 0)
                    //    {
                    //        ts = tm;
                    //        foreach (KeyValuePair<string, GameObject> s in sensors)
                    //        {
                    //            string name = s.Key;
                    //            name = name.Replace("Sensor", "");
                    //            string[] pieces = name.Split('_');
                    //            int index = Convert.ToInt32(pieces[1]) * 16 + Convert.ToInt32(pieces[0]);
                    //            Sensor sens = GameManager.instance.GetSensorInfo(Convert.ToInt32(pieces[1]), Convert.ToInt32(pieces[0])).Value;
                    //            if ((sens.WavelenghtIdle - wav[index].Value) > 0)
                    //            {
                    //                s.Value.GetComponent<MeshRenderer>().material.color = Color.cyan;
                    //            }
                    //            else
                    //            {
                    //                s.Value.GetComponent<MeshRenderer>().material.color = Color.magenta;
                    //            }
                    //        }
                    //    }
                    //}
                    //GameManager.instance.changeProperty = false;

                //}
                //if (showAll)
                //    ShowAll();
                #endregion
                break;

            case Status.CHANGEPOS:
                #region CHANGEPOS_CASE
                // Update rotation object
                if (Input.GetMouseButtonDown(0)) {
                    mPosDelta = Input.mousePosition - mPrevPos;
                    if (Vector3.Dot(transform.up, Vector3.up) >= 0)
                        transform.Rotate(transform.up, -Vector3.Dot(mPosDelta, Camera.main.transform.right), Space.World);
                    else
                        transform.Rotate(transform.up, Vector3.Dot(mPosDelta, Camera.main.transform.right), Space.World);
                    transform.Rotate(Camera.main.transform.right, Vector3.Dot(mPosDelta, Camera.main.transform.up), Space.World);
                }
                mPrevPos = Input.mousePosition;

                // Update info panel for sensor and change position of a selected sensor
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity)) {
                    if(!showAll)
                    ShowText(hit.point, hit.collider.name);

                    // End change sensor's position 
                    if (Input.GetMouseButtonDown(0) && selected) {
                        selected = false;
                        Component halo = selectedSensor.GetComponent("Halo");
                        halo.GetType().GetProperty("enabled").SetValue(halo, false, null);

                        selectedSensor = null;
                        if (GameManager.instance.measurementType == Measurement.TEMPERATURE) {
                            colliderSphere.enabled = true;
                            colliderSphere = null;
                        }
                        else {
                            colliderBox.enabled = true;
                            colliderBox = null;
                        }
                        //value = 0.0f;
                    }
                    
                    // Select sensor to move
                    if (Input.GetMouseButtonDown(0) && hit.transform.name.Contains("Sensor")) {
                        if (!selected) {
                            selected = true;
                            selectedSensor = hit.transform.gameObject;
                            if (GameManager.instance.measurementType == Measurement.TEMPERATURE) {
                                colliderSphere = selectedSensor.GetComponent<SphereCollider>();
                                colliderSphere.enabled = false;
                            }
                            else {
                                colliderBox = selectedSensor.GetComponent<BoxCollider>();
                                colliderBox.enabled = false;
                            }
                            Component halo = selectedSensor.GetComponent("Halo");
                            halo.GetType().GetProperty("enabled").SetValue(halo, true, null);
                        }
                    }

                    if ((Input.GetMouseButtonDown(1) || Input.GetMouseButton(1)) && hit.transform.name.Contains("Sensor")) {
                        selectedSensor = hit.transform.gameObject;
                        Vector3 pivot = selectedSensor.transform.position;

                        if (GameManager.instance.measurementType == Measurement.TEMPERATURE) {
                            colliderSphere = selectedSensor.GetComponent<SphereCollider>();
                            colliderSphere.enabled = false;
                        }
                        else {
                            colliderBox = selectedSensor.GetComponent<BoxCollider>();
                            colliderBox.enabled = false;
                        }

                        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, Mathf.Infinity)) {
                            MeshCollider meshf = GetComponent<MeshCollider>();
                            Vector3 normal = meshf.sharedMesh.normals[meshf.sharedMesh.triangles[hit.triangleIndex*3]];
                            
                            selectedSensor.transform.RotateAround(pivot, transform.rotation * normal, 1);
                            selectedSensor = null;
                        }

                        if (GameManager.instance.measurementType == Measurement.TEMPERATURE) {
                            colliderSphere.enabled = true;
                            colliderSphere = null;
                        }
                        else {
                            colliderBox.enabled = true;
                            colliderBox = null;
                        }
                    }

                    // Update position
                    if (selected) {
                        if(!showAll)
                            infoPanel.GetComponent<RectTransform>().position = new Vector3(Input.mousePosition.x, Input.mousePosition.y + 40.0f, 0.0f);
                        else
                            GameObject.Find("/Canvas" + generalPanel.name+"/" + selectedSensor.name).GetComponent<RectTransform>().position = new Vector3(Input.mousePosition.x, Input.mousePosition.y + 40.0f, 0.0f);
                        selectedSensor.transform.position = hit.point;
                        
                        selectedSensor.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    }
                   
                }
                #endregion
                break;
                default:
                    if (showAll) 
                    {
                        showAll = false;
                        HideAll();
                    }
                break;
        }
        
        //if (showAll)
        //    ShowAll();

        // Update position of the sensor on the material if the main object was moved
        if (transform.localPosition != currentPosition || transform.localScale != currentScale || transform.localRotation != currentRotation) 
        {
            Vector4[] positions = new Vector4[64];
            Vector4[] properties = new Vector4[64];
            List<GameObject> sensorList = sensors.Values.ToList();
            foreach (KeyValuePair<string, GameObject> g in sensors) 
            {
                if (g.Value.activeSelf) 
                {
                    string name =g.Key;
                    name = name.Replace("Sensor", "");
                    string[] pieces = name.Split('_');
                    int index = Convert.ToInt32(pieces[1]) *16 + Convert.ToInt32(pieces[0]);
                    positions[index] = g.Value.transform.position;
                    if (GameManager.instance.statusGame == Status.CHANGEPOS)
                        properties[index] = GameManager.instance.defaultProperties;
                }
            }

            material.SetVectorArray("_Points", positions);
            if (GameManager.instance.statusGame == Status.CHANGEPOS)
                material.SetVectorArray("_Properties", properties);

            currentPosition = transform.localPosition;
            currentRotation = transform.localRotation;
            currentScale = transform.localScale;
            if (showAll)
                ShowAll();
        }
    }

    public void UpdateShader ( Vector4[] newProperties, List<KeyValuePair<UInt64, float>> wav ) {
        // Update postion panel if near sensor
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity)) {
            if (!showAll)
                ShowText(hit.point, hit.collider.name,wav,newProperties);
        }
        // Update sensor' properies on material
        material.SetVectorArray("_Properties", newProperties);
        Debug.Log("Update data - " + wav[0].Value + " " + wav[0].Key);
            if (GameManager.instance.measurementType == Measurement.DISPLACEMENT) {
                UInt64 tm = wav.Where( e => e.Value != 0.0f).First().Key;
                //Debug.Log((long)tm / 1000 + " " + DateTimeOffset.Now);
                //Debug.Log((DateTimeOffset.FromUnixTimeMilliseconds((long)tm/1000) - DateTimeOffset.Now).TotalMilliseconds);
                
                if ((tm - ts) > 0) {
                    ts = tm;
                    foreach (KeyValuePair<string, GameObject> s in sensors) {
                        string name = s.Key;
                        name = name.Replace("Sensor", "");
                        string[] pieces = name.Split('_');
                        int index = Convert.ToInt32(pieces[1]) * 16 + Convert.ToInt32(pieces[0]);
                        Sensor sens = GameManager.instance.GetSensorInfo(Convert.ToInt32(pieces[1]), Convert.ToInt32(pieces[0])).Value;
                        if ((sens.WavelenghtIdle - wav[index].Value) > 0) {
                            s.Value.GetComponent<MeshRenderer>().material.color = Color.cyan;
                        }
                        else {
                            s.Value.GetComponent<MeshRenderer>().material.color = Color.magenta;
                        }
                    }
                }
            }

        if (showAll)
            ShowAll(wav);
    }

    void OnMouseDrag () {
        if (GameManager.instance.statusGame != Status.MENU) {
            mPosDelta = Input.mousePosition - mPrevPos;
            if (Vector3.Dot(transform.up, Vector3.up) >= 0)
                transform.Rotate(transform.up, -Vector3.Dot(mPosDelta, Camera.main.transform.right), Space.World);
            else
                transform.Rotate(transform.up, Vector3.Dot(mPosDelta, Camera.main.transform.right), Space.World);
            transform.Rotate(Camera.main.transform.right, Vector3.Dot(mPosDelta, Camera.main.transform.up), Space.World);
        }
    }
    
    void OnMouseExit () {
        HideText();
    }

    #region METHODS_IMPORT_MONITORED_OBJECT

    /// <summary>
    /// Start import new Monitored object model (only .obj format)
    /// </summary>
    /// <param name="sourceObject">New GameObject model on the scene hierarchy</param>
    public void OpenNewMonitoredObject ( GameObject sourceObject ) {
        // Check if another import operation is executed at the moment
        if (newLoadedModel != null) return;

        // In case there is no operation at the moment, clear the current object of
        // some previous transformation and disable the mesh renderer component
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        GetComponent<MeshRenderer>().enabled = false;

        // Clear all the sensor information
        sensors.Values.ToList().ForEach(e => Destroy(e));
        
        sensors.Clear();
        newLoadedModel = sourceObject;

        // Combine all the meshes in a single one
        MeshFilter[] meshFilters = newLoadedModel.GetComponentsInChildren<MeshFilter>();
        List<CombineInstance> combine = new List<CombineInstance>();

        // Create an unique mesh based on the children's mesh
        for (uint i = 0; i < meshFilters.Length; i++) {
            Match result = Regex.Match(meshFilters[i].gameObject.name.ToLower(), pattern);
            if (!meshFilters[i].gameObject.name.Equals(name) && !result.Success) {
                for (int j = 0; j < meshFilters[i].mesh.subMeshCount; j++) {
                    CombineInstance ci = new CombineInstance();
                    ci.mesh = meshFilters[i].mesh;
                    ci.subMeshIndex = j;
                    ci.transform = meshFilters[i].transform.localToWorldMatrix;
                    combine.Add(ci);
                }
                meshFilters[i].gameObject.SetActive(false);
            }
        }

        // Combine all the meshes
        CombineMeshes(combine, newLoadedModel);

        // Center the object to the origin
        Vector3? center = CenterMesh(newLoadedModel);
        if (center == null) {
            GameManager.instance.SetErrorMessage("Problem when importing the 3D model");
            CancelImport();
            return;
        }

        // Save the center of the main meshes
        originalCenter = center.Value;

        // Normalize the mesh and save the dimension in wich it was normalized
        float? dimension = NormalizeMesh(newLoadedModel);
        if (dimension == null) {
            GameManager.instance.SetErrorMessage("Problem when importing the 3D model");
            CancelImport();
            return;
        }
        maxDimension = dimension.Value;

        // Check if more than one sensors exists
        List<MeshFilter> listSensor = meshFilters.Where(s => {
            Match result = Regex.Match(s.name.ToLower(), pattern);
            return result.Success;
        }).ToList();

        for (int i = 0; i < listSensor.Count; i++) {
            combine.Clear();
            Vector3 firstVertex = listSensor[i].mesh.vertices[0];
            for (int j = 0; j < listSensor[i].mesh.subMeshCount; j++) {
                CombineInstance ci = new CombineInstance();
                ci.mesh = listSensor[i].mesh;
                ci.subMeshIndex = j;
                ci.transform = listSensor[i].transform.localToWorldMatrix;
                combine.Add(ci);
            }
            // Combine each sensor in only one object
            GameObject go = new GameObject();
            go.name = listSensor[i].name;
            CombineMeshes(combine, go);

            Vector3 firstCombinedVertex = go.GetComponent<MeshFilter>().mesh.vertices[0];

            // Center it respect of its own center
            Vector3? v = CenterMesh(go);
            if (v == null) {
                GameManager.instance.SetErrorMessage("Problem when importing the 3D model");
                CancelImport();
                return;
            }

            // Move it and normalize it based on the center and dimension of the main mesh
            go.transform.position = 1.5f * ((v.Value - originalCenter) / maxDimension);

            float newX, newY, newZ;
            int v1 = (int) firstVertex.x;
            int v2 = (int)firstCombinedVertex.x;
            newX   = (v1 ^ v2) >= 0 ? go.transform.position.x : -go.transform.position.x;

            v1     = (int) firstVertex.y;
            v2     = (int) firstCombinedVertex.y;
            newY   = (v1 ^ v2) >= 0 ? go.transform.position.y : -go.transform.position.y;

            v1     = (int) firstVertex.z;
            v2     = (int) firstCombinedVertex.z;
            newZ   = (v1 ^ v2) >= 0 ? go.transform.position.z : -go.transform.position.z;

            go.transform.position = new Vector3(newX,newY,newZ);

            try {
                ReduceSizeMesh(go, maxDimension);
            }
            catch (Exception) {
                GameManager.instance.SetErrorMessage("Problem when importing the 3D model");
                CancelImport();
                return;
            }

            // Set local scale and destroy previous object
            go.transform.localScale = Vector3.one;

            Destroy(listSensor[i].gameObject);
            go.transform.parent = newLoadedModel.transform;
            go.gameObject.SetActive(true);
        }
        newLoadedModel.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// Update rotation of the object. This is usefull for the movement in the monitoring and configuration phase
    /// </summary>
    /// <param name="field">Input filed updated</param>
    public void UpdateRotation ( TMP_InputField field ) {
        // In case there is no import operation and the text is not a number, return
        if (newLoadedModel == null) return;
        if (field.text.Equals("")) return;

        // Update model and global variable data
        if (field.name.Contains("X")) {
            rotationVector.x = Convert.ToSingle(field.text);
            newLoadedModel.transform.rotation = Quaternion.Euler(rotationVector);
        }
        if (field.name.Contains("Y")) {
            rotationVector.y = Convert.ToSingle(field.text);
            newLoadedModel.transform.rotation = Quaternion.Euler(rotationVector);
        }
        if (field.name.Contains("Z")) {
            rotationVector.z = Convert.ToSingle(field.text);
            newLoadedModel.transform.rotation = Quaternion.Euler(rotationVector);
        }

    }

    /// <summary>
    /// Update local scale of the object
    /// </summary>
    /// <param name="field">Slider updated</param>
    public void UpdateScale ( Slider slider ) {
        // In case there is no import operation or the slider is outsite its domain
        if (newLoadedModel == null) return;
        if (slider.value < 0) return;// || slider.value > 1) return;

        scaleFloat = slider.value;
        newLoadedModel.transform.localScale = new Vector3(scaleFloat, scaleFloat, scaleFloat);
    }

    /// <summary>
    /// Method that combine the mesehes
    /// </summary>
    /// <param name="list">list of CombineInstance</param>
    /// <param name="objectC">Object to save the merged mesh</param>
    private void CombineMeshes ( List<CombineInstance> list, GameObject objectC ) {
        // In case of no children, skip this part
        if (list.Count > 0) {
            if (objectC.GetComponent<MeshFilter>() == null)
                objectC.AddComponent<MeshFilter>();
            if (objectC.GetComponent<MeshRenderer>() == null)
                objectC.AddComponent<MeshRenderer>();

            objectC.GetComponent<MeshFilter>().mesh = new Mesh();
            objectC.GetComponent<MeshFilter>().mesh.CombineMeshes(list.ToArray());
            objectC.GetComponent<MeshFilter>().mesh.RecalculateNormals();
            objectC.GetComponent<MeshFilter>().mesh.RecalculateTangents();
            objectC.GetComponent<MeshRenderer>().material = material;

            objectC.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Normalize the mesh of the GameObject passed as argument with the max extension's bound of the mesh itselft.
    /// </summary>
    /// <param name="objToNorm">The Game object which contain the object to normalize</param>
    /// <returns>Null if some error occurs, the max dimension of the mesh otherwise</returns>
    private float? NormalizeMesh ( GameObject objToNorm ) {
        MeshFilter meshFilter = null;
        Vector3[] vertices;
        Vector3 extends;
        float maxD = 0.0f;

        if (objToNorm == null) return null;
        meshFilter = objToNorm.GetComponent<MeshFilter>();

        extends = meshFilter.mesh.bounds.extents;
        maxD = extends.x;
        maxD = maxD < extends.y ? extends.y : maxD;
        maxD = maxD < extends.z ? extends.z : maxD;
        vertices = meshFilter.mesh.vertices;

        if (maxD > 1.0f) {
            for (int i = 0; i < vertices.Length; i++) {
                Vector3 vertex = vertices[i];
                vertex.x = 1.5f * (vertex.x / maxD);
                vertex.y = 1.5f * (vertex.y / maxD);
                vertex.z = 1.5f * (vertex.z / maxD);
                vertices[i] = vertex;
            }
        }

        meshFilter.mesh.vertices = vertices;
        meshFilter.mesh.RecalculateNormals();
        meshFilter.mesh.RecalculateBounds();
        objToNorm.GetComponent<MeshFilter>().mesh = meshFilter.mesh;

        return maxD;
    }

    /// <summary>
    /// Resize the mesh
    /// </summary>
    /// <param name="objToNorm">The Game object which contain the object to resize</param>
    /// <param name="size">The size to update</param>
    private void ReduceSizeMesh ( GameObject objToNorm, float size ) {
        MeshFilter meshFilter = null;
        Vector3[] vertices;

        if (objToNorm == null) throw new Exception("Object null");

        meshFilter = objToNorm.GetComponent<MeshFilter>();
        vertices = meshFilter.mesh.vertices;

        if (size > 1.0f) {
            for (int i = 0; i < vertices.Length; i++) {
                Vector3 vertex = vertices[i];
                vertex.x = 1.5f * (vertex.x / size);
                vertex.y = 1.5f * (vertex.y / size);
                vertex.z = 1.5f * (vertex.z / size);
                vertices[i] = vertex;
            }
        }
        meshFilter.mesh.vertices = vertices;
        meshFilter.mesh.RecalculateNormals();
        meshFilter.mesh.RecalculateBounds();

        objToNorm.GetComponent<MeshFilter>().mesh = meshFilter.mesh;
    }

    /// <summary>
    /// Center the mesh
    /// </summary>
    /// <param name="objToNorm">The Game object to center</param>
    /// <returns>Null if some error occurs, the old center of the mesh otherwise</returns>
    private Vector3? CenterMesh ( GameObject obToCenter ) {
        MeshFilter meshFilter = null;
        Vector3[] vertices;
        Vector3 originalC;

        if (obToCenter == null) return null;

        meshFilter = obToCenter.GetComponent<MeshFilter>();
        originalC = meshFilter.mesh.bounds.center;
        vertices = meshFilter.mesh.vertices;

        for (int i = 0; i < vertices.Length; i++) {
            vertices[i] -= originalC;
        }
        meshFilter.mesh.vertices = vertices;
        meshFilter.mesh.RecalculateNormals();
        meshFilter.mesh.RecalculateBounds();

        obToCenter.GetComponent<MeshFilter>().mesh = meshFilter.mesh;

        return originalC;
    }

    /// <summary>
    /// Apply on the mesh trasformation of rotation an scale
    /// </summary>
    /// <param name="objToTransf">The mesh to transform</param>
    /// <param name="rotationVect">Rotation value</param>
    /// <param name="scaleValue">Scale value</param>
    private void TransforMesh ( GameObject objToTransf, Vector3 rotationVect, float scaleValue ) {
        MeshFilter meshFilter = null;
        Vector3[] vertices;

        if (objToTransf == null) throw new Exception("Object null");
        meshFilter = objToTransf.GetComponent<MeshFilter>();
        vertices = meshFilter.mesh.vertices;

        for (int i = 0; i < vertices.Length; i++) {
            vertices[i] = Quaternion.Euler(rotationVect) * vertices[i] * scaleValue;
        }

        meshFilter.mesh.vertices = vertices;
        meshFilter.mesh.RecalculateNormals();
        meshFilter.mesh.RecalculateBounds();

        objToTransf.GetComponent<MeshFilter>().mesh = meshFilter.mesh;
    }

    /// <summary>
    /// Confirm the model imported
    /// </summary>
    public void ConfirmModel () {
        if (newLoadedModel == null) return;

        try {
            TransforMesh(newLoadedModel, rotationVector, scaleFloat);
        }
        catch (Exception) {
            GameManager.instance.SetErrorMessage("Problem when importing the 3D model");
            CancelImport();
            return;
        }

        MeshFilter[] meshFilters = newLoadedModel.GetComponentsInChildren<MeshFilter>(true);
        List<MeshFilter> listSensor = meshFilters.Where(s => {
            Match result = Regex.Match(s.name.ToLower(), pattern);
            return result.Success;
        }).ToList();

        listSensor.ForEach(e => {
            GameObject sensor = Instantiate(prefabSphere, new Vector3(0, 0, 0), Quaternion.identity).gameObject;

            string name = e.name;
            name = name.ToLower().Replace("sensor", "");
            string[] pieces = name.Split('_');
            sensor.name = "Sensor" + pieces[0] + "_" + pieces[1];

            sensor.transform.position = e.gameObject.transform.position;
            sensor.transform.rotation = e.gameObject.transform.rotation;
            sensor.transform.parent = transform;
            sensors.Add(sensor.name, sensor);

            GameObject display = Instantiate(panelPrefab);
            display.name = sensor.name;
            display.transform.parent = generalPanel.transform;

            Sensor s = new Sensor(Convert.ToInt32(pieces[0]), 0.0f, 0.0f, true); //GameManager.instance.defaultVarTemp
            s.Channel = Convert.ToInt32(pieces[1]);
            s.Position = sensor.transform.position;

            GameManager.instance.UpdateSensorInfo(s);
        });

        GetComponent<MeshFilter>().mesh = newLoadedModel.GetComponent<MeshFilter>().mesh;
        GetComponent<MeshCollider>().sharedMesh = newLoadedModel.GetComponent<MeshFilter>().mesh;
        name = newLoadedModel.name;
        Destroy(newLoadedModel);
        newLoadedModel = null;
        GameManager.instance.SetModelExist(true);
        GetComponent<MeshRenderer>().enabled = true;
    }

    /// <summary>
    /// Stop the import procedure
    /// </summary>
    public void CancelImport () {
        Destroy(newLoadedModel);
        newLoadedModel = null;
    }
    #endregion

    #region MONITOREDOBJ_METHODS

    /// <summary>
    /// Update sensor information
    /// </summary>
    /// <param name="defaultProperties">Default properties value</param>
    public void SetSensor ( Vector2 defaultProperties ) {
        //Create the sensors and attach them to the sensors object
        List<Sensor> sensorList = GameManager.instance.GetSensorInfo();
        Vector4[] properties = new Vector4[64];
        Vector4[] positions = new Vector4[64];

        // In case the object was moved before...
        Quaternion oldRotation = transform.rotation;
        transform.rotation = Quaternion.identity;

        // For each sensor...
        foreach (Sensor s in sensorList) {
            // Check if it's active...
            if (s.Active) {
                string sensorName = "Sensor" + s.SensorID + "_" + s.Channel;
                // If it dosen't exists...
                if (!sensors.ContainsKey(sensorName)) {
                    // Check if the position is zero...
                    if (s.Position.Equals(Vector3.zero)) {
                        // If yes, check the nearest hit point of the monitored object surface
                        if (Physics.Raycast(new Ray(Camera.main.transform.position, Camera.main.transform.forward), out RaycastHit hit, Mathf.Infinity)) {
                            s.UpdatePosition(hit.point);
                            GameManager.instance.UpdateSensorPosition(s.Channel, s.SensorID, hit.point);
                        }
                    }
                    // Generate a new sensor object as child of the monitored object
                    GameObject sensor = null;
                    if (GameManager.instance.measurementType == Measurement.TEMPERATURE) {
                        sensor = Instantiate(prefabSphere, new Vector3(0, 0, 0), Quaternion.identity).gameObject;
                        sensor.transform.localScale = Vector3.one * GameManager.instance.defaultSizeSensor;
                    }
                    else {
                        sensor = Instantiate(prefabCube, new Vector3(0, 0, 0), Quaternion.identity).gameObject;
                        sensor.transform.localScale = new Vector3(GameManager.instance.defaultSizeSensor * 3, GameManager.instance.defaultSizeSensor, GameManager.instance.defaultSizeSensor);
                    }
                    GameObject display = Instantiate(panelPrefab);
                    display.name = sensorName;

                    //display.transform.parent = generalPanel.transform;
                    display.transform.SetParent(generalPanel.transform);
                    sensor.name = sensorName;

                    Vector3 finalPosition = s.Position;
                    // Update position depending on the center and resize of the object
                    if (!originalCenter.Equals(Vector3.zero)) {
                        finalPosition = s.Position - originalCenter;
                        if (maxDimension > 1f)
                            finalPosition = 1.5f * finalPosition / maxDimension;
                        if (rotationVector.x != 0.0f || rotationVector.y != 0.0f || rotationVector.z != 0.0f)
                            finalPosition = Quaternion.Euler(rotationVector) * finalPosition;
                        if (scaleFloat > 1f)
                            finalPosition = finalPosition * scaleFloat;
                    }

                    sensor.transform.position = finalPosition;
                    sensor.transform.parent = transform;
                    sensors.Add(sensorName, sensor);
                }
                else {
                    sensors[sensorName].SetActive(true);
                    // If it exists, update the position if is different from zero
                    if (!s.Position.Equals(Vector3.zero) && !sensors[sensorName].transform.position.Equals(s.Position))
                        sensors[sensorName].transform.position = s.Position;
                }
                positions[s.Channel * 16 + s.SensorID] = s.Position;
            }
            else {
                positions[s.Channel * 16 + s.SensorID] = Vector4.zero;
                string sensorName = "Sensor" + s.SensorID + "_" + s.Channel;
                if (sensors.ContainsKey(sensorName)) sensors[sensorName].SetActive(false);
            }
        }

        // Update properties of each sensor
        for (int i = 0; i < positions.Length; i++) {
            if (!sensorList[i].Active) {
                properties[i] = new Vector2(0, 0);
                GameManager.instance.SensorRadiuses[i] = 0.0f;
                GameManager.instance.SensorIntensities[i] = 0.0f;
            }
            else {
                properties[i] = new Vector2(defaultProperties.x, defaultProperties.y);
                GameManager.instance.SensorRadiuses[i] = defaultProperties.x;
                GameManager.instance.SensorIntensities[i] = defaultProperties.y;
            }
        }
        
        // Update information on the material
        material.SetInt("_Points_Length", 64);
        material.SetVectorArray("_Points", positions);
        material.SetVectorArray("_Properties", properties);

        // Update sensors' properties
        GameManager.instance.SetInitProperty(properties);

        // Set old rotation value
        transform.rotation = oldRotation;
    }

    /// <summary>
    /// Change size of each sensor
    /// </summary>
    /// <param name="newSize">New sensor's size</param>
    public void ChangeSizeSensor (float newSize) {
        if(sensors.Count > 0) {
            foreach(KeyValuePair<string,GameObject> s in sensors)
                if(GameManager.instance.measurementType == Measurement.TEMPERATURE)
                    s.Value.transform.localScale = Vector3.one * newSize;
                else
                    s.Value.transform.localScale = new Vector3(newSize*3, newSize, newSize);
        }
    }

    /// <summary>
    /// Change type sensor based on the type of measurement
    /// </summary>
    public void ChangeTypeSensor () {
        if (GameManager.instance.measurementType == Measurement.DISPLACEMENT) {
            // Update sensors to be a displacement one
            foreach (KeyValuePair<string, GameObject> sensor in sensors) {
                sensor.Value.transform.parent = null;

                sensor.Value.transform.GetComponent<MeshFilter>().mesh = prefabCube.transform.GetComponent<MeshFilter>().sharedMesh;
                sensor.Value.transform.localScale = new Vector3(sensor.Value.transform.localScale.x * 3, sensor.Value.transform.localScale.x, sensor.Value.transform.localScale.x);

                sensor.Value.GetComponent<SphereCollider>().enabled = false;

                if (sensor.Value.GetComponent<BoxCollider>() == null)
                    sensor.Value.AddComponent<BoxCollider>();
                else
                    sensor.Value.GetComponent<BoxCollider>().enabled = true;
                sensor.Value.transform.GetComponent<MeshFilter>().mesh.RecalculateBounds();
                sensor.Value.transform.parent = transform;
            }
        }
        else {
            // Update sensors to be a temperature one
            foreach (KeyValuePair<string, GameObject> sensor in sensors) {
                sensor.Value.transform.parent = null;
                sensor.Value.transform.GetComponent<MeshFilter>().mesh = prefabSphere.transform.GetComponent<MeshFilter>().sharedMesh;
                sensor.Value.transform.localScale = sensor.Value.transform.localScale;
                sensor.Value.GetComponent<BoxCollider>().enabled = false;
                if (sensor.Value.GetComponent<SphereCollider>() == null)
                    sensor.Value.AddComponent<SphereCollider>();
                else
                    sensor.Value.GetComponent<SphereCollider>().enabled = true;

                sensor.Value.transform.GetComponent<MeshFilter>().mesh.RecalculateBounds();
                sensor.Value.transform.parent = transform;
            }
        }
    }

    /// <summary>
    /// Change material's texture 
    /// </summary>
    /// <param name="data">New texture</param>
    public void ChangeTexture ( Texture2D data ) {
        material.SetTexture("_HeatTex", data);
    }


    #endregion

    #region METHODS_PANELS

    /// <summary>
    /// Show panel with sensor information
    /// </summary>
    /// <param name="point">Point on surface object</param>
    private void ShowText ( Vector3 point, string nameCollider ) {
        if (nameCollider.Contains("Sensor"))
            UpdatePanelSensor(nameCollider, infoPanel, Input.mousePosition);
        else {
            foreach (KeyValuePair<string, GameObject> s in sensors) {
                Vector4 currentPosition = s.Value.transform.position;

                string name = s.Key;
                name = name.Replace("Sensor", "");
                string[] pieces = name.Split('_');
                int      index  = Convert.ToInt32(pieces[1]) *16 + Convert.ToInt32(pieces[0]);

                float currentRadius = GameManager.instance.SensorRadiuses[index];

                float x  = point.x - currentPosition.x;
                float y  = point.y - currentPosition.y;
                float z  = point.z - currentPosition.z;
                float x1 = Mathf.Pow(x, 2);
                float y1 = Mathf.Pow(y, 2);
                float z1 = Mathf.Pow(z, 2);

                if (x1 + y1 + z1 < (currentRadius * currentRadius)) {
                    UpdatePanelSensor(s.Key, infoPanel, Input.mousePosition);
                    break;
                }
                else
                    HideText();
            }
        }
    }

    /// <summary>
    /// Show all sensor's panel 
    /// </summary>
    private void ShowAll () {
        Transform[] listChild = generalPanel.transform.GetComponentsInChildren<Transform>();
        foreach (Transform t in listChild) {
            if (t.name.Contains("Sensor")) {
                if (!sensors.ContainsKey(t.name)) continue;
                if (!sensors[t.name].activeSelf) continue;
                UpdatePanelSensor(t.name, t.gameObject, Camera.main.WorldToScreenPoint(sensors[t.name].transform.position));
            }
        }
    }

    /// <summary>
    /// Show panel with sensor information
    /// </summary>
    /// <param name="point">Point on surface object</param>
    private void ShowText ( Vector3 point, string nameCollider, List<KeyValuePair<UInt64, float>> wav, Vector4[] newProperties ) {
        if (nameCollider.Contains("Sensor"))
            UpdatePanelSensor(nameCollider, infoPanel, Input.mousePosition);
        else {
            foreach (KeyValuePair<string, GameObject> s in sensors) {
                Vector4 currentPosition = s.Value.transform.position;

                string name = s.Key;
                name = name.Replace("Sensor", "");
                string[] pieces = name.Split('_');
                int      index  = Convert.ToInt32(pieces[1]) *16 + Convert.ToInt32(pieces[0]);

                float currentRadius = newProperties[index].x;

                float x  = point.x - currentPosition.x;
                float y  = point.y - currentPosition.y;
                float z  = point.z - currentPosition.z;
                float x1 = Mathf.Pow(x, 2);
                float y1 = Mathf.Pow(y, 2);
                float z1 = Mathf.Pow(z, 2);

                if (x1 + y1 + z1 < (currentRadius * currentRadius)) {
                    UpdatePanelSensor(s.Key, infoPanel, Input.mousePosition, wav);
                    break;
                }
                else
                    HideText();
            }
        }
    }

    /// <summary>
    /// Show all sensor's panel 
    /// </summary>
    private void ShowAll ( List<KeyValuePair<UInt64, float>> wav) {
        Transform[] listChild = generalPanel.transform.GetComponentsInChildren<Transform>();
        foreach (Transform t in listChild) {
            if (t.name.Contains("Sensor")) {
                if (!sensors.ContainsKey(t.name)) continue;
                if (!sensors[t.name].activeSelf) continue;
                UpdatePanelSensor(t.name, t.gameObject, Camera.main.WorldToScreenPoint(sensors[t.name].transform.position),wav);
            }
        }
    }

    /// <summary>
    /// Hide all the sensor's panel
    /// </summary>
    private void HideAll () {
        Transform[] listChild = generalPanel.transform.GetComponentsInChildren<Transform>();
        foreach (Transform t in listChild) {
            if (t.name.Contains("Sensor")) {
                string name = t.name;
                name = name.Replace("Sensor", "");
                string[] pieces = name.Split('_');
                int index = Convert.ToInt32(pieces[1]) *16 + Convert.ToInt32(pieces[0]);

                // Aggiorno grafica
                Image panelImage = t.gameObject.GetComponent<Image>();

                // Change alpha channel
                Color panelColor = panelImage.color;
                panelColor.a = 0;
                panelImage.color = panelColor;

                Color textColor = t.gameObject.GetComponentInChildren<Text>().color;
                textColor.a = 0;
                t.gameObject.GetComponentInChildren<Text>().color = textColor;
            }
        }
    }

    /// <summary>
    /// Hide pannel when outside of object
    /// </summary>
    private void HideText () {
        Image panelImage = infoPanel.GetComponent<Image>();
        Color panelColor = panelImage.color;
        panelColor.a = 0;
        panelImage.color = panelColor;

        Color textColor = infoPanel.GetComponentInChildren<Text>().color;
        textColor.a = 0;
        infoPanel.GetComponentInChildren<Text>().color = textColor;
    }

    /// <summary>
    /// Update the visibility of the sensor's panel
    /// </summary>
    public void SetShowAllToggle () {
        showAll = !showAll;
        if (showAll)
            ShowAll();
        else
            HideAll();
    }

    /// <summary>
    /// Update sensor panel position and text
    /// </summary>
    /// <param name="nameSensor">Name of the sensor's panel to update</param>
    /// <param name="panel">Panel to update</param>
    /// <param name="position">Position to move the panel</param>
    private void UpdatePanelSensor ( string nameSensor, GameObject panel, Vector2 position ) {
        string name = nameSensor;
        name = name.Replace("Sensor", "");
        string[] pieces = name.Split('_');
        int      index  = Convert.ToInt32(pieces[1]) *16 + Convert.ToInt32(pieces[0]);

        Image panelImage = panel.GetComponent<Image>();

        // Change alpha channel
        Color panelColor = panelImage.color;
        panelColor.a = 0.6f;
        panelImage.color = panelColor;

        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        float xt = position.x;
        float yt = position.y + 40.0f;
        rectTransform.transform.position = new Vector3(xt, yt, 0.0f);

        Color textColor = panel.GetComponentInChildren<Text>().color; ;
        textColor.a = 1;
        panel.GetComponentInChildren<Text>().color = textColor;

        if (GameManager.instance.statusGame == Status.MONITORING)
            panel.GetComponentInChildren<Text>().text = "Sensor:" + "Ch" + (Convert.ToInt32(pieces[1]) + 1) + "Gr" + (Convert.ToInt32(pieces[0]) + 1) + "\nWaveLength:" + GameManager.instance.CurrentSensorWavelength[index].Value.ToString("0.0000");
        if (GameManager.instance.statusGame == Status.CHANGEPOS)
            panel.GetComponentInChildren<Text>().text = "Ch" + (Convert.ToInt32(pieces[1]) + 1) + "Gr" + (Convert.ToInt32(pieces[0]) + 1);
    }


    private void UpdatePanelSensor ( string nameSensor, GameObject panel, Vector2 position, List<KeyValuePair<UInt64, float>> wav) {
        string name = nameSensor;
        name = name.Replace("Sensor", "");
        string[] pieces = name.Split('_');
        int      index  = Convert.ToInt32(pieces[1]) *16 + Convert.ToInt32(pieces[0]);

        Image panelImage = panel.GetComponent<Image>();

        // Change alpha channel
        Color panelColor = panelImage.color;
        panelColor.a = 0.6f;
        panelImage.color = panelColor;

        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        float xt = position.x;
        float yt = position.y + 40.0f;
        rectTransform.transform.position = new Vector3(xt, yt, 0.0f);

        Color textColor = panel.GetComponentInChildren<Text>().color; ;
        textColor.a = 1;
        panel.GetComponentInChildren<Text>().color = textColor;

        if (GameManager.instance.statusGame == Status.MONITORING)
            panel.GetComponentInChildren<Text>().text = "Sensor:" + "Ch" + (Convert.ToInt32(pieces[1]) + 1) + "Gr" + (Convert.ToInt32(pieces[0]) + 1) + "\nWaveLength:" + wav[index].Value.ToString("0.0000");
        if (GameManager.instance.statusGame == Status.CHANGEPOS)
            panel.GetComponentInChildren<Text>().text = "Ch" + (Convert.ToInt32(pieces[1])+1) + "Gr" + (Convert.ToInt32(pieces[0])+1);
    }
    #endregion

}
