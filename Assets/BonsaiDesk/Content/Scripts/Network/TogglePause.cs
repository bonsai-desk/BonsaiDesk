using System;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using Mirror;
using UnityEngine;

public class TogglePause : NetworkBehaviour
{
    public float gestureActivateDistance;
    public MeshRenderer iconRenderer;
    public TogglePauseMorph togglePauseMorph;

    [SyncVar(hook = nameof(SetPaused))] private bool paused = true;

    private bool leftPointing = false;
    private bool rightPointing = false;

    private OVRSkeleton.SkeletonType currentGestureSkeleton = OVRSkeleton.SkeletonType.None;
    private Vector3 gestureStartPosition = Vector3.zero;

    private void Start()
    {
        updateIcons(paused);
    }

    private void Update()
    {
        iconRenderer.enabled =
            (leftPointing || rightPointing || currentGestureSkeleton != OVRSkeleton.SkeletonType.None);
    }

    [Command(ignoreAuthority = true)]
    void CmdSetPaused(bool paused)
    {
        this.paused = paused;
    }

    void SetPaused(bool oldPaused, bool newPaused)
    {
        updateIcons(newPaused);
    }

    void updateIcons(bool paused)
    {
        togglePauseMorph.SetLerp(paused ? 0 : 1);
    }

    public void Point(OVRSkeleton.SkeletonType skeletonType, bool pointing)
    {
        if (skeletonType == OVRSkeleton.SkeletonType.HandLeft)
            leftPointing = pointing;
        else
            rightPointing = pointing;
    }

    public void StartToggleGesture(OVRSkeleton.SkeletonType skeletonType, Vector3 position)
    {
        if (currentGestureSkeleton == OVRSkeleton.SkeletonType.None)
        {
            currentGestureSkeleton = skeletonType;
            gestureStartPosition = position;
        }
    }

    public void StopToggleGesture(OVRSkeleton.SkeletonType skeletonType, Vector3 position)
    {
        if (currentGestureSkeleton == skeletonType)
        {
            currentGestureSkeleton = OVRSkeleton.SkeletonType.None;
            float distance = Vector3.Distance(gestureStartPosition, position);
            bool currentlyPaused = paused;
            if (distance > gestureActivateDistance)
            {
                updateIcons(!currentlyPaused);
                CmdSetPaused(!paused);
            }
            else
            {
                updateIcons(currentlyPaused);
            }
        }
    }

    public void UpdateToggleGesturePosition(OVRSkeleton.SkeletonType skeletonType, Vector3 position)
    {
        if (currentGestureSkeleton == skeletonType)
        {
            float distance = Vector3.Distance(gestureStartPosition, position);
            float lerp = Mathf.Clamp01(distance / gestureActivateDistance);
            if (!paused)
                lerp = 1 - lerp;
            togglePauseMorph.SetLerp(lerp);
        }
    }
}