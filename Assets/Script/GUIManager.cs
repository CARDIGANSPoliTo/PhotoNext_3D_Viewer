using System;

using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;


using UnityEngine;
using UnityEngine.UI;
using GracesGames.SimpleFileBrowser.Scripts;
using Dummiesman;
using System.Globalization;
using ChartAndGraph;
using TMPro;

public class GUIManager : MonoBehaviour
{
    private GameObject MenuRoot = null;

    // Menu Network variables
    //----------------------------------------
    [HideInInspector]
    public GameObject MenuNetwork = null;
    [HideInInspector]
    public GameObject ContentSN = null;
    [HideInInspector]
    public GameObject ContentSC = null;
    [HideInInspector]
    public GameObject ContentMC = null;
    [HideInInspector]
    public GameObject ContentMI = null;
    [HideInInspector]
    public GameObject ContentMGL = null;
    [HideInInspector]
    public GameObject[] contents;

    // Network variables
    //-------------------------------------------------
    private Button saveButton = null;
    private Dropdown networkDropdown = null;
    private InputField IP = null;
    private InputField port = null;
    private InputField DBName = null;
    private InputField collName = null;
    private Text IPErrorMessage = null;
    private Text portErrorMessage = null;
    private Text DBNameErrorMessage = null;
    private Text collNameErrorMessage = null;

    // Menu Sensor variables
    //----------------------------------------
    [SerializeField]
    public GameObject prefabFiber;
    [SerializeField]
    public GameObject FileBrowserPrefab;

    // Open file browser variables
    //----------------------------------------
    [HideInInspector]
    public string[] fileExtentions = new string[] { "obj" };


    public Material pointMaterial, fillMaterial;
    private double lineThickness = 2.0, pointSize = 5.0;
    private bool stetchFill = false;
    //bool isChartHide = false;
    private ulong countPoint = 0;
    private uint maxCount = 20000;

    // Monitored Object Configuration variables
    //-------------------------------------------------
    private MonitoredObject monitoredObject = null;
    private GameObject startButton = null;


    void Awake ()
    {
        // Get all elements if they are null
        if (MenuRoot == null)
            MenuRoot = GameObject.Find("Menu");

        // Get Network Configuration menu root and sub elements
        if (MenuNetwork == null) 
        {
            MenuNetwork = MenuRoot.transform.Find("ServerNetwork Configuration").gameObject;
            ContentSN = MenuNetwork.transform.Find("ContentSN").gameObject;            
        }

        saveButton = GameObject.Find("ConfirmConfiguration").GetComponent<Button>();
        networkDropdown = ContentSN.transform.Find("DropdownNetwork").GetComponent<Dropdown>();
        IP = GameObject.Find("InputFieldIP").GetComponent<InputField>();
        port = GameObject.Find("InputFieldPort").GetComponent<InputField>();
        DBName = ContentSN.transform.Find("InputFieldDBName").GetComponent<InputField>();
        collName = ContentSN.transform.Find("InputFieldCollectionName").GetComponent<InputField>();
        IPErrorMessage = GameObject.Find("IPErrorMessage").GetComponent<Text>();
        portErrorMessage = GameObject.Find("PortErrorMessage").GetComponent<Text>();
        DBNameErrorMessage = GameObject.Find("DBNameErrorMessage").GetComponent<Text>();
        collNameErrorMessage = GameObject.Find("CollNameErrorMessage").GetComponent<Text>();

        // Get Sensor Configuration menu root and sub elements
        if (ContentSC == null) 
        {
            ContentSC = MenuRoot.transform.Find("Sensor Configuration").Find("ContentConfiguration").gameObject;
            GameObject go = ContentSC.transform.Find("Sensors Data").Find("ScrollViewData").Find("Viewport").gameObject;
            ScrollRect[] contentScrolls = go.GetComponentsInChildren<ScrollRect>();
            contents = new GameObject[contentScrolls.Length];

            for (uint i = 0; i < contentScrolls.Length; i++)
                contents[i] = contentScrolls[i].content.gameObject;
        }

        // Get Measurement Configuration menu root and sub elements
        if (ContentMC == null) 
        {
            ContentMC = MenuRoot.transform.Find("Measurement Configuration").gameObject;
        }

        if (ContentMI == null) 
        {
            ContentMI = GameObject.Find("MenuImport").gameObject;
        }

        if (ContentMGL == null) 
        {
            ContentMGL = GameObject.Find("ChartMenu").gameObject;
        }

        // Create for each channel 16 input blocks with idle and max variation filed
        for (int i = 0; i < contents.Length; i++) {
            for (int j = 0; j < 16; j++) {
                GameObject fiberElement = Instantiate(prefabFiber);
                fiberElement.name = "FiberPanel_" + i + "_Sensor_" + j;

                Transform text = fiberElement.transform.Find("IDFiber");
                text.gameObject.GetComponent<Text>().text = Convert.ToString(j + 1);

                InputField idle = fiberElement.transform.Find("IdleWL").GetComponent<InputField>();
                InputField variation = fiberElement.transform.Find("VariationWL").GetComponent<InputField>();
                idle.onEndEdit.AddListener(delegate { OnInputChange(fiberElement.name, idle); });
                variation.onEndEdit.AddListener(delegate { OnInputChange(fiberElement.name, variation); });

                fiberElement.transform.SetParent(contents[i].transform);
            }
        }

        // Initialize monitored object variables.
        monitoredObject = GameObject.Find("MonitoredObject").GetComponent<MonitoredObject>();
        startButton = GameObject.Find("Canvas").transform.Find("ButtonStartSimulation").gameObject;
    }

