// Cristian Pop - https://boxophobic.com/

using UnityEngine;
using UnityEditor;
using Boxophobic.StyledGUI;
using Boxophobic.Utils;
using System.IO;
//using System.Collections.Generic;
//using UnityEngine.SceneManagement;
//using UnityEditor.SceneManagement;

public class HeightFogHub : EditorWindow
{
    string boxophobicFolder = "Assets/BOXOPHOBIC";

    string[] packagePaths;
    string[] packageOptions;

    string packagesPath;
    int packageIndex;

    //string[] shaderPaths;
    //string[] materialPaths;
    //string aciveScene = "";        
    //string shadersPath;
    //int unityMajorVersion;
    //int version;
    //string savedPackageName = "";

    GUIStyle stylePopup;

    Color bannerColor;
    string bannerText;
    string helpURL;
    static HeightFogHub window;
    Vector2 scrollPosition = Vector2.zero;

    [MenuItem("Window/BOXOPHOBIC/Atmospheric Height Fog/Hub")]
    public static void ShowWindow()
    {
        window = GetWindow<HeightFogHub>(false, "Atmospheric Height Fog", true);
        window.minSize = new Vector2(389, 220);
    }

    void OnEnable()
    {
        bannerColor = new Color(0.474f, 0.709f, 0.901f);
        bannerText = "Atmospheric Height Fog";
        helpURL = "https://docs.google.com/document/d/1pIzIHIZ-cSh2ykODSZCbAPtScJ4Jpuu7lS3rNEHCLbc/edit#heading=h.hbq3w8ae720x";

        boxophobicFolder = BoxophobicUtils.GetBoxophobicFolder();

        packagesPath = boxophobicFolder + "/Atmospheric Height Fog/Core/Packages";
        //shadersPath = boxophobicFolder + "/Polyverse Wind/Core/Shaders";

        GetPackages();
        //GetShaders();
        //GetMaterials();

        //unityMajorVersion = int.Parse(Application.unityVersion.Substring(0, 4));
        //unityMinorVersion = Application.unityVersion.Substring(5, 1);

        //version = SettingsUtils.LoadSettingsData(boxophobicFolder + "/User/The Vegetation Engine/Version.asset", -99);
    }

    void OnGUI()
    {
        SetGUIStyles();

        StyledGUI.DrawWindowBanner(bannerColor, bannerText, helpURL);

        GUILayout.BeginHorizontal();
        GUILayout.Space(15);

        GUILayout.BeginVertical();

        //scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false, GUILayout.Width(this.position.width - 28), GUILayout.Height(this.position.height - 80));

        DrawInstallMessage();
        DrawRenderPipelineSelection();
        DrawSetupButton();

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

    void DrawInstallMessage()
    {
        EditorGUILayout.HelpBox("Click the Install Render Pipeline to switch to another Render Pipeline. For Universal Render Pipeline, Depth Texture and one of the following features need to be enabled for the depth to work properly: Opaque Texure, HDR or Post Processing!", MessageType.Info, true);
    }

    void DrawRenderPipelineSelection()
    {
        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Render Pipeline", ""));
        packageIndex = EditorGUILayout.Popup(packageIndex, packageOptions, stylePopup);
        GUILayout.EndHorizontal();
    }

    void DrawSetupButton()
    {
        GUILayout.Space(25);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Install Render Pipeline"/*, GUILayout.Width(160)*/))
        {
            //SettingsUtils.SaveSettingsData(boxophobicFolder + "/User/The Vegetation Engine/Pipeline.asset", packageOptions[packageIndex]);

            ImportPackage();

            GUIUtility.ExitGUI();
        }

        GUILayout.EndHorizontal();
    }

    void GetPackages()
    {
        packagePaths = Directory.GetFiles(packagesPath, "*.unitypackage", SearchOption.TopDirectoryOnly);

        packageOptions = new string[packagePaths.Length];

        for (int i = 0; i < packageOptions.Length; i++)
        {
            packageOptions[i] = Path.GetFileNameWithoutExtension(packagePaths[i].Replace("Built-in Pipeline", "Standard"));
        }
    }

    void ImportPackage()
    {
        AssetDatabase.ImportPackage(packagePaths[packageIndex], false);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Atmospheric Height Fog] " + packageOptions[packageIndex] + " package imported into your project!");
    }

    /// <summary>
    /// UNUSED. KEEPT AROUND FOR LATER USAGE
    /// </summary>

    //void GetShaders()
    //{
    //    shaderPaths = Directory.GetFiles(shadersPath, "*.shader", SearchOption.AllDirectories);
    //}

    //void GetMaterials()
    //{
    //    materialPaths = Directory.GetFiles("Assets/", "*.mat", SearchOption.AllDirectories);
    //}

    //void GetLastSettings()
    //{
    //    //savedPackageName = SettingsUtils.LoadSettingsData(boxophobicFolder + "/User/The Vegetation Engine/Pipeline.asset", "");

    //    for (int i = 0; i < packageOptions.Length; i++)
    //    {
    //        if (packageOptions[i] == savedPackageName)
    //        {
    //            packageIndex = i;
    //        }
    //    }
    //}

    //void SaveScene()
    //{
    //    if (SceneManager.GetActiveScene() != null || SceneManager.GetActiveScene().name != "")
    //    {
    //        if (SceneManager.GetActiveScene().isDirty)
    //        {
    //            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    //            AssetDatabase.SaveAssets();
    //            AssetDatabase.Refresh();
    //        }

    //        aciveScene = SceneManager.GetActiveScene().path;
    //        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
    //    }
    //}

    //void ReopenScene()
    //{
    //    if (aciveScene != "")
    //    {
    //        EditorSceneManager.OpenScene(aciveScene);
    //    }
    //}
}

