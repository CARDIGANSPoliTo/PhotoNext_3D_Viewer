using System.Collections.Concurrent;
using System.Collections.Generic;
using System;

using System.Linq;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Status application support variables
//-------------------------------------------------
[Serializable]
public enum Status {
    MONITORING,
    MENU,
    CHANGEPOS,
    IMPORT
}

// Measurement supported
//-------------------------------------------------
[Serializable]
public enum Measurement {
    TEMPERATURE,
    DISPLACEMENT
}

/// <summary>
/// Singleton class used to manage the shared information inside the project
/// </summary>
public class GameManager : MonoBehaviour {

    // Network Configuration variables
    //-------------------------------------------------

    // Information Network obtained
    [SerializeField]
    public MongoDBManager MongoInstance = null;
    [SerializeField]
    public TCPManager TCPInstance = null;
    [SerializeField]
    public ChangeStreamManager ChangeStreamInstance = null;

    LogManager log = new LogManager();

    [SerializeField, HideInInspector]
    public bool SetConfiguration { get; private set; }
    [SerializeField, HideInInspector]
    public bool modelChosen { get; private set; }

    [SerializeField, HideInInspector]
    bool database = false;

    //Antonio: activation var for changestream
    [SerializeField, HideInInspector]
    bool changestream = false;

    // Monitored Object Configuration variables
    //-------------------------------------------------
    [SerializeField]
    public MonitoredObject monitoredObject = null;

    [SerializeField, HideInInspector]
    public KeyValuePair<UInt64,float>[] CurrentSensorWavelength { get; private set; }
    public ConcurrentQueue<List<KeyValuePair<UInt64, float>>> LogDataQueue;
    public ConcurrentQueue<KeyValuePair<Int64, Int64>> MeasurementLatencyQueue;

    [SerializeField, HideInInspector]
    public UInt64 timestampUpdate = 0;

    [SerializeField, HideInInspector]
    public List<Sensor> SensorsFromNetwork = new List<Sensor>();
    object objLock = new object();

    [SerializeField, HideInInspector]
    public volatile bool isChange = false;
    [SerializeField, HideInInspector]
    public volatile bool stopSimulation = false;

    [SerializeField, HideInInspector]
    public Vector2 defaultProperties = new Vector2(0.2f, 0.35f);

    [SerializeField, HideInInspector]
    public Vector4[] SensorPropertyOld { get; private set; }
    [SerializeField, HideInInspector]
    public Vector4[] SensorPropertyNew { get; private set; }
    object propertyLock = new object();
    public volatile bool changeProperty = false;
    public float globalMaxVariation = 0.0f;
    
    [HideInInspector]
    public float defaultSizeSensor = 0.03f;
    [HideInInspector]
    public float defaultVarTemp = 0.0f;
    [HideInInspector]
    public float defaultVarDisp = 0.0f;

    [SerializeField, HideInInspector]
    public float[] SensorRadiuses = new float[64];
    [SerializeField, HideInInspector]
    public float[] SensorIntensities = new float[64];

    // GUI Configuration variables
    //-------------------------------------------------
    [SerializeField]
    public GameObject menuPanel = null;
    [SerializeField]
    public GameObject menuImportPanel = null;
    [SerializeField]
    public GameObject startButton = null;
    [SerializeField]
    public GUIManager guiManager = null;
    [SerializeField]
    public GameObject samplingRateLabel = null;
    [SerializeField]
    public GameObject samplingRateInputField = null;

    [SerializeField]
    public GameObject exitText = null;
    [SerializeField]
    public GameObject errorPanel = null;

    [SerializeField, HideInInspector]
    public Status statusGame { get; private set; }

    [SerializeField, HideInInspector]
    public Measurement measurementType { get; private set; }


    // Singleton variable
    //-------------------------------------------------
    //Static instance of GameManager which allows it to be accessed by any other script.
    [HideInInspector]
    public static GameManager instance = null;

    // Multithread support variables
    //-------------------------------------------------
    // Boolean variable used when closing the application for terminate the secondary thread
    [HideInInspector]
    public volatile bool TermianteThread = false;

    [HideInInspector]
    public ConcurrentQueue<string> errorMessages = new ConcurrentQueue<string>();


