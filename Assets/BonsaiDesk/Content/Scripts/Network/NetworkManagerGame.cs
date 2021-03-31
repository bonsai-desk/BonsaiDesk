using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dissonance;
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
    private const float PingNetCooldown = 1.0f;
    private const float PingTimeoutBeforeDisconnect = 2.0f;

    public static NetworkManagerGame Singleton;
    public static EventHandler<NetworkConnection> ServerAddPlayer;
    public static EventHandler<NetworkConnection> ServerDisconnect;
    [HideInInspector] public bool roomOpen;
    public bool serverOnlyIfEditor;

    public bool visualizeAuthority;

    public DissonanceComms _comms;

    public GameObject networkHandLeftPrefab;
    public GameObject networkHandRightPrefab;

    public readonly Dictionary<NetworkConnection, PlayerInfo> PlayerInfos =
        new Dictionary<NetworkConnection, PlayerInfo>();

    private bool _hasFocus = true;

    private double _lastGoodPingRecieved = Mathf.NegativeInfinity;
    private float _lastPingNet = Mathf.NegativeInfinity;
    private float _lastStartHost = Mathf.NegativeInfinity;

    private bool _roomJoinInProgress;

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

        HandleCommsUpdate();
    }

    private void OnApplicationFocus(bool focus)
    {
        _hasFocus = focus;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SetCommsActive(false);
        }
    }

    public override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        StopXR();
    }

    private IEnumerator CheckInternetAccess()
    {
        var req = new UnityWebRequest("http://google.com/generate_204");
        yield return req.SendWebRequest();
        if (req.responseCode == 204 && Time.realtimeSinceStartup > _lastGoodPingRecieved)
        {
            _lastGoodPingRecieved = Time.realtimeSinceStartup;
        }
    }

    private void HandlePingUpdate()
    {
        if (Time.realtimeSinceStartup - _lastPingNet > PingNetCooldown)
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
            Debug.Log(
                $"[BONSAI] NetworkManager prevent Update while isDisconnecting={isDisconnecting} _roomJoinInProgress={_roomJoinInProgress}");
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
            var pingTimeout = Time.realtimeSinceStartup - _lastGoodPingRecieved > PingTimeoutBeforeDisconnect;
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
                        Debug.Log("[bonsai] Ping timeout as client");
                        StopClient();
                    }

                    break;
                case NetworkManagerMode.Host:
                    if (isLANOnly)
                    {
                        StopClientIfGoodPing();
                    }
                    else if (pingTimeout)
                    {
                        Debug.Log("[bonsai] Ping timeout as host");
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
        if (Time.realtimeSinceStartup - _lastGoodPingRecieved < 1.0f)
        {
            Debug.Log("[bonsai] Got a good ping, disconnecting from LAN");
            isLANOnly = false;
            StopClient();
        }
    }

    private void HandleCommsUpdate()
    {
        if (mode == NetworkManagerMode.ClientOnly || mode == NetworkManagerMode.Host)
        {
            var oriented = MoveToDesk.Singleton.oriented;
            var commsActive = IsCommsActive();
            if (_hasFocus && oriented && !commsActive)
            {
                SetCommsActive(true);
            }
            else if ((!_hasFocus || !oriented) && commsActive)
            {
                SetCommsActive(false);
            }
        }
        else if (IsCommsActive())
        {
            SetCommsActive(false);
        }
    }

    private void SetCommsActive(bool active)
    {
        Debug.Log($"[BONSAI] Set Comms {active}");
        if (_comms is null)
        {
            Debug.LogWarning("[BONSAI] Trying to set active on comms when null");
            return;
        }

        if (active)
        {
            _comms.IsMuted = false;
            _comms.IsDeafened = false;
        }
        else
        {
            _comms.IsMuted = true;
            _comms.IsDeafened = true;
        }
    }

    private bool IsCommsActive()
    {
        return !_comms.IsMuted && !_comms.IsDeafened;
    }

    private void HandleKickConnectionId(int id)
    {
        Debug.Log($"[Bonsai] NetworkManager Kick (id={id})");
        StartCoroutine(KickClient(id));
    }

    private void HandleLeaveRoom()
    {
        Debug.Log("[BONSAI] NetworkManager LeaveRoom");
        Debug.Log("[BONSAI] StopClient");
        StopClient();
    }

    private void HandleJoinRoom(TableBrowserMenu.RoomData roomData)
    {
        if (!_roomJoinInProgress)
        {
            if (HostEndPoint.Address.ToString() == roomData.ip_address && HostEndPoint.Port == roomData.port)
            {
                Debug.LogWarning("[BONSAI] Tried to join own hosted room as client, ignoring");
            }
            else
            {
                StartCoroutine(JoinRoom(roomData));
            }
        }
        else
        {
            Debug.LogWarning("[BONSAI] Ignoring attempt to join room while room join is in progress");
        }
    }

    private IEnumerator JoinRoom(TableBrowserMenu.RoomData roomData)
    {
        Debug.Log("[BONSAI] NetworkManager Begin JoinRoom");
        _roomJoinInProgress = true;
        if (mode == NetworkManagerMode.Host || !(HostEndPoint is null))
        {
            Debug.Log("[bonsai] NetworkManger StopHost before join room");
            StopHost();
            InfoChange?.Invoke(this, new EventArgs());
        }

        var t0 = Time.realtimeSinceStartup;
        while (!(HostEndPoint is null))
        {
            Debug.Log(
                $"[BONSAI] JoinRoom: wait for HostEndPoint to be null {HostEndPoint.Address} {HostEndPoint.Port}");
            if (HostEndPoint.Address.ToString() == "127.0.0.1" || HostEndPoint.Address.ToString() == "localhost")
            {
                // This happens when you are a LAN host then try to join a room
                Debug.Log("[bonsai] NetworkManager breaking HostEndPoint null check since localhost");
                break;
            }

            if (Time.realtimeSinceStartup - t0 > 2f)
            {
                Debug.Log(
                    "[bonsai] NetworkManager breaking loop since spent too long waiting for HostEndPoint to be null");
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
            Debug.LogWarning("[Bonsai] NetworkManager tried to join room while a hosting/client, ignoring");
        }

        _roomJoinInProgress = false;
    }

    private void HandleCloseRoom()
    {
        Debug.Log("[BONSAI] NetworkManager CloseRoom");
        roomOpen = false;
        StartCoroutine(KickClients());
        InfoChange?.Invoke(this, new EventArgs());
    }

    private void HandleOpenRoom()
    {
        Debug.Log("[BONSAI] NetworkManager OpenRoom");
        roomOpen = true;
        InfoChange?.Invoke(this, new EventArgs());
    }

    public override void OnServerConnect(NetworkConnection conn)
    {
        base.OnServerConnect(conn);
        Debug.Log("[BONSAI] NetworkManager ServerConnect");
    }

    //this doesn't call the base function because we need to instantiate and spawn the player ourselves here so we can change the spotId
    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        Debug.Log("[BONSAI] NetworkManager ServerAddPlayer");

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
        var player = startPos != null
            ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
            : Instantiate(playerPrefab);

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
        Debug.Log("[BONSAI] ServerDisconnect");

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
        Debug.Log($"[BONSAI] OnClientConnect {conn.connectionId} {conn.isReady}");

        base.OnClientConnect(conn);

        NetworkClient.RegisterHandler<ShouldDisconnectMessage>(OnShouldDisconnect);
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        Debug.Log("[BONSAI] OnClientDisconnect");

        NetworkClient.UnregisterHandler<ShouldDisconnectMessage>();

        base.OnClientDisconnect(conn);
    }

    public override void OnFatalError(string error)
    {
        Debug.LogWarning($"[BONSAI] OnFatalError: {error}");
        base.OnFatalError(error);
    }

    public override void OnServerPrepared(string hostAddress, ushort hostPort)
    {
        Debug.Log($"[BONSAI] OnServerPrepared ({hostAddress} : {hostPort}) isLanOnly={isLANOnly}");
    }

    private void OnShouldDisconnect(ShouldDisconnectMessage _)
    {
        Debug.Log("[BONSAI] NetworkManger ShouldDisconnect");
        StopClient();
    }

    private static IEnumerator StartXR()
    {
        Debug.Log("[BONSAI] Initializing XR");
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.LogError("[BONSAI] Initializing XR Failed. Check Editor or Player log for details.");
        }
        else
        {
            Debug.Log("[BONSAI] Starting XR");
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }
    }

    private static void StopXR()
    {
        if (XRGeneralSettings.Instance.Manager.isInitializationComplete)
        {
            Debug.Log("[BONSAI] Stopping XR");
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            Debug.Log("[BONSAI] Stopped XR");
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
            Debug.Log($"[BONSAI] Updated UserInfo in PlayerInfos -> {userInfo.DisplayName}");
        }
        else
        {
            Debug.LogWarning("[BONSAI] Tried to update PlayerInfos but failed");
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

        Debug.LogError("[BONSAI] No open spot");
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
        if (Time.realtimeSinceStartup - _lastStartHost > StartHostCooldown)
        {
            Debug.Log("[BONSAI] NetworkManager StartHost");
            StartHost();
            _lastStartHost = Time.realtimeSinceStartup;
        }
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