using System;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using Mirror;
using UnityEngine;

public class TogglePause : NetworkBehaviour
{
    public GameObject icons;
    public GameObject playIcon;
    public GameObject pauseIcon;

    [SyncVar(hook = nameof(SetPaused))] private bool paused = true;

    private bool leftPointing = false;
    private bool rightPointing = false;

    private OVRSkeleton.SkeletonType currentGestureSkeleton = OVRSkeleton.SkeletonType.None;
    private Vector3 gestureStartPosition = Vector3.zero;
    private bool gestureComplete = false;

    private void Start()
    {
        updateIcons(paused);
    }

    private void Update()
    {
        icons.SetActive(leftPointing || rightPointing || currentGestureSkeleton != OVRSkeleton.SkeletonType.None);
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
        playIcon.SetActive(paused);
        pauseIcon.SetActive(!paused);
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
            gestureComplete = false;
            currentGestureSkeleton = skeletonType;
            gestureStartPosition = position;
        }
    }

    public void StopToggleGesture(OVRSkeleton.SkeletonType skeletonType)
    {
        if (currentGestureSkeleton == skeletonType)
        {
            currentGestureSkeleton = OVRSkeleton.SkeletonType.None;
        }
    }

    public void UpdateToggleGesturePosition(OVRSkeleton.SkeletonType skeletonType, Vector3 position)
    {
        if (currentGestureSkeleton == skeletonType)
        {
            float distance = Vector3.Distance(gestureStartPosition, position);
            if (distance > 0.1f && !gestureComplete)
            {
                gestureComplete = true;
                CmdSetPaused(!paused);
            }
        }
    }
}
