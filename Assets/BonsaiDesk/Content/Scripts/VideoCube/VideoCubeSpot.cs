using System;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

public class VideoCubeSpot : NetworkBehaviour
{
    public static VideoCubeSpot Instance;

    private NetworkIdentity _currentVideoIdentity;
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

        TableBrowserMenu.Singleton.EjectVideo -= HandleEjectVideo;

        TableBrowserMenu.Singleton.EjectVideo += HandleEjectVideo;

        Instance = this;
    }

    private void HandleEjectVideo(object sender, EventArgs e)
    {
        CmdEjectCurrentVideo();
    }

    [Command(ignoreAuthority = true)]
    private void CmdEjectCurrentVideo()
    {
        if (_currentVideoIdentity)
        {
            EjectCurrentVideo();
        }
    }

    [Server]
    public void ServerEjectCurrentVideo()
    {
        if (_currentVideoIdentity)
        {
            EjectCurrentVideo();
        }
    } 

    private void Update()
    {
        if (!_currentVideoIdentity)
            return;

        if (_lerpTime < 1)
        {
            _lerpTime += Time.deltaTime * (1f / AnimationTime);

            float t = CubicBezier.EaseOut.Sample(_lerpTime);
            _currentVideoIdentity.transform.position = Vector3.Lerp(_startPosition, transform.position, t);
            _currentVideoIdentity.transform.rotation = Quaternion.Lerp(_startRotation, transform.rotation, t);

            if (_lerpTime >= 1f)
            {
                _currentVideoIdentity.GetComponent<VideoCubeControl>().SetServerLerping(false);
                _currentVideoIdentity.GetComponent<AutoAuthority>().SetInUse(false);
                //start video
                var videoId = _currentVideoIdentity.GetComponent<VideoCubeControl>().videoId;
                if (!string.IsNullOrEmpty(videoId))
                    PlayVideo?.Invoke(videoId);
            }
        }
        else if (_currentVideoIdentity.transform.position != transform.position)
        {
            _currentVideoIdentity = null;
            //stop video
            StopVideo?.Invoke();
        }
    }

    private void EjectCurrentVideo()
    {
        _currentVideoIdentity.GetComponent<VideoCubeControl>().SetServerLerping(false);
        _currentVideoIdentity.GetComponent<AutoAuthority>().isKinematic = false;
        _currentVideoIdentity.GetComponent<AutoAuthority>().SetInUse(false);

        //if it had activated
        if (_lerpTime >= 1f)
        {
            var videoBody = _currentVideoIdentity.GetComponent<Rigidbody>();
            videoBody.isKinematic = false;
            videoBody.angularVelocity = new Vector3(-Mathf.PI, Random.value - 0.5f, Random.value - 0.5f);
            videoBody.velocity = new Vector3((2 * Random.value - 1) / 2, 1f + (Random.value * 0.15f), -(2f + (Random.value * 0.15f)));
        }
    }

    [Command(ignoreAuthority = true)]
    public void CmdSetNewVideo(NetworkIdentity videoIdentity)
    {
        SetNewVideo?.Invoke(videoIdentity.GetComponent<VideoCubeControl>().videoId);
        if (_currentVideoIdentity)
        {
            EjectCurrentVideo();
        }

        // _initDistance = Vector3.Distance(transform.position, videoIdentity.transform.position);
        // _initAngle = Quaternion.Angle(transform.rotation, videoIdentity.transform.rotation);
        _startPosition = videoIdentity.transform.position;
        _startRotation = videoIdentity.transform.rotation;
        _lerpTime = 0;

        _currentVideoIdentity = videoIdentity;
        videoIdentity.GetComponent<VideoCubeControl>().SetServerLerping(true);

        //strip authority away from client
        var autoAuthority = videoIdentity.GetComponent<AutoAuthority>();
        autoAuthority.ServerForceNewOwner(uint.MaxValue, NetworkTime.time, true);
        autoAuthority.isKinematic = true;
    }
}