    void Awake () {
        //Check if instance already exists
        if (instance == null)

        //if not, set instance to this
        instance = this;

        //If instance already exists and it's not this:
        else if (instance != this)

        //Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a GameManager.
        Destroy(gameObject);

        //Sets this to not be destroyed when reloading scene
        DontDestroyOnLoad(gameObject);

        //Set default variables
        statusGame = Status.MENU;
        measurementType = Measurement.TEMPERATURE;
        SetConfiguration = false;

        // TODO: To change to the new configuration
        CurrentSensorWavelength = new KeyValuePair<UInt64, float>[64];
        LogDataQueue = new ConcurrentQueue<List<KeyValuePair<ulong, float>>>();
        MeasurementLatencyQueue = new ConcurrentQueue<KeyValuePair<long, long>>();
        SensorPropertyNew = new Vector4[64];
        SensorPropertyOld = new Vector4[64];
        SensorRadiuses = new float[64];
        SensorIntensities = new float[64];
        modelChosen = false;

        // It exists only one object of this kind
        if (monitoredObject == null)
            monitoredObject = GameObject.FindObjectOfType<MonitoredObject>();
    }

    public List<KeyValuePair<UInt64,float>> GetCurrentSensorWavelength() {
        lock (CurrentSensorWavelength) {
            return CurrentSensorWavelength.ToList();
        }
    }

    public void AddElementQueue (List<KeyValuePair<ulong, float>> element) {
        LogDataQueue.Enqueue(element);
    }

    public void AddMeasurementLatencyQueue (KeyValuePair<long, long> element) {
        MeasurementLatencyQueue.Enqueue(element);
    }

    public void SetCurrentSensorWavelength (int id, float wavelength, UInt64 timestamp) {
        lock (CurrentSensorWavelength) {
            CurrentSensorWavelength[id] = new KeyValuePair<ulong, float>(timestamp, wavelength);
        }
    }

    public void SetCurrentSensorsWavelength (List<KeyValuePair<ulong,float>> listUpdated) {
        lock (CurrentSensorWavelength) {
            CurrentSensorWavelength = listUpdated.ToArray();
        }
    }

    void Update () {
        // Show error message from other threads
        if (errorMessages.Count > 0) {
            string errorMessage = "";
            if (errorMessages.TryDequeue(out errorMessage)) {
                SetErrorMessage(errorMessage);
            }
        }

        // Manage exit to different status and return to the idle one
        if (Input.GetKeyUp(KeyCode.Escape)) {
            if (statusGame != Status.IMPORT)
                StopMonitoring();
            else
                CancelImport();
        }

        // In case of changes on the sensors data from different thread, it is guaranteed the mutal exclusion
        lock (objLock) {
            if (isChange) {
                InitMonitoredObject();
                guiManager.UpdateFiberPanel(SensorsFromNetwork.ToList());
                isChange = false;
            }
        }

        // In case of error on other threads, the status of the game is set to idle 
        if (stopSimulation) {
            StopMonitoring();
        }
    }

    void OnApplicationQuit () {
        TermianteThread = true;
        if (SetConfiguration && !database &&!changestream) {
            TCPInstance.Send(TypeMessage.EndThreadPacket);
            if (statusGame == Status.MONITORING) {
                TCPInstance.Send(TypeMessage.RequestDataEnd);
                statusGame = Status.MENU;
            }
        }else if (SetConfiguration && !database && changestream)//Antonio - puó essere fatto meglio
        {
            TermianteThread = true;
            if (statusGame == Status.MONITORING)
            {
                statusGame = Status.MENU;
            }

        }
    }

    // Error managing Methods
    //-------------------------------------------------
    #region ERROR_METHODS
    public void AddErrorMessage (string errorMsg) {
        if (errorMsg != null && !errorMsg.Equals(""))
            errorMessages.Enqueue(errorMsg);
    }
    public void SetErrorMessage (string errorMessage) {
        errorPanel.transform.GetChild(0).GetComponent<Text>().text = errorMessage;
    }
    #endregion

    // Network Configuration Methods
    //-------------------------------------------------
    #region NETCONF_METHODS

    /// <summary>
    /// Confirm/Delete network configuration, it starts/stops the database or the tcp server based on the choice of the user.
    /// </summary>
    public bool ConfirmNetworkConfiguration ()
    {
        SetConfiguration = !SetConfiguration;
        if (SetConfiguration) 
        {
            if (StartUp()) 
            {
                return true;
            }
            else 
            {
                SetConfiguration = false;
                return false;
            }
        }
        else 
        {
            ShutDown();
        }
        return true;
    }

