#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class PreBuild : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        var go = GameObject.FindObjectOfType<NetworkManagerGame>();
        if (go)
        {
            go.BuildId = PlayerSettings.Android.bundleVersionCode;
        }
        else
        {
            Debug.LogError("Cant find NetworkManagerGame for build");
        }

        try
        {
            var strDir = System.IO.Directory.GetCurrentDirectory();
            var passPath = System.IO.Path.Combine(strDir, "android_pass.txt");

            if (!System.IO.File.Exists(passPath))
            {
                System.IO.File.Create(passPath);
            }

            var lines = System.IO.File.ReadAllLines(passPath);
            if (lines.Length == 2)
            {
                PlayerSettings.keystorePass = lines[0];
                PlayerSettings.keyaliasPass = lines[1];
            }
            else
            {
                Debug.LogError("android_pass.txt should have exactly 2 lines.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Get android_pass.txt error: " + e);
        }
    }
}
#endif