    void Start ()
    {

        // Disable the sensor and measure menu
        if (!GameManager.instance.SetConfiguration) {
            InputField[] listif = ContentSC.GetComponentsInChildren<InputField>();
            Dropdown dropdownMeasure = ContentMC.GetComponentInChildren<Dropdown>();
            Button[] buttons = ContentSC.GetComponentsInChildren<Button>();
            Slider sliderMeasure = ContentMC.GetComponentInChildren<Slider>();

            listif.ToList().ForEach(e => {
                e.enabled = !e.enabled;
                e.gameObject.GetComponent<Image>().color = e.enabled ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1);
            });

            buttons.ToList().ForEach(e => {
                e.enabled = !e.enabled;
                e.gameObject.GetComponent<Image>().color = e.enabled ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1);
            });

            dropdownMeasure.enabled = !dropdownMeasure.enabled;
            dropdownMeasure.GetComponent<Image>().color = dropdownMeasure.enabled ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1);
            sliderMeasure.enabled = !sliderMeasure.enabled;
            sliderMeasure.GetComponentsInChildren<Image>().ToList().ForEach(e => {
                e.color = sliderMeasure.enabled ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1);
            });
        }
    }

    //private void Update ()
    //{
        //if (GameManager.instance.statusGame == Status.MONITORING && !isChartHide)
        //    UpdateCategory();
    //}

    #region GRATING_GUI_MANAGING
    /// <summary>
    /// Manage the InputField's grating  
    /// </summary>
    /// <param name="inputFieldName">Name of the input panel</param>
    /// <param name="inputFild">InputPanel object</param>
    public void OnInputChange (string inputFieldName, InputField inputFild)
    {

        string[] pieces = inputFieldName.Split('_');
        Debug.Log(inputFieldName);
        int channel = Convert.ToInt32(pieces[1]);
        int id = Convert.ToInt32(pieces[3]);
        Sensor? sn = GameManager.instance.GetSensorInfo(channel, id);
        Sensor s;
        if (sn == null) {
            if (GameManager.instance.measurementType == Measurement.TEMPERATURE)
                s = new Sensor(id, 0.0f, 0.0f, true); //GameManager.instance.defaultVarTemp
            else
                s = new Sensor(id, 0.0f, 0.0f, true); //GameManager.instance.defaultVarDisp
            s.Channel = channel;
        }
        else
            s = sn.Value;

        //float f;
        //if (!float.TryParse(inputFild.text, out f)) {
        //    inputFild.text = "";
        //    return;
        //}

        if (!inputFild.text.Equals("")) {
            float f;
            float.TryParse(inputFild.text, NumberStyles.Any, CultureInfo.CurrentUICulture, out f);
            float.TryParse(inputFild.text.Replace('.', ','), NumberStyles.Any, CultureInfo.CurrentCulture, out f);
            float.TryParse(inputFild.text, NumberStyles.Any, CultureInfo.InvariantCulture, out f);

            if (inputFild.name.Equals("IdleWL")) {
                s.WavelenghtIdle = f;//Convert.ToSingle(inputFild.text);
                if (s.MaxVariation == 0.0f) s.MaxVariation = 0.0f;// GameManager.instance.measurementType == Measurement.TEMPERATURE ?
                                                                  // GameManager.instance.defaultVarTemp : GameManager.instance.defaultVarDisp;
            }
            if (inputFild.name.Equals("VariationWL"))
                s.MaxVariation = f;//Convert.ToSingle(inputFild.text);
            s.Active = true;
        }
        else 
        {
            if (inputFild.name.Equals("IdleWL"))
                s.WavelenghtIdle = 0.0f;
            if (inputFild.name.Equals("VariationWL"))
                s.MaxVariation = 0.0f;
            if (s.WavelenghtIdle == 0.0f && s.MaxVariation == 0.0f)
                s.Active = false;
            s.Active = false;
        }

        GameManager.instance.UpdateSensorInfo(s);
        Debug.Log(id + " " + channel + " change value");
    }

    /// <summary>
    /// Update the fiber panel in case of a configuration packet is received
    /// </summary>
    /// <param name="SensorsFromNetwork">List of the sensors</param>
    public void UpdateFiberPanel (List<Sensor> SensorsFromNetwork)
    {
        for (int i = 0; i < contents.Length; i++) {
            for (int j = 0; j < 16; j++) {
                Transform fiberPanel = contents[i].transform.GetChild(j);

                if (SensorsFromNetwork[i * 16 + j].Active) {
                    InputField idle = fiberPanel.transform.Find("IdleWL").GetComponent<InputField>();
                    InputField variation = fiberPanel.transform.Find("VariationWL").GetComponent<InputField>();
                    idle.text = SensorsFromNetwork[i * 16 + j].WavelenghtIdle.ToString().Replace(',', '.');
                    variation.text = SensorsFromNetwork[i * 16 + j].MaxVariation.ToString().Replace(',', '.');
                }
            }
        }
    }

    /// <summary>
    /// Update the fiber panel in case of a configuration packet is received
    /// </summary>
    /// <param name="SensorsFromNetwork">List of the sensors</param>
    public void ClearFiberPanel ()
    {
        for (int i = 0; i < contents.Length; i++) {
            for (int j = 0; j < 16; j++) {
                Transform fiberPanel = contents[i].transform.GetChild(j);
                InputField idle = fiberPanel.transform.Find("IdleWL").GetComponent<InputField>();
                InputField variation = fiberPanel.transform.Find("VariationWL").GetComponent<InputField>();
                idle.text = "";
                variation.text = "";

            }
        }
    }

    /// <summary>
    /// Actuvate/Deactivate sensor configuration menu
    /// </summary>
    /// <param name="value">Dropdown menu value</param>
    public void ToggleSensorConfigurationMenu (bool value)
    {
        InputField[] listif = ContentSC.GetComponentsInChildren<InputField>();
        listif.ToList().ForEach(e => {
            e.enabled = value;
            e.gameObject.GetComponent<Image>().color = e.enabled ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1);
        });

        Button[] buttons = ContentSC.GetComponentsInChildren<Button>();
        buttons.ToList().ForEach(e => {
            e.enabled = value;
            e.gameObject.GetComponent<Image>().color = e.enabled ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1);
        });


    }
    #endregion 

    #region NETWORKCONFIG_GUI_MANAGING
    /// <summary>
    /// Set the current configuration on GameManager
    /// </summary>
    /// <param name="button">Button object</param>
    public void ConfirmNetworkConfiguration (GameObject button)
    {
        Text textButton = button.transform.GetComponentInChildren<Text>();
        InputField[] listif = ContentSN.GetComponentsInChildren<InputField>();
        //Dropdown dropdownMeasure = ContentMC.GetComponentInChildren<Dropdown>();
        //Slider sliderMeasure = ContentMC.GetComponentInChildren<Slider>();

        //Save the network information.
        if (networkDropdown.value == 1) 
        {
            GameManager.instance.UpdateTcpInfomation(IP.text, Convert.ToInt32(port.text));
        }
        else if (networkDropdown.value == 0) 
        {
            GameManager.instance.UpdateMongoInfomation(IP.text, Convert.ToInt32(port.text), DBName.text, collName.text);
        }
        //Antonio: add informations for the changestream manager
        else if (networkDropdown.value == 2)
        {
            GameManager.instance.UpdateChangeStreamInformation(IP.text, Convert.ToInt32(port.text), DBName.text, collName.text);
        }

        if (!GameManager.instance.SetConfiguration) 
        {
            textButton.text = "Cancel";
            EnableStartButton(true);
        }           
        else 
        {
            textButton.text = "Save";
            EnableStartButton(false);
        }            
        if (!GameManager.instance.ConfirmNetworkConfiguration()) 
        {
            textButton.text = "Save";
            EnableStartButton(false);
            return;
        }

        // Deactivate the network configuration GUI.

        listif.ToList().ForEach(e => {
            e.enabled = !e.enabled;
            e.gameObject.GetComponent<Image>().color = e.enabled ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1);
        });

        networkDropdown.enabled = !networkDropdown.enabled;
        networkDropdown.gameObject.GetComponent<Image>().color = networkDropdown.enabled ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1);

        if (GameManager.instance.modelChosen) {
            if (GameManager.instance.SetConfiguration) {
                ToggleMeasureConfigurationMenu(true);
                ToggleSensorConfigurationMenu(true);
            }
            else {
                ToggleMeasureConfigurationMenu(false);
                ToggleSensorConfigurationMenu(false);
            }

            // Activate sensor configuration gui
            //listif = ContentSC.GetComponentsInChildren<InputField>();
            //listif.ToList().ForEach(e => {
            //    e.enabled = !e.enabled;
            //    e.gameObject.GetComponent<Image>().color = e.enabled ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1);
            //});
            //Button[] buttons = ContentSC.GetComponentsInChildren<Button>();
            //buttons.ToList().ForEach(e => {
            //    e.enabled = !e.enabled;
            //    e.gameObject.GetComponent<Image>().color = e.enabled ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1);
            //});

            //// Activate the measurement configuration
            //dropdownMeasure.enabled = !dropdownMeasure.enabled;
            //dropdownMeasure.GetComponent<Image>().color = dropdownMeasure.enabled ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1);

            //sliderMeasure.enabled = !sliderMeasure.enabled;
            //sliderMeasure.GetComponentsInChildren<Image>().ToList().ForEach(e => {
            //    e.color = sliderMeasure.enabled ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1);
            //});
        }
    }

    public void ToggleMeasureConfigurationMenu (bool value)
    {
        Dropdown dropdownMeasure = ContentMC.GetComponentInChildren<Dropdown>();
        Slider sliderMeasure = ContentMC.GetComponentInChildren<Slider>();

        // Activate the measurement configuration
        dropdownMeasure.enabled = value;
        dropdownMeasure.GetComponent<Image>().color = dropdownMeasure.enabled ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1);

        sliderMeasure.enabled = value;
        sliderMeasure.GetComponentsInChildren<Image>().ToList().ForEach(e => {
            e.color = sliderMeasure.enabled ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1);
        });

    }

    /// <summary>
    /// Change GUI depending on the type of network configuration
    /// </summary>
    /// <param name="value">Dropdown menu value</param>
    public void ToggleNetworkConfigurationMenu (bool value)
    {
        InputField[] listif = ContentSN.GetComponentsInChildren<InputField>();

        // Enable mongo input field
        listif.ToList().ForEach(e => 
        {
            e.enabled = value;
            e.gameObject.GetComponent<Image>().color = e.enabled ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1);
        });

        networkDropdown.enabled = value;
        networkDropdown.gameObject.GetComponent<Image>().color = networkDropdown.enabled ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1);

        Button button = ContentSN.GetComponentInChildren<Button>();
        button.enabled = value;
        button.GetComponent<Image>().color = button.enabled ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1);
    }

    /// <summary>
    /// Verifies if the IP address is valid in real time: if true, and the other options are valid, enables the "Start" button.
    /// </summary>
    public void VerifyIPAddress ()
    {
        //Strong IPV4 address verification with Regex.
        if (Regex.IsMatch(IP.text, @"^(([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])\.){3}([01]?[0-9]?[0-9]|2[0-4][0-9]|25[0-5])$")) {
            IPErrorMessage.color = new Color(0, 0.392157f, 0); // Dark green.
            IPErrorMessage.text = "The IP address is valid";

            // TCP Case: check only if port is valid.
            if (networkDropdown.value == 1) 
            {
                if ((portErrorMessage.text.Equals("The port is valid"))) 
                {
                    saveButton.interactable = true;
                }
                else 
                {
                    saveButton.interactable = false;
                }
            }
            // MongoDB Case: check if port, database name and collection name are valid.
            else if (networkDropdown.value == 0) 
            {
                if (portErrorMessage.text.Equals("The port is valid") && (DBNameErrorMessage.text.Equals("The database name is valid")) && (collNameErrorMessage.text.Equals("The collection name is valid"))) 
                {
                    saveButton.interactable = true;
                }
                else 
                {
                    saveButton.interactable = false;
                }
            }
            else if (networkDropdown.value == 2)
            {
                saveButton.interactable = true;
            }
        }
        else 
        {
            IPErrorMessage.color = Color.red;
            IPErrorMessage.text = "The IP address is not valid";
            saveButton.interactable = false;
        }
    }

    /// <summary>
    /// Verifies if the port is valid in real time: if true, and the other options are valid, enables the "Start" button.
    /// </summary>
    public void VerifyPort ()
    {
        int portNumber = 0;

        if (int.TryParse(port.text, out portNumber))
        {
            if (portNumber >= 0 && portNumber <= 65535) 
            {
                portErrorMessage.color = new Color(0, 0.392157f, 0); // Dark green.
                portErrorMessage.text = "The port is valid";

                // TCP Case: check only if IP is valid.
                if (networkDropdown.value == 1) 
                {
                    if ((IPErrorMessage.text.Equals("The IP address is valid"))) 
                    {
                        saveButton.interactable = true;                    
                    }
                    else 
                    {
                        saveButton.interactable = false;
                    }
                }
                // MongoDB Case: check if IP, database name and collection name are valid.
                else if (networkDropdown.value == 0) 
                {
                    if (IPErrorMessage.text.Equals("The IP address is valid") && (DBNameErrorMessage.text.Equals("The database name is valid")) && (collNameErrorMessage.text.Equals("The collection name is valid"))) 
                    {
                        saveButton.interactable = true;
                    }
                    else 
                    {
                        saveButton.interactable = false;
                    }
                }
            }
            else 
            {
                portErrorMessage.color = Color.red;
                portErrorMessage.text = "The port is not valid";
                saveButton.interactable = false;
            }
        }
        else 
        {
            portErrorMessage.color = Color.red;
            portErrorMessage.text = "The port is not valid";
            saveButton.interactable = false;
        }
    }

    /// <summary>
    /// Verifies if the database name is valid in real time: if true, and the other options are valid, enables the "Start" button.
    /// </summary>
    public void VerifyDBName ()
    {
        if ((!DBName.text.Equals("")) && (!DBName.text.Contains(" "))) 
        {
            DBNameErrorMessage.color = new Color(0, 0.392157f, 0); // Dark green.
            DBNameErrorMessage.text = "The database name is valid";

            //If the IP address is also valid, verifies, in the MongoDB case, that a Database Name and a Collection Name are present and do not contain white spaces.
            if ((IPErrorMessage.text.Equals("The IP address is valid")) && (portErrorMessage.text.Equals("The port is valid")) && (collNameErrorMessage.text.Equals("The collection name is valid"))) 
            {
                saveButton.interactable = true;
            }
        }
        else
        {
            DBNameErrorMessage.color = Color.red;
            DBNameErrorMessage.text = "The database name is not valid";
            saveButton.interactable = false;
        }
    }

    /// <summary>
    /// Verifies if the collection name is valid in real time: if true, and the other options are valid, enables the "Start" button.
    /// </summary>
    public void VerifyCollectionName ()
    {
        if ((!collName.text.Equals("")) && (!collName.text.Contains(" ")))
        {
            collNameErrorMessage.color = new Color(0, 0.392157f, 0); // Dark green.
            collNameErrorMessage.text = "The collection name is valid";

            //If the IP address is also valid, verifies, in the MongoDB case, that a Database Name and a Collection Name are present and do not contain white spaces.
            if ((IPErrorMessage.text.Equals("The IP address is valid")) && (portErrorMessage.text.Equals("The port is valid")) && (DBNameErrorMessage.text.Equals("The database name is valid"))) 
            {
                saveButton.interactable = true;
            }
        }
        else 
        {
            collNameErrorMessage.color = Color.red;
            collNameErrorMessage.text = "The collection name is not valid";
            saveButton.interactable = false;
        }
    }

    /// <summary>
    /// Check if the model has been chosen in order to enable the start button
    /// </summary>
    private void EnableStartButton(bool value)
    {
        if (GameManager.instance.GetModelExist()) 
        {
            startButton.SetActive(value);
        }
    }

    #endregion

    #region BROWSER_METHODS

    /// <summary>
    /// Call the File Browser prefab and instantiate it
    /// </summary>
    public void CallerFileBrowser ()
    {
        // Create the file browser and name it
        GameObject.Find("Canvas").transform.GetComponentsInChildren<Image>(true).Where(e => e.name.Equals("BlockPanel")).ToList()[0].gameObject.SetActive(true);

        GameObject fileBrowserObject = Instantiate(FileBrowserPrefab, transform);
        fileBrowserObject.name = "FileBrowser";

        // Set the mode to save or load
        FileBrowser fileBrowserScript = fileBrowserObject.GetComponent<FileBrowser>();
        fileBrowserScript.SetupFileBrowser(ViewMode.Landscape);
        fileBrowserScript.OpenFilePanel(fileExtentions);

        // Subscribe to OnFileSelect event (call LoadFileUsingPath using path) 
        fileBrowserScript.OnFileSelect += LoadFileUsingPath;
        fileBrowserScript.OnFileBrowserClose += CloseBrowser;
    }

    /// <summary>
    /// Load a file if the path is not null
    /// </summary>
    /// <param name="path">Path of the selected file</param>
    private void LoadFileUsingPath (string path)
    {
        if (path != null) {
            if (path.Length != 0) {
                GameObject obj = new OBJLoader().Load(path);
                GameManager.instance.ManageImportMonitoredObject(obj);

                Button button = ContentMI.transform.Find("ButtonImport").GetComponentInChildren<Button>();
                button.gameObject.SetActive(false);
                button = ContentMI.transform.Find("ButtonPrefab").GetComponentInChildren<Button>();
                button.gameObject.SetActive(false);
                TMP_Dropdown dropdown = ContentMI.transform.GetComponentInChildren<TMP_Dropdown>(true);
                dropdown.gameObject.SetActive(false);

                GameObject submenu = ContentMI.transform.Find("ConfigMenu").gameObject;
                submenu.SetActive(true);
            }
            else {
                GameObject errorLog = GameObject.Find("InformationPanel");
                errorLog.transform.GetChild(0).GetComponent<Text>().text = "An invalid path has been given";
            }
        }
        else {
            GameObject errorLog = GameObject.Find("InformationPanel");
            errorLog.transform.GetChild(0).GetComponent<Text>().text = "No file has been selected";
        }
        GameObject.Find("Canvas").transform.GetComponentsInChildren<Image>(true).Where(e => e.name.Equals("BlockPanel")).ToList()[0].gameObject.SetActive(false);
    }

    /// <summary>
    /// In case the browser is closed the black panel is deactivate
    /// </summary>
    private void CloseBrowser ()
    {
        GameObject.Find("Canvas").transform.GetComponentsInChildren<Image>(true).Where(e => e.name.Equals("BlockPanel")).ToList()[0].gameObject.SetActive(false);
    }

    /// <summary>
    /// Reset import menu Gui
    /// </summary>
    public void ResetImportMenu ()
    {
        Button[] button = ContentMI.transform.GetComponentsInChildren<Button>(true);
        button.ToList().ForEach(b => {
            b.gameObject.SetActive(true);
        });
        TMP_Dropdown dropdown = ContentMI.transform.GetComponentInChildren<TMP_Dropdown>(true);
        dropdown.gameObject.SetActive(true);

        GameObject submenu = ContentMI.transform.Find("ConfigMenu").gameObject;
        submenu.SetActive(false);
    }

    public void LoadDefaultPrefab (TMP_Dropdown value)
    {
        string path = "Prefab\\Models\\" + value.options[value.value].text;
        GameObject go = Instantiate(Resources.Load(path)) as GameObject;
        if (go == null) ResetImportMenu();
        else {
            GameManager.instance.ManageImportMonitoredObject(go);
            GameObject.Find("Canvas").transform.GetComponentsInChildren<Image>(true).Where(e => e.name.Equals("BlockPanel")).ToList()[0].gameObject.SetActive(false);

            RectTransform rectT = ContentMI.GetComponent<RectTransform>();
            rectT.sizeDelta = new Vector2(rectT.rect.width, 150);

            Button[] button = ContentMI.transform.GetComponentsInChildren<Button>();
            button.ToList().ForEach(b => {
                b.gameObject.SetActive(false);
            });

            value.gameObject.SetActive(false);

            GameObject submenu = ContentMI.transform.Find("ConfigMenu").gameObject;

            submenu.SetActive(true);
        }
    }

    #endregion

    #region GRAPH_METHODS

    UInt64 ts = 0;
    UInt64 baseTimestamp = 0;

    /// <summary>
    /// Insert a new category on the Line graph
    /// </summary>
    public void InsertCategory ()
    {
        GraphChartBase graph = ContentMGL.GetComponentInChildren<GraphChartBase>(true);
        MaterialTiling lineTiling = new MaterialTiling(true, 10);
        int countActiveSensor = 0;
        countPoint = 0;
        graph.DataSource.AutomaticHorizontalView = true;

        if (graph != null) {
            graph.DataSource.StartBatch();
            List<Sensor> sensors = GameManager.instance.GetSensorInfo();//.Where(e => e.Active).ToList();
            sensors.ForEach(e => {
                string nameCategory = $"Ch{(e.Channel + 1)}Gr{(e.SensorID + 1)}";
                if (e.Active) {
                    Color color = UnityEngine.Random.ColorHSV();
                    Material newmat = new Material(pointMaterial);
                    newmat.color = color;
                    countActiveSensor++;
                    try {
                        graph.DataSource.AddCategory(nameCategory, newmat, lineThickness, lineTiling, fillMaterial, stetchFill, newmat, pointSize);
                    }
                    catch (Exception) {
                        Debug.Log("Category already exists");
                        graph.DataSource.ClearCategory(nameCategory);
                    }
                }
                else {
                    graph.DataSource.RemoveCategory(nameCategory);
                }
            });
            if (sensors.Where(s => s.Active).Count() > 40) maxCount = 200;
            else maxCount = 20000;
            ts = 0;
            baseTimestamp = 0;
            graph.DataSource.EndBatch();
        }
        GameObject legend = ContentMGL.GetComponentInChildren<GridLayoutGroup>(true).gameObject;
        Vector2 sizeCell = legend.GetComponent<GridLayoutGroup>().cellSize;
        RectTransform rectT = legend.GetComponent<RectTransform>();
        rectT.sizeDelta = new Vector2((float)((countActiveSensor / 16) * sizeCell.x + sizeCell.x), (float)((countActiveSensor) < 16 ? (countActiveSensor % 16) * sizeCell.y : sizeCell.y * 16));
    }

    /// <summary>
    /// Update the line graph, a point for each active sensor
    /// </summary>
    public void UpdateCategory ()
    {
        GraphChartBase graph = ContentMGL.GetComponentInChildren<GraphChartBase>(true);
        if (graph == null) return;

        List<KeyValuePair<UInt64, float>> sensors = GameManager.instance.GetCurrentSensorWavelength();
        if (GameManager.instance.GetCurrentSensorWavelength().Where(e => e.Value != 0.0f).Count() <= 0) return;
        UInt64 tm = GameManager.instance.GetCurrentSensorWavelength().Where(e => e.Value != 0.0f).First().Key;

        if (tm > ts) 
        {
            ts = tm;
            if (baseTimestamp == 0) baseTimestamp = ts;

            for (int i = 0; i < sensors.Count; i++) 
            {
                if (sensors[i].Value == 0.0f) continue;


                int channel = i / 16;
                int grating = i % 16;

                string nameCategory = $"Ch{(channel + 1)}Gr{(grating + 1)}";
                UInt64 timeSinceStart = (sensors[i].Key - baseTimestamp) / 400000;
                Sensor sensor = GameManager.instance.SensorsFromNetwork[i];
                graph.DataSource.AddPointToCategoryRealtime(nameCategory, timeSinceStart, (sensors[i].Value - sensor.WavelenghtIdle));
            }
        }
    }

    /// <summary>
    /// Update the line graph, a point for each active sensor
    /// </summary>
    public void UpdateCategory (List<KeyValuePair<UInt64, float>> wav)
    {
        GraphChartBase graph = ContentMGL.GetComponentInChildren<GraphChartBase>(true);
        if (graph == null) return;

        if (wav.Where(e => e.Value != 0.0f).Count() <= 0) return;
        UInt64 tm = wav.Where(e => e.Value != 0.0f).First().Key;

        if (tm > ts) {
            if (ts != 0) {
                DateTimeOffset update = DateTimeOffset.FromUnixTimeMilliseconds(((long)tm / 1000));
                DateTimeOffset old = DateTimeOffset.FromUnixTimeMilliseconds(((long)ts / 1000));
                TimeSpan diff = update - old;
                if (diff.TotalMinutes > 1) {
                    Debug.Log(diff.Hours);
                    return;
                }
            }

            ts = tm;
            if (baseTimestamp == 0) baseTimestamp = ts;
            bool isFirst = false;
            for (int i = 0; i < wav.Count; i++) 
            {
                if (wav[i].Value == 0.0f) continue;
                if (countPoint > maxCount) 
                    {
                    graph.DataSource.AutomaticHorizontalView = false;
                    graph.DataSource.HorizontalViewSize = maxCount;
                }
                else
                {
                    graph.DataSource.AutomaticHorizontalView = true;
                }
                if (!isFirst)
                {
                    isFirst = true;
                    countPoint++;
                }
                int channel = i / 16;
                int grating = i % 16;

                string nameCategory = $"Ch{(channel + 1)}Gr{(grating + 1)}";
                Int64 timeSinceStart = Math.Abs((long)wav[i].Key - (long)baseTimestamp) / 400000;
                Sensor s = GameManager.instance.SensorsFromNetwork[i];
                if (s.WavelenghtIdle <= 0.0f) continue;
                graph.DataSource.AddPointToCategoryRealtime(nameCategory, timeSinceStart, (wav[i].Value - s.WavelenghtIdle));
            }
        }
    }

    /// <summary>
    /// Set the line graph as visible
    /// </summary>
    public void ShowGraph ()
    {
        //isChartHide = false;
        ContentMGL.GetComponentInChildren<GraphChartBase>(true).gameObject.transform.parent.gameObject.SetActive(true);
        Button button = ContentMGL.GetComponentInChildren<Button>(true);
        button.gameObject.SetActive(true);
        button.gameObject.GetComponent<RectTransform>().localPosition = new Vector3(8, 269, 0);
        button.GetComponentInChildren<Text>().text = "Hide\nGraph";
    }

    /// <summary>
    /// Set the line graph as not visible
    /// </summary>
    public void HideGraph ()
    {
        //isChartHide = true;
        ContentMGL.GetComponentInChildren<GraphChartBase>(true).gameObject.transform.parent.gameObject.SetActive(false);

        Button button = ContentMGL.GetComponentInChildren<Button>(true);
        if (GameManager.instance.statusGame == Status.MONITORING)
            button.gameObject.SetActive(true);
        else
            button.gameObject.SetActive(false);


        button.gameObject.GetComponent<RectTransform>().localPosition = new Vector3(8, 10, 0);
        button.GetComponentInChildren<Text>().text = "Show\nGraph";
    }

    public void HideButton (Button button)
    {
        if (button.GetComponentInChildren<Text>().text.Contains("Hide"))
            HideGraph();
        else
            ShowGraph();
    }

    public void UpdateSampleWindow(string sampleWindow)
    {
        GraphChartBase graph = ContentMGL.GetComponentInChildren<GraphChartBase>(true);
        if (graph == null) return;

        try
        {
            maxCount = UInt32.Parse(sampleWindow);
        }catch(FormatException)
        {
            Debug.LogError("Invalid Sample Window");
        }
    }


    #endregion
}
