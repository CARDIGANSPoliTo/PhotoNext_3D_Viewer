using System.Linq;

using UnityEngine;
using UnityEngine.UI;

public class GradientGUI : MonoBehaviour {
    [SerializeField]
    public Image sliderBG = null;
    [SerializeField]
    public Slider slider = null;
    [SerializeField]
    public GameObject colorChoose;
    [SerializeField]
    public RectTransform cursor;
    [SerializeField]
    public GameObject defaultSelected;

    [SerializeField,HideInInspector]
    public CustomGradient customHeatMap = new CustomGradient();
    
    [SerializeField,HideInInspector]
    RectTransform rectObj     = null;
    [SerializeField,HideInInspector]
    Image      pickerImage    = null;
    [SerializeField,HideInInspector]
    GameObject selectedPicker = null;

    int indexPicker;

    void Start () {
        Sprite        sprite    = null;
        RectTransform rect      = null;

        Texture2D textureColor  = new Texture2D(360,1);
        Color[]   matrixColor   = new Color[360];

        Color startColor        = Color.white;

        float color;

        // Create all color gradient
        rectObj = GetComponent<RectTransform>();
        for (int i = 0; i < 360; i++)
            matrixColor[i] = Color.HSVToRGB((float)i / 360, 1, 1);
        textureColor.SetPixels(matrixColor);
        textureColor.Apply();

        // Apply gradient texture to slider
        sprite = Sprite.Create(textureColor, new Rect(0, 0, 360, 1), Vector2.zero);
        pickerImage = GetComponent<Image>();
        sliderBG.sprite = sprite;
        rect = colorChoose.GetComponent<RectTransform>();
        color = ((float)rect.rect.width / 8) / (float)rect.rect.width;

        // Create two additional key for the gradient
        customHeatMap.AddKey(Color.white, color);
        customHeatMap.AddKey(Color.white, color * 2);
        customHeatMap.AddKey(Color.white, color * 3);
        customHeatMap.AddKey(Color.white, color * 4);
        customHeatMap.AddKey(Color.white, color * 5);
        customHeatMap.AddKey(Color.white, color * 6);
        customHeatMap.AddKey(Color.white, color * 7);
        customHeatMap.AddKey(Color.white, color * 8);

        // Apply default gradient image to be display
        sprite = Sprite.Create(CalculateTexture(0), new Rect(0, 0, 255, 255), Vector2.zero);
        pickerImage.sprite = sprite;
        selectedPicker = defaultSelected; ;
    }

    #region  GRADIENTGUI_METHODS
    /// <summary>
    /// Select color based on its index inside the gradient
    /// </summary>
    /// <param name="index">Index</param>
    public void SelectedColor ( int index ) {
        indexPicker = index;
    }

    /// <summary>
    /// Selected Color is displated by a black outline
    /// </summary>
    /// <param name="selected">Selected object</param>
    public void SelectedColor ( GameObject selected ) {
        Outline outlinePrev = selectedPicker.GetComponent<Outline>();
        outlinePrev.enabled = false;
        Outline outlineSelected = selected.GetComponent<Outline>();
        outlineSelected.enabled = true;
        selectedPicker = selected;

    }

    /// <summary>
    /// Update gradient texture displated
    /// </summary>
    public void UpdateTexture () {
        Texture2D texture  = CalculateTexture(slider.value);
        Sprite    sprite   = Sprite.Create(texture, new Rect(0, 0, 255, 255), Vector2.zero);
        pickerImage.sprite = sprite;
    }

    /// <summary>
    /// Calculate texture based on hue value
    /// </summary>
    /// <param name="hue">hue value [0,1]</param>
    /// <returns>Texture gradient of the input hue</returns>
    Texture2D CalculateTexture ( float hue ) {
        Texture2D   texture      = new Texture2D(255, 255);
        Color[]     matrixPixels = new Color[texture.width * texture.height];

        for (int j = 0; j < 255; j++)
            for (int k = 0; k < 255; k++)
                matrixPixels[j * 255 + k] = Color.HSVToRGB(hue, (float)k / 255, (float)j / 255);

        texture.SetPixels(matrixPixels);
        texture.Apply();
        return texture;
    }

    /// <summary>
    /// Pick color on displayed texture
    /// </summary>
    public void PickColor () {
        RectTransform rect = GetComponent<RectTransform>();
        Color colorPicked;
        RectTransform rectColor;
        Texture2D texutureColor;
        Sprite      spriteColor;
        Image        imageColor;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, Input.mousePosition, null, out Vector2 localCursor)) {
            cursor.position = Input.mousePosition;

            // Get cursor position on the image
            localCursor.x += rect.rect.width / 2;
            localCursor.y += rect.rect.height / 2;
            localCursor.x = Mathf.Abs(localCursor.x);
            localCursor.y = Mathf.Abs(localCursor.y);
            if (localCursor.x >= 256) localCursor.x = 255;
            if (localCursor.y >= 256) localCursor.y = 255;

            // Get color of the pixel based on the localCursor coordinate;
            colorPicked = pickerImage.sprite.texture.GetPixel((int)localCursor.x, (int)localCursor.y);
            customHeatMap.UpdateKeyColor(indexPicker, colorPicked);

            // Create texture and update information on the gradient
            rectColor = colorChoose.GetComponent<RectTransform>();
            texutureColor = customHeatMap.GetTexture(255);
            spriteColor = Sprite.Create(texutureColor, new Rect(0, 0, texutureColor.width, texutureColor.height), Vector2.zero);
            imageColor = colorChoose.GetComponent<Image>();

            imageColor.sprite = spriteColor;
            selectedPicker.GetComponent<Image>().color = colorPicked;
        }

    }

    /// <summary>
    /// Clear gradient data
    /// </summary>
    public void ClearData () {
        customHeatMap.UpdateKeyColor(0, Color.white);
        customHeatMap.UpdateKeyColor(1, Color.white);
        customHeatMap.UpdateKeyColor(2, Color.white);
        customHeatMap.UpdateKeyColor(3, Color.white);

        colorChoose.transform.GetComponentsInChildren<Image>().ToList().ForEach(e => {
            e.color = Color.white;
        });

        Image image = colorChoose.GetComponent<Image>();
        image.color = Color.white;
        image.sprite = null;
    }
    #endregion
}
