using System.Collections;
using Mirror;
using mixpanel;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;

public class VideoCubeControl : NetworkBehaviour
{
    [SyncVar(hook = nameof(VideoIdHook))] public string videoId;

    [SyncVar(hook = nameof(ServerLerpingHook))]
    private bool _serverLerping;

    public BoxCollider boxCollider;
    public VideoCube videoCube;

    public override void OnStartClient()
    {
        base.OnStartClient();
        StartCoroutine(videoCube.LoadThumbnail(videoId));
        SetInteractable(!_serverLerping);
    }

    [Server]
    public void SetServerLerping(bool serverLerping)
    {
        if (serverLerping == _serverLerping)
        {
            return;
        }

        _serverLerping = serverLerping;
        SetInteractable(!serverLerping);
    }

    private void SetInteractable(bool interactable)
    {
        boxCollider.enabled = interactable;
    }

    public void StartVideo()
    {
        VideoCubeSpot.Instance.CmdSetNewVideo(netIdentity);
        Mixpanel.Track("Start Video");
    }

    private void VideoIdHook(string oldValue, string newValue)
    {
        StartCoroutine(videoCube.LoadThumbnail(newValue));
    }

    private void ServerLerpingHook(bool oldValue, bool newValue)
    {
        SetInteractable(!newValue);
    }
}