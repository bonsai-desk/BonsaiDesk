// Cristian Pop - https://boxophobic.com/

using UnityEngine;
using Boxophobic.StyledGUI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[RequireComponent(typeof(BoxCollider))]
[HelpURL("https://docs.google.com/document/d/1pIzIHIZ-cSh2ykODSZCbAPtScJ4Jpuu7lS3rNEHCLbc/edit#heading=h.hd5jt8lucuqq")]
public class HeightFogOverride : StyledMonoBehaviour
{
    [StyledBanner(0.474f, 0.709f, 0.901f, "Height Fog Override", "", "https://docs.google.com/document/d/1pIzIHIZ-cSh2ykODSZCbAPtScJ4Jpuu7lS3rNEHCLbc/edit#heading=h.hd5jt8lucuqq")]
    public bool styledBanner;

    //[StyledMessage("Info", "Shader keyword switching is not always updated in realtime in edit in newer Unity versions. Move a slider or save the scene to update the fog until the issue is fixed (if fixable).", 0, 0)]
    //public bool messageUpdate = true;

    [StyledMessage("Info", "The Height Fog Global object is missing from your scene! Please add it before using the Height Fog Override component!", 5, 0)]
    public bool messageNoHeightFogGlobal = false;

    [StyledCategory("Settings")]
    public bool categorySettings;

    [Tooltip("Choose if the fog settings are set on game start or updated in realtime for animation purposes.")]
    public FogUpdateMode updateMode = FogUpdateMode.OnLoad;

    [StyledCategory("Volume")]
    public bool categoryVolume;

    //public float volumeShapeFade = 1.0f;
    //public float volumeExitFade = 10.0f;
    public float volumeDistanceFade = 3.0f;
    [Range(0f, 1f)]
    public float volumeVisibility = 0.2f;

    [StyledCategory("Fog")]
    public bool categoryFog;

    [Tooltip("Shareable fog preset material.")]
    public Material fogPreset = null;
    [HideInInspector]
    public Material fogPresetOld = null;

    [StyledMessage("Info", "The is not a valid Fog Preset material! Please assign the correct shader first!", 10, 0)]
    public bool messageInvalidPreset = false;

    [Space(10)]
    [Range(0f, 1f)]
    public float fogIntensity = 1.0f;

    [Space(10)]
    public FogAxisMode fogAxisMode = FogAxisMode.YAxis;

