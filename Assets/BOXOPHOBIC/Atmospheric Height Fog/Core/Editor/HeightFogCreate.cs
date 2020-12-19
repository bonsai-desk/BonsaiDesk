// Cristian Pop - https://boxophobic.com/

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class HeightFogCreate
{
    [MenuItem("GameObject/BOXOPHOBIC/Atmospheric Height Fog/Global Volume", false, 9)]
    static void CreateGlobalVolume()
    {
        if (GameObject.Find("Height Fog Global") != null)
        {
            Debug.Log("[Atmospheric Height Fog] " + "Height Fog Global is already added to your scene!");
            return;
        }

        GameObject go = new GameObject();
        go.AddComponent<HeightFogGlobal>();
        go.name = "Height Fog Global";

        Selection.activeGameObject = go;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("GameObject/BOXOPHOBIC/Atmospheric Height Fog/Override Volume", false, 9)]
    static void CreateOverrideVolume()
    {
        if (GameObject.Find("Height Fog Global") == null)
        {
            Debug.Log("[Atmospheric Height Fog] " + "Height Fog Global must be added to the scene first!");
            return;
        }

        GameObject go = new GameObject();
        go.AddComponent<HeightFogOverride>();
        go.name = "Height Fog Override";

        Selection.activeGameObject = go;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}

