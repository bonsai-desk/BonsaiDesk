using System;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using Mirror;
using UnityEngine;

public class TogglePause : NetworkBehaviour
{
    public float gestureActivateDistance;
    public float pointMovement;
    public float fadeTime;
    public TogglePauseMorph togglePauseMorph;

    [SyncVar(hook = nameof(SetPaused))] private bool paused = true;

    private OVRSkeleton.SkeletonType currentPointSkeleton = OVRSkeleton.SkeletonType.None;

    private OVRSkeleton.SkeletonType currentGestureSkeleton = OVRSkeleton.SkeletonType.None;
    private float gestureStartDistance;
    private bool pausedStateAtGestureStart;

    private Vector3 startPosition;

    private float lastInteractTime = 0;
    private float interactStartTime = 0;

    private void Start()
    {
        startPosition = transform.position;
        updateIcons(paused);
    }

    private void Update()
    {
        if (currentPointSkeleton != OVRSkeleton.SkeletonType.None ||
            currentGestureSkeleton != OVRSkeleton.SkeletonType.None)
        {
            float t = Time.time - interactStartTime;
            float lerp = Mathf.Clamp01(t / fadeTime);
            togglePauseMorph.SetFade(CubicBezier.EaseOut.Sample(lerp));
        }
        else
        {
            float t = Time.time - lastInteractTime;
            float lerp = Mathf.Clamp01(t / fadeTime);
            togglePauseMorph.SetFade(1 - CubicBezier.EaseIn.Sample(lerp));
        }
    }

    [Command(ignoreAuthority = true)]
    void CmdSetPaused(bool paused)
    {
        this.paused = paused;
    }

    void SetPaused(bool oldPaused, bool newPaused)
    {
        if (currentGestureSkeleton == OVRSkeleton.SkeletonType.None)
            updateIcons(newPaused);
    }

    void updateIcons(bool paused)
    {
        togglePauseMorph.SetLerp(paused ? 0 : 1);
    }

    public void Point(OVRSkeleton.SkeletonType skeletonType, bool pointing, Vector3 position)
    {
        if (currentPointSkeleton == OVRSkeleton.SkeletonType.None && currentGestureSkeleton == OVRSkeleton.SkeletonType.None && pointing)
        {
            interactStartTime = Time.time;
        }

        if (currentPointSkeleton == OVRSkeleton.SkeletonType.None || currentPointSkeleton == skeletonType)
        {
            if (pointing)
            {
                currentPointSkeleton = skeletonType;
                lastInteractTime = Time.time;

                if (currentGestureSkeleton == OVRSkeleton.SkeletonType.None)
                {
                    Vector3 direction = (position - startPosition).normalized;
                    Vector3 newPosition = startPosition + (direction * pointMovement);
                    newPosition.z = Mathf.Clamp(newPosition.z, 0.0001f, newPosition.z);
                    transform.position = newPosition;
                }
            }
            else
            {
                currentPointSkeleton = OVRSkeleton.SkeletonType.None;
            }
        }
    }

    public void StartToggleGesture(OVRSkeleton.SkeletonType skeletonType, Vector3 position)
    {
        if (currentGestureSkeleton == OVRSkeleton.SkeletonType.None && currentPointSkeleton == skeletonType)
        {
            currentGestureSkeleton = skeletonType;
            gestureStartDistance = Vector3.Distance(startPosition, position);
            pausedStateAtGestureStart = paused;
        }
    }

    public void StopToggleGesture(OVRSkeleton.SkeletonType skeletonType, Vector3 position)
    {
        if (currentGestureSkeleton == skeletonType)
        {
            currentGestureSkeleton = OVRSkeleton.SkeletonType.None;
            lastInteractTime = Time.time;
            if (Vector3.Distance(startPosition, position) - gestureStartDistance > gestureActivateDistance)
            {
                updateIcons(!pausedStateAtGestureStart);
                CmdSetPaused(!pausedStateAtGestureStart);
            }
            else
            {
                updateIcons(paused);
            }
        }
    }

    public void UpdateToggleGesturePosition(OVRSkeleton.SkeletonType skeletonType, Vector3 position)
    {
        if (currentGestureSkeleton == skeletonType)
        {
            float distance = Vector3.Distance(startPosition, position) - gestureStartDistance;
            float lerp = Mathf.Clamp01(distance / gestureActivateDistance);
            if (!paused)
                lerp = 1 - lerp;
            togglePauseMorph.SetLerp(CubicBezier.EaseInOut.Sample(lerp));
        }
    }
}