using System;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AutoBrowser))]
public class AutoBrowserEditor : Editor
{
    public override void OnInspectorGUI()
    {

        var autoBrowser = target as AutoBrowser;
        
        autoBrowser.initialURL = EditorGUILayout.TextField("Initial URL", autoBrowser.initialURL);

        autoBrowser.width = EditorGUILayout.FloatField("Width", autoBrowser.width);

        autoBrowser.aspect = EditorGUILayout.Vector2Field("Aspect Ratio", autoBrowser.aspect);
        
        EditorGUILayout.Space();
        
        autoBrowser.autoSetResolution = GUILayout.Toggle(autoBrowser.autoSetResolution, 
            " Automatically Determine Resolution");
        
        EditorGUILayout.Space();

        if (!autoBrowser.autoSetResolution)
        {
            autoBrowser.xResolution = EditorGUILayout.IntField("X Resolution", autoBrowser.xResolution);
        }
        else
        {
            EditorGUILayout.LabelField("Calculated X Resolution", 
                AutoBrowser.GetResolution(
                  autoBrowser.width, 
                    autoBrowser.distanceEstimate,
                    autoBrowser.pixelPerDegree
                ).ToString()
            );
            autoBrowser.distanceEstimate =
                EditorGUILayout.FloatField("Distance Estimate:", autoBrowser.distanceEstimate);
            autoBrowser.pixelPerDegree =
                EditorGUILayout.IntField("Pixels Per Degree", autoBrowser.pixelPerDegree);
        }
            
        EditorGUILayout.Space();
        
        autoBrowser.dummyTexture = (Texture) EditorGUILayout.ObjectField("Dummy Texture", autoBrowser.dummyTexture, typeof(Texture), true);

        autoBrowser.holePuncherMaterial = (Material) EditorGUILayout.ObjectField("Hole Puncher Material",
            autoBrowser.holePuncherMaterial, typeof(Material), true);
    }

    private void OnValidate()
    {
        Repaint();
    }
}
