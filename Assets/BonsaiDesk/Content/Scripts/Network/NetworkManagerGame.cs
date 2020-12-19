using System.Collections;
using Mirror;
using NobleConnect.Mirror;
using System.Collections.Generic;
using System.Linq;
using Dissonance;
using OVRSimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Networking;

public class NetworkSyncTest : NetworkBehaviour
{
    public static void RegisterHandlers()
    {
        NetworkClient.RegisterHandler<NumberMessage>(OnNumber);
    }
    
    public static void UnregisterHandlers()
    {
        NetworkClient.UnregisterHandler<NumberMessage>();
    }

    private struct NumberMessage : NetworkMessage
    {
        public int TheNumber;
    }

    private static void OnNumber(NetworkConnection conn, NumberMessage msg)
    {
        Debug.Log("[BONSAI] OnNumber" + msg.TheNumber);
    }

}

public class NetworkManagerGame : NobleNetworkManager
{
    public static new NetworkManagerGame singleton;
    
    #region Player Props

    public Dictionary<NetworkConnection, PlayerInfo> playerInfo = new Dictionary<NetworkConnection, PlayerInfo>();

    [System.Serializable]
    public class PlayerInfo
    {
        public int spot;
        public int youtubePlayerState;
        public float youtubePlayerCurrentTime;

        public PlayerInfo(int spot)
        {
            this.spot = spot;
            youtubePlayerState = -1;
            youtubePlayerCurrentTime = 0;
        }
    }

    public void ResetPlayerInfoTime()
    {
        foreach (var player in playerInfo)
        {
            player.Value.youtubePlayerCurrentTime = 0;
        }
    }

    private bool[] spotInUse = new bool[2];

    public static int colorIndex = 0;

    #endregion
    
    #region Video Props
    
    public enum VideoState
    {
        none,
        cued,
        playing
    }
    
    public VideoState videoState = VideoState.none;
    
    #endregion
    
    #region Control props
    
    public TextMeshProUGUI textMesh;
    public BonsaiScreenFade fader;

    public string apiBaseUri = "https://api.desk.link";
    public float waitBeforeSpawnButton = 0.4f;
    public int roomTagLength = 4;
    public float refreshRoomEverySeconds = 5;

    public GameObject neutralButtons;
    public GameObject hostButtons;
    public GameObject clientButtons;
    public GameObject hostStartedButtons;
    public GameObject clientStartedButtons;
    
    private string _assignedRoomTag = "";
    private DissonanceComms _comms;

    private ConnectionState _connectionState;
    private string _enteredRoomTag = "";
    private string _fakeRoomTag;
    private Coroutine _refreshRoomCoroutine;
    private UnityWebRequest _roomRequest;

    private ConnectionState State
    {
        get => _connectionState;
        set
        {
            HandleState(_connectionState, Work.Cleanup);
            _connectionState = value;
            HandleState(value, Work.Setup);
        }
    }
    private enum ConnectionState
    {
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


    
    #endregion
    
    #region Buttons

    public void ClickStartHost()
    {
        State = ConnectionState.HostCreating;
    }

    public void ClickStopHost()
    {
        State = ConnectionState.Neutral;
    }

    public void ClickStartClient()
    {
        State = ConnectionState.ClientEntry;
    }

    public void AppendRoomString(string s)
    {
        _enteredRoomTag += s;
        if (_enteredRoomTag.Length >= roomTagLength && _roomRequest == null)
            State = ConnectionState.ClientConnecting;
        else
            UpdateText(textMesh, ConnectionState.ClientEntry, enteredRoomTag: _enteredRoomTag,
                fakeRoomTag: _fakeRoomTag);
    }

    public void ClickStopClient()
    {
        State = ConnectionState.Loading;
    }

    public void ClickExitClient()
    {
        StartCoroutine(FadeThenReturnToLoading());
    }

    public void ClickExitHost()
    {
        State = ConnectionState.Loading;
    }

    #endregion Buttons
    
    #region Utilities
    
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

    private void HandleState(ConnectionState state, Work work)
    {
        var updateText = true;
        switch (state)
        {
            // Waiting for a HostEndPoint
            case ConnectionState.Loading:
                if (work == Work.Setup)
                {
                    GameObject.Find("GameManager").GetComponent<MoveToDesk>()
                        .SetTableEdge(GameObject.Find("DefaultEdge").transform);
                    SetCommsActive(_comms, false);

                    if (HostEndPoint != null) updateText = false;
                    StartCoroutine(StartHostAfterDisconnect());
                }

                break;

            // Has a HostEndPoint and now can open room or join a room
            case ConnectionState.Neutral:
                if (work == Work.Setup)
                {
                    if (fader.currentAlpha != 0) fader.FadeIn();
                    ActivateButtons(neutralButtons, waitBeforeSpawnButton);
                }
                else
                {
                    DisableButtons(neutralButtons);
                }

                break;

            // Post and wait for room tag from Bonsai
            case ConnectionState.HostCreating:
                if (work == Work.Setup)
                    StartCoroutine(PostCreateRoom(HostEndPoint.Address.ToString(), (ushort) HostEndPoint.Port));

                break;

            // Waiting for a client to connect to room
            case ConnectionState.HostWaiting:
                if (work == Work.Setup)
                {
                    ActivateButtons(hostButtons, waitBeforeSpawnButton);
                }
                else
                {
                    DisableButtons(hostButtons);
                    StopCoroutine(_refreshRoomCoroutine);
                    StartCoroutine(DeleteRoom());
                }

                break;

            // Has a client connected
            case ConnectionState.Hosting:
                if (work == Work.Setup)
                {
                    ActivateButtons(hostStartedButtons, waitBeforeSpawnButton);
                    SetCommsActive(_comms, true);
                }
                else
                {
                    DisableButtons(hostStartedButtons);
                    StartCoroutine(DeleteRoom());
                    StartCoroutine(KickClients());
                }

                break;

            // Client can enter room tag to join
            case ConnectionState.ClientEntry:
                if (work == Work.Setup)
                {
                    _enteredRoomTag = "";
                    ActivateButtons(clientButtons, waitBeforeSpawnButton);
                }
                else
                {
                    DisableButtons(clientButtons);
                }

                break;

            // Client give room tag to Bonsai for HostEndPoint and connects
            case ConnectionState.ClientConnecting:
                if (work == Work.Setup) StartCoroutine(JoinRoom(apiBaseUri, _enteredRoomTag));

                break;

            // Client connected to a host
            case ConnectionState.ClientConnected:
                if (work == Work.Setup)
                {
                    fader.FadeIn();
                    ActivateButtons(clientStartedButtons, waitBeforeSpawnButton);
                    SetCommsActive(_comms, true);
                }
                else
                {
                    DisableButtons(clientStartedButtons);
                    client?.Disconnect();
                    StopClient();
                }

                break;

            default:
                Debug.LogError("[BONSAI] HandleState not handled");
                break;
        }

        if (work == Work.Setup && updateText)
            UpdateText(textMesh, state, _assignedRoomTag, _enteredRoomTag, _fakeRoomTag);
    }

    private IEnumerator StartHostAfterDisconnect()
    {
        while (isDisconnecting) yield return null;
        if (HostEndPoint == null)
            StartHost();
        else
            State = ConnectionState.Neutral;
    }

    private IEnumerator SmoothStartClient()
    {
        fader.FadeOut();
        yield return new WaitForSeconds(fader.fadeTime);
        StopHost();
        while (HostEndPoint != null) yield return null;
        StartClient();
    }

    private IEnumerator FadeThenReturnToLoading()
    {
        fader.FadeOut();
        yield return new WaitForSeconds(fader.fadeTime);
        State = ConnectionState.Loading;
    }

