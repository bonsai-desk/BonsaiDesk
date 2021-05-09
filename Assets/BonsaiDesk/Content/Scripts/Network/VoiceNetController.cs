using System;
using Mirror;
using UnityEngine;
using VivoxUnity;

public class VoiceNetController : NetworkBehaviour
{
    private string _lobbyChannelName = "";

    public override void OnStartServer()
    {
        NetworkManagerGame.Singleton.ServerAddPlayer -= HandleServerAddPlayer;

        NetworkManagerGame.Singleton.ServerAddPlayer += HandleServerAddPlayer;

        _lobbyChannelName = Guid.NewGuid().ToString();

        BonsaiLog($"Set lobby channel name: {_lobbyChannelName}");
    }

    [Server]
    private void HandleServerAddPlayer(NetworkConnection conn, bool isLanOnly)
    {
        if (!isLanOnly)
        {
            TargetJoinVoice(conn, _lobbyChannelName);
        }
        else
        {
            BonsaiLog("Not joining voice channel since isLanOnly");
        }
    }

    [TargetRpc]
    private void TargetJoinVoice(NetworkConnection target, string lobbyName)
    {
        BonsaiLog($"Rpc join voice channel ({lobbyName})");
        VoiceManager.Singleton.StartJoinChannel(lobbyName);
    }

    public override void OnStopClient()
    {
        BonsaiLog($"StopClient while {VoiceManager.Singleton.LoginState}");
        if (VoiceManager.Singleton.LoginState == LoginState.LoggedIn)
        {
            VoiceManager.Singleton.DisconnectAllChannels();
        }
    }

    public void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            if (_lobbyChannelName.Length > 0)
            {
                BonsaiLog($"Re join voice channel ({_lobbyChannelName})");
                VoiceManager.Singleton.StartJoinChannel(_lobbyChannelName);
            }
            else
            {
                BonsaiLogError("Lobby channel name was empty on restart so can't rejoin");
            }
        }
        
    }

    private void BonsaiLog(string msg)
    {
        Debug.Log("<color=green>BonsaiVoiceNet: </color>: " + msg);
    }

    private void BonsaiLogWarning(string msg)
    {
        Debug.LogWarning("<color=green>BonsaiVoiceNet: </color>: " + msg);
    }

    private void BonsaiLogError(string msg)
    {
        Debug.LogError("<color=green>BonsaiVoiceNet: </color>: " + msg);
    }
}