using System;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using Mirror;
using UnityEngine;

public delegate void PauseEvent(bool paused);

public class TogglePause : NetworkBehaviour
{
    public event PauseEvent PauseChanged;
    
    public float gestureActivateDistance;
    public float pointMovement;
    public float fadeTime;
    public TogglePauseMorph togglePauseMorph;

    [SyncVar(hook = nameof(SetPaused))] private bool paused = true;

    private OVRSkeleton.SkeletonType currentPointSkeleton = OVRSkeleton.SkeletonType.None;

    private OVRSkeleton.SkeletonType currentGestureSkeleton = OVRSkeleton.SkeletonType.None;
    private float gestureStartDistance;
    private bool pausedStateAtGestureStart;

    private float lastInteractTime = -10f;
    private float interactStartTime = -10f;

    private void Start()
    {
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
        Debug.Log("[BONSAI] SetPaused " + newPaused);
        if (currentGestureSkeleton == OVRSkeleton.SkeletonType.None)
            updateIcons(newPaused);
        
        PauseChanged?.Invoke(newPaused);
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
                    Vector3 direction = (position - transform.position).normalized;
                    Vector3 newPosition = transform.position + (direction * pointMovement);
                    newPosition.z = Mathf.Clamp(newPosition.z, 0.0001f, newPosition.z);
                    togglePauseMorph.transform.position = newPosition;
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
            gestureStartDistance = Vector3.Distance(transform.position, position);
            pausedStateAtGestureStart = paused;
        }
    }

    public void StopToggleGesture(OVRSkeleton.SkeletonType skeletonType, Vector3 position)
    {
        if (currentGestureSkeleton == skeletonType)
        {
            currentGestureSkeleton = OVRSkeleton.SkeletonType.None;
            lastInteractTime = Time.time;
            if (Vector3.Distance(transform.position, position) - gestureStartDistance > gestureActivateDistance)
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
            float distance = Vector3.Distance(transform.position, position) - gestureStartDistance;
            float lerp = Mathf.Clamp01(distance / gestureActivateDistance);
            if (!paused)
                lerp = 1 - lerp;
            togglePauseMorph.SetLerp(CubicBezier.EaseInOut.Sample(lerp));
        }
    }
}