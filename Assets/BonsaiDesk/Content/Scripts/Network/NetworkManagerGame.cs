﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dissonance;
using Mirror;
using NobleConnect.Mirror;
using OVRSimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Networking;
using UnityEngine.XR.Management;

public class NetworkManagerGame : NobleNetworkManager
{
    public static NetworkManagerGame Singleton;
    public TableBrowser tableBrowser;
    private float postRoomInfoEvery = 1f;
    private float postRoomInfoLast;

    public bool serverOnlyIfEditor;
    public bool visualizeAuthority;

    public MoveToDesk moveToDesk;

    private Camera _camera;

    public TogglePause togglePause;
    


    public readonly Dictionary<NetworkConnection, PlayerInfo> PlayerInfos =
        new Dictionary<NetworkConnection, PlayerInfo>();

    [Serializable]
    public class PlayerInfo
    {
        public int spot;

        public PlayerInfo(int spot)
        {
            this.spot = spot;
        }
    }

    private readonly bool[] _spotInUse = new bool[2];

    public static int AssignedColorIndex;



    public enum VideoState
    {
        None,
        Cued,
        Playing
    }

    public VideoState videoState = VideoState.None;



    public TextMeshProUGUI textMesh;
    public BonsaiScreenFade fader;

    public string apiBaseUri = "https://api.desk.link";
    public float waitBeforeSpawnButton = 0.4f;
    public int roomTagLength = 4;
    public float refreshRoomEverySeconds = 5;

    public GameObject neutralButtons;
    public GameObject relayFailedButtons;

    private string _assignedRoomTag = "";
    private DissonanceComms _comms;

    private ConnectionState _connectionState;
    private string _enteredRoomTag = "";
    private string _fakeRoomTag;
    private UnityWebRequest _roomRequest;

    public ConnectionState State
    {
        get => _connectionState;
        set
        {
            if (_connectionState == value) Debug.LogWarning("[BONSAI] Trying to set state to itself: " + State);

            Debug.Log("[BONSAI] HandleState Cleanup " + _connectionState);
            HandleState(_connectionState, Work.Cleanup);

            _connectionState = value;

            Debug.Log("[BONSAI] HandleState Setup " + value);
            HandleState(value, Work.Setup);
        }
    }

    public enum ConnectionState
    {
        Waking,
        RelayError,
        Loading,
        Neutral,
        HostCreating,
        HostWaiting,
        Hosting,
        ClientEntry,
        ClientConnecting,
        ClientConnected
    }

    private enum Work
    {
        Setup,
        Cleanup
    }



    private IEnumerator StopHostFadeReturnToLoading()
    {
        fader.FadeOut();
        yield return new WaitForSeconds(fader.fadeTime);
        StopHost();
        yield return new WaitForSeconds(0.5f);
        State = ConnectionState.Loading;
        fader.FadeIn();
    }

    private void UpdateClientEntryText()
    {
        var displayRoomTag = _enteredRoomTag + _fakeRoomTag.Substring(
            0, _fakeRoomTag.Count() - _enteredRoomTag.Count());

        textMesh.text = "Join\n\n" + displayRoomTag;
    }

    private void OnShouldDisconnect(ShouldDisconnectMessage _)
    {
        StartCoroutine(FadeThenReturnToLoading());
    }

    private static void SetCommsActive(DissonanceComms comms, bool active)
    {
        if (comms == null) return;
        if (active)
        {
            comms.IsMuted = false;
            comms.IsDeafened = false;
        }
        else
        {
            comms.IsMuted = true;
            comms.IsDeafened = true;
        }
    }

    private IEnumerator FadeToState(ConnectionState newState)
    {
        fader.FadeOut();
        yield return new WaitForSeconds(fader.fadeTime);
        State = newState;
        yield return new WaitForSeconds(0.5f);
        fader.FadeIn();
    }

