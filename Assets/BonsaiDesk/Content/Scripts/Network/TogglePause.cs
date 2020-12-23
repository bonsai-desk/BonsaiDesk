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
    public MeshRenderer iconRenderer;
    public TogglePauseMorph togglePauseMorph;

    [SyncVar(hook = nameof(SetPaused))] private bool paused = true;

    private OVRSkeleton.SkeletonType currentPointSkeleton = OVRSkeleton.SkeletonType.None;

    private OVRSkeleton.SkeletonType currentGestureSkeleton = OVRSkeleton.SkeletonType.None;
    private Vector3 gestureStartPosition = Vector3.zero;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
        updateIcons(paused);
    }

    private void Update()
    {
        iconRenderer.enabled =
            (currentPointSkeleton != OVRSkeleton.SkeletonType.None ||
             currentGestureSkeleton != OVRSkeleton.SkeletonType.None);
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

    public void Point(OVRSkeleton.SkeletonType skeletonType, bool pointing, Vector3 position)
    {
        if (currentPointSkeleton == OVRSkeleton.SkeletonType.None || currentPointSkeleton == skeletonType)
        {
            if (pointing)
            {
                currentPointSkeleton = skeletonType;

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