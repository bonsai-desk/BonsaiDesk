// Cristian Pop - https://boxophobic.com/

using UnityEngine;
using UnityEditor;
using Boxophobic.StyledGUI;
using Boxophobic.Utils;
using System.IO;

namespace AtmosphericHeightFog
{
    public class HeightFogHub : EditorWindow
    {
#if UNITY_2019_3_OR_NEWER
    const int GUI_HEIGHT = 18;
#else
        const int GUI_HEIGHT = 14;
#endif

        string folderAsset = "Assets/BOXOPHOBIC/Atmospheric Height Fog";

        string[] pipelinePaths;
        string[] pipelineOptions;
        string pipelinesPath;
        int pipelineIndex;

        int assetVersion;
        string bannerVersion;

        GUIStyle stylePopup;

        Color bannerColor;
        string bannerText;
        string helpURL;
        static HeightFogHub window;
        //Vector2 scrollPosition = Vector2.zero;

        [MenuItem("Window/BOXOPHOBIC/Atmospheric Height Fog/Hub", false, 1000)]
        public static void ShowWindow()
        {
            window = GetWindow<HeightFogHub>(false, "Atmospheric Height Fog", true);
            window.minSize = new Vector2(389, 220);
        }

        void OnEnable()
        {
            bannerColor = new Color(0.55f, 0.7f, 1f);
            bannerText = "Atmospheric Height Fog";
            helpURL = "https://docs.google.com/document/d/1pIzIHIZ-cSh2ykODSZCbAPtScJ4Jpuu7lS3rNEHCLbc/edit#heading=h.hbq3w8ae720x";

            //Safer search, there might be many user folders
            string[] searchFolders;

            searchFolders = AssetDatabase.FindAssets("Atmospheric Height Fog");

            for (int i = 0; i < searchFolders.Length; i++)
            {
                if (AssetDatabase.GUIDToAssetPath(searchFolders[i]).EndsWith("Atmospheric Height Fog.pdf"))
                {
                    folderAsset = AssetDatabase.GUIDToAssetPath(searchFolders[i]);
                    folderAsset = folderAsset.Replace("/Atmospheric Height Fog.pdf", "");
                }
            }

            pipelinesPath = folderAsset + "/Core/Pipelines";

            GetPackages();

            assetVersion = SettingsUtils.LoadSettingsData(folderAsset + "/Core/Editor/Version.asset", -99);
            bannerVersion = assetVersion.ToString();
            bannerVersion = bannerVersion.Insert(1, ".");
            bannerVersion = bannerVersion.Insert(3, ".");

            bannerColor = new Color(0.55f, 0.7f, 1f);
            bannerText = "Atmospheric Height Fog " + bannerVersion;
        }

        void OnGUI()
        {
            SetGUIStyles();

            StyledGUI.DrawWindowBanner(bannerColor, bannerText, helpURL);

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);

            GUILayout.BeginVertical();

            //scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false, GUILayout.Width(this.position.width - 28), GUILayout.Height(this.position.height - 80));

            EditorGUILayout.HelpBox("Click the Import Render Pipeline to switch to another render pipeline. For Universal Render Pipeline, follow the instructions below to enable the fog rendering!", MessageType.Info, true);

            if (pipelineOptions[pipelineIndex].Contains("Universal 7.1.8"))
            {
                EditorGUILayout.HelpBox("For Universal 7.1.8+ Pipeline, Depth Texture and one of the following features need to be enabled for the depth to work properly: Opaque Texure, HDR or Post Processing!", MessageType.Info, true);
            }

            if (pipelineOptions[pipelineIndex].Contains("Universal 7.4.1"))
            {
                EditorGUILayout.HelpBox("For Universal 7.4.1+ Pipeline, Depth Texture need to be enabled on the render pipeline asset!", MessageType.Info, true);
            }

            DrawInterface();

            //GUILayout.EndScrollView();

            GUILayout.EndVertical();

            GUILayout.Space(13);
            GUILayout.EndHorizontal();
        }

        void SetGUIStyles()
        {
            stylePopup = new GUIStyle(EditorStyles.popup)
            {
                alignment = TextAnchor.MiddleCenter
            };
        }

        void DrawInterface()
        {
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Render Pipeline", ""), GUILayout.Width(220));
            pipelineIndex = EditorGUILayout.Popup(pipelineIndex, pipelineOptions, stylePopup);
            if (GUILayout.Button("Import", GUILayout.Width(80), GUILayout.Height(GUI_HEIGHT)))
            {
                ImportPackage();

                GUIUtility.ExitGUI();
            }
            GUILayout.EndHorizontal();
        }

        void GetPackages()
        {
            pipelinePaths = Directory.GetFiles(pipelinesPath, "*.unitypackage", SearchOption.TopDirectoryOnly);

            pipelineOptions = new string[pipelinePaths.Length];

            for (int i = 0; i < pipelineOptions.Length; i++)
            {
                pipelineOptions[i] = Path.GetFileNameWithoutExtension(pipelinePaths[i].Replace("Built-in Pipeline", "Standard"));
            }
        }

        void ImportPackage()
        {
            AssetDatabase.ImportPackage(pipelinePaths[pipelineIndex], true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Atmospheric Height Fog] " + pipelineOptions[pipelineIndex] + " package imported in your project!");
        }
    }
}

