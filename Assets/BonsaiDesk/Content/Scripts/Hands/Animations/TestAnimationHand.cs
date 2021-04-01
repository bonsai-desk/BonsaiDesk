using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TestAnimationHand : MonoBehaviour
{
    public Animator animator;
    // public AnimationClip animation;

    private void Start()
    {
        // if (!Application.isEditor)
        // {
        //     return;
        // }
        //
        // var clip = new AnimationClip();
        //
        // var bindings = AnimationUtility.GetCurveBindings(animation);
        // for (int i = 0; i < bindings.Length; i++)
        // {
        //     var animationCurve = AnimationUtility.GetEditorCurve(animation, bindings[i]);
        //     if (animationCurve.keys.Length != 1)
        //     {
        //         Debug.LogError($"Keys length {animationCurve.keys.Length} does not equal 1.");
        //         return;
        //     }
        //
        //     //editing the key value wasn't working so just make a new keys array
        //     animationCurve.keys = new[] {new Keyframe(animationCurve.keys[0].time, animationCurve.keys[0].value + 25f)};
        //     clip.SetCurve(bindings[i].path, bindings[i].type, bindings[i].propertyName, animationCurve);
        // }
        //
        // AssetDatabase.CreateAsset(clip, "Assets/BonsaiDesk/Content/Animations/Hands/FistCopy.anim");
    }

    private void Update()
    {
        var value = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch);
        animator.SetFloat("DefaultFistBlend", value);
    }
}