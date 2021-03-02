using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class LightManager : MonoBehaviour {
    public Texture2D[] Lights;
    public Material Garden;
    private float[] lightLevels = new float[64];

    public FilterMode filterMode = FilterMode.Bilinear;
    public TextureWrapMode wrapMode = TextureWrapMode.Clamp;
    public TextureFormat textureFormat = TextureFormat.RGBA32;
    public bool mipChain = false;
    
    // Start is called before the first frame update
    void Start() {
        var x = Lights[0];
        var arr = new Texture2DArray(x.width, x.height, Lights.Length, textureFormat, mipChain);

        arr.filterMode = filterMode;
        arr.wrapMode   = wrapMode;
        
        for (int i = 0; i < Lights.Length; i++) {
            arr.SetPixels(Lights[i].GetPixels(), i);
        }
        arr.Apply();
        //AssetDatabase.CreateAsset(arr, "Assets/LightsArr.tarr");
        lightLevels[0] = 1;
        lightLevels[1] = 1;
        lightLevels[2] = 1;
        lightLevels[3] = 1;
        lightLevels[4] = 1;
        Garden.SetInt("numLights", Lights.Length);
        Garden.SetFloatArray("lightLevels", lightLevels);
        Garden.SetTexture("Lights", arr);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
