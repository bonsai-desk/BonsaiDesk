using System;
using Mirror;
using TMPro;
using UnityEngine;

public class NetworkVideoPlayer : NetworkBehaviour
{
    public enum VideoPlayerState
    {
        Loading,
        Ready
    }

    public TextMeshProUGUI textMesh;


    [SyncVar(hook = nameof(SetState))] public VideoPlayerState state;

    public void Start()
    {
        SetState(state, state);
    }

    [Command(ignoreAuthority = true)]
    public void CmdSetState(VideoPlayerState newState)
    {
        state = newState;
    }

    public void ClickLoading()
    {
        CmdSetState(VideoPlayerState.Loading);
    }

    public void ClickReady()
    {
        CmdSetState(VideoPlayerState.Ready);
    }

    private void SetState(VideoPlayerState oldState, VideoPlayerState newState)
    {
        switch (newState)
        {
            case VideoPlayerState.Loading:
                textMesh.text = "LOADING";
                break;

            case VideoPlayerState.Ready:
                textMesh.text = "READY";
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }
    }
}