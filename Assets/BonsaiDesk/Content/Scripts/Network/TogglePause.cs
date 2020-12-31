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
    [SyncVar(hook = nameof(OnSetInteractable))]
    private bool _interactable = true;
    
    [SyncVar(hook = nameof(SetPaused))] private bool _paused = true;

    [SyncVar] private bool _inUse = false;

    [SyncVar(hook = nameof(OnSetAuthority))]
    private uint _authorityIdentityId = uint.MaxValue;

    [SyncVar] private float _visibilitySynced;
    private float _visibilityLocal;

    [SyncVar] private float _positionSynced;
    private float _positionLocal;

    [SyncVar] private Vector3 _positionVector3Synced = Vector3.zero;
    private Vector3 _positionVector3Local = Vector3.zero;

    private float Visibility
    {
        get
        {
            if (NetworkClient.isConnected && NetworkClient.connection.identity.netId == _authorityIdentityId)
                return _visibilityLocal;
            return _visibilitySynced;
        }
        set
        {
            _visibilityLocal = value;
            CmdSetVisibility(value);
        }
    }

    private float Position
    {
        get
        {
            if (NetworkClient.isConnected && NetworkClient.connection.identity.netId == _authorityIdentityId)
                return _positionLocal;
            return _positionSynced;
        }
        set
        {
            _positionLocal = value;
            CmdSetPosition(value);
        }
    }

    private Vector3 PositionVector3
    {
        get
        {
            if (NetworkClient.isConnected && NetworkClient.connection.identity.netId == _authorityIdentityId)
                return _positionVector3Local;
            return _positionVector3Synced;
        }
        set
        {
            _positionVector3Local = value;
            CmdSetPositionVector3(value);
        }
    }

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

    private Vector3 _targetLocalPosition = Vector3.zero;

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (_paused)
        {
            _visibilitySynced = 1;
        }
    }

    private void Start()
    {
        updateIcons(_paused);
    }

    private void Update()
    {
        // if (!_interactable)
        //     return;

        if (!(NetworkClient.isConnected && NetworkClient.connection.identity.netId == _authorityIdentityId))
        {
            togglePauseMorph.SetVisibility(Visibility);
            togglePauseMorph.transform.localPosition = PositionVector3 * Position;
            return;
        }

        bool interacting = currentPointSkeleton != OVRSkeleton.SkeletonType.None ||
                           currentGestureSkeleton != OVRSkeleton.SkeletonType.None;

        bool shouldBeVisible = _paused || interacting;

        float targetVisibility = shouldBeVisible ? 1 : 0;
        float targetPosition = interacting ? 1 : 0;

        Vector3 start = PositionVector3;
        if (Mathf.Approximately(Position, 0))
            start = _targetLocalPosition;
        PositionVector3 = Vector3.MoveTowards(start, _targetLocalPosition, Time.deltaTime);

        //if not already at the target
        if (!Mathf.Approximately(Visibility, targetVisibility))
        {
            CubicBezier easeFunction = shouldBeVisible ? CubicBezier.EaseOut : CubicBezier.EaseIn;
            float t = easeFunction.SampleInverse(Visibility);
            float step = (1f / fadeTime) * Time.deltaTime;
            t = Mathf.MoveTowards(t, targetVisibility, step);
            Visibility = easeFunction.Sample(t);
            togglePauseMorph.SetVisibility(Visibility);
        }

        if (!Mathf.Approximately(Position, targetPosition))
        {
            CubicBezier easeFunction = interacting ? CubicBezier.EaseOut : CubicBezier.EaseIn;
            float t = easeFunction.SampleInverse(Position);
            float step = (1f / fadeTime) * Time.deltaTime;
            t = Mathf.MoveTowards(t, targetPosition, step);
            Position = easeFunction.Sample(t);
        }

        togglePauseMorph.transform.localPosition = PositionVector3 * Position;
    }

    [Server] //This is Server only for a reason. Don't make it a command!!!
    public void SetInteractable(bool interactable)
    {
        _interactable = interactable;
        iconRenderer.enabled = interactable;
    }

    private void OnSetInteractable(bool oldValue, bool newValue)
    {
        iconRenderer.enabled = newValue;
        if (!newValue)
        {
            currentPointSkeleton = OVRSkeleton.SkeletonType.None;
            currentGestureSkeleton = OVRSkeleton.SkeletonType.None;
            updateIcons(_paused);
        }
    }

    [Command(ignoreAuthority = true)]
    private void CmdSetPaused(bool paused)
    {
        _paused = paused;
    }

    private void SetPaused(bool oldPaused, bool newPaused)
    {
        Debug.Log("[BONSAI] SetPaused " + newPaused);
        if (currentGestureSkeleton == OVRSkeleton.SkeletonType.None)
            updateIcons(newPaused);

        PauseChanged?.Invoke(newPaused);
    }

    private void OnSetAuthority(uint oldValue, uint newValue)
    {
        if (NetworkClient.isConnected && NetworkClient.connection.identity.netId == newValue)
        {
            _visibilityLocal = _visibilitySynced;
            _positionLocal = _positionSynced;
            _positionVector3Local = _positionVector3Synced;
        }
    }

    [Command(ignoreAuthority = true)]
    private void CmdSetAuthority(uint userId)
    {
        _authorityIdentityId = userId;
        _inUse = true;
    }

    [Command(ignoreAuthority = true)]
    private void CmdSetInUse(bool inUse)
    {
        _inUse = inUse;
    }

    [Command(ignoreAuthority = true)]
    private void CmdSetVisibility(float visibility)
    {
        _visibilitySynced = visibility;
    }

    [Command(ignoreAuthority = true)]
    private void CmdSetPosition(float position)
    {
        _positionSynced = position;
    }

    [Command(ignoreAuthority = true)]
    private void CmdSetPositionVector3(Vector3 positionVector3)
    {
        _positionVector3Synced = positionVector3;
    }

    private void updateIcons(bool paused)
    {
        togglePauseMorph.SetPaused(paused ? 1 : 0);
    }

    public void Point(OVRSkeleton.SkeletonType skeletonType, bool pointing, Vector3 position)
    {
        if (!_interactable)
            return;

        if (currentPointSkeleton == OVRSkeleton.SkeletonType.None || currentPointSkeleton == skeletonType)
        {
            if (pointing)
            {
                uint userId = NetworkClient.connection.identity.netId;
                if (_authorityIdentityId != userId)
                {
                    if (!_inUse)
                        CmdSetAuthority(userId);
                    return;
                }

                if (!_inUse)
                    CmdSetInUse(true);

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
                uint userId = NetworkClient.connection.identity.netId;
                if (_authorityIdentityId == userId && _inUse)
                {
                    CmdSetInUse(false);
                }

                currentPointSkeleton = OVRSkeleton.SkeletonType.None;
            }
        }
    }

    public void StartToggleGesture(OVRSkeleton.SkeletonType skeletonType, Vector3 position)
    {
        if (!_interactable)
            return;

        if (currentGestureSkeleton == OVRSkeleton.SkeletonType.None && currentPointSkeleton == skeletonType)
        {
            currentGestureSkeleton = skeletonType;
            gestureStartDistance = Vector3.Distance(transform.position, position);
            pausedStateAtGestureStart = _paused;
        }
    }

    public void StopToggleGesture(OVRSkeleton.SkeletonType skeletonType, Vector3 position)
    {
        if (!_interactable)
            return;

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
        if (!_interactable)
            return;

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