using System;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AutoBrowser))]
public class AutoBrowserEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var autoBrowser = target as AutoBrowser;

        autoBrowser.startingAspect = EditorGUILayout.Vector2Field("Starting Aspect", autoBrowser.startingAspect);
        
        autoBrowser.holePuncher = (Transform) EditorGUILayout.ObjectField("Bounds",
            autoBrowser.holePuncher, typeof(Transform), true);

        EditorGUILayout.Space();

        autoBrowser.togglePause = (TogglePause) EditorGUILayout.ObjectField("Toggle Pause",
            autoBrowser.togglePause, typeof(TogglePause), true);
        
        autoBrowser.holePuncherMaterial = (Material) EditorGUILayout.ObjectField("Hole Puncher Material",
            autoBrowser.holePuncherMaterial, typeof(Material), true);

        EditorGUILayout.Space();

        autoBrowser.autoSetResolution = GUILayout.Toggle(autoBrowser.autoSetResolution,
            " Automatically Determine Resolution");

        EditorGUILayout.Space();

        if (!autoBrowser.autoSetResolution)
        {
            autoBrowser.yResolution = EditorGUILayout.IntField("Y Resolution", autoBrowser.yResolution);
        }
        else if (autoBrowser.holePuncher != null)
        {
            EditorGUILayout.LabelField("Calculated Y Resolution",
                AutoBrowser.ResolvablePixels(
                    autoBrowser.holePuncher.localScale.y,
                    autoBrowser.distanceEstimate,
                    autoBrowser.pixelPerDegree
                ).ToString()
            );
            autoBrowser.distanceEstimate =
                EditorGUILayout.FloatField("Distance Estimate:", autoBrowser.distanceEstimate);
            autoBrowser.pixelPerDegree =
                EditorGUILayout.IntField("Pixels Per Degree", autoBrowser.pixelPerDegree);
        }
        else
        {
            EditorGUILayout.LabelField("Hole Puncher must be set to use auto resolution");
        }
    }

    private void OnValidate()
    {
        Repaint();
    }
}