    /// <summary>
    ///  Change the network configuration based on the choise made by the user
    /// </summary>
    /// <param name="configNetwork">Dropdown object which contain the changed value</param>
    public void SetNetworkConfiguration (Dropdown configNetwork)
    {
        //Antonio: pre set anti errore
        changestream = false;
        InputField DBName = GameObject.Find("InputFieldDBName").GetComponent<InputField>();
        InputField colName = GameObject.Find("InputFieldCollectionName").GetComponent<InputField>();
        Text IPErrorMessage = GameObject.Find("IPErrorMessage").GetComponent<Text>();
        Text portErrorMessage = GameObject.Find("PortErrorMessage").GetComponent<Text>();
        Button startButton = GameObject.Find("ConfirmConfiguration").GetComponent<Button>();

        // TCP Case.
        if (configNetwork.value == 1) 
        {
            DBName.interactable = false;
            colName.interactable = false;
            database = false;

            //Verifies if the ip and the port are valid in order to enable the start button.
            if ((IPErrorMessage.text.Equals("The IP address is valid")) && (portErrorMessage.text.Equals("The port is valid"))) 
            {
                startButton.interactable = true;
            }
            else 
            {
                startButton.interactable = false;
            }
        }
        // MongoDB Case.
        else if (configNetwork.value == 0) 
        {
            Text DBNameErrorMessage = GameObject.Find("DBNameErrorMessage").GetComponent<Text>();
            Text collNameErrorMessage = GameObject.Find("CollNameErrorMessage").GetComponent<Text>();
            DBName.interactable = true;
            colName.interactable = true;
            database = true;

            //Verifies if the IP address, the port, the database name and the collection name are valid in order to enable the start button.
            if ((IPErrorMessage.text.Equals("The IP address is valid")) && (portErrorMessage.text.Equals("The port is valid")) && (DBNameErrorMessage.text.Equals("The database name is valid")) && (collNameErrorMessage.text.Equals("The collection name is valid"))) 
            {
                startButton.interactable = true;
            }
            else 
            {
                startButton.interactable = false;
            }
        }
        //ChangeStream case
        else if(configNetwork.value == 2)
        {
            //Antonio: dropdown menu trigger
            changestream = true;
            DBName.interactable = true;
            colName.interactable = true;
            database = false;
            startButton.interactable = true;
        }
        return;
    }

    /// <summary>
    /// Update TCP configuration. In case the value is "" it is not update
    /// </summary>
    /// <param name="ip">New ip</param>
    /// <param name="port">New port</param>
    public void UpdateTcpInfomation (string ip, int port)
    {
        TCPInformation tcp = TCPInstance.AddressInfo;
        if (!ip.Equals(""))
            tcp.IPAddress = ip;
        if (port > 0)
            tcp.Port = port;
        TCPInstance.AddressInfo = tcp;
    }

    /// <summary>
    /// Update sensor measurement configuration. It change the object from a sphere to a square
    /// </summary>
    /// <param name="dropdownMenu"></param>
    public void UpdateMeasurementInfo (Dropdown dropdownMenu) {
        measurementType = (Measurement)dropdownMenu.value;

        // Need to change configuration of the sensors. 
        // In case it was a temperature(displacement) measurement -> all sensor object has to change to displacement(temperture) sensor
        // Need to change material configuration and sensor object shape and material
        monitoredObject.ChangeTypeSensor();
    }

    /// <summary>
    /// Update Mongo configuration. If one of the input value is equal to 0 or "" it is not update
    /// </summary>
    /// <param name="ip">New ip</param>
    /// <param name="port">New port</param>
    /// <param name="dbname">New database name</param>
    /// <param name="collectionname">New collection name</param>
    public void UpdateMongoInfomation (string ip, int port, string DBName, string collectionName)
    {
        MongoInformation mongo = MongoInstance.mongoInfo;
        mongo.IPAddress = ip;
        mongo.Port = port;
        mongo.DBName = DBName;
        mongo.CollectionName = collectionName;

        MongoInstance.mongoInfo = mongo;
    }

