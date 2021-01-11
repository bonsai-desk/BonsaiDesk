using System;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

public class TabletSpot : NetworkBehaviour
{
    public static TabletSpot Instance;

    private NetworkIdentity _currentTabletIdentity;
    private const float AnimationTime = 0.5f;
    private float _lerpTime;
    private Vector3 _startPosition;
    private Quaternion _startRotation;
    private Vector3 _startScale;
    
    public event Action<string> SetNewVideo;

    public event Action<string> PlayVideo;
    public event Action StopVideo;

    public override void OnStartServer()
    {
        base.OnStartServer();
        Instance = this;
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        Instance = this;
    }

    private void Update()
    {
        if (!_currentTabletIdentity)
            return;

        if (_lerpTime < 1)
        {
            _lerpTime += Time.deltaTime * (1f / AnimationTime);

            float t = CubicBezier.EaseOut.Sample(_lerpTime);
            _currentTabletIdentity.transform.position = Vector3.Lerp(_startPosition, transform.position, t);
            _currentTabletIdentity.transform.rotation = Quaternion.Lerp(_startRotation, transform.rotation, t);

            if (_lerpTime >= 1f)
            {
                _currentTabletIdentity.GetComponent<TabletControl>().SetServerLerping(false);
                _currentTabletIdentity.GetComponent<AutoAuthority>().SetInUse(false);
                //start video
                var videoId = _currentTabletIdentity.GetComponent<TabletControl>().videoId;
                if (!string.IsNullOrEmpty(videoId))
                    PlayVideo?.Invoke(videoId);
            }
        }
        else if (_currentTabletIdentity.transform.position != transform.position)
        {
            _currentTabletIdentity = null;
            //stop video
            StopVideo?.Invoke();
        }
    }

    [Command(ignoreAuthority = true)]
    public void CmdSetNewVideo(NetworkIdentity tabletIdentity)
    {
        SetNewVideo?.Invoke(tabletIdentity.GetComponent<TabletControl>().videoId);
        if (_currentTabletIdentity)
        {
            _currentTabletIdentity.GetComponent<TabletControl>().SetServerLerping(false);
            _currentTabletIdentity.GetComponent<AutoAuthority>().isKinematic = false;
            _currentTabletIdentity.GetComponent<AutoAuthority>().SetInUse(false);

            //if it had activated
            if (_lerpTime >= 1f)
            {
                var tabletBody = _currentTabletIdentity.GetComponent<Rigidbody>();
                tabletBody.isKinematic = false;
                tabletBody.angularVelocity = new Vector3(-Mathf.PI, Random.value - 0.5f, Random.value - 0.5f);
                tabletBody.velocity = new Vector3(1f + (Random.value * 0.15f), 2f + (Random.value * 0.15f),
                    Random.value * 0.3f - 0.15f);
            }
        }

        // _initDistance = Vector3.Distance(transform.position, tabletIdentity.transform.position);
        // _initAngle = Quaternion.Angle(transform.rotation, tabletIdentity.transform.rotation);
        _startPosition = tabletIdentity.transform.position;
        _startRotation = tabletIdentity.transform.rotation;
        _lerpTime = 0;

        _currentTabletIdentity = tabletIdentity;
        tabletIdentity.GetComponent<TabletControl>().SetServerLerping(true);

        //strip authority away from client
        var autoAuthority = tabletIdentity.GetComponent<AutoAuthority>();
        autoAuthority.ServerForceNewOwner(uint.MaxValue, NetworkTime.time, true);
        autoAuthority.isKinematic = true;
    }
}