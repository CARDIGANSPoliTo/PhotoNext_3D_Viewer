using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TextureManager : MonoBehaviour {
    [SerializeField]
    public GUIManager guiManager = null;
    [SerializeField]
    public GradientGUI gradientManager = null;
    [SerializeField]
    public GameObject ColorPicker = null;


    [SerializeField,HideInInspector]
    List<Texture2D> heatmaps = new List<Texture2D>();
    [SerializeField,HideInInspector]
    GameObject previousSelection = null;
    [SerializeField,HideInInspector]
    ConcurrentQueue<Texture2D> heatMapToCreate = new ConcurrentQueue<Texture2D>();

    int sizeNewMap = 16;
    Texture2D defaultMapText;
    const string defaultMap ="heatramp5";


    void Start () {
        Object[] listTexture = Resources.LoadAll("HeatMap");
        
        // Create list of texture as images
        foreach(Object o in listTexture){
            GameObject newMap = new GameObject();
            RectTransform rect = newMap.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(rect.sizeDelta.x+14, 10);

            Image image = newMap.AddComponent<Image>();
            Texture2D ot = (Texture2D) o;
            Sprite sprite = Sprite.Create(ot, new Rect(0, 0, ot.width, ot.height), Vector2.zero);
            image.sprite = sprite;

            Outline outline = newMap.AddComponent<Outline>();
            if (!o.name.Equals(defaultMap))
                outline.enabled = false;
            else
                defaultMapText = ot;        
            EventTrigger trigger = newMap.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener(( data ) => {

            SetOutline(newMap); GameObject.FindObjectOfType<MonitoredObject>().ChangeTexture(ot); });
            trigger.triggers.Add(entry);

            LayoutElement layoutElement = newMap.AddComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1;
            layoutElement.minWidth      = 14;
            layoutElement.preferredWidth = 14;
            
            newMap.transform.SetParent(transform);
            newMap.transform.SetAsFirstSibling();
            GetComponent<RectTransform>().sizeDelta = new Vector2(GetComponent<RectTransform>().sizeDelta.x + sizeNewMap, GetComponent<RectTransform>().sizeDelta.y);
            rect.transform.localScale = Vector3.one;
            heatmaps.Add(ot);
         }
        previousSelection = transform.GetChild(0).gameObject;
    }

    public void SetOutline(GameObject selectedTexture) {
        previousSelection.GetComponent<Outline>().enabled = false;
        selectedTexture.GetComponent<Outline>().enabled = true;
        previousSelection = selectedTexture;
    }

    public void ActivatePickColor () {
        EventTrigger[] listChild = gameObject.GetComponentsInChildren<EventTrigger>();
        listChild.ToList().ForEach(e => {
            e.enabled = !e.enabled;
        });
        ColorPicker.SetActive(true);

        // Blocco tutto sotto chimando Guimanager
        guiManager.ToggleNetworkConfigurationMenu(false);
        guiManager.ToggleSensorConfigurationMenu(false);
    }

    public void DeactivatePickColor() {
        EventTrigger[] listChild = gameObject.GetComponentsInChildren<EventTrigger>();
        listChild.ToList().ForEach(e => {
            e.enabled = !e.enabled;
        });
        ColorPicker.SetActive(false);

        // Blocco tutto sotto chimando Guimanager
        if (!GameManager.instance.SetConfiguration){
            guiManager.ToggleNetworkConfigurationMenu(true);
        }
        else
            guiManager.ToggleSensorConfigurationMenu(true);
    }

    public void AddTexture () {
        heatMapToCreate.Enqueue(gradientManager.customHeatMap.GetTexture(255));
        DeactivatePickColor();

    }

    public void AddNewTexture (Texture2D newTexture){// CustomGradient gradient){// Image SourceTexture) {
        //Texture2D newTexture = gradientManager.customHeatMap.GetTexture(255);// gradient.GetTexture(256);// SourceTexture.sprite.texture;
        GameObject newMap = new GameObject();
        RectTransform rect = newMap.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(rect.sizeDelta.x + 14, 10);

        Image image = newMap.AddComponent<Image>();
        //image.sprite = SourceTexture.sprite;
        Sprite sprite = Sprite.Create(newTexture, new Rect(0, 0, newTexture.width, newTexture.height), Vector2.zero);
        image.sprite = sprite;

        Outline outline = newMap.AddComponent<Outline>();
        outline.enabled = false;

        EventTrigger trigger = newMap.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener(( data ) => {
            Debug.Log(data.selectedObject);
            SetOutline(newMap); GameObject.FindObjectOfType<MonitoredObject>().ChangeTexture(newTexture); });
        trigger.triggers.Add(entry);

        LayoutElement layoutElement = newMap.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1;
        layoutElement.minWidth = 14;
        layoutElement.preferredWidth = 14;

        newMap.transform.SetParent(transform);
        newMap.transform.SetAsFirstSibling();
        GetComponent<RectTransform>().sizeDelta = new Vector2(GetComponent<RectTransform>().sizeDelta.x + sizeNewMap, GetComponent<RectTransform>().sizeDelta.y);
        rect.transform.localScale = Vector3.one;
        heatmaps.Add(newTexture);
        SetOutline(newMap);
        GameObject.FindObjectOfType<MonitoredObject>().ChangeTexture(newTexture);
    }

    void Update () {
        if(heatMapToCreate.Count > 0) {
            while(heatMapToCreate.Count > 0) {
                Texture2D texture;
                if(heatMapToCreate.TryDequeue(out texture))
                    AddNewTexture(texture);
            }
        }
    }

    void OnApplicationQuit () {
        GameObject.FindObjectOfType<MonitoredObject>().ChangeTexture(defaultMapText);
    }
}