    //Antonio
    /// <summary>
    /// Update Mongo configuration for changestream. If one of the input value is equal to 0 or "" it is not update
    /// </summary>
    /// <param name="ip">New ip</param>
    /// <param name="port">New port</param>
    /// <param name="dbname">New database name</param>
    /// <param name="CollName">Collection name</param>
    public void UpdateChangeStreamInformation(string ip, int port, string DBName, string CollName)
    {
        ChangeStreamInstance.IPAddress = ip;
        ChangeStreamInstance.Port = port;
        ChangeStreamInstance.DbName = DBName;
        ChangeStreamInstance.CollName = CollName;
    }
    #endregion

    // Sensor Configuration Methods
    //-------------------------------------------------
    #region SENSORCONF_METHODS
    /// <summary>
    /// Request sensor configuration from server/database
    /// </summary>
    public void RequestConfiguration () {
        if (!SetConfiguration) {
            SetErrorMessage("Before starting or ending the monitoring operation it is necessary to confirm the network configuration");
            return;
        }

        if (!database)
        {
            if (changestream){
                ChangeStreamInstance.GetConfiguration();
            }
            else
            {
                Task.Run(() => { TCPInstance.Send(TypeMessage.RequestConfig); });
            }
        }  
        else
            MongoInstance.RequestSensorsConfiguration();
    }

    /// <summary>
    /// Start sensor position status in which the user can change the sensor's position on the surface of the object
    /// </summary>
    public void ChangeSensorPosition () {
        if (SensorsFromNetwork.Count <= 0) return;
        statusGame = Status.CHANGEPOS;
        exitText.SetActive(true);
        exitText.GetComponent<Text>().text = "Click ESC to finish";

        menuPanel.SetActive(false);
        startButton.SetActive(false);
        menuImportPanel.SetActive(false);
    }

    /// <summary>
    /// Update all sensors information
    /// </summary>
    /// <param name="sensors">List with update sensor's info</param>
    public void UpdateSensorsInfo (List<Sensor> sensors) {
        lock (objLock) {
            Sensor s;

            if (SensorsFromNetwork.Count == 0) {
                SensorsFromNetwork = sensors;
                if (statusGame != Status.MONITORING)
                    isChange = true;
            }
            else {
                bool isChangeOne = false;
                for (int i = 0; i < sensors.Count; i++) {
                    if ((SensorsFromNetwork[i].WavelenghtIdle != sensors[i].WavelenghtIdle) ||
                        (SensorsFromNetwork[i].MaxVariation != sensors[i].MaxVariation) ||
                        (SensorsFromNetwork[i].Position != sensors[i].Position)) {

                        if (!isChangeOne) isChangeOne = true;

                        s = SensorsFromNetwork[i];

                        s.WavelenghtIdle = sensors[i].WavelenghtIdle;
                        s.MaxVariation = sensors[i].MaxVariation;
                        s.Position = sensors[i].Position;
                        s.Active = sensors[i].Active;

                        SensorsFromNetwork[i] = s;
                    }
                }
                if (statusGame != Status.MONITORING)
                    isChange = true;
            }
            globalMaxVariation = SensorsFromNetwork.Max(e => e.MaxVariation);
        }
    }

    /// <summary>
    /// Return only one sensor data
    /// </summary>
    /// <param name="channel">Channel of the sensor</param>
    /// <param name="grating">Grating of the sensor</param>
    public Sensor? GetSensorInfo (int channel, int grating) {
        lock (objLock) {
            if (SensorsFromNetwork.Count == 0) return null;
            else return SensorsFromNetwork[channel * 16 + grating];
        }
    }