    private void HandleState(ConnectionState state, Work work)
    {
        switch (state)
        {
            // Default state on start
            case ConnectionState.Waking:
                break;

            case ConnectionState.RelayError:
                if (work == Work.Setup)
                {
                    isLANOnly = true;
                    textMesh.text = "Internet Disconnected!\n\n\n\n(reconnect)";
                    Debug.Log("[BONSAI] RelayError Setup");
                    ActivateButtons(relayFailedButtons, waitBeforeSpawnButton);
                }
                else
                {
                    DisableButtons(relayFailedButtons);
                }

                break;

            // Waiting for a HostEndPoint
            case ConnectionState.Loading:
                if (work == Work.Setup)
                {
                    Debug.Log("[BONSAI] Loading Setup isLanOnly " + isLANOnly);

                    textMesh.text = "Setting Up";
                    if (client != null) StopClient();
                    GameObject.Find("GameManager").GetComponent<MoveToDesk>()
                        .SetTableEdge(GameObject.Find("DefaultEdge").transform);
                    SetCommsActive(_comms, false);

                    StartCoroutine(StartHostAfterDisconnect());
                }

                break;

            // Has a HostEndPoint and now can open room or join a room
            case ConnectionState.Neutral:
                if (work == Work.Setup)
                {
                    var end = "    |    Join Desk";
                    textMesh.text = "Open Desk" + end;

                    if (fader.currentAlpha != 0) fader.FadeIn();
                    ActivateButtons(neutralButtons, waitBeforeSpawnButton);

                    if (serverOnlyIfEditor && Application.isEditor)
                        State = ConnectionState.HostCreating;
                }
                else
                {
                    DisableButtons(neutralButtons);
                }

                break;

            // Post and wait for room tag from Bonsai
            case ConnectionState.HostCreating:
                if (work == Work.Setup)
                {
                    textMesh.text = "\nOpening\n\n\n\nClose Desk";
                }

                break;

            // Waiting for a client to connect to room
            case ConnectionState.HostWaiting:
                if (work == Work.Setup)
                {
                    textMesh.text = "\n" + _assignedRoomTag + "\n\n\n\nClose Desk";
                }
                else
                {
                    StartCoroutine(DeleteRoom());
                }

                break;

            // Has a client connected
            case ConnectionState.Hosting:
                if (work == Work.Setup)
                {
                    textMesh.text = "Exit";
                    SetCommsActive(_comms, true);
                }
                else
                {
                    StartCoroutine(DeleteRoom());
                    StartCoroutine(KickClients());
                }

                break;

            // Client can enter room tag to join
            case ConnectionState.ClientEntry:
                if (work == Work.Setup)
                {
                    _enteredRoomTag = "";
                    UpdateClientEntryText();
                }
                else
                {
                }

                break;

            // Client give room tag to Bonsai for HostEndPoint and connects
            case ConnectionState.ClientConnecting:
                if (work == Work.Setup)
                {
                    textMesh.text = "Join\n\n[" + _enteredRoomTag + "]";
                    StartCoroutine(JoinRoom(apiBaseUri, _enteredRoomTag));
                }

                break;

            // Client connected to a host
            case ConnectionState.ClientConnected:
                if (work == Work.Setup)
                {
                    textMesh.text = "Exit";
                    fader.FadeIn();
                    SetCommsActive(_comms, true);
                }
                else
                {
                    client?.Disconnect();
                    StopClient();
                }

                break;

            default:
                textMesh.text = "UpdateText switch default";
                Debug.LogError("[BONSAI] HandleState not handled");
                break;
        }
    }

    private IEnumerator StartHostAfterDisconnect()
    {
        while (isDisconnecting) yield return null;

        if (HostEndPoint == null || isLANOnly)
        {
            Debug.Log("[BONSAI] StartHostAfterDisconnect StartHost ");
            isLANOnly = false;
            if (serverOnlyIfEditor && Application.isEditor)
                StartServer();
            else
                StartHost();
        }
        else
        {
            State = ConnectionState.Neutral;
        }

        if (fader.currentAlpha != 0) fader.FadeIn();
    }

