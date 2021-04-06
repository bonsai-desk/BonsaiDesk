using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using NobleConnect.Mirror;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.Management;

// this is a modified version of NobleNetworkManager
// this version cleans up AddListener properly

public class NetworkManagerGame : BonsaiNetworkManager
{
    public enum ConnectionState
    {
        RelayError,
        Loading,
        Hosting,
        ClientConnecting,
        ClientConnected
    }

    private const double StartHostCooldown = 0.5f;

    private const float HardKickDelay = 0.5f;
    private const float PingInternetEvery = 2.0f;
    private const int PingInternetRequestTimeout = 4;
    private const float PingInternetTimeoutBeforeDisconnect = 5.0f;

    public static NetworkManagerGame Singleton;
    public static EventHandler<NetworkConnection> ServerAddPlayer;
    public static EventHandler<NetworkConnection> ServerDisconnect;
    public static EventHandler ServerPrepared;
    public static EventHandler<NetworkConnection> ClientConnect;
    public static EventHandler<NetworkConnection> ClientDisconnect;

    [HideInInspector] public bool roomOpen;
    public bool serverOnlyIfEditor;

    public bool visualizeAuthority;

    public GameObject networkHandLeftPrefab;
    public GameObject networkHandRightPrefab;

    public int BuildId;

    public readonly Dictionary<NetworkConnection, PlayerInfo> PlayerInfos = new Dictionary<NetworkConnection, PlayerInfo>();

    private double _lastGoodPingReceived = Mathf.NegativeInfinity;
    private float _lastPingNet = Mathf.NegativeInfinity;
    private float _lastStartHost = Mathf.NegativeInfinity;

    private bool _roomJoinInProgress;

    private float _unpausedAt = Mathf.NegativeInfinity;

    public EventHandler InfoChange;

