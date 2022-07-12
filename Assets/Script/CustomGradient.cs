using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Not mine

[System.Serializable]
public class CustomGradient {
    public enum BlendMode { Linear, Discrete };
    public BlendMode blendMode;
    public bool randomizeColor;

    [System.Serializable]
    public struct ColorKey {
        [SerializeField]
        Color color;
        [SerializeField]
        float time;

        public ColorKey ( Color color, float time ) {
            this.color = color;
            this.time = time;
        }

        public Color Color {
            get {
                return color;
            }
        }

        public float Time {
            get {
                return time;
            }
        }
    }

    [SerializeField]
    List<ColorKey> keys = new List<ColorKey>();

    public CustomGradient () {
        AddKey(Color.white, 0);
        AddKey(Color.white, 1);
    }

    public Color Evaluate ( float time ) {
        if (keys.Count == 0) return Color.white;
        ColorKey keyLeft = keys[0];
        ColorKey keyRight = keys[keys.Count -1];

        for (int i = 0; i < keys.Count ; i++) {
            if (keys[i].Time <= time) {
                keyLeft = keys[i];
            }
            if (keys[i].Time >= time) {
                keyRight = keys[i];
                break;
            }
        }

        if (blendMode == BlendMode.Linear) {
            float blendTime = Mathf.InverseLerp(keyLeft.Time, keyRight.Time, time);
            return Color.Lerp(keyLeft.Color, keyRight.Color, blendTime);
        }
        return keyRight.Color;
    }

    public ColorKey GetKey(int i ) {
        return keys[i];
    }

    public int NumbKeys () {
        return keys.Count;
    }

    public int AddKey(Color color, float time) {
        ColorKey newkey = new ColorKey(color,time);
        // Key sorted by time
        keys.Add(newkey);
        keys = keys.OrderBy(i => i.Time).ToList();
        return keys.IndexOf(newkey);
    }

    public void RemoveKey(int index ) {
        if(keys.Count >= 2)
            keys.RemoveAt(index);
    }
    
    public void UpdateKeyColor(int index, Color col) {
        keys[index] = new ColorKey(col, keys[index].Time);
    }


    public int UpdateKeyTime(int index, float time ) {
        Color col = keys[index].Color;
        RemoveKey(index);
        return AddKey(col, time);
    }

    public Texture2D GetTexture(int with) {
        Texture2D texture = new Texture2D(with,32);
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] colors = new Color[with*32];
        for (int j = 0; j < 32; j++) {
            for (int i = 0; i < with; i++) {
                colors[j*with + i] = Evaluate((float)i / (with - 1));
            }
        }
        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }
}
