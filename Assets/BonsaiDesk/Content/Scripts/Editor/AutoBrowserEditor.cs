using System;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AutoBrowser))]
public class AutoBrowserEditor : Editor
{
    public override void OnInspectorGUI()
    {

        var autoBrowser = target as AutoBrowser;

        autoBrowser.height = EditorGUILayout.FloatField("Height", autoBrowser.height);

        autoBrowser.aspect = EditorGUILayout.Vector2Field("Initial Aspect Ratio", autoBrowser.aspect);
        
        EditorGUILayout.Space();
        
        autoBrowser.autoSetResolution = GUILayout.Toggle(autoBrowser.autoSetResolution, 
            " Automatically Determine Resolution");
        
        EditorGUILayout.Space();

        if (!autoBrowser.autoSetResolution)
        {
            autoBrowser.yResolution = EditorGUILayout.IntField("Y Resolution", autoBrowser.yResolution);
        }
        else
        {
            EditorGUILayout.LabelField("Calculated Y Resolution", 
                AutoBrowser.ResolvablePixels(
                  autoBrowser.height, 
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

        autoBrowser.holePuncherMaterial = (Material) EditorGUILayout.ObjectField("Hole Puncher Material",
            autoBrowser.holePuncherMaterial, typeof(Material), true);
        
        autoBrowser.dummyMaterial = (Material) EditorGUILayout.ObjectField("Dummy Material",
            autoBrowser.dummyMaterial, typeof(Material), true);
    }

    private void OnValidate()
    {
        Repaint();
    }
}
