using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CopyAnimationForLeftHand))]
public class CopyAnimationForLeftHandEditor : Editor
{
    private SerializedProperty animationClips;

    private void OnEnable()
    {
        animationClips = serializedObject.FindProperty("animationClips");
    }

    public override void OnInspectorGUI()
    {
        var copyAnimationForLeftHand = (CopyAnimationForLeftHand)target;
        
        if (GUILayout.Button("Make Copies"))
        {
            copyAnimationForLeftHand.GenerateAnimations();
        }

        serializedObject.Update();
        EditorGUILayout.PropertyField(animationClips);
        serializedObject.ApplyModifiedProperties();
    }
}