    [ColorUsage(false, true)]
    public Color fogColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);

    public float fogDistanceStart = 0.0f;
    public float fogDistanceEnd = 30.0f;
    public float fogHeightStart = 0.0f;
    public float fogHeightEnd = 5.0f;

    [StyledCategory("Skybox")]
    public bool categotySkybox;

    [Range(0f, 1f)]
    public float skyboxFogHeight = 0.5f;
    [Range(0f, 1f)]
    public float skyboxFogFill = 0.0f;

    [StyledCategory("Directional")]
    public bool categoryDirectional;

    public FogDirectionalMode directionalMode = FogDirectionalMode.Off;
    [Range(0f, 1f)]
    public float directionalIntensity = 1.0f;

    [ColorUsage(false, true)]
    public Color directionalColor = new Color(1f, 0.75f, 0.5f, 1f);

    [StyledCategory("Noise")]
    public bool categoryNoise;

    public FogNoiseMode noiseMode = FogNoiseMode.Off;
    [Range(0f, 1f)]
    public float noiseIntensity = 1.0f;
    public float noiseDistanceEnd = 60.0f;
    public float noiseScale = 1.0f;
    public Vector3 noiseSpeed = new Vector3(0.0f, 0.0f, 0.0f);

    [StyledSpace(5)]
    public bool styledSpace0;

    [HideInInspector]
    public bool firstTime = true;
    [HideInInspector]
    public bool upgradedTo100 = false;

    Material localMaterial;
    Collider volumeCollider;
    HeightFogGlobal globalFog = null;
    Camera cam;
    bool distanceSent = false;

    void Start()
    {
        volumeCollider = GetComponent<Collider>();
        volumeCollider.isTrigger = true;

        if (GameObject.Find("Height Fog Global") != null)
        {
            GameObject globalFogGO = GameObject.Find("Height Fog Global");
            globalFog = globalFogGO.GetComponent<HeightFogGlobal>();

            // Fix override Diectional and Noise blending
            // Upgrade from v0.9.0 to v1.0.0
            if (upgradedTo100 == false)
            {
                directionalMode = globalFog.directionalMode;
                noiseMode = globalFog.noiseMode;

                upgradedTo100 = true;
            }

            messageNoHeightFogGlobal = false;
        }
        else
        {
            messageNoHeightFogGlobal = true;
        }

        localMaterial = new Material(Shader.Find("BOXOPHOBIC/Atmospherics/Height Fog Preset"));
        localMaterial.name = "Local";

        SetLocalMaterial();
    }

    void Update()
    {
        GetCamera();

        if (cam == null || globalFog == null)
        {
            return;
        }

        // Only update the fog when in SceneMode or FogUpdateMode is set to Realtime
        if (Application.isPlaying == false || updateMode == FogUpdateMode.Realtime)
        {
            SetLocalMaterial();
        }

        Vector3 camPos = cam.transform.position;
        Vector3 closestPos = volumeCollider.ClosestPoint(camPos);

        float dist = Vector3.Distance(camPos, closestPos);

        if (dist > volumeDistanceFade && distanceSent == false)
        {
            globalFog.overrideCamToVolumeDistance = Mathf.Infinity;
            distanceSent = true;
        }
        else if (dist < volumeDistanceFade)
        {
            globalFog.overrideMaterial = localMaterial;
            globalFog.overrideCamToVolumeDistance = dist;
            globalFog.overrideVolumeDistanceFade = volumeDistanceFade;
            distanceSent = false;
        }

#if UNITY_EDITOR
        if (fogPreset != null)
        {
            if (fogPreset.HasProperty("_HeightFogPreset"))
            {
                if (fogPresetOld != fogPreset)
                {
                    SetPresetToScript();
                    SetLocalMaterial();

                    fogPresetOld = fogPreset;
                }

                if (Selection.Contains(gameObject))
                {
                    fogPreset.CopyPropertiesFromMaterial(localMaterial);
                    EditorUtility.SetDirty(fogPreset);
                }
                else
                {
                    SetPresetToScript();
                    SetLocalMaterial();
                }

                messageInvalidPreset = false;
            }
            else
            {
                messageInvalidPreset = true;
            }
        }
        else
        {
            messageInvalidPreset = false;
        }
#endif
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(fogColor.r, fogColor.g, fogColor.b, volumeVisibility);
        Gizmos.DrawCube(transform.position, new Vector3(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z));
        Gizmos.DrawCube(transform.position, new Vector3(transform.lossyScale.x + (volumeDistanceFade * 2), transform.lossyScale.y + (volumeDistanceFade * 2), transform.lossyScale.z + (volumeDistanceFade * 2)));
    }

    void GetCamera()
    {
        cam = null;

        if (Camera.current != null)
        {
            cam = Camera.current;
        }

        if (Camera.main != null)
        {
            cam = Camera.main;
        }
    }

    void SetPresetToScript()
    {
        fogIntensity = fogPreset.GetFloat("_FogIntensity");

        if (fogPreset.GetInt("_FogAxisMode") == 0)
        {
            fogAxisMode = FogAxisMode.XAxis;
        }
        else if (fogPreset.GetInt("_FogAxisMode") == 1)
        {
            fogAxisMode = FogAxisMode.YAxis;
        }
        else if (fogPreset.GetInt("_FogAxisMode") == 2)
        {
            fogAxisMode = FogAxisMode.ZAxis;
        }

        fogColor = fogPreset.GetColor("_FogColor");
        fogDistanceStart = fogPreset.GetFloat("_FogDistanceStart");
        fogDistanceEnd = fogPreset.GetFloat("_FogDistanceEnd");
        fogHeightStart = fogPreset.GetFloat("_FogHeightStart");
        fogHeightEnd = fogPreset.GetFloat("_FogHeightEnd");

        skyboxFogHeight = fogPreset.GetFloat("_SkyboxFogHeight");
        skyboxFogFill = fogPreset.GetFloat("_SkyboxFogFill");

        directionalColor = fogPreset.GetColor("_DirectionalColor");
        directionalIntensity = fogPreset.GetFloat("_DirectionalIntensity");

        noiseIntensity = fogPreset.GetFloat("_NoiseIntensity");
        noiseDistanceEnd = fogPreset.GetFloat("_NoiseDistanceEnd");
        noiseScale = fogPreset.GetFloat("_NoiseScale");
        noiseSpeed = fogPreset.GetVector("_NoiseSpeed");

        if (fogPreset.GetInt("_DirectionalMode") == 1)
        {
            directionalMode = FogDirectionalMode.On;
        }
        else
        {
            directionalMode = FogDirectionalMode.Off;
        }

        if (fogPreset.GetInt("_NoiseMode") == 2)
        {
            noiseMode = FogNoiseMode.Procedural3D;
        }
        else
        {
            noiseMode = FogNoiseMode.Off;
        }
    }

    void SetLocalMaterial()
    {
        localMaterial.SetFloat("_FogIntensity", fogIntensity);

        if (fogAxisMode == FogAxisMode.XAxis)
        {
            localMaterial.SetInt("_FogAxisMode", 0);
        }
        else if (fogAxisMode == FogAxisMode.YAxis)
        {
            localMaterial.SetInt("_FogAxisMode", 1);
        }
        else if (fogAxisMode == FogAxisMode.ZAxis)
        {
            localMaterial.SetInt("_FogAxisMode", 2);
        }

        localMaterial.SetColor("_FogColor", fogColor);
        localMaterial.SetFloat("_FogDistanceStart", fogDistanceStart);
        localMaterial.SetFloat("_FogDistanceEnd", fogDistanceEnd);
        localMaterial.SetFloat("_FogHeightStart", fogHeightStart);
        localMaterial.SetFloat("_FogHeightEnd", fogHeightEnd);

        localMaterial.SetFloat("_SkyboxFogHeight", skyboxFogHeight);
        localMaterial.SetFloat("_SkyboxFogFill", skyboxFogFill);

        localMaterial.SetFloat("_DirectionalIntensity", directionalIntensity);
        localMaterial.SetColor("_DirectionalColor", directionalColor);

        localMaterial.SetFloat("_NoiseIntensity", noiseIntensity);
        localMaterial.SetFloat("_NoiseDistanceEnd", noiseDistanceEnd);
        localMaterial.SetFloat("_NoiseScale", noiseScale);
        localMaterial.SetVector("_NoiseSpeed", noiseSpeed);

        if (directionalMode == FogDirectionalMode.On)
        {
            localMaterial.SetInt("_DirectionalMode", 1);
            localMaterial.SetFloat("_DirectionalModeBlend", 1.0f);
        }
        else
        {
            localMaterial.SetInt("_DirectionalMode", 0);
            localMaterial.SetFloat("_DirectionalModeBlend", 0.0f);
        }

        if (noiseMode == FogNoiseMode.Procedural3D)
        {
            localMaterial.SetInt("_NoiseMode", 2);
            localMaterial.SetFloat("_NoiseModeBlend", 1.0f);
        }
        else
        {
            localMaterial.SetInt("_NoiseMode", 0);
            localMaterial.SetFloat("_NoiseModeBlend", 0.0f);
        }
    }
}