    /// <summary>
    /// Update a sensor information. If the strucutre is empty, fill it with default value
    /// </summary>
    /// <param name="sensor">Sensor object</param>
    public void UpdateSensorInfo (Sensor sensor)
    {
        if (SensorsFromNetwork.Count > 0) {
            int index = sensor.Channel * 16 + sensor.SensorID;
            Sensor s = SensorsFromNetwork[sensor.Channel * 16 + sensor.SensorID];
            s.Active = sensor.Active;
            s.WavelenghtIdle = sensor.WavelenghtIdle;
            s.MaxVariation = sensor.MaxVariation;

            SensorsFromNetwork[index] = s;
            if (statusGame != Status.MONITORING)
                isChange = true;
        }
        else 
        {
            List<Sensor> sensors = new List<Sensor>();
            int count = 0;
            int id_s = 0;
            int channel_s = 0;

            for (int i = 0; i < 16 * 4; i++) {
                if (id_s >= 16)
                    id_s = 0;

                channel_s = count / 16;
                Sensor s = new Sensor(id_s, 0.0f, 0.0f, false);

                if (channel_s == sensor.Channel && id_s == sensor.SensorID) {
                    s.WavelenghtIdle = sensor.WavelenghtIdle;
                    s.MaxVariation = sensor.MaxVariation;
                    s.Active = true;
                }

                s.Channel = channel_s;
                s.Position = Vector4.zero;
                sensors.Add(s);

                count++;
                id_s++;
            }
            SensorsFromNetwork = sensors;
            globalMaxVariation = SensorsFromNetwork.Max(e => e.MaxVariation);
            if(statusGame != Status.MONITORING)
                isChange = true;
        }
    }

    /// <summary>
    /// Get current list of sensor data
    /// </summary>
    /// <returns>List of all sensors</returns>
    public List<Sensor> GetSensorInfo () {
        lock (objLock) {
            return SensorsFromNetwork;
        }
    }

    /// <summary>
    /// Update only the position of the sensor
    /// </summary>
    /// <param name="channel">Sensor's channel</param>
    /// <param name="grating">Sensor's grating</param>
    /// <param name="newPos">Sensor's new position</param>
    /// <returns>True if the sensor was updated, false otherwise</returns>
    public bool UpdateSensorPosition (int channel, int grating, Vector3 newPos) {
        lock (objLock) {
            int index = (channel * 16) + grating;
            if (index >= 64) return false;

            SensorsFromNetwork[index].UpdatePosition(newPos);
            return true;
        }
    }

    /// <summary>
    /// Update only sensor's size
    /// </summary>
    /// <param name="newSize">Sensor's size</param>
    public void ChangeSizeSensor ( Slider newSize ) {
        monitoredObject.ChangeSizeSensor(newSize.value);
        defaultSizeSensor = newSize.value;
    }
    

    /// <summary>
    /// Update old sensors properties
    /// </summary>
    /// <param name="property">Old sensors properties</param>
    public void SetOldSensorProperty (Vector4[] property) {
        SensorPropertyOld = property;
    }

    /// <summary>
    /// Update new sensors properties
    /// </summary>
    /// <param name="property">new sensors properties</param>
    public void SetNewSensorProperty (Vector4[] property) {
        lock (propertyLock) {
            SensorPropertyNew = property;
            changeProperty = true;
        }
    }

    public Vector4[] GetNewSensorProperty () {
        lock (propertyLock) {
            Vector4[] copy = new Vector4[SensorPropertyNew.Length];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = SensorPropertyNew[i];
            return copy;    
        }
    }
    /// <summary>
    /// Init sensors properties
    /// </summary>
    /// <param name="property">Initial sensors properties</param>
    public void SetInitProperty ( Vector4[] property ) {
        SensorPropertyOld = property;
        SensorPropertyNew = property;
    }

    /// <summary>
    /// Clear all the sensors
    /// </summary>
    public void ClearSensorsInfo ()
    {
        lock (objLock) {
            SensorsFromNetwork.Clear();
        }
        guiManager.ClearFiberPanel();
    }

    public void UpdateMonitoredObject ( Vector4[] newProperties, List<KeyValuePair<UInt64, float>> wav )
    {
        monitoredObject.UpdateShader(newProperties, wav);
        guiManager.UpdateCategory(wav);
    }

    #endregion

    // Component/Status managing Methods
    //-------------------------------------------------
    #region STATUSMANAGING_METHODS

    /// <summary>
    /// One-shoot update.
    /// </summary>
    public void UpdateData (Vector4[] properties, List<KeyValuePair<UInt64, float>> wavelength_data, long now, long currentTimestamp)
    {
        List<KeyValuePair<UInt64, float>> wavelength_data_test = wavelength_data.ToList();
        SetNewSensorProperty(properties);
        SetCurrentSensorsWavelength(wavelength_data);
        AddElementQueue(wavelength_data_test);
        UpdateMonitoredObject(properties, wavelength_data_test);
        AddMeasurementLatencyQueue(new KeyValuePair<long, long>(now, currentTimestamp));
    }

