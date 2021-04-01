using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class CopyAnimationForLeftHand : MonoBehaviour
{
    public AnimationClip[] animationClips;

    public void GenerateAnimations()
    {
        if (!Application.isEditor)
        {
            return;
        }

        for (int n = 0; n < animationClips.Length; n++)
        {
            var animation = animationClips[n];

            var clip = new AnimationClip();

            var bindings = AnimationUtility.GetCurveBindings(animation);
            for (int i = 0; i < bindings.Length; i++)
            {
                var animationCurve = AnimationUtility.GetEditorCurve(animation, bindings[i]);
                if (animationCurve.keys.Length != 1)
                {
                    Debug.LogError($"Keys length {animationCurve.keys.Length} does not equal 1.");
                    return;
                }

                //editing the key value wasn't working so just make a new keys array
                animationCurve.keys = new[] {new Keyframe(animationCurve.keys[0].time, animationCurve.keys[0].value)};
                clip.SetCurve(bindings[i].path, bindings[i].type, bindings[i].propertyName, animationCurve);
            }

            var name = animation.name.Substring(0, animation.name.Length - "Right".Length);
            AssetDatabase.CreateAsset(clip, $"Assets/BonsaiDesk/Content/Animations/Hands/Left/{name}Left.anim");
        }

        print("Finished converting animations for left hand.");
    }
}