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
    public bool Interactable => _interactable;
    
    [SyncVar(hook = nameof(OnSetPaused))] private bool _paused = true;
    private bool _probablyPaused = false;

    [SyncVar] private bool _inUse = false;

    [SyncVar(hook = nameof(OnSetAuthority))]
    private uint _authorityIdentityId = uint.MaxValue;

    public uint AuthorityIdentityId => _authorityIdentityId;

    [SyncVar] private float _visibilitySynced;
    private float _visibilityLocal;

    [SyncVar] private float _positionSynced;
    private float _positionLocal;

    [SyncVar] private Vector3 _positionVector3Synced = Vector3.zero;
    private Vector3 _positionVector3Local = Vector3.zero;

    [SyncVar] private float _pauseMorphSynced;
    private float _pauseMorphLocal;

    private float Visibility
    {
        get
        {
            // if (isServer && _authorityIdentityId == uint.MaxValue || NetworkClient.connection != null &&
            //     NetworkClient.connection.identity != null &&
            //     NetworkClient.connection.identity.netId == _authorityIdentityId)
            return _visibilityLocal;
            // return _visibilitySynced;
        }
        set
        {
            _visibilityLocal = value;
            if (isServer && !isClient)
                _visibilitySynced = value;
            else if (NetworkClient.connection != null && NetworkClient.connection.identity != null)
                CmdSetVisibility(value);
        }
    }

    private float Position
    {
        get
        {
            // if (isServer && _authorityIdentityId == uint.MaxValue || NetworkClient.connection != null &&
            //     NetworkClient.connection.identity != null &&
            //     NetworkClient.connection.identity.netId == _authorityIdentityId)
            return _positionLocal;
            // return _positionSynced;
        }
        set
        {
            _positionLocal = value;
            if (isServer && !isClient)
                _positionSynced = value;
            else if (NetworkClient.connection != null && NetworkClient.connection.identity != null)
                CmdSetPosition(value);
        }
    }

    private Vector3 PositionVector3
    {
        get
        {
            // if (isServer && _authorityIdentityId == uint.MaxValue || NetworkClient.connection != null &&
            //     NetworkClient.connection.identity != null &&
            //     NetworkClient.connection.identity.netId == _authorityIdentityId)
            return _positionVector3Local;
            // return _positionVector3Synced;
        }
        set
        {
            _positionVector3Local = value;
            if (isServer && !isClient)
                _positionVector3Synced = value;
            else if (NetworkClient.connection != null && NetworkClient.connection.identity != null)
                CmdSetPositionVector3(value);
        }
    }

    private float PauseMorph
    {
        get
        {
            // if (isServer && _authorityIdentityId == uint.MaxValue || NetworkClient.connection != null &&
            //     NetworkClient.connection.identity != null &&
            //     NetworkClient.connection.identity.netId == _authorityIdentityId)
            return _pauseMorphLocal;
            // return _pauseMorphSynced;
        }
        set
        {
            _pauseMorphLocal = value;
            if (isServer && !isClient || isServer && isClient) {
                // Cameron: added (isServer && isClient) condition
                // this prevents "Send command attempted with no client running" which
                // occurs sometimes when shutting down a room as a host
                _pauseMorphSynced = value;
            }
            else if (NetworkClient.connection != null && NetworkClient.connection.identity != null)
                CmdSetPauseMorph(value);
        }
    }

    public float gestureActivateDistance;
    public float pointMovement;
    public float fadeTime;
    public TogglePauseMorph togglePauseMorph;
    public MeshRenderer iconRenderer;

    public event PauseEvent CmdSetPausedServer;
    public event PauseEvent PauseChangedClient;

    private OVRSkeleton.SkeletonType currentPointSkeleton = OVRSkeleton.SkeletonType.None;

    [HideInInspector] public OVRSkeleton.SkeletonType currentGestureSkeleton = OVRSkeleton.SkeletonType.None;
    private float gestureStartDistance;
    private bool pausedStateAtGestureStart;

    private Vector3 _targetLocalPosition = Vector3.zero;

    public override void OnStartServer()
    {
        base.OnStartServer();

        _paused                                       =  true;
        _visibilitySynced                             =  1;
        _positionSynced                               =  0;
        
        NetworkManagerGame.ServerDisconnect -= HandleServerDisconnect;
        
        NetworkManagerGame.ServerDisconnect += HandleServerDisconnect;
    }

    private void HandleServerDisconnect(object _, NetworkConnection conn) {
        // Cameron: moved this here from NetworkManagerGame
		if (conn.identity != null && AuthorityIdentityId == conn.identity.netId) {
			RemoveClientAuthority();
		}
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        updateIcons(_paused);
    }

    private void Update()
    {
        if (!(isServer && _authorityIdentityId == uint.MaxValue || NetworkClient.connection != null &&
            NetworkClient.connection.identity != null &&
            NetworkClient.connection.identity.netId == _authorityIdentityId))
        {
            _visibilityLocal = Mathf.MoveTowards(_visibilityLocal, _visibilitySynced,
                Mathf.Abs(_visibilitySynced - _visibilityLocal) * Time.deltaTime * (1f / syncInterval));
            _positionLocal = Mathf.MoveTowards(_positionLocal, _positionSynced,
                Mathf.Abs(_positionSynced - _positionLocal) * Time.deltaTime * (1f / syncInterval));
            _positionVector3Local = Vector3.MoveTowards(_positionVector3Local, _positionVector3Synced,
                Vector3.Distance(_positionVector3Synced, _positionVector3Local) * Time.deltaTime * (1f / syncInterval));
            _pauseMorphLocal = Mathf.MoveTowards(_pauseMorphLocal, _pauseMorphSynced,
                Mathf.Abs(_pauseMorphSynced - _pauseMorphLocal) * Time.deltaTime * (1f / syncInterval));

            togglePauseMorph.SetVisibility(Visibility);
            togglePauseMorph.transform.localPosition = PositionVector3 * Position;
            togglePauseMorph.SetPaused(PauseMorph);
            return;
        }

        bool interacting = currentPointSkeleton != OVRSkeleton.SkeletonType.None ||
                           currentGestureSkeleton != OVRSkeleton.SkeletonType.None;

        if (!interacting && _inUse)
        {
            _inUse = false;
            CmdSetInUse(false);
        }

        bool shouldBeVisible = (_paused || _probablyPaused) || interacting;

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
        }

        if (!Mathf.Approximately(Position, targetPosition))
        {
            CubicBezier easeFunction = interacting ? CubicBezier.EaseOut : CubicBezier.EaseIn;
            float t = easeFunction.SampleInverse(Position);
            float step = (1f / fadeTime) * Time.deltaTime;
            t = Mathf.MoveTowards(t, targetPosition, step);
            Position = easeFunction.Sample(t);
        }

        togglePauseMorph.SetVisibility(Visibility);
        togglePauseMorph.transform.localPosition = PositionVector3 * Position;
        togglePauseMorph.SetPaused(PauseMorph);
    }

    [Server] //This is Server only for a reason. Don't make it a command!!!
    public void SetInteractable(bool interactable)
    {
        _interactable = interactable;
        iconRenderer.enabled = interactable;
        updateIcons(_paused);

        if (!interactable)
        {
            _authorityIdentityId = uint.MaxValue;
            _inUse = false;
        }
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

    [Server]
    public void ServerSetPaused(bool paused)
    {
        if (_paused != paused)
        {
            _paused = paused;
        }
        updateIcons(paused);

        //throw new NotImplementedException("[Bonsai] ServerSetPaused");

        //TODO should this event fire?
        // PauseChangedServer?.Invoke(paused);
    }

    [Command(ignoreAuthority = true)]
    private void CmdSetPaused(bool paused)
    {
        if (!_interactable) return;
        
        // todo maybe everything should hold for this condtion
        if (_paused != paused)
        {
            CmdSetPausedServer?.Invoke(paused);
            _paused = paused;
        }
        updateIcons(paused);
    }

    private void OnSetPaused(bool oldPaused, bool newPaused)
    {
        if (currentGestureSkeleton == OVRSkeleton.SkeletonType.None)
            updateIcons(newPaused);

        _probablyPaused = false;

        if (oldPaused != newPaused)
        {
            PauseChangedClient?.Invoke(newPaused);
        }
    }

    private void OnSetAuthority(uint oldValue, uint newValue)
    {
        // if (NetworkClient.connection != null && NetworkClient.connection.identity != null &&
        //     NetworkClient.connection.identity.netId == newValue)
        // {
        _visibilityLocal = _visibilitySynced;
        _positionLocal = _positionSynced;
        _positionVector3Local = _positionVector3Synced;
        // }
    }

    [Command(ignoreAuthority = true)]
    private void CmdSetAuthority(uint userId)
    {
        _authorityIdentityId = userId;
        _inUse = true;
        _pauseMorphSynced = _paused ? 1 : 0;
    }

    [Server]
    public void RemoveClientAuthority()
    {
        _authorityIdentityId = uint.MaxValue;
        _inUse = false;
        PauseMorph = _paused ? 1 : 0;
        togglePauseMorph.SetPaused(PauseMorph);
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
        PauseMorph = paused ? 1 : 0;
    }

    [Command(ignoreAuthority = true)]
    private void CmdSetPauseMorph(float pauseValue)
    {
        _pauseMorphSynced = pauseValue;
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
            PauseMorph = pausedLerp;

            if (distance > gestureActivateDistance)
            {
                currentPointSkeleton = OVRSkeleton.SkeletonType.None;
                currentGestureSkeleton = OVRSkeleton.SkeletonType.None;
                updateIcons(!pausedStateAtGestureStart);
                _probablyPaused = !pausedStateAtGestureStart;
                CmdSetPaused(!pausedStateAtGestureStart);
            }
        }
    }
}