    /// <summary>
    /// Start mongo database connection, in case of error it's closed
    /// </summary>
    bool InitMongoDBDatabase () {
        if (MongoInstance != null) {
            if (!MongoInstance.InitMongoDB()) {
                ShutDown();
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Start TCP listener, in case of error it's closed
    /// </summary>
    bool InitTCPManager () {
        if (TCPInstance != null)
            if (!TCPInstance.InitTcpListener()){
                ShutDown();
                return false;
            }
        return true;
    }

    bool InitChangeStream()
    {
        if (ChangeStreamInstance != null)
        {
            if (!ChangeStreamInstance.SetupChangeStream())
            {
                ShutDown();
                return false;
            }
        }  
        return true;
    }

    /// <summary>
    /// Init Mongo/TCP manager and the relative thread
    /// </summary>
    public bool StartUp() {
        TermianteThread = false;
        if (database)
        {
            return InitMongoDBDatabase();
        }
        else
        {
            if (changestream)
            {
                return InitChangeStream();
            }
            return InitTCPManager();
        }
    }

    /// <summary>
    /// Shutdown Mongo/TCP manager and the relative thread
    /// </summary>
    public void ShutDown() {
        TermianteThread = true;

        if (!database && !changestream)
            TCPInstance.Send(TypeMessage.EndThreadPacket);
    }
    
    /// <summary>
    /// Start the monitoring phase of the object
    /// </summary>
    public void StartMonitoring() {
        if (!SetConfiguration) {
            SetErrorMessage("Before starting or ending the monitoring operation it is necessary to confirm the network configuration");
            return;
        }

        if ((SensorsFromNetwork.Count <= 0 || SensorsFromNetwork.Where(s => s.Active == true).Count() <= 0)) {
            SetErrorMessage("There is no active sensor in the configuration");
            return;
        }

        statusGame = Status.MONITORING;
        TermianteThread = false;
        if (!database && !changestream)
        {
            Task.Run(() => { TCPInstance.Send(TypeMessage.RequestDataStart); });
        }
        else if (!database && changestream)
        {
            Task.Run(() => { ChangeStreamInstance.StartWatch(); }) ;
        }
        else//database
        {
            MongoInstance.StartRequestSensorData();
        }

        log.InitLog();
        ActivateGUIElements(true);
    }
    
    /// <summary>
    /// Stop the monitoring phase of the object
    /// </summary>
    public void StopMonitoring() {
        if (!SetConfiguration) {
            SetErrorMessage("Before starting or ending the monitoring operation it is necessary to confirm the network configuration");
            return;
        }

        stopSimulation = false;
        Status prevStatus = statusGame;
        statusGame = Status.MENU;

        if (prevStatus == Status.MONITORING){
            if (!database)
            {
                if (changestream)
                {
                    TermianteThread = true;
                }
                else
                {
                    Task.Run(() => { TCPInstance.Send(TypeMessage.RequestDataEnd); });
                }  
            }
            else
                TermianteThread = true;
        }
        
        ActivateGUIElements(false);
        changeProperty = false;
    }

    private void ActivateGUIElements(bool value)
    {
        if (value == true) 
        {
            guiManager.InsertCategory();
            guiManager.ShowGraph();
            exitText.GetComponent<Text>().text = "Click ESC to finish";
        }
        else 
        {
            guiManager.HideGraph();
            guiManager.UpdateFiberPanel(SensorsFromNetwork.ToList());
            exitText.GetComponentInChildren<Toggle>().isOn = false;
        }
        menuPanel.SetActive(!value);
        startButton.SetActive(!value);
        exitText.SetActive(value);
        menuImportPanel.SetActive(!value);
        samplingRateLabel.SetActive(value);
        samplingRateInputField.SetActive(value);
    }

    public void UpdateGraph() {
        guiManager.UpdateCategory();
    }


    /// <summary>
    /// Init the monitored object by initialize all the sensors on its surface
    /// </summary>
    void InitMonitoredObject () {
        if (monitoredObject != null) {
            monitoredObject.SetSensor(defaultProperties);
        }
    }

    /// <summary>
    /// Manage the import status
    /// </summary>
    /// <param name="obj">The new object to import on the scene</param>
    public void ManageImportMonitoredObject (GameObject obj) {
        statusGame = Status.IMPORT;
        menuPanel.SetActive(false);
        startButton.SetActive(false);
        exitText.SetActive(true);
        exitText.GetComponent<Text>().text = "Click ESC to cancel import";
        exitText.GetComponentInChildren<Toggle>().gameObject.SetActive(false);

        monitoredObject.OpenNewMonitoredObject(obj);
    }

    /// <summary>
    /// Manage in the import status the rotation of the object
    /// </summary>
    /// <param name="input">The new rotation value</param>
    public void ManageRotationMonitoredObject (TMP_InputField input) {
        monitoredObject.UpdateRotation(input);
    }

    /// <summary>
    /// Manage in the import status the scale of the object
    /// </summary>
    /// <param name="input">The new size</param>
    public void ManageScaleMonitoredObject (Slider input) {
        monitoredObject.UpdateScale(input);
    }

    /// <summary>
    /// Confirm the imported object
    /// </summary>
    public void ConfirmImportMonitoredObject ()
    {
        ClearSensorsInfo();
        monitoredObject.ConfirmModel();
        menuPanel.SetActive(true);

        exitText.GetComponentInChildren<Toggle>(true).gameObject.SetActive(true);
        CurrentSensorWavelength = new KeyValuePair<UInt64, float>[64];
        exitText.SetActive(false);

        if (SetConfiguration) {
            guiManager.ToggleMeasureConfigurationMenu(true);
            guiManager.ToggleSensorConfigurationMenu(true);
        }
        statusGame = Status.MENU;

        //Checks if the IP and other connection data were filled.
        Dropdown dropD = GameObject.Find("DropdownNetwork").GetComponent<Dropdown>();
        Text IPErrorMessage = GameObject.Find("IPErrorMessage").GetComponent<Text>();
        Text portErrorMessage = GameObject.Find("PortErrorMessage").GetComponent<Text>();

        // TCP Case: checks if iP and port are valid.
        if (dropD.value == 1) {
            //If the IP address is also valid, verifies, in the MongoDB case, that a Database Name and a Collection Name are present and do not contain white spaces.
            if ((IPErrorMessage.text.Equals("The IP address is valid")) && (portErrorMessage.text.Equals("The port is valid"))) {
                startButton.SetActive(true);
            }
        }
        // MongoDB Case: check also if database name and collection name are valid.
        else if (dropD.value == 0) {
            Text DBNameErrorMessage = GameObject.Find("DBNameErrorMessage").GetComponent<Text>();
            Text collNameErrorMessage = GameObject.Find("CollNameErrorMessage").GetComponent<Text>();
            //If the IP address is also valid, verifies, in the MongoDB case, that a Database Name and a Collection Name are present and do not contain white spaces.
            if ((IPErrorMessage.text.Equals("The IP address is valid")) && (portErrorMessage.text.Equals("The port is valid")) && (DBNameErrorMessage.text.Equals("The database name is valid")) && (collNameErrorMessage.text.Equals("The collection name is valid"))) {
                startButton.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Stop the import operation
    /// </summary>
    public void CancelImport () {
        monitoredObject.CancelImport();

        menuPanel.SetActive(true);
        startButton.SetActive(true);
        exitText.GetComponentInChildren<Toggle>(true).gameObject.SetActive(true);
        exitText.SetActive(false);
        statusGame = Status.MENU;
        guiManager.ResetImportMenu();
        if (SetConfiguration) {
            guiManager.ToggleMeasureConfigurationMenu(true);
            guiManager.ToggleSensorConfigurationMenu(true);
        }
    }

    public void SetModelExist (bool value) {
        modelChosen = value;
        if(value && SetConfiguration) {
            guiManager.ToggleMeasureConfigurationMenu(true);
            guiManager.ToggleSensorConfigurationMenu(true);
        }
    }

    public bool GetModelExist ()
    {
        return modelChosen;
    }

    /// <summary>
    /// Get the name of the current monitored object
    /// </summary>
    /// <returns></returns>
    public string GetNameMonitoredObject() {
        return monitoredObject.name;
    }

    /// <summary>
    /// Set the toggle button
    /// </summary>
    public void SetShowAllToggle () {
        monitoredObject.SetShowAllToggle();
    }

    #endregion

}