    public ConnectionState State
    {
        get
        {
            switch (mode)
            {
                case NetworkManagerMode.Offline:
                    return ConnectionState.RelayError;
                case NetworkManagerMode.ServerOnly:
                    return ConnectionState.Hosting;
                case NetworkManagerMode.ClientOnly:
                    return ConnectionState.ClientConnected;
                case NetworkManagerMode.Host:
                    return ConnectionState.Hosting;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public override void Awake()
    {
        base.Awake();
        if (Singleton == null)
        {
            Singleton = this;
        }
    }

    public override void Start()
    {
        base.Start();

        // todo make these into EventHandler
        TableBrowserMenu.JoinRoom += HandleJoinRoom;
        TableBrowserMenu.LeaveRoom += HandleLeaveRoom;
        TableBrowserMenu.KickConnectionId += HandleKickConnectionId;
        TableBrowserMenu.OpenRoom += HandleOpenRoom;
        TableBrowserMenu.CloseRoom += HandleCloseRoom;

        if (Application.isEditor && !serverOnlyIfEditor)
        {
            StartCoroutine(StartXR());
        }
    }

    public override void Update()
    {
        base.Update();

        HandlePingUpdate();

        HandleNetworkUpdate();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            if (mode == NetworkManagerMode.ClientOnly)
            {
                HandleLeaveRoom();
            }

            if (mode == NetworkManagerMode.Host)
            {
                BonsaiLog("Stop Client/Server");
                StopHost();
            }
        }
        else
        {
            _unpausedAt = Time.realtimeSinceStartup;
        }
    }

    public override void OnApplicationQuit()
    {
        base.OnApplicationQuit();

        StopXR();
    }

    public bool IsInternetGood()
    {
        return Time.realtimeSinceStartup - _lastGoodPingReceived < PingInternetTimeoutBeforeDisconnect;
    }

    private IEnumerator CheckInternetAccess()
    {
        var uwr = new UnityWebRequest("http://google.com/generate_204") {timeout = PingInternetRequestTimeout};
        yield return uwr.SendWebRequest();
        if (uwr.responseCode == 204 && Time.realtimeSinceStartup > _lastGoodPingReceived)
        {
            _lastGoodPingReceived = Time.realtimeSinceStartup;
        }
    }

    private void HandlePingUpdate()
    {
        if (Time.realtimeSinceStartup - _lastPingNet > PingInternetEvery)
        {
            _lastPingNet = Time.realtimeSinceStartup;
            StartCoroutine(CheckInternetAccess());
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        PlayerInfos.Clear();
    }

    private void HandleNetworkUpdate()
    {
        if (isDisconnecting || _roomJoinInProgress)
        {
            BonsaiLog($"Prevent Update while isDisconnecting={isDisconnecting} _roomJoinInProgress={_roomJoinInProgress}");
            return;
        }

        if (Application.isEditor && serverOnlyIfEditor)
        {
            if (mode != NetworkManagerMode.ServerOnly)
            {
                roomOpen = true;
                StartServer();
            }
        }
        else
        {
            var pingTimeout = Time.realtimeSinceStartup - _lastGoodPingReceived > PingInternetTimeoutBeforeDisconnect;
            switch (mode)
            {
                case NetworkManagerMode.Offline:
                    MaybeStartHost();
                    break;
                case NetworkManagerMode.ServerOnly:
                    break;
                case NetworkManagerMode.ClientOnly:
                    if (pingTimeout)
                    {
                        BonsaiLog("Ping timeout as client");
                        MessageStack.Singleton.AddMessage("Internet Disconnected as Client");
                        StopClient();
                    }

                    break;
                case NetworkManagerMode.Host:
                    if (isLANOnly)
                    {
                        StopClientIfGoodPing();
                    }
                    else if (pingTimeout && Time.realtimeSinceStartup - _unpausedAt > 5.0f)
                    {
                        BonsaiLog("Ping timeout as host");
                        MessageStack.Singleton.AddMessage("Internet Disconnected as Host");
                        StopHost();
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void StopClientIfGoodPing()
    {
        if (Time.realtimeSinceStartup - _lastGoodPingReceived < 1.0f)
        {
            BonsaiLog("Got a good ping, disconnecting from LAN");
            MessageStack.Singleton.AddMessage("Reconnected to the Internet");
            isLANOnly = false;
            StopClient();
        }
    }

    private void HandleKickConnectionId(int id)
    {
        BonsaiLog($"Kick (id={id})");
        StartCoroutine(KickClient(id));
    }

    private void HandleLeaveRoom()
    {
        BonsaiLog("LeaveRoom and StopClient");
        StopClient();
    }

    private void HandleJoinRoom(TableBrowserMenu.RoomData roomData)
    {
        if (!_roomJoinInProgress)
        {
            if (HostEndPoint.Address.ToString() == roomData.ip_address && HostEndPoint.Port == roomData.port)
            {
                BonsaiLogWarning("Tried to join own hosted room as client, ignoring");
            }
            else
            {
                StartCoroutine(JoinRoom(roomData));
            }
        }
        else
        {
            BonsaiLogWarning("Ignoring attempt to join room while room join is in progress");
        }
    }

    private IEnumerator JoinRoom(TableBrowserMenu.RoomData roomData)
    {
        BonsaiLog("Begin JoinRoom");
        _roomJoinInProgress = true;
        if (mode == NetworkManagerMode.Host || !(HostEndPoint is null))
        {
            BonsaiLog("StopHost before join room");
            StopHost();
            InfoChange?.Invoke(this, new EventArgs());
        }

        var t0 = Time.realtimeSinceStartup;
        while (!(HostEndPoint is null))
        {
            BonsaiLog($"Wait for HostEndPoint to be null in JoinRoom {HostEndPoint.Address} {HostEndPoint.Port}");
            if (HostEndPoint.Address.ToString() == "127.0.0.1" || HostEndPoint.Address.ToString() == "localhost")
            {
                // This happens when you are a LAN host then try to join a room
                BonsaiLog("Breaking HostEndPoint null check in JoinRoom since localhost");
                break;
            }

            if (Time.realtimeSinceStartup - t0 > 2f)
            {
                BonsaiLog("Breaking loop since spent too long waiting for HostEndPoint to be null in JionRoom");
                break;
            }

            yield return null;
        }

        if (mode == NetworkManagerMode.Offline)
        {
            networkAddress = roomData.ip_address;
            networkPort = roomData.port;
            StartClient();
            InfoChange?.Invoke(this, new EventArgs());
        }
        else
        {
            BonsaiLogWarning("Tried to join room while a hosting/client, ignoring");
        }

        _roomJoinInProgress = false;
    }

    private void HandleCloseRoom()
    {
        BonsaiLog("CloseRoom");
        roomOpen = false;
        StartCoroutine(KickClients());
        InfoChange?.Invoke(this, new EventArgs());
    }

    private void HandleOpenRoom()
    {
        BonsaiLog("OpenRoom");
        roomOpen = true;
        InfoChange?.Invoke(this, new EventArgs());
    }

    public override void OnServerConnect(NetworkConnection conn)
    {
        base.OnServerConnect(conn);
        BonsaiLog("ServerConnect");
    }

    //this doesn't call the base function because we need to instantiate and spawn the player ourselves here so we can change the spotId
    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        BonsaiLog("ServerAddPlayer");

        var openSpot = OpenSpotId();
        PlayerInfos.Add(conn, new PlayerInfo(openSpot, "NoName"));

        SpawnPlayer(conn, openSpot);

        ServerAddPlayer?.Invoke(this, conn);
        InfoChange?.Invoke(this, new EventArgs());
    }

    private void SpawnPlayer(NetworkConnection conn, int spot)
    {
        //instantiate player
        var startPos = GetStartPosition();
        var player = startPos != null ? Instantiate(playerPrefab, startPos.position, startPos.rotation) : Instantiate(playerPrefab);

        //setup player and spawn hands
        var networkVRPlayer = player.GetComponent<NetworkVRPlayer>();
        var pid = player.GetComponent<NetworkIdentity>();

        var leftHand = Instantiate(networkHandLeftPrefab);
        var lid = leftHand.GetComponent<NetworkIdentity>();

        var rightHand = Instantiate(networkHandRightPrefab);
        var rid = rightHand.GetComponent<NetworkIdentity>();

        NetworkServer.Spawn(leftHand, conn);
        NetworkServer.Spawn(rightHand, conn);
        networkVRPlayer.SetHandIdentities(lid, rid);
        networkVRPlayer.SetSpot(spot);
        NetworkServer.AddPlayerForConnection(conn, player);

        leftHand.GetComponent<NetworkHand>().ownerIdentity = pid;
        rightHand.GetComponent<NetworkHand>().ownerIdentity = pid;
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        BonsaiLog("ServerDisconnect");

        ServerDisconnect?.Invoke(this, conn);

        PlayerInfos.Remove(conn);

        var tmp = new HashSet<NetworkIdentity>(conn.clientOwnedObjects);
        foreach (var identity in tmp)
        {
            var autoAuthority = identity.GetComponent<AutoAuthority>();
            if (autoAuthority != null)
            {
                if (autoAuthority.InUse)
                {
                    autoAuthority.SetInUse(false);
                }

                identity.RemoveClientAuthority();
            }
        }

        // call the base after the ServerDisconnect event otherwise null reference gets passed to subscribers
        base.OnServerDisconnect(conn);

        InfoChange?.Invoke(this, new EventArgs());
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);

        BonsaiLog($"OnClientConnect {conn.connectionId} {conn.isReady}");

        NetworkClient.RegisterHandler<ShouldDisconnectMessage>(OnShouldDisconnect);

        ClientConnect?.Invoke(this, conn);
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        BonsaiLog("OnClientDisconnect");

        NetworkClient.UnregisterHandler<ShouldDisconnectMessage>();

        ClientDisconnect?.Invoke(this, conn);

        base.OnClientDisconnect(conn);
    }

    public override void OnFatalError(string error)
    {
        BonsaiLogWarning($"OnFatalError: {error}");
        base.OnFatalError(error);
    }

    public override void OnServerPrepared(string hostAddress, ushort hostPort)
    {
        BonsaiLog($"OnServerPrepared ({hostAddress}:{hostPort}) isLanOnly={isLANOnly}");
        ServerPrepared?.Invoke(this, new EventArgs());
    }

    private void OnShouldDisconnect(ShouldDisconnectMessage _)
    {
        BonsaiLog("ShouldDisconnect");
        StopClient();
    }

    private IEnumerator StartXR()
    {
        BonsaiLog("Initializing XR");
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            BonsaiLogError("Initializing XR Failed. Check Editor or Player log for details.");
        }
        else
        {
            BonsaiLog("Starting XR");
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }
    }

    private void StopXR()
    {
        if (XRGeneralSettings.Instance.Manager.isInitializationComplete)
        {
            BonsaiLog("Stopping XR");
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        }
    }

    public void UpdateUserInfo(uint netId, UserInfo userInfo)
    {
        var updated = false;
        foreach (var conn in PlayerInfos.Keys.ToList())
        {
            if (netId == conn.identity.netId)
            {
                PlayerInfos[conn].User = userInfo;
                updated = true;
            }
        }

        if (updated)
        {
            BonsaiLog($"Updated UserInfo in PlayerInfos -> {userInfo.DisplayName}");
        }
        else
        {
            BonsaiLogWarning("Tried to update PlayerInfos but failed");
        }

        InfoChange?.Invoke(this, new EventArgs());
    }

    private IEnumerator KickClients()
    {
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn.connectionId != NetworkConnection.LocalConnectionId)
            {
                RequestDisconnectClient(conn);
            }
        }

        yield return new WaitForSeconds(HardKickDelay);

        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn.connectionId != NetworkConnection.LocalConnectionId)
            {
                DisconnectClient(conn);
            }
        }
    }