    private IEnumerator SmoothStartClient()
    {
        fader.FadeOut();
        yield return new WaitForSeconds(fader.fadeTime);
        Debug.Log("[BONSAI] SmoothStartClient StopHost");
        StopHost();
        if (HostEndPoint != null) yield return null;
        Debug.Log("[BONSAI] HostEndPoint == null");
        Debug.Log("[BONSAI] StartClient");
        StartClient();
    }

    private IEnumerator FadeThenReturnToLoading()
    {
        fader.FadeOut();
        yield return new WaitForSeconds(fader.fadeTime);
        State = ConnectionState.Loading;
    }

    private void ActivateButtons(GameObject buttons, float delayTime)
    {
        StartCoroutine(DelayActivateButtons(buttons, delayTime));
    }

    private static void DisableButtons(GameObject buttons)
    {
        foreach (Transform child in buttons.transform)
        {
            var button = child.GetComponent<HoleButton>();
            if (button != null) button.DisableButton();
        }
    }

    private static IEnumerator DelayActivateButtons(GameObject buttons, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        foreach (Transform child in buttons.transform)
        {
            var button = child.gameObject;

            if (button != null) button.SetActive(true);
        }
    }

    private IEnumerator KickClients()
    {
        foreach (var conn in NetworkServer.connections.Values.ToList()
            .Where(conn => conn.connectionId != NetworkConnection.LocalConnectionId))
            conn.Send(new ShouldDisconnectMessage());
        yield return new WaitForSeconds(fader.fadeTime + 0.15f);
        foreach (var conn in NetworkServer.connections.Values.ToList()
            .Where(conn => conn.connectionId != NetworkConnection.LocalConnectionId))
            conn.Disconnect();
    }

    private void VoidAndDeafen()
    {
        moveToDesk.ResetPosition();
        SetCommsActive(_comms, false);
    }

    private void HandleOrientationChange(bool oriented)
    {
        if (oriented)
            _camera.cullingMask |= 1 << LayerMask.NameToLayer("networkPlayer");
        else
            _camera.cullingMask &= ~(1 << LayerMask.NameToLayer("networkPlayer"));
        if (State != ConnectionState.ClientConnected && State != ConnectionState.Hosting) return;
        SetCommsActive(_comms, oriented);
    }



    private IEnumerator JoinRoom(string baseUri, string roomID)
    {
        using (var www = UnityWebRequest.Get(baseUri + "/rooms/" + roomID))
        {
            _roomRequest = www;
            yield return www.SendWebRequest();
            if (www.isHttpError)
            {
                State = ConnectionState.ClientEntry;
                textMesh.text = "Could Not Find\n\n[" + roomID + "]\n\n(try again)";
                _roomRequest = null;
            }
            else if (www.isNetworkError)
            {
                StartCoroutine(FadeToState(ConnectionState.RelayError));
                //textMesh.text = "Internet not working!\n\n[" + roomID + "]\n\n(try again)";
                _roomRequest = null;
            }
            else
            {
                yield return www.downloadHandler.isDone;
                var jsonNode = JSONNode.Parse(www.downloadHandler.text) as JSONObject;
                Debug.Log("[BONSAI] GetRoom text " + www.downloadHandler.text);

                if (jsonNode is null)
                {
                    State = ConnectionState.ClientEntry;
                    textMesh.text = "Server Returned Empty Response\n\n\n\n(try again)";
                    _roomRequest = null;
                    yield break;
                }

                var ipAddress = jsonNode["ip_address"];
                var port = jsonNode["port"];
                if (ipAddress != null && port != null)
                {
                    _roomRequest = null;
                    networkAddress = ipAddress;
                    networkPort = port.AsInt;
                    StartCoroutine(SmoothStartClient());
                }
                else
                {
                    State = ConnectionState.ClientEntry;
                    textMesh.text = "Server Returned Bad Info\n\n\n\n(try again)";
                    _roomRequest = null;
                }
            }
        }
    }

