using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dissonance;
using Mirror;
using NobleConnect.Mirror;
using UnityEngine;
using UnityEngine.XR.Management;
using Logger = NobleConnect.Logger;

// this is a modified version of NobleNetworkManager
// this version cleans up AddListener properly

public class NetworkManagerGame : BonsaiNetworkManager {
	public enum ConnectionState {
		RelayError,
		Loading,
		Hosting,
		ClientConnecting,
		ClientConnected
	}

	private const double StartHostCooldown = 0.5f;

	private const float HardKickDelay = 0.5f;

	public static NetworkManagerGame Singleton;
	public static EventHandler<NetworkConnection> ServerAddPlayer;
	public static EventHandler<NetworkConnection> ServerDisconnect;
	[HideInInspector] public bool roomOpen;
	public bool serverOnlyIfEditor;

	public bool visualizeAuthority;

	public readonly Dictionary<NetworkConnection, PlayerInfo> PlayerInfos =
		new Dictionary<NetworkConnection, PlayerInfo>();

	private DissonanceComms _comms;
	private float _lastStartHost = Mathf.NegativeInfinity;

	private bool _roomJoinInProgress;

	public EventHandler InfoChange;

	public ConnectionState State {
		get {
			switch (mode) {
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

	public override void Awake() {
		base.Awake();
		if (Singleton == null) {
			Singleton = this;
		}
	}

	public override void Start() {
		base.Start();

		logLevel = Logger.Level.Debug;

		// todo make these into EventHandler
		TableBrowserMenu.JoinRoom         += HandleJoinRoom;
		TableBrowserMenu.LeaveRoom        += HandleLeaveRoom;
		TableBrowserMenu.KickConnectionId += HandleKickConnectionId;
		TableBrowserMenu.OpenRoom         += HandleOpenRoom;
		TableBrowserMenu.CloseRoom        += HandleCloseRoom;

		// todo make this into EventHandler
		MoveToDesk.OrientationChanged += HandleOrientationChanged;

		_comms = GetComponent<DissonanceComms>();
		SetCommsActive(false);

		if (Application.isEditor && !serverOnlyIfEditor) {
			StartCoroutine(StartXR());
		}
	}

	public override void Update() {
		base.Update();

		if (isDisconnecting || _roomJoinInProgress) {
			Debug.Log(
				$"[BONSAI] NetworkManager prevent Update while isDisconnecting={isDisconnecting} _roomJoinInProgress={_roomJoinInProgress}");
			return;
		}

		if (serverOnlyIfEditor) {
			if (mode != NetworkManagerMode.ServerOnly) {
				roomOpen = true;
				StartServer();
			}
		}
		else {
			switch (mode) {
				case NetworkManagerMode.Offline:
					MaybeStartHost();
					break;
				case NetworkManagerMode.ServerOnly:
					break;
				case NetworkManagerMode.ClientOnly:
					break;
				case NetworkManagerMode.Host:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

	private void OnApplicationFocus(bool focus) {
		if (focus) {
			if (MoveToDesk.Singleton.oriented) {
				Debug.Log("[BONSAI] Setting comms active");
				SetCommsActive(true);
			}
		}
		else {
			Debug.Log("[BONSAI] Setting comms inactive");
			SetCommsActive(false);
		}
	}

	private void OnApplicationPause(bool pauseStatus) {
		if (pauseStatus) {
			SetCommsActive(false);
		}
	}

	public override void OnApplicationQuit() {
		base.OnApplicationQuit();
		StopXR();
	}

	private IEnumerator DelayStopHost(float seconds) {
		yield return new WaitForSeconds(seconds);
		Debug.Log("Stop Hosting");
		StopHost();
	}

	private void HandleOrientationChanged(bool oriented) {
		if (!oriented) {
			SetCommsActive(false);
		}
	}

	private void SetCommsActive(bool active) {
		if (_comms is null) {
			Debug.LogWarning("[BONSAI] Trying to set active on comms when null");
			return;
		}

		if (active) {
			_comms.IsMuted    = false;
			_comms.IsDeafened = false;
		}
		else {
			_comms.IsMuted    = true;
			_comms.IsDeafened = true;
		}
	}

	private void HandleKickConnectionId(int id) {
		Debug.Log($"[Bonsai] NetworkManager Kick (id={id})");
		StartCoroutine(KickClient(id));
	}

	private void HandleLeaveRoom() {
		Debug.Log("[Bonsai] NetworkManager LeaveRoom");
		StartCoroutine(DelayStopClient(0.1f));
	}

	private void HandleJoinRoom(TableBrowserMenu.RoomData roomData) {
		if (!_roomJoinInProgress) {
			StartCoroutine(JoinRoom(roomData));
		}
		else {
			Debug.LogWarning("[BONSAI] Ignoring attempt to join room while room join is in progress");
		}
	}

	private IEnumerator JoinRoom(TableBrowserMenu.RoomData roomData) {
		Debug.Log("[Bonsai] NetworkManager Begin JoinRoom");
		_roomJoinInProgress = true;
		if (mode == NetworkManagerMode.Host || !(HostEndPoint is null)) {
			StopHost();
		}

		while (!(HostEndPoint is null)) {
			Debug.Log("[BONSAI] JoinRoom: wait for HostEndPoint to be null");
			yield return null;
		}

		if (mode == NetworkManagerMode.Offline) {
			networkAddress = roomData.ip_address;
			networkPort    = roomData.port;
			StartClient();
		}
		else {
			Debug.LogWarning("[Bonsai] NetworkManager tried to join room while a hosting/client, ignoring");
		}

		_roomJoinInProgress = false;
	}

	private void HandleCloseRoom() {
		Debug.Log("[BONSAI] NetworkManager CloseRoom");
		roomOpen = false;
		StartCoroutine(KickClients());
		InfoChange?.Invoke(this, new EventArgs());
	}

	private void HandleOpenRoom() {
		Debug.Log("[BONSAI] NetworkManager OpenRoom");
		roomOpen = true;
		InfoChange?.Invoke(this, new EventArgs());
	}

	public override void OnServerConnect(NetworkConnection conn) {
		base.OnServerConnect(conn);
		Debug.Log("[BONSAI] NetworkManager ServerConnect");
	}

	public override void OnServerAddPlayer(NetworkConnection conn) {
		base.OnServerAddPlayer(conn);
		Debug.Log("[BONSAI] NetworkManager ServerAddPlayer");

		var openSpot = OpenSpotId();
		PlayerInfos.Add(conn, new PlayerInfo(openSpot, "NoName"));
		conn.Send(new SpotMessage(openSpot));

		// todo move into hosting mode if not already

		ServerAddPlayer?.Invoke(this, conn);
	}

	public override void OnServerDisconnect(NetworkConnection conn) {
		Debug.Log("[BONSAI] ServerDisconnect");

		ServerDisconnect?.Invoke(this, conn);

		PlayerInfos.Remove(conn);

		var tmp = new HashSet<NetworkIdentity>(conn.clientOwnedObjects);
		foreach (var identity in tmp) {
			var autoAuthority = identity.GetComponent<AutoAuthority>();
			if (autoAuthority != null) {
				if (autoAuthority.InUse) {
					autoAuthority.SetInUse(false);
				}

				identity.RemoveClientAuthority();
			}
		}

		// call the base after the ServerDisconnect event otherwise null reference gets passed to subscribers
		base.OnServerDisconnect(conn);
	}

	public override void OnClientConnect(NetworkConnection conn) {
		Debug.Log($"[BONSAI] OnClientConnect {conn.connectionId} {conn.isReady}");

		base.OnClientConnect(conn);

		NetworkClient.RegisterHandler<SpotMessage>(OnSpot);
		NetworkClient.RegisterHandler<ShouldDisconnectMessage>(OnShouldDisconnect);
	}

	public override void OnFatalError(string error) {
		base.OnFatalError(error);
		Debug.LogWarning($"[BONSAI] OnFatalError: {error}");
	}

	public override void OnServerPrepared(string hostAddress, ushort hostPort) {
		Debug.Log($"[BONSAI] OnServerPrepared ({hostAddress} : {hostPort}) isLanOnly={isLANOnly}");
	}

	private void OnShouldDisconnect(ShouldDisconnectMessage _) {
		Debug.Log("[BONSAI] NetworkManger ShouldDisconnect");
		client?.Disconnect();
	}

	private void OnSpot(SpotMessage spot) {
		switch (spot.ID) {
			case 0:
				GameObject.Find("GameManager").GetComponent<MoveToDesk>()
				          .SetTableEdge(GameObject.Find("DefaultEdge").transform);
				break;
			case 1:
				GameObject.Find("GameManager").GetComponent<MoveToDesk>()
				          .SetTableEdge(GameObject.Find("AcrossEdge").transform);
				break;
		}
	}

	private static IEnumerator StartXR() {
		Debug.Log("[BONSAI] Initializing XR");
		yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

		if (XRGeneralSettings.Instance.Manager.activeLoader == null) {
			Debug.LogError("[BONSAI] Initializing XR Failed. Check Editor or Player log for details.");
		}
		else {
			Debug.Log("[BONSAI] Starting XR");
			XRGeneralSettings.Instance.Manager.StartSubsystems();
		}
	}

	private static void StopXR() {
		if (XRGeneralSettings.Instance.Manager.isInitializationComplete) {
			Debug.Log("[BONSAI] Stopping XR");
			XRGeneralSettings.Instance.Manager.StopSubsystems();
			XRGeneralSettings.Instance.Manager.DeinitializeLoader();
			Debug.Log("[BONSAI] Stopped XR");
		}
	}

	public void UpdateUserInfo(uint netId, UserInfo userInfo) {
		var updated = false;
		foreach (var conn in PlayerInfos.Keys.ToList()) {
			if (netId == conn.identity.netId) {
				PlayerInfos[conn].User = userInfo;
				updated                = true;
			}
		}

		if (updated) {
			Debug.Log($"[BONSAI] Updated UserInfo in PlayerInfos -> {userInfo.DisplayName}");
		}
		else {
			Debug.LogWarning("[BONSAI] Tried to update PlayerInfos but failed");
		}
	}

	private IEnumerator KickClients() {
		foreach (var conn in NetworkServer.connections.Values) {
			if (conn.connectionId != NetworkConnection.LocalConnectionId) {
				RequestDisconnectClient(conn);
			}
		}

		yield return new WaitForSeconds(HardKickDelay);

		foreach (var conn in NetworkServer.connections.Values) {
			if (conn.connectionId != NetworkConnection.LocalConnectionId) {
				DisconnectClient(conn);
			}
		}
	}

	private IEnumerator KickClient(int id) {
		foreach (var conn in NetworkServer.connections.Values) {
			if (conn.connectionId == id) {
				RequestDisconnectClient(conn);
			}
		}

		yield return new WaitForSeconds(HardKickDelay);

		foreach (var conn in NetworkServer.connections.Values) {
			if (conn.connectionId == id) {
				DisconnectClient(conn);
			}
		}
	}

	private int OpenSpotId() {
		var spots = new List<int> {0, 1};
		foreach (var info in PlayerInfos.Values) {
			spots.Remove(info.Spot);
		}

		if (spots.Count > 0) {
			return spots[0];
		}

		Debug.LogError("[BONSAI] No open spot");
		return 0;
	}

	private void RequestDisconnectClient(NetworkConnection conn) {
		conn.Send(new ShouldDisconnectMessage());
	}

	private void DisconnectClient(NetworkConnection conn) {
		conn.Disconnect();
	}

	private void MaybeStartHost() {
		if (Time.time - _lastStartHost > StartHostCooldown) {
			Debug.Log("[BONSAI] NetworkManager StartHost");
			StartHost();
			_lastStartHost = Time.time;
		}
	}

	private IEnumerator DelayStopClient(float seconds) {
		yield return new WaitForSeconds(seconds);
		Debug.Log("[BONSAI] StopClient");
		StopClient();
		_lastStartHost = Time.time;
	}

	[Serializable]
	public class PlayerInfo {
		public int Spot;
		public UserInfo User;

		public PlayerInfo(int spot, string user) {
			Spot = spot;
			User = new UserInfo(user);
		}
	}

	public readonly struct UserInfo {
		public readonly string DisplayName;

		public UserInfo(string displayName) {
			DisplayName = displayName;
		}
	}

	private struct ShouldDisconnectMessage : NetworkMessage { }

	private struct SpotMessage : NetworkMessage {
		public readonly int ID;

		public SpotMessage(int id) {
			ID = id;
		}
	}
}