    private IEnumerator KickClient(int id)
    {
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn.connectionId == id)
            {
                RequestDisconnectClient(conn);
            }
        }

        yield return new WaitForSeconds(HardKickDelay);

        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn.connectionId == id)
            {
                DisconnectClient(conn);
            }
        }
    }

    private int OpenSpotId()
    {
        var spots = new List<int>();
        for (var i = 0; i < SpotManager.Instance.spotInfo.Length; i++)
        {
            spots.Add(i);
        }

        foreach (var info in PlayerInfos.Values)
        {
            spots.Remove(info.Spot);
        }

        if (spots.Count > 0)
        {
            return spots[0];
        }

        BonsaiLogError("No open spot");
        return 0;
    }

    private void RequestDisconnectClient(NetworkConnection conn)
    {
        conn.Send(new ShouldDisconnectMessage());
    }

    private void DisconnectClient(NetworkConnection conn)
    {
        conn.Disconnect();
    }

    private void MaybeStartHost()
    {
        if (Time.realtimeSinceStartup - _lastStartHost > StartHostCooldown && HostEndPoint is null)
        {
            BonsaiLog($"StartHost {mode} {State} ({HostEndPoint})");
            StartHost();
            _lastStartHost = Time.realtimeSinceStartup;
        }
        else if (!(HostEndPoint is null))
        {
            // todo this can get stuck if somehow host mode is offline but HostEndPoint is not null
            BonsaiLog("HostEndpoint is not null");
        }
    }

    private void BonsaiLog(string msg)
    {
        Debug.Log("<color=orange>BonsaiNetwork: </color>: " + msg);
    }

    private void BonsaiLogWarning(string msg)
    {
        Debug.LogWarning("<color=orange>BonsaiNetwork: </color>: " + msg);
    }

    private void BonsaiLogError(string msg)
    {
        Debug.LogError("<color=orange>BonsaiNetwork: </color>: " + msg);
    }

    [Serializable]
    public class PlayerInfo
    {
        public int Spot;
        public UserInfo User;

        public PlayerInfo(int spot, string user)
        {
            Spot = spot;
            User = new UserInfo(user);
        }
    }

    public readonly struct UserInfo
    {
        public readonly string DisplayName;

        public UserInfo(string displayName)
        {
            DisplayName = displayName;
        }
    }

    private struct ShouldDisconnectMessage : NetworkMessage { }
}