    private void HandleJoinRoom(TableBrowser.RoomData roomData)
    {
        Debug.Log($"[Bonsai] NetworkManager Join Room {roomData.ip_address} {roomData.port}");
        networkAddress = roomData.ip_address;
        networkPort = roomData.port;
        StartCoroutine(SmoothStartClient());
    }


    private IEnumerator DeleteRoom()
    {
        using (var www = UnityWebRequest.Delete(apiBaseUri + "/rooms/" + _assignedRoomTag))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
                Debug.LogWarning("[BONSAI] DeleteRoom FAIL");
            else
                Debug.Log("[BONSAI] DeleteRoom OK");
        }
    }

    
    public event Action<NetworkConnection> ServerAddPlayer;
    public event Action<NetworkConnection> ServerDisconnect;

    public override void Awake()
    {
        base.Awake();

        if (Singleton == null)
            Singleton = this;
    }

    public override void Update()
    {
        base.Update();
        if (HostEndPoint != null && Time.time - postRoomInfoLast > postRoomInfoEvery)
        {
            tableBrowser.PostRoomInfo(HostEndPoint.Address.ToString(), (ushort) HostEndPoint.Port);
            postRoomInfoLast = Time.time;
        }
    }

    public override void Start()
    {
        base.Start();

        tableBrowser.JoinRoom += HandleJoinRoom;

        _camera = GameObject.Find("CenterEyeAnchor").GetComponent<Camera>();

        for (var i = 0; i < _spotInUse.Length; i++)
            _spotInUse[i] = false;

        _fakeRoomTag = new string('-', roomTagLength);

        State = ConnectionState.Loading;

        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);

        _comms = GetComponent<DissonanceComms>();
        SetCommsActive(_comms, false);

        OVRManager.HMDUnmounted += VoidAndDeafen;

        MoveToDesk.OrientationChanged += HandleOrientationChange;

        if (serverOnlyIfEditor && Application.isEditor)
        {
            neutralButtons.SetActive(false);
            tableBrowser.ToggleHidden();
        }

        if (Application.isEditor && !serverOnlyIfEditor)
        {
            StartCoroutine(StartXR());
        }
    }

    private IEnumerator StartXR()
    {
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
        XRGeneralSettings.Instance.Manager.StartSubsystems();
    }

    public override void OnFatalError(string error)
    {
        base.OnFatalError(error);
        Debug.Log("[BONSAI] OnFatalError");
        Debug.Log(error);
        State = ConnectionState.RelayError;
    }

    public override void OnServerPrepared(string hostAddress, ushort hostPort)
    {
        Debug.Log("[BONSAI] OnServerPrepared isLanOnly: " + isLANOnly);
        Debug.Log("[BONSAI] OnServerPrepared: " + hostAddress + ":" + hostPort);

        // triggers on startup
        State = !isLANOnly ? ConnectionState.Neutral : ConnectionState.RelayError;
    }

    public override void OnServerConnect(NetworkConnection conn)
    {
        Debug.Log("[BONSAI] OnServerConnect");

        base.OnServerConnect(conn);
        
        var openSpotId = -1;
        for (var i = 0; i < _spotInUse.Length; i++)
            if (!_spotInUse[i])
            {
                openSpotId = i;
                break;
            }

        if (openSpotId == -1)
        {
            Debug.LogError("No open spot.");
            openSpotId = 0;
        }

        _spotInUse[openSpotId] = true;
        PlayerInfos.Add(conn, new PlayerInfo(openSpotId));

        // triggers when client joins
        if (NetworkServer.connections.Count > 1) State = ConnectionState.Hosting;
    }

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        Debug.Log("[BONSAI] OnServerAddPlayer");
        conn.Send(new SpotMessage
        {
            SpotId = PlayerInfos[conn].spot,
            ColorIndex = PlayerInfos[conn].spot
        });

        base.OnServerAddPlayer(conn);
        
        ServerAddPlayer?.Invoke(conn);
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        Debug.Log("[BONSAI] OnServerDisconnect");

        if (!conn.isAuthenticated) return;
        
        ServerDisconnect?.Invoke(conn);

        var spotId = PlayerInfos[conn].spot;

        var spotUsedCount = 0;
        foreach (var player in PlayerInfos)
            if (player.Value.spot == spotId)
                spotUsedCount++;
        if (spotUsedCount <= 1) _spotInUse[spotId] = false;
        PlayerInfos.Remove(conn);

        var tmp = new HashSet<NetworkIdentity>(conn.clientOwnedObjects);
        foreach (var identity in tmp)
        {
            var autoAuthority = identity.GetComponent<AutoAuthority>();
            if (autoAuthority != null)
            {
                if (autoAuthority.InUse)
                    autoAuthority.SetInUse(false);
                identity.RemoveClientAuthority();
            }
        }

        if (conn.identity != null && togglePause.AuthorityIdentityId == conn.identity.netId)
        {
            togglePause.RemoveClientAuthority();
        }

        base.OnServerDisconnect(conn);

        // triggers when last client leaves
        if (NetworkServer.connections.Count == 1) State = ConnectionState.Loading;
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        Debug.Log("[BONSAI] OnClientConnect");

        // For some reason OnClientConnect triggers twice occasionally. This is a hack to ignore the second trigger.
        if (conn.isReady) return;

        base.OnClientConnect(conn);

        NetworkClient.RegisterHandler<SpotMessage>(OnSpot);
        NetworkClient.RegisterHandler<ShouldDisconnectMessage>(OnShouldDisconnect);

        // triggers when client connects to remote host
        if (NetworkServer.connections.Count == 0) State = ConnectionState.ClientConnected;
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        Debug.Log("[BONSAI] OnClientDisconnect");

        NetworkClient.UnregisterHandler<SpotMessage>();
        NetworkClient.UnregisterHandler<ShouldDisconnectMessage>();

        switch (State)
        {
            case ConnectionState.ClientConnected:
                // this happens on client when the host exits rudely (power off, etc)
                // base method stops client with a delay so it can gracefully disconnct
                // since the client is getting booted here, we don't need to wait (which introduces bugs)
                fader.SetFadeLevel(1.0f);
                State = ConnectionState.Loading;
                break;
            case ConnectionState.ClientConnecting:
                //this should happen on client trying to connect to a paused host
                StopClient();
                State = ConnectionState.Loading;
                break;
            default:
                base.OnClientDisconnect(conn);
                break;
        }
    }

    private void OnApplicationFocus(bool focus)
    {
        if (moveToDesk.oriented) SetCommsActive(_comms, focus);
    }

    private void OnApplicationPause(bool pause)
    {
        if (!pause) return;
        SetCommsActive(_comms, false);
        moveToDesk.ResetPosition();
    }



    public void ClickRelayFailed()
    {
        StartCoroutine(StopHostFadeReturnToLoading());
    }


    public void ClickExitClient()
    {
        StartCoroutine(FadeThenReturnToLoading());
    }



    private struct ShouldDisconnectMessage : NetworkMessage
    {
    }

    public struct SpotMessage : NetworkMessage
    {
        public int ColorIndex;
        public int SpotId;
    }

    private static void OnSpot(NetworkConnection conn, SpotMessage msg)
    {
        switch (msg.SpotId)
        {
            case 0:
                GameObject.Find("GameManager").GetComponent<MoveToDesk>()
                    .SetTableEdge(GameObject.Find("DefaultEdge").transform);
                break;
            case 1:
                GameObject.Find("GameManager").GetComponent<MoveToDesk>()
                    .SetTableEdge(GameObject.Find("AcrossEdge").transform);
                break;
        }

        AssignedColorIndex = msg.ColorIndex;
    }

}