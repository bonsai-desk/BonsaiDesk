using System.Collections;
using System.Linq;
using Dissonance;
using Mirror;
using OVRSimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Networking;

public class NetworkConnectControl : NetworkManagerGame
{
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

    public override void Start()
    {
        base.Start();

        _fakeRoomTag = new string('-', roomTagLength);

        State = ConnectionState.Loading;

        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);

        _comms = GetComponent<DissonanceComms>();
        _comms.IsMuted = true;
        _comms.IsDeafened = true;

        OVRManager.HMDMounted += () =>
        {
            if (State != ConnectionState.ClientConnected && State != ConnectionState.Hosting) return;
            _comms.IsMuted = false;
            _comms.IsDeafened = false;
        };
        OVRManager.HMDUnmounted += () =>
        {
            if (State != ConnectionState.ClientConnected && State != ConnectionState.Hosting) return;
            _comms.IsMuted = true;
            _comms.IsDeafened = true;
        };
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

    private struct ShouldDisconnect : NetworkMessage
    {
    }

    #region hooks

    public override void OnServerPrepared(string hostAddress, ushort hostPort)
    {
        // Get your HostEndPoint here.
        Debug.Log("[BONSAI] OnServerPrepared: " + hostAddress + ":" + hostPort);
        State = ConnectionState.Neutral;
    }

    public override void OnServerConnect(NetworkConnection conn)
    {
        Debug.Log("[BONSAI] OnClientConnect");
        base.OnServerConnect(conn);
        if (NetworkServer.connections.Count > 1) State = ConnectionState.Hosting;
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        base.OnServerDisconnect(conn);
        if (NetworkServer.connections.Count == 1) State = ConnectionState.Loading;
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        Debug.Log("[BONSAI] OnClientConnect");
        base.OnClientConnect(conn);
        NetworkClient.RegisterHandler<ShouldDisconnect>(OnShouldDisconnect);
        if (NetworkServer.connections.Count == 0) State = ConnectionState.ClientConnected;
    }

    private void OnShouldDisconnect(ShouldDisconnect _)
    {
        StartCoroutine(FadeThenReturnToLoading());
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        Debug.Log("[BONSAI] OnClientDisconnect");
        NetworkClient.UnregisterHandler<SpotMessage>();
        NetworkClient.UnregisterHandler<ActionMessage>();
        NetworkClient.UnregisterHandler<ShouldDisconnect>();
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
        if (!focus)
        {
            if (_comms != null)
            {
                _comms.IsMuted = true;
                _comms.IsDeafened = true;
            }
        }
        else
        {
            if (_comms != null)
            {
                _comms.IsMuted = false;
                _comms.IsDeafened = false;
            }
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (!pause)
        {
            if (_comms == null) return;
            _comms.IsMuted = true;
            _comms.IsDeafened = true;
        }
        else
        {
            if (_comms == null) return;
            _comms.IsMuted = false;
            _comms.IsDeafened = false;
        }
    }

    #endregion hooks

    #region Public Methods

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
            UpdateText(ConnectionState.ClientEntry);
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

    #endregion Public Methods

    #region Utilities

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
                    if (_comms != null)
                    {
                        _comms.IsMuted = true;
                        _comms.IsDeafened = true;
                    }

                    if (HostEndPoint != null) updateText = false;
                    StartCoroutine(NeutralAfterDisconnect());
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
                    _comms.IsMuted = false;
                    _comms.IsDeafened = false;
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
                    _comms.IsMuted = false;
                    _comms.IsDeafened = false;
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

        if (work == Work.Setup && updateText) UpdateText(state);
    }

    private IEnumerator NeutralAfterDisconnect()
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

    private void UpdateText(ConnectionState newState)
    {
        switch (newState)
        {
            case ConnectionState.Loading:
                textMesh.text = "Setting Up";
                break;

            case ConnectionState.ClientEntry:
                var displayRoomTag = _enteredRoomTag + _fakeRoomTag.Substring(
                    0, _fakeRoomTag.Count() - _enteredRoomTag.Count()
                );
                textMesh.text = "Join\n\n" + displayRoomTag;
                break;

            case ConnectionState.ClientConnecting:
                textMesh.text = "Join\n\n[" + _enteredRoomTag + "]";
                break;

            case ConnectionState.ClientConnected:
                textMesh.text = "Exit";
                break;

            case ConnectionState.HostCreating:
                textMesh.text = "\nOpening\n\n\n\nClose Desk";
                break;

            case ConnectionState.HostWaiting:
                textMesh.text = "\n" + _assignedRoomTag + "\n\n\n\nClose Desk";
                break;

            case ConnectionState.Hosting:
                textMesh.text = "Exit";
                break;

            case ConnectionState.Neutral:
                var end = "    |    Join Desk";
                textMesh.text = "Open Desk" + end;
                break;

            default:
                textMesh.text = "UpdateText switch default";
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
            conn.Send(new ShouldDisconnect());
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
}