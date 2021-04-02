using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class LightManager : MonoBehaviour
{
    public static LightManager Singleton;
    public Texture2D[] Lights;
    public Material Garden;

    public FilterMode filterMode = FilterMode.Bilinear;
    public TextureWrapMode wrapMode = TextureWrapMode.Clamp;
    public TextureFormat textureFormat = TextureFormat.RGBA32;
    public bool mipChain;
    public float candleLevel = 1;

    public float backRoomLevel;

    public float backRoomLevelTarget;

    public float mainRoomLevel;

    public float mainRoomLevelTarget = 1;

    [Header("ring")] public float ringScale = 1f;

    public float ringFloor;
    public float ringSpeed = 1;

    [Header("Pulse Data (scale, floor, speed)")]
    public Vector3[] PulseData;

    // ambient, main, area, back
    private readonly float[] lightLevels = new float[64];

    private void Awake()
    {
        if (Singleton == null)
        {
            Singleton = this;
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
        var x = Lights[0];
        var arr = new Texture2DArray(x.width, x.height, Lights.Length, textureFormat, mipChain);

        arr.filterMode = filterMode;
        arr.wrapMode = wrapMode;

        for (var i = 0; i < Lights.Length; i++)
        {
            arr.SetPixels(Lights[i].GetPixels(), i);
        }

        arr.Apply();
        //AssetDatabase.CreateAsset(arr, "Assets/LightsArr.tarr");
        lightLevels[0] = 1;
        lightLevels[1] = mainRoomLevel;
        lightLevels[2] = 1;
        lightLevels[3] = backRoomLevel;
        Garden.SetInt("numLights", Lights.Length);
        Garden.SetFloatArray("lightLevels", lightLevels);
        Garden.SetTexture("Lights", arr);

        MoveToDesk.OrientationChanged += HandleOrient;

#if UNITY_EDITOR
        if (NetworkManagerGame.Singleton.serverOnlyIfEditor)
        {
            StartCoroutine(FadeOnMain());
        }
#endif
    }


    // Update is called once per frame
    private void Update()
    {
        candleLevel = Mathf.Clamp(candleLevel + (Random.value - 0.51f) / 50f, 0, 1);
        lightLevels[2] = Mathf.Clamp(Pulse(PulseData[0], 0) - candleLevel, 0, 1);

        const float scale = 1 / 25f;

        if (!Mathf.Approximately(mainRoomLevel, mainRoomLevelTarget))
        {
            mainRoomLevel += scale * (mainRoomLevelTarget - mainRoomLevel);
        }

        if (!Mathf.Approximately(backRoomLevel, backRoomLevelTarget))
        {
            backRoomLevel += scale * (backRoomLevelTarget - backRoomLevel);
        }

        lightLevels[1] = mainRoomLevel;
        lightLevels[3] = backRoomLevel;

        Garden.SetFloatArray("lightLevels", lightLevels);
    }

    private void OnApplicationQuit()
    {
        Garden.SetInt("numLights", 0);
    }

    private void HandleOrient(bool oriented)
    {
        if (oriented)
        {
            StartCoroutine(FadeOnMain());
        }
    }

    private IEnumerator FadeOnMain()
    {
        float x = 0;

        while (x < 1)
        {
            x += 0.02f;
            lightLevels[2] = CubicBezier.EaseIn.Sample(x);
            yield return null;
        }

        lightLevels[2] = 1f;
    }

    private static float Gauss(float ringScale, float ringFloor, float ringSpeed, float x, float shift = 0)
    {
        return (float) (ringFloor + (1 - ringFloor) * Math.Exp(-ringScale * Math.Pow(Math.Sin(ringSpeed * x - shift), 2f)));
    }

    private void Ring(int[] idxs)
    {
        var numLights = idxs.Length;
        var twoPi = 2 * Math.PI;
        for (var i = 0; i < idxs.Length; i++)
        {
            lightLevels[idxs[i]] = Gauss(ringScale, ringFloor, ringSpeed, Time.time, (float) twoPi * (i + 1) / numLights);
        }
    }

    private float Pulse(Vector3 pulseData, float offset)
    {
        return Gauss(pulseData[0], pulseData[1], pulseData[2], Time.time + offset);
    }
}