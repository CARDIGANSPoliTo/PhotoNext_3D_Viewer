using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GradientEditor : EditorWindow
{
    CustomGradient gradient;
    const int borderSize = 10;
    const float keyWidth = 10;
    const float keyHeight = 20;
    Rect[] keyRects;
    bool isDownOverKey;
    int selectedKey;
    bool needRepaint;
    Rect gradientPreviewRect;

    public void SetGradient(CustomGradient gradient ) {
        this.gradient = gradient;
    }

    private void OnEnable () {
        titleContent.text = "Gradient Editor";
        position.Set(position.x, position.y, 400, 150);
        minSize = new Vector2(200, 150);
        maxSize = new Vector2(1920, 150);
    }

    private void OnDisable ()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    private void OnGUI ()
    {
        Draw();

        HandleInput();

        if(needRepaint)
        {
            needRepaint = false;
            Repaint();
        }

    }

    void Draw ()
    {
        gradientPreviewRect = new Rect(borderSize,borderSize, position.width - borderSize*2, 25);
        GUI.DrawTexture(gradientPreviewRect, gradient.GetTexture((int)gradientPreviewRect.width));

        keyRects = new Rect[gradient.NumbKeys()];
        for (int i = 0; i < gradient.NumbKeys(); i++)
        {
            CustomGradient.ColorKey key =  gradient.GetKey(i);
            Rect keyRect = new Rect(gradientPreviewRect.x + gradientPreviewRect.width * key.Time - keyWidth/2f, gradientPreviewRect.yMax + borderSize, keyWidth, keyHeight);
            if (i == selectedKey)
            {
                EditorGUI.DrawRect(new Rect(keyRect.x - 2, keyRect.y - 2, keyRect.width + 4, keyRect.height + 4), Color.black);
            }

            EditorGUI.DrawRect(keyRect, key.Color);
            keyRects[i] = keyRect;
        }

        Rect settingRect = new Rect(borderSize, keyRects[0].yMax + borderSize, position.width - borderSize * 2, position.height-borderSize*2);
        GUILayout.BeginArea(settingRect);
        EditorGUI.BeginChangeCheck();
        Color newColor = EditorGUILayout.ColorField(gradient.GetKey(selectedKey).Color);
        if (EditorGUI.EndChangeCheck())
        {
            gradient.UpdateKeyColor(selectedKey, newColor);
        }
        gradient.blendMode = (CustomGradient.BlendMode)EditorGUILayout.EnumPopup("Blend mode", gradient.blendMode);
        gradient.randomizeColor = EditorGUILayout.Toggle("Randomize Color", gradient.randomizeColor);
        GUILayout.EndArea();

    }

    void HandleInput ()
    {
        Event guiEvent = Event.current;

        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0)
        {
            for (int i = 0; i < keyRects.Length; i++)
            {
                if (keyRects[i].Contains(guiEvent.mousePosition))
                {
                    isDownOverKey = true;
                    selectedKey = i;
                    needRepaint = true;
                    break;
                }
            }
            if (!isDownOverKey)
            {
                float keyTime = Mathf.InverseLerp(gradientPreviewRect.x, gradientPreviewRect.xMax, guiEvent.mousePosition.x);
                Color interpolateColor = gradient.Evaluate(keyTime);
                Color randomColor = new Color(Random.value, Random.value, Random.value);
                selectedKey = gradient.AddKey(gradient.randomizeColor? randomColor : interpolateColor, keyTime);
                isDownOverKey = true;
                needRepaint = true;
            }
        }

        if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0)
        {
            isDownOverKey = false;
        }


        if (isDownOverKey && guiEvent.type == EventType.MouseDrag && guiEvent.button == 0)
        {
            float keyTime = Mathf.InverseLerp(gradientPreviewRect.x, gradientPreviewRect.xMax, guiEvent.mousePosition.x);
            gradient.UpdateKeyTime(selectedKey, keyTime);
            needRepaint = true;
        }

        if (guiEvent.keyCode == KeyCode.Backspace && guiEvent.type == EventType.KeyDown)
        {
            gradient.RemoveKey(selectedKey);
            if (selectedKey >= gradient.NumbKeys())
                selectedKey--;
            needRepaint = true;
        }
    }
    
}