    private static void UpdateText(TextMeshProUGUI textMeshPro, ConnectionState newState,
        string assignedRoomTag = "none",
        string enteredRoomTag = "none", string fakeRoomTag = "none"
    )
    {
        switch (newState)
        {
            case ConnectionState.Loading:
                textMeshPro.text = "Setting Up";
                break;

            case ConnectionState.ClientEntry:
                var displayRoomTag = enteredRoomTag + fakeRoomTag.Substring(
                    0, fakeRoomTag.Count() - enteredRoomTag.Count()
                );
                textMeshPro.text = "Join\n\n" + displayRoomTag;
                break;

            case ConnectionState.ClientConnecting:
                textMeshPro.text = "Join\n\n[" + enteredRoomTag + "]";
                break;

            case ConnectionState.ClientConnected:
                textMeshPro.text = "Exit";
                break;

            case ConnectionState.HostCreating:
                textMeshPro.text = "\nOpening\n\n\n\nClose Desk";
                break;

            case ConnectionState.HostWaiting:
                textMeshPro.text = "\n" + assignedRoomTag + "\n\n\n\nClose Desk";
                break;

            case ConnectionState.Hosting:
                textMeshPro.text = "Exit";
                break;

            case ConnectionState.Neutral:
                var end = "    |    Join Desk";
                textMeshPro.text = "Open Desk" + end;
                break;

            default:
                textMeshPro.text = "UpdateText switch default";
                break;
        }
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

    #endregion Utilities

    #region Requests

    private IEnumerator JoinRoom(string baseUri, string roomID)
    {
        // TODO handle parse errors better
        using (var www = UnityWebRequest.Get(baseUri + "/rooms/" + roomID))
        {
            _roomRequest = www;
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
            {
                State = ConnectionState.ClientEntry;
                textMesh.text = "Could Not Find\n\n[" + roomID + "]\n\n(try again)";
                _roomRequest = null;
            }
            else
            {
                yield return www.downloadHandler.isDone;
                var jsonNode = JSONNode.Parse(www.downloadHandler.text) as JSONObject;
                Debug.Log("[BONSAI] GetRoom text " + www.downloadHandler.text);
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
                    textMesh.text = "Parse ip/port Error!";
                }
            }
        }
    }

    private IEnumerator PostCreateRoom(string ipAddress, ushort port)
    {
        // TODO handle parse errors better
        Debug.Log("[BONSAI] PostCreateRoom:" + ipAddress + ":" + port);
        var form = new WWWForm();
        form.AddField("ip_address", ipAddress);
        form.AddField("port", port);

        var delay = new WaitForSeconds(0.25f);

        using (var www = UnityWebRequest.Post(apiBaseUri + "/rooms", form))
        {
            yield return www.SendWebRequest();
            yield return delay;

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log("[BONSAI] CreateRoom fail");
            }
            else
            {
                yield return www.downloadHandler.isDone;
                var jsonNode = JSONNode.Parse(www.downloadHandler.text) as JSONObject;
                if (jsonNode["tag"])
                {
                    _assignedRoomTag = jsonNode["tag"];
                    _refreshRoomCoroutine = StartCoroutine(RefreshRoomEverySeconds());
                    State = ConnectionState.HostWaiting;
                }
                else
                {
                    textMesh.text = "Parse room/tag error";
                }
            }
        }
    }

