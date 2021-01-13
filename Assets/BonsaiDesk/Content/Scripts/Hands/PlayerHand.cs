using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHand : MonoBehaviour
{
    [HideInInspector] public OVRSkeleton.SkeletonType skeletonType;
    [HideInInspector] public OVRHandTransformMapper physicsMapper;

    private IHandTick[] _handTicks;
    private Dictionary<Type, IHandTick> _handTicksDictionary;

    private readonly Dictionary<Gesture, bool> _gestures = new Dictionary<Gesture, bool>();
    private readonly Dictionary<Gesture, bool> _lastGestures = new Dictionary<Gesture, bool>();
    private readonly Dictionary<Gesture, float> _lastGestureActiveTime = new Dictionary<Gesture, float>();

    public PlayerHand OtherHand => InputManager.Hands.GetOtherHand(skeletonType).PlayerHand;

    public Transform palm;

    public enum Gesture
    {
        IndexPinching,
        Fist,
        FlatFist,
        WeakFist,
        WeakFlatFist,
        WeakPalm
    }

    private void Start()
    {
        _handTicks = GetComponentsInChildren<IHandTick>();
        _handTicksDictionary = new Dictionary<Type, IHandTick>();
        foreach (var handTick in _handTicks)
        {
            _handTicksDictionary.Add(handTick.GetType(), handTick);
            handTick.playerHand = this;
        }

        foreach (Gesture gesture in (Gesture[]) Gesture.GetValues(typeof(Gesture)))
        {
            _gestures.Add(gesture, false);
            _lastGestures.Add(gesture, false);
        }

        if (skeletonType == OVRSkeleton.SkeletonType.HandRight)
        {
            var otherHandTransform = InputManager.Hands.GetOtherHand(skeletonType).PlayerHand.transform;
            foreach (Transform child in transform)
            {
                if (child.name != "Canvas")
                {
                    var otherChild = otherHandTransform.Find(child.name);
                    if (otherChild)
                    {
                        MirrorHandChild(child, otherChild);
                    }
                    else
                    {
                        Debug.LogError("Left hand does not have matching child object");
                    }
                }
            }
        }
    }

    private void MirrorHandChild(Transform source, Transform other)
    {
        //all of this is awful and still only results in the forward direction being mirrored correctly

        var handRotationFix = Quaternion.AngleAxis(180f, Vector3.forward);

        var localPosition = source.localPosition;
        localPosition = handRotationFix * localPosition;
        localPosition.z *= -1f;
        other.localPosition = localPosition;

        var localRotation = source.localRotation;
        localRotation = handRotationFix * localRotation;

        var r1 = localRotation;
        r1.x *= -1f;
        r1.y *= -1f;
        r1 *= Quaternion.AngleAxis(180f, Vector3.right);

        var r2 = localRotation * Quaternion.AngleAxis(90f, Vector3.right);
        r2.x *= -1f;
        r2.y *= -1f;
        r2 *= Quaternion.AngleAxis(180f, Vector3.right);

        localRotation = Quaternion.LookRotation(r1 * Vector3.forward, r2 * Vector3.up);
        localRotation = localRotation * Quaternion.AngleAxis(180f, Vector3.forward);

        other.localRotation = localRotation;
    }

    public bool GetGesture(Gesture gesture)
    {
        if (_gestures.TryGetValue(gesture, out var value))
        {
            return value;
        }

        return false;
    }

    public bool GetLastGesture(Gesture gesture)
    {
        if (_lastGestures.TryGetValue(gesture, out var value))
        {
            return value;
        }

        return false;
    }

    public bool GetGestureStart(Gesture gesture)
    {
        return GetGesture(gesture) && !GetLastGesture(gesture);
    }

    public bool GetGestureStop(Gesture gesture)
    {
        return !GetGesture(gesture) && GetLastGesture(gesture);
    }

    public bool GetGestureActiveWithin(Gesture gesture, float time)
    {
        if (_lastGestureActiveTime.TryGetValue(gesture, out float value))
        {
            return Time.time - value <= time;
        }

        return false;
    }

    public void UpdateLastGestures()
    {
        foreach (Gesture gesture in (Gesture[]) Gesture.GetValues(typeof(Gesture)))
        {
            _lastGestures[gesture] = _gestures[gesture];
        }
    }

    public T GetIHandTick<T>()
    {
        return (T) _handTicksDictionary[typeof(T)];
    }

    public void UpdateGestures()
    {
        float fistStrength = FistStrength();
        float flatFistStrength = FlatFistStrength();

        UpdateGesture(Gesture.IndexPinching, IndexPinching());
        UpdateGesture(Gesture.Fist, fistStrength > 0.7f);
        UpdateGesture(Gesture.FlatFist, flatFistStrength > 0.7f);
        UpdateGesture(Gesture.WeakFist, fistStrength > 0.5f);
        UpdateGesture(Gesture.WeakFlatFist, flatFistStrength > 0.5f);
        UpdateGesture(Gesture.WeakPalm, fistStrength < 0.35f);
    }

    void UpdateGesture(Gesture gesture, bool active)
    {
        _gestures[gesture] = active;
        if (active)
            _lastGestureActiveTime[gesture] = Time.time;
    }

    public void RunHandTicks()
    {
        for (var i = 0; i < _handTicks.Length; i++)
        {
            _handTicks[i].Tick();
        }
    }

    public bool IndexPinching()
    {
        var indexTip = physicsMapper.CustomBones[(int) OVRSkeleton.BoneId.Hand_IndexTip].position;
        var thumbTip = physicsMapper.CustomBones[(int) OVRSkeleton.BoneId.Hand_ThumbTip].position;
        var distance = Vector3.Distance(indexTip, thumbTip);
        return distance < 0.015f;
    }

    public Vector3 PinchPosition()
    {
        var indexTip = physicsMapper.CustomBones[(int) OVRSkeleton.BoneId.Hand_IndexTip].position;
        var thumbTip = physicsMapper.CustomBones[(int) OVRSkeleton.BoneId.Hand_ThumbTip].position;
        return (indexTip + thumbTip) / 2f;
    }

    public float FingerCloseStrength(OVRSkeleton.BoneId boneId)
    {
        float r1 = Vector3.Angle(physicsMapper.transform.right, physicsMapper.CustomBones[(int) boneId].right);
        float r2 = Vector3.Angle(physicsMapper.CustomBones[(int) boneId].right,
            physicsMapper.CustomBones[(int) boneId + 2].right);

        r1 /= 60f;
        r2 /= 175f;

        return Mathf.Clamp01((r1 + r2) / 2f);
    }

    public float FlatFingerStrength(OVRSkeleton.BoneId boneId)
    {
        float r1 = Vector3.Angle(physicsMapper.transform.right, physicsMapper.CustomBones[(int) boneId].right);
        r1 /= 60f;
        return Mathf.Clamp01(r1);
    }

    public float FlatFistStrength()
    {
        float minStrength = FlatFingerStrength(OVRSkeleton.BoneId.Hand_Index1);
        float strength = FlatFingerStrength(OVRSkeleton.BoneId.Hand_Middle1);
        if (strength < minStrength)
            minStrength = strength;
        strength = FlatFingerStrength(OVRSkeleton.BoneId.Hand_Ring1);
        if (strength < minStrength)
            minStrength = strength;
        strength = FlatFingerStrength(OVRSkeleton.BoneId.Hand_Pinky1);
        if (strength < minStrength)
            minStrength = strength;
        return minStrength;
    }

    public float FistStrength()
    {
        float minStrength = FingerCloseStrength(OVRSkeleton.BoneId.Hand_Index1);
        float strength = FingerCloseStrength(OVRSkeleton.BoneId.Hand_Middle1);
        if (strength < minStrength)
            minStrength = strength;
        strength = FingerCloseStrength(OVRSkeleton.BoneId.Hand_Ring1);
        if (strength < minStrength)
            minStrength = strength;
        strength = FingerCloseStrength(OVRSkeleton.BoneId.Hand_Pinky1);
        if (strength < minStrength)
            minStrength = strength;
        return minStrength;
    }
}