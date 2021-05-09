#if UNITY_EDITOR
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
    }
}
#endif
