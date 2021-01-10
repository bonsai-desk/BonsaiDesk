// Cristian Pop - https://boxophobic.com/

using UnityEngine;
using Boxophobic;
using Boxophobic.StyledGUI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AtmosphericHeightFog
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [HelpURL("https://docs.google.com/document/d/1pIzIHIZ-cSh2ykODSZCbAPtScJ4Jpuu7lS3rNEHCLbc/edit#heading=h.pzat2b29j9a0")]
    public class HeightFogPerObject : StyledMonoBehaviour
    {
        [StyledBanner(0.474f, 0.709f, 0.901f, "Height Fog Per Object", "", "https://docs.google.com/document/d/1pIzIHIZ-cSh2ykODSZCbAPtScJ4Jpuu7lS3rNEHCLbc/edit#heading=h.pzat2b29j9a0")]
        public bool styledBanner;

        [StyledMessage("Info", "The Object does not have a Mesh Renderer!", 5, 5)]
        public bool messageNoRenderer = false;

        [StyledMessage("Info", "Objects using multiple materials are not supported!", 5, 5)]
        public bool messageMultiMaterials = false;

        [StyledMessage("Info", "The Object does not have a Material assigned!", 5, 5)]
        public bool messageNoMaterial = false;

        [StyledMessage("Info", "Please note that the Height Fog Per Object option will not work for all transparent objects. Available in Play mode only. Please read the documentation for more!", 0, 0)]
        public bool messageTransparencySupport = true;

        [StyledCategory("Settings")]
        public bool categoryMaterial;

        public Material customFogMaterial = null;

        [StyledMessage("Info", "The is not a valid Height Fog material! Please assign the correct shader first!", 5, 0)]
        public bool messageInvalidFogMaterial = false;

        [StyledSpace(5)]
        public bool styledSpace0;

        int transparencyRenderQueue = 3002;

        Material originalMaterial;
        Material instanceMaterial;
        Material transparencyMaterial;

        GameObject transparencyGO;

        void Awake()
        {
            if (GameObjectIsInvalid())
            {
                return;
            }

#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                GameObjectDisableBathingFlag();
                return;
            }
#endif

            transparencyGO = new GameObject(gameObject.name + " (Height Fog Object)");

            transparencyGO.transform.parent = gameObject.transform;
            transparencyGO.transform.localPosition = Vector3.zero;
            transparencyGO.transform.localRotation = Quaternion.identity;
            transparencyGO.transform.localScale = Vector3.one;

            transparencyGO.AddComponent<MeshFilter>();
            transparencyGO.AddComponent<MeshRenderer>();

            transparencyGO.GetComponent<MeshFilter>().sharedMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;

            Material originalMaterial = gameObject.GetComponent<MeshRenderer>().sharedMaterial;

            instanceMaterial = new Material(originalMaterial);
            instanceMaterial.name = originalMaterial.name + " (Instance)";
            //instanceMaterial.SetOverrideTag("DisableBatching", "True");

            if (customFogMaterial == null)
            {
                transparencyMaterial = new Material(instanceMaterial);
                transparencyMaterial.shader = Shader.Find("BOXOPHOBIC/Atmospherics/Height Fog Per Object");
                transparencyMaterial.name = originalMaterial.name + " (Generic Fog)";
            }
            else if (customFogMaterial != null)
            {
                if (customFogMaterial.HasProperty("_IsHeightFogShader"))
                {
                    transparencyMaterial = customFogMaterial;
                    transparencyMaterial.name = originalMaterial.name + " (Custom Fog)";
                }
                else
                {
                    transparencyMaterial = new Material(instanceMaterial);
                    transparencyMaterial.shader = Shader.Find("BOXOPHOBIC/Atmospherics/Height Fog Per Object");
                    transparencyMaterial.name = originalMaterial.name + " (Generic Fog)";
                }
            }

            if (transparencyMaterial.HasProperty("_IsStandardPipeline"))
            {
                transparencyRenderQueue = 3002;
            }
            else
            {
                transparencyRenderQueue = 3102;
            }

            instanceMaterial.renderQueue = transparencyRenderQueue;
            transparencyMaterial.renderQueue = transparencyRenderQueue + 1;

            gameObject.GetComponent<MeshRenderer>().material = instanceMaterial;
            transparencyGO.GetComponent<MeshRenderer>().material = transparencyMaterial;

        }
#if UNITY_EDITOR
        void Update()
        {
            if (Application.isPlaying == true)
            {
                return;
            }

            if (gameObject.isStatic)
            {
                GameObjectDisableBathingFlag();
            }

            if (customFogMaterial == null)
            {
                messageInvalidFogMaterial = false;
            }
            else if (customFogMaterial != null)
            {
                if (customFogMaterial.HasProperty("_IsHeightFogShader") == false)
                {
                    messageInvalidFogMaterial = true;
                }
                else
                {
                    messageInvalidFogMaterial = false;
                }
            }
        }
#endif

        bool GameObjectIsInvalid()
        {
            bool invalid = false;

            if (gameObject.GetComponent<MeshRenderer>() == null)
            {
                messageNoRenderer = true;
                invalid = true;
            }

            else if (gameObject.GetComponent<MeshRenderer>().sharedMaterials.Length > 1)
            {
                messageMultiMaterials = true;
                invalid = true;
            }

            else if (gameObject.GetComponent<MeshRenderer>().sharedMaterial == null)
            {
                messageNoMaterial = true;
                invalid = true;
            }

            return invalid;
        }

#if UNITY_EDITOR
        void GameObjectDisableBathingFlag()
        {
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(gameObject);
            flags = flags & ~(StaticEditorFlags.BatchingStatic);
            GameObjectUtility.SetStaticEditorFlags(gameObject, flags);
        }
#endif
    }
}