    private IEnumerator DeleteRoom()
    {
        using (var www = UnityWebRequest.Delete(apiBaseUri + "/rooms/" + _assignedRoomTag))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
                Debug.Log("[BONSAI] DeleteRoom fail");
            else
                Debug.Log("[BONSAI] DeleteRoom OK");
        }
    }

    private IEnumerator PostRefreshRoom()
    {
        var form = new WWWForm();
        using (var www = UnityWebRequest.Post(apiBaseUri + "/rooms/" + _assignedRoomTag + "/refresh", form))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
                Debug.Log("[BONSAI] PostRefreshRoom fail");
            else
                Debug.Log("[BONSAI] PostRefreshRoom OK");
        }
    }

    private IEnumerator RefreshRoomEverySeconds()
    {
        while (true)
        {
            yield return new WaitForSeconds(refreshRoomEverySeconds);
            yield return PostRefreshRoom();
        }
    }

    #endregion Requests

    #region Overrides

    public override void Awake()
    {
        base.Awake();

        if (singleton == null)
            singleton = this;
    }

    public override void Start()
    {
        base.Start();

        for (int i = 0; i < spotInUse.Length; i++)
            spotInUse[i] = false;
        
        _fakeRoomTag = new string('-', roomTagLength);

        State = ConnectionState.Loading;

        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);

        _comms = GetComponent<DissonanceComms>();
        SetCommsActive(_comms, false);

        OVRManager.HMDMounted += () =>
        {
            if (State != ConnectionState.ClientConnected && State != ConnectionState.Hosting) return;
            SetCommsActive(_comms, true);
        };
        OVRManager.HMDUnmounted += () =>
        {
            if (State != ConnectionState.ClientConnected && State != ConnectionState.Hosting) return;
            SetCommsActive(_comms, false);
        };
        
    }
    
    public override void OnServerPrepared(string hostAddress, ushort hostPort)
    {
        Debug.Log("[BONSAI] OnServerPrepared: " + hostAddress + ":" + hostPort);
        
        // triggers on startup
        State = ConnectionState.Neutral;
    }
    
    public override void OnServerConnect(NetworkConnection conn)
    {
        Debug.Log("[BONSAI] OnServerConnect");
        base.OnServerConnect(conn);
        int openSpotId = -1;
        for (int i = 0; i < spotInUse.Length; i++)
        {
            if (!spotInUse[i])
            {
                openSpotId = i;
                break;
            }
        }

        if (openSpotId == -1)
        {
            Debug.LogError("No open spot.");
            openSpotId = 0;
        }
        spotInUse[openSpotId] = true;
        playerInfo.Add(conn, new PlayerInfo(openSpotId));
        
        // triggers when client joins
        if (NetworkServer.connections.Count > 1) State = ConnectionState.Hosting;
    }
    
    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        Debug.Log("[BONSAI] OnServerAddPlayer");
        conn.Send(new SpotMessage()
        {
            spotId = playerInfo[conn].spot,
            colorIndex = playerInfo[conn].spot
        });

        base.OnServerAddPlayer(conn);
    }
    
    public override void OnServerDisconnect(NetworkConnection conn)
    {
        Debug.Log("[BONSAI] OnServerDisconnect");
        base.OnServerDisconnect(conn);
        int spotId = playerInfo[conn].spot;

        int spotUsedCount = 0;
        foreach (var player in playerInfo)
        {
            if (player.Value.spot == spotId)
                spotUsedCount++;
        }
        if (spotUsedCount <= 1)
        {
            spotInUse[spotId] = false;
        }
        playerInfo.Remove(conn);

        HashSet<NetworkIdentity> tmp = new HashSet<NetworkIdentity>(conn.clientOwnedObjects);
        foreach (NetworkIdentity netIdentity in tmp)
        {
            if (netIdentity != null && (netIdentity.gameObject.CompareTag("KeepOnDisconnect") || netIdentity.gameObject.CompareTag("BlockArea")))
            {
                netIdentity.RemoveClientAuthority();
            }
        }

        NetworkServer.DestroyPlayerForConnection(conn);
        
        // triggers when last client leaves
        if (NetworkServer.connections.Count == 1) State = ConnectionState.Loading;
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        Debug.Log("[BONSAI] OnClientConnect");
        base.OnClientConnect(conn);

        NetworkClient.RegisterHandler<SpotMessage>(OnSpot);
        NetworkClient.RegisterHandler<ActionMessage>(OnAction);
        NetworkClient.RegisterHandler<ShouldDisconnectMessage>(OnShouldDisconnect);
        NetworkSyncTest.RegisterHandlers();
        
        // triggers when client connects to remote host
        if (NetworkServer.connections.Count == 0) State = ConnectionState.ClientConnected;
    }
    
    public override void OnClientDisconnect(NetworkConnection conn)
    {
        Debug.Log("[BONSAI] OnClientDisconnect");
        
        NetworkClient.UnregisterHandler<SpotMessage>();
        NetworkClient.UnregisterHandler<ActionMessage>();
        NetworkClient.UnregisterHandler<ShouldDisconnectMessage>();
        NetworkSyncTest.UnregisterHandlers();
        
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
        SetCommsActive(_comms, focus);
    }

    private void OnApplicationPause(bool pause)
    {
        SetCommsActive(_comms, !pause);
    }

    #endregion

    #region Messages
    
    private struct ShouldDisconnectMessage : NetworkMessage
    {
    }

    public class SpotMessage : NetworkMessage
    {
        public int spotId;
        public int colorIndex;
    }

    public class ActionMessage : NetworkMessage
    {
        public int actionId;
    }

    private static void OnSpot(NetworkConnection conn, SpotMessage msg)
    {
        switch (msg.spotId)
        {
            case 0:
                GameObject.Find("GameManager").GetComponent<MoveToDesk>().SetTableEdge(GameObject.Find("DefaultEdge").transform);
                break;
            case 1:
                GameObject.Find("GameManager").GetComponent<MoveToDesk>().SetTableEdge(GameObject.Find("AcrossEdge").transform);
                break;
        }

        colorIndex = msg.colorIndex;
    }

    private static void OnAction(NetworkConnection conn, ActionMessage msg)
    {
        switch (msg.actionId)
        {
            case 0: //play video
                BrowserManager.instance.StartVideo();
                break;

            case 1: //resume video
                BrowserManager.instance.ResumeVideo();
                break;

            case 2: //pause video
                BrowserManager.instance.PauseVideo();
                break;

            default:
                break;
        }
    }

    #endregion
}