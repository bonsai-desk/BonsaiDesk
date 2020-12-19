// Cristian Pop - https://boxophobic.com/

using UnityEngine;
using Boxophobic.StyledGUI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
public class HeightFogGlobal : StyledMonoBehaviour
{
    [StyledBanner(0.474f, 0.709f, 0.901f, "Height Fog Global", "", "https://docs.google.com/document/d/1pIzIHIZ-cSh2ykODSZCbAPtScJ4Jpuu7lS3rNEHCLbc/edit#heading=h.kfvqsi6kusw4")]
    public bool styledBanner;

    //[StyledMessage("Info", "Shader keyword switching is not always updated in realtime in edit in newer Unity versions. Move a slider or save the scene to update the fog until the issue is fixed (if fixable).", 0, 0)]
    //public bool messageUpdate = true;

    [StyledCategory("Update")]
    public bool categoryUpdate;

    [Tooltip("Choose if the fog settings are set on game start or updated in realtime for animation purposes.")]
    public FogUpdateMode updateMode = FogUpdateMode.OnLoad;

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
    public Color fogColor = new Color(0.5f, 0.75f, 1.0f, 1.0f);

    public float fogDistanceStart = 0.0f;
    public float fogDistanceEnd = 30.0f;
    public float fogHeightStart = 0.0f;
    public float fogHeightEnd = 5.0f;

    [StyledCategory("Skybox")]
    public bool categorySkybox;

    [Range(0f, 1f)]
    public float skyboxFogHeight = 0.5f;
    [Range(0f, 1f)]
    public float skyboxFogFill = 0.0f;

    [StyledCategory("Directional")]
    public bool categoryDirectional;

    [SerializeField]
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
    public Material heightFogMaterial;
    [HideInInspector]
    public Material blendMaterial;
    [HideInInspector]
    public Material localMaterial;
    [HideInInspector]
    public Material overrideMaterial;
    [HideInInspector]
    public float overrideCamToVolumeDistance = 1.0f;
    [HideInInspector]
    public float overrideVolumeDistanceFade = 0.0f;
    [HideInInspector]
    public float updater;

    Camera cam;

    void Awake()
    {
        gameObject.name = "Height Fog Global";

        gameObject.transform.position = Vector3.zero;
        gameObject.transform.rotation = Quaternion.identity;

        GetCamera();

        if (cam != null)
        {
            SetFogSphereSize();
            SetFogSpherePosition();
            cam.depthTextureMode = DepthTextureMode.Depth;            
        }
        else
        {
            Debug.Log("[Atmospheric Height Fog] Camera not found! Make sure you have a camera in the scene or your camera has the MainCamera tag!");
        }

        var sphereMeshGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var sphereMesh = sphereMeshGO.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(sphereMeshGO);

        gameObject.GetComponent<MeshFilter>().sharedMesh = sphereMesh;

        localMaterial = new Material(Shader.Find("BOXOPHOBIC/Atmospherics/Height Fog Preset"));
        localMaterial.name = "Local";

        SetLocalMaterial();

        overrideMaterial = new Material(localMaterial);
        overrideMaterial.name = "Override";

        blendMaterial = new Material(localMaterial);
        blendMaterial.name = "Blend";         

        heightFogMaterial = new Material(Shader.Find("Hidden/BOXOPHOBIC/Atmospherics/Height Fog Global"));
        heightFogMaterial.name = "Height Fog Global";

        RenderPipelineSetTransparentQueue();

        gameObject.GetComponent<MeshRenderer>().sharedMaterial = heightFogMaterial;

        SetGlobalShader();
        SetGlobalKeywords();
    }

    void OnEnable()
    {
        gameObject.GetComponent<MeshRenderer>().enabled = true;
    }

    void OnDisable()
    {
        gameObject.GetComponent<MeshRenderer>().enabled = false;

        Shader.DisableKeyword("AHF_ENABLED");
    }

    void OnDestroy()
    {
        Shader.DisableKeyword("AHF_ENABLED");
    }

    void Update()
    {        
        if (gameObject.name != "Height Fog Global")
        {
            gameObject.name = "Height Fog Global";
        }

        if (cam == null)
        {
            Debug.Log("[Atmospheric Height Fog] " + "Make sure you set scene camera tag to Main Camera for the fog to work!");
            return;
        }

        SetFogSphereSize();
        SetFogSpherePosition();

        // Only update the fog when in SceneMode or FogUpdateMode is set to Realtime
        if (Application.isPlaying == false || updateMode == FogUpdateMode.Realtime)
        {
            SetLocalMaterial();
        }

        if (overrideCamToVolumeDistance > overrideVolumeDistanceFade)
        {
            blendMaterial.CopyPropertiesFromMaterial(localMaterial);
        }
        else if (overrideCamToVolumeDistance < overrideVolumeDistanceFade)
        {
            var lerp = 1 - (overrideCamToVolumeDistance / overrideVolumeDistanceFade);
            blendMaterial.Lerp(localMaterial, overrideMaterial, lerp);
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
        SetGlobalShader();
        SetGlobalKeywords();

#if UNITY_EDITOR

        RenderPipelineSetTransparentQueue();

#endif

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

    void SetGlobalShader()
    {
        Shader.SetGlobalFloat("AHF_FogIntensity", blendMaterial.GetFloat("_FogIntensity"));

        if (blendMaterial.GetInt("_FogAxisMode") == 0)
        {
            Shader.SetGlobalVector("AHF_FogAxisOption", new Vector4(1,0,0,0));
        }
        else if (blendMaterial.GetInt("_FogAxisMode") == 1)
        {
            Shader.SetGlobalVector("AHF_FogAxisOption", new Vector4(0, 1, 0, 0));
        }
        else if (blendMaterial.GetInt("_FogAxisMode") == 2)
        {
            Shader.SetGlobalVector("AHF_FogAxisOption", new Vector4(0, 0, 1, 0));
        }

        Shader.SetGlobalColor("AHF_FogColor", blendMaterial.GetColor("_FogColor"));

        Shader.SetGlobalFloat("AHF_FogDistanceStart", blendMaterial.GetFloat("_FogDistanceStart"));
        Shader.SetGlobalFloat("AHF_FogDistanceEnd", blendMaterial.GetFloat("_FogDistanceEnd"));
        Shader.SetGlobalFloat("AHF_FogHeightStart", blendMaterial.GetFloat("_FogHeightStart"));
        Shader.SetGlobalFloat("AHF_FogHeightEnd", blendMaterial.GetFloat("_FogHeightEnd"));

        Shader.SetGlobalFloat("AHF_SkyboxFogHeight", blendMaterial.GetFloat("_SkyboxFogHeight"));
        Shader.SetGlobalFloat("AHF_SkyboxFogFill", blendMaterial.GetFloat("_SkyboxFogFill"));

        Shader.SetGlobalFloat("AHF_DirectionalModeBlend", blendMaterial.GetFloat("_DirectionalModeBlend"));
        Shader.SetGlobalColor("AHF_DirectionalColor", blendMaterial.GetColor("_DirectionalColor"));
        Shader.SetGlobalFloat("AHF_DirectionalIntensity", blendMaterial.GetFloat("_DirectionalIntensity"));

        Shader.SetGlobalFloat("AHF_NoiseModeBlend", blendMaterial.GetFloat("_NoiseModeBlend"));
        Shader.SetGlobalFloat("AHF_NoiseIntensity", blendMaterial.GetFloat("_NoiseIntensity"));
        Shader.SetGlobalFloat("AHF_NoiseDistanceEnd", blendMaterial.GetFloat("_NoiseDistanceEnd"));
        Shader.SetGlobalFloat("AHF_NoiseScale", blendMaterial.GetFloat("_NoiseScale"));
        Shader.SetGlobalVector("AHF_NoiseSpeed", blendMaterial.GetVector("_NoiseSpeed"));
    }

    void SetGlobalKeywords()
    {
        Shader.EnableKeyword("AHF_ENABLED");

        if (blendMaterial.GetFloat("_DirectionalModeBlend") > 0)
        {
            Shader.DisableKeyword("AHF_DIRECTIONALMODE_OFF");
            Shader.EnableKeyword("AHF_DIRECTIONALMODE_ON");
        }
        else
        {
            Shader.DisableKeyword("AHF_DIRECTIONALMODE_ON");
            Shader.EnableKeyword("AHF_DIRECTIONALMODE_OFF");
        }

        if (blendMaterial.GetFloat("_NoiseModeBlend") > 0)
        {
            Shader.DisableKeyword("AHF_NOISEMODE_OFF");
            Shader.EnableKeyword("AHF_NOISEMODE_PROCEDURAL3D");
        }
        else
        {
            Shader.DisableKeyword("AHF_NOISEMODE_PROCEDURAL3D");
            Shader.EnableKeyword("AHF_NOISEMODE_OFF");
        }
    }

    void SetFogSphereSize()
    {
        var cameraFar = cam.farClipPlane - 1;
        gameObject.transform.localScale = new Vector3(cameraFar, cameraFar, cameraFar);
    }

    void SetFogSpherePosition()
    {
        transform.position = cam.transform.position;
    }

    void RenderPipelineSetTransparentQueue()
    {
        if (heightFogMaterial.HasProperty("_IsStandardPipeline"))
        {
            heightFogMaterial.renderQueue = 3001;
        }
        else
        {
            heightFogMaterial.renderQueue = 3101;
        }
    }
}

