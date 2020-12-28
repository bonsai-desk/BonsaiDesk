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

    private float _visibility;

    private Vector3 _targetLocalPosition = Vector3.zero;

    private void Start()
    {
        updateIcons(paused);
    }

    private void Update()
    {
        float targetVisibility;
        CubicBezier easeFunction;

        //if pointing or mid gesture
        if (currentPointSkeleton != OVRSkeleton.SkeletonType.None ||
            currentGestureSkeleton != OVRSkeleton.SkeletonType.None)
        {
            targetVisibility = 1;
            easeFunction = CubicBezier.EaseOut;
        }
        else
        {
            targetVisibility = 0;
            easeFunction = CubicBezier.EaseIn;
        }

        if (!Mathf.Approximately(_visibility, targetVisibility))
        {
            float t = easeFunction.SampleInverse(_visibility);
            float step = (1f / fadeTime) * Time.deltaTime;
            t = Mathf.MoveTowards(t, targetVisibility, step);
            _visibility = easeFunction.Sample(t);
            togglePauseMorph.SetVisibility(_visibility);
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
        togglePauseMorph.SetPaused(paused ? 1 : 0);
    }

    public void Point(OVRSkeleton.SkeletonType skeletonType, bool pointing, Vector3 position)
    {
        if (currentPointSkeleton == OVRSkeleton.SkeletonType.None &&
            currentGestureSkeleton == OVRSkeleton.SkeletonType.None && pointing)
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
            float pausedLerp = CubicBezier.EaseInOut.Sample(lerp);
            if (paused)
                pausedLerp = 1 - pausedLerp;
            togglePauseMorph.SetPaused(pausedLerp);
        }
    }
}