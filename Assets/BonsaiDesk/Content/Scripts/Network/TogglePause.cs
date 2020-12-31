using System;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using Mirror;
using UnityEngine;
using UnityEngine.Profiling;

public delegate void PauseEvent(bool paused);

public class TogglePause : NetworkBehaviour
{
    [SyncVar(hook = nameof(SetPaused))] private bool _paused = true;
    // [SyncVar] private bool _interactable = true;

    [SyncVar(hook = nameof(SetActiveUser))]
    private uint _activeUser = uint.MaxValue;

    private uint _localActiveUser = uint.MaxValue;
    
    public float gestureActivateDistance;
    public float pointMovement;
    public float fadeTime;
    public TogglePauseMorph togglePauseMorph;
    public MeshRenderer iconRenderer;

    public event PauseEvent PauseChanged;

    private OVRSkeleton.SkeletonType currentPointSkeleton = OVRSkeleton.SkeletonType.None;

    [HideInInspector] public OVRSkeleton.SkeletonType currentGestureSkeleton = OVRSkeleton.SkeletonType.None;
    private float gestureStartDistance;
    private bool pausedStateAtGestureStart;

    private float _visibility;
    private float _position;

    private Vector3 _targetLocalPosition = Vector3.zero;
    private Vector3 _localPosition = Vector3.zero;

    private void Start()
    {
        updateIcons(_paused);
    }

    private void Update()
    {
        // if (!_interactable)
        //     return;

        bool interacting = currentPointSkeleton != OVRSkeleton.SkeletonType.None ||
                           currentGestureSkeleton != OVRSkeleton.SkeletonType.None;

        bool shouldBeVisible = _paused || interacting;

        float targetVisibility = shouldBeVisible ? 1 : 0;
        float targetPosition = interacting ? 1 : 0;

        if (Mathf.Approximately(_position, 0))
            _localPosition = _targetLocalPosition;
        _localPosition = Vector3.MoveTowards(_localPosition, _targetLocalPosition, Time.deltaTime);

        //if not already at the target
        if (!Mathf.Approximately(_visibility, targetVisibility))
        {
            CubicBezier easeFunction = shouldBeVisible ? CubicBezier.EaseOut : CubicBezier.EaseIn;
            float t = easeFunction.SampleInverse(_visibility);
            float step = (1f / fadeTime) * Time.deltaTime;
            t = Mathf.MoveTowards(t, targetVisibility, step);
            _visibility = easeFunction.Sample(t);
            togglePauseMorph.SetVisibility(_visibility);
        }

        if (!Mathf.Approximately(_position, targetPosition))
        {
            CubicBezier easeFunction = interacting ? CubicBezier.EaseOut : CubicBezier.EaseIn;
            float t = easeFunction.SampleInverse(_position);
            float step = (1f / fadeTime) * Time.deltaTime;
            t = Mathf.MoveTowards(t, targetPosition, step);
            _position = easeFunction.Sample(t);
        }

        togglePauseMorph.transform.localPosition = _localPosition * _position;
    }

    // public void SetInteractable(bool interactable)
    // {
    //     _interactable = interactable;
    //
    //     iconRenderer.enabled = _interactable;
    //     if (!_interactable)
    //     {
    //         currentPointSkeleton = OVRSkeleton.SkeletonType.None;
    //         currentGestureSkeleton = OVRSkeleton.SkeletonType.None;
    //         updateIcons(_paused);
    //     }
    // }

    [Command(ignoreAuthority = true)]
    public void CmdSetPaused(bool paused)
    {
        this.paused = paused;
    }

    private void SetPaused(bool oldPaused, bool newPaused)
    {
        Debug.Log("[BONSAI] SetPaused " + newPaused);
        if (currentGestureSkeleton == OVRSkeleton.SkeletonType.None)
            updateIcons(newPaused);

        PauseChanged?.Invoke(newPaused);
    }

    [Command(ignoreAuthority = true)]
    private void CmdSetActiveUser(uint userId)
    {
        _activeUser = userId;
    }

    private void SetActiveUser(uint oldUser, uint newUser)
    {
        _localActiveUser = uint.MaxValue;
    }

    private void updateIcons(bool paused)
    {
        togglePauseMorph.SetPaused(paused ? 1 : 0);
    }

    public void Point(OVRSkeleton.SkeletonType skeletonType, bool pointing, Vector3 position)
    {
        // if (!_interactable)
        //     return;

        uint userId = NetworkClient.connection.identity.netId;
        bool valid = _activeUser == uint.MaxValue || _activeUser == userId || _localActiveUser == userId;
        if (!valid)
            return;
        if (_activeUser == uint.MaxValue && _localActiveUser == uint.MaxValue)
        {
            _localActiveUser = userId;
            CmdSetActiveUser(userId);
        }

        if (currentPointSkeleton == OVRSkeleton.SkeletonType.None || currentPointSkeleton == skeletonType)
        {
            if (pointing)
            {
                currentPointSkeleton = skeletonType;

                if (currentGestureSkeleton == OVRSkeleton.SkeletonType.None)
                {
                    Vector3 direction = (position - transform.position).normalized;
                    Vector3 newPosition = transform.position + (direction * pointMovement);
                    _targetLocalPosition = transform.InverseTransformPoint(newPosition);
                }
            }
            else
            {
                currentPointSkeleton = OVRSkeleton.SkeletonType.None;
                _localActiveUser = uint.MaxValue;
                CmdSetActiveUser(uint.MaxValue);
            }
        }
    }

    public void StartToggleGesture(OVRSkeleton.SkeletonType skeletonType, Vector3 position)
    {
        // if (!_interactable)
        //     return;

        if (currentGestureSkeleton == OVRSkeleton.SkeletonType.None && currentPointSkeleton == skeletonType)
        {
            currentGestureSkeleton = skeletonType;
            gestureStartDistance = Vector3.Distance(transform.position, position);
            pausedStateAtGestureStart = _paused;
        }
    }

    public void StopToggleGesture(OVRSkeleton.SkeletonType skeletonType, Vector3 position)
    {
        // if (!_interactable)
        //     return;

        if (currentGestureSkeleton == skeletonType)
        {
            currentGestureSkeleton = OVRSkeleton.SkeletonType.None;
            float distance = Vector3.Distance(transform.position, position) - gestureStartDistance;
            if (distance <= gestureActivateDistance)
            {
                updateIcons(_paused);
            }
        }
    }

    public void UpdateToggleGesturePosition(OVRSkeleton.SkeletonType skeletonType, Vector3 position)
    {
        // if (!_interactable)
        //     return;

        if (currentGestureSkeleton == skeletonType)
        {
            float distance = Vector3.Distance(transform.position, position) - gestureStartDistance;
            float lerp = Mathf.Clamp01(distance / gestureActivateDistance);
            float pausedLerp = CubicBezier.EaseInOut.Sample(lerp);
            if (_paused)
                pausedLerp = 1 - pausedLerp;
            togglePauseMorph.SetPaused(pausedLerp);

            if (distance > gestureActivateDistance)
            {
                currentPointSkeleton = OVRSkeleton.SkeletonType.None;
                currentGestureSkeleton = OVRSkeleton.SkeletonType.None;
                updateIcons(!pausedStateAtGestureStart);
                CmdSetPaused(!pausedStateAtGestureStart);
            }
        }
    }
}