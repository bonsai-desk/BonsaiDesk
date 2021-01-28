using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dissonance;
using Mirror;
using NobleConnect.Mirror;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Networking;
using UnityEngine.XR.Management;

public class NetworkManagerGame : NobleNetworkManager {
	public enum ConnectionState {
		RelayError,
		Loading,
		Hosting,
		ClientConnecting,
		ClientConnected
	}

	public static NetworkManagerGame Singleton;
	public static int AssignedColorIndex;

	[Header("Bonsai Network Manager")] public bool serverOnlyIfEditor;
	public bool visualizeAuthority;
	public bool browserReady;
	public TableBrowser tableBrowser;
	public MoveToDesk moveToDesk;
	public TogglePause togglePause;
	public BonsaiScreenFade fader;
	private readonly bool[] _spotInUse = new bool[2];

	public readonly Dictionary<NetworkConnection, PlayerInfo> PlayerInfos =
		new Dictionary<NetworkConnection, PlayerInfo>();

	private readonly float postRoomInfoEvery = 1f;
	private Camera _camera;
	private DissonanceComms _comms;
	private ConnectionState _connectionState = ConnectionState.RelayError;
	private float _postRoomInfoLast;
	private UnityWebRequest _roomRequest;

	public ConnectionState State {
		get => _connectionState;
		set {
			if (_connectionState == value) {
				Debug.LogWarning("[BONSAI] Trying to set state to itself: " + State);
			}

			Debug.Log("[BONSAI] HandleState Cleanup " + _connectionState);
			HandleState(_connectionState, Work.Cleanup);

			_connectionState = value;

			Debug.Log("[BONSAI] HandleState Setup " + value);
			HandleState(value, Work.Setup);
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

		tableBrowser.BrowserReady += () =>
		{
			browserReady = true;
			tableBrowser.ToggleHidden();
		};

		tableBrowser.JoinRoom         += HandleJoinRoom;
		tableBrowser.LeaveRoom        += HandleLeaveRoom;
		tableBrowser.KickAll          += HandleKickAll;
		tableBrowser.KickConnectionId += HandleKickConnectionId;

		_camera = GameObject.Find("CenterEyeAnchor").GetComponent<Camera>();

		for (var i = 0; i < _spotInUse.Length; i++) {
			_spotInUse[i] = false;
		}

		State = ConnectionState.Loading;

		if (!Permission.HasUserAuthorizedPermission(Permission.Microphone)) {
			Permission.RequestUserPermission(Permission.Microphone);
		}

		_comms = GetComponent<DissonanceComms>();
		SetCommsActive(_comms, false);

		OVRManager.HMDUnmounted += VoidAndDeafen;

		MoveToDesk.OrientationChanged += HandleOrientationChange;

		if (Application.isEditor && !serverOnlyIfEditor) {
			StartCoroutine(StartXR());
		}
	}

	private void HandleKickConnectionId(int id) {
		Debug.Log($"[BONSAI] Kick Id {id}");
		StartCoroutine(KickClient(id));
	}

	public override void Update() {
		base.Update();

		if (browserReady && Time.time - _postRoomInfoLast > postRoomInfoEvery) {
			_postRoomInfoLast = Time.time;
			tableBrowser.PostNetworkState(State.ToString());
			tableBrowser.PostPlayerInfo(PlayerInfos);
			if (HostEndPoint != null) {
				tableBrowser.PostRoomInfo(HostEndPoint.Address.ToString(), HostEndPoint.Port.ToString());
			}
			else {
				tableBrowser.PostRoomInfo("", "");
			}
		}

		// TODO 
		// StartCoroutine(StopHostFadeReturnToLoading());
		// i.e. automatically try to reconnect to the relay service if not connected
	}

	private void OnApplicationFocus(bool focus) {
		if (moveToDesk.oriented) {
			SetCommsActive(_comms, focus);
		}
	}

	private void OnApplicationPause(bool pause) {
		if (!pause) {
			return;
		}

		SetCommsActive(_comms, false);
		moveToDesk.ResetPosition();
	}

	private void OnShouldDisconnect(ShouldDisconnectMessage _) {
		StartCoroutine(FadeThenReturnToLoading());
	}

	private static void SetCommsActive(DissonanceComms comms, bool active) {
		if (comms == null) {
			return;
		}

		if (active) {
			comms.IsMuted    = false;
			comms.IsDeafened = false;
		}
		else {
			comms.IsMuted    = true;
			comms.IsDeafened = true;
		}
	}

	private void HandleState(ConnectionState state, Work work) {
		switch (state) {
			case ConnectionState.RelayError:
				// set to loading to get out of here
				// you will get bounced back if there is no internet
				if (work == Work.Setup) {
					isLANOnly = true;
					Debug.Log("[BONSAI] RelayError Setup");
				}

				break;

			// Waiting for a HostEndPoint
			case ConnectionState.Loading:
				if (work == Work.Setup) {
					Debug.Log("[BONSAI] Loading Setup isLanOnly " + isLANOnly);

					if (client != null) {
						StopClient();
					}

					moveToDesk.SetTableEdge(GameObject.Find("DefaultEdge").transform);
					SetCommsActive(_comms, false);
					StartCoroutine(StartHostAfterDisconnect());
				}

				break;

			// Has a client connected
			case ConnectionState.Hosting:
				if (work == Work.Setup) {
					SetCommsActive(_comms, true);
				}
				else {
					StartCoroutine(KickClients());
				}

				break;

			// Client connected to a host
			case ConnectionState.ClientConnected:
				if (work == Work.Setup) {
					// todo set fade mask 0
					SetCommsActive(_comms, true);
				}
				else {
					client?.Disconnect();
					StopClient();
				}

				break;

			default:
				Debug.LogWarning($"[BONSAI] HandleState not handled {State}");
				break;
		}
	}

	private IEnumerator StartHostAfterDisconnect() {
		while (isDisconnecting) {
			yield return null;
		}

		if (HostEndPoint == null || isLANOnly) {
			Debug.Log("[BONSAI] StartHostAfterDisconnect StartHost ");
			isLANOnly = false;
			if (serverOnlyIfEditor && Application.isEditor) {
				StartServer();
			}
			else {
				StartHost();
			}
		}
		else {
			State = ConnectionState.Hosting;
		}

		// todo set fade mask 0
	}

	private IEnumerator SmoothStartClient() {
		State = ConnectionState.ClientConnecting;
		// todo set fade mask 1
		yield return new WaitForSeconds(fader.fadeTime);
		Debug.Log("[BONSAI] SmoothStartClient StopHost");
		StopHost();
		if (HostEndPoint != null) {
			yield return null;
		}

		Debug.Log("[BONSAI] HostEndPoint == null");
		Debug.Log("[BONSAI] StartClient");
		StartClient();
	}

	private IEnumerator FadeThenReturnToLoading() {
		// todo set fade mask 1
		yield return new WaitForSeconds(fader.fadeTime);
		State = ConnectionState.Loading;
	}

	private IEnumerator KickClients() {
		foreach (var conn in NetworkServer.connections.Values.ToList()
		                                  .Where(conn => conn.connectionId != NetworkConnection.LocalConnectionId)) {
			conn.Send(new ShouldDisconnectMessage());
		}

		yield return new WaitForSeconds(fader.fadeTime + 0.15f);
		foreach (var conn in NetworkServer.connections.Values.ToList()
		                                  .Where(conn => conn.connectionId != NetworkConnection.LocalConnectionId)) {
			conn.Disconnect();
		}
	}

	private IEnumerator KickClient(int id) {
		NetworkConnectionToClient _conn = null;
		foreach (var conn in NetworkServer.connections.Values.ToList()) {
			if (conn.connectionId != NetworkConnection.LocalConnectionId && conn.connectionId == id) {
				_conn = conn;
				break;
			}
		}
		if (_conn != null) {
			_conn.Send(new ShouldDisconnectMessage());
			yield return new WaitForSeconds(fader.fadeTime + 0.15f);
			_conn.Disconnect();
		}
	}

	private void VoidAndDeafen() {
		moveToDesk.ResetPosition();
		SetCommsActive(_comms, false);
	}

	private void HandleOrientationChange(bool oriented) {
		if (oriented) {
			_camera.cullingMask |= 1 << LayerMask.NameToLayer("networkPlayer");
		}
		else {
			_camera.cullingMask &= ~(1 << LayerMask.NameToLayer("networkPlayer"));
		}

		if (State != ConnectionState.ClientConnected && State != ConnectionState.Hosting) {
			return;
		}

		SetCommsActive(_comms, oriented);
	}

	private void HandleJoinRoom(TableBrowser.RoomData roomData) {
		Debug.Log($"[Bonsai] NetworkManager Join Room {roomData.ip_address} {roomData.port}");
		networkAddress = roomData.ip_address;
		networkPort    = roomData.port;
		StartCoroutine(SmoothStartClient());
	}

	private void HandleLeaveRoom() {
		StartCoroutine(FadeThenReturnToLoading());
	}

	private void HandleKickAll() {
		State = ConnectionState.Loading;
	}

	public event Action<NetworkConnection> ServerAddPlayer;

	public event Action<NetworkConnection> ServerDisconnect;

	private IEnumerator StartXR() {
		yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
		XRGeneralSettings.Instance.Manager.StartSubsystems();
	}

	public override void OnFatalError(string error) {
		base.OnFatalError(error);
		Debug.Log("[BONSAI] OnFatalError");
		Debug.Log(error);
		State = ConnectionState.RelayError;
	}

	public override void OnServerPrepared(string hostAddress, ushort hostPort) {
		Debug.Log($"[BONSAI] OnServerPrepared ({hostAddress} : {hostPort}) isLanOnly={isLANOnly}");
		State = !isLANOnly ? ConnectionState.Hosting : ConnectionState.RelayError;
	}

	public override void OnServerConnect(NetworkConnection conn) {
		Debug.Log("[BONSAI] OnServerConnect");

		base.OnServerConnect(conn);

		var openSpotId = -1;
		for (var i = 0; i < _spotInUse.Length; i++) {
			if (!_spotInUse[i]) {
				openSpotId = i;
				break;
			}
		}

		if (openSpotId == -1) {
			Debug.LogError("No open spot.");
			openSpotId = 0;
		}

		_spotInUse[openSpotId] = true;
		PlayerInfos.Add(conn, new PlayerInfo(openSpotId));

		// triggers when client joins
		if (NetworkServer.connections.Count > 1 && State != ConnectionState.Hosting) {
			State = ConnectionState.Hosting;
		}
	}

	public override void OnServerAddPlayer(NetworkConnection conn) {
		Debug.Log("[BONSAI] OnServerAddPlayer");
		conn.Send(new SpotMessage {
			SpotId     = PlayerInfos[conn].spot,
			ColorIndex = PlayerInfos[conn].spot
		});

		base.OnServerAddPlayer(conn);

		if (State != ConnectionState.Hosting) {
			State = ConnectionState.Hosting;
		}

		ServerAddPlayer?.Invoke(conn);
	}

	public override void OnServerDisconnect(NetworkConnection conn) {
		Debug.Log("[BONSAI] OnServerDisconnect");

		if (!conn.isAuthenticated) {
			return;
		}

		ServerDisconnect?.Invoke(conn);

		var spotId = PlayerInfos[conn].spot;

		var spotUsedCount = 0;
		foreach (var player in PlayerInfos) {
			if (player.Value.spot == spotId) {
				spotUsedCount++;
			}
		}

		if (spotUsedCount <= 1) {
			_spotInUse[spotId] = false;
		}

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

		if (conn.identity != null && togglePause.AuthorityIdentityId == conn.identity.netId) {
			togglePause.RemoveClientAuthority();
		}

		base.OnServerDisconnect(conn);

		// triggers when last client leaves
		if (NetworkServer.connections.Count == 1) {
			State = ConnectionState.Loading;
		}
	}

	public override void OnClientConnect(NetworkConnection conn) {
		Debug.Log("[BONSAI] OnClientConnect");

		// For some reason OnClientConnect triggers twice occasionally. This is a hack to ignore the second trigger.
		if (conn.isReady) {
			return;
		}

		base.OnClientConnect(conn);

		NetworkClient.RegisterHandler<SpotMessage>(OnSpot);
		NetworkClient.RegisterHandler<ShouldDisconnectMessage>(OnShouldDisconnect);

		// triggers when client connects to remote host
		if (NetworkServer.connections.Count == 0) {
			State = ConnectionState.ClientConnected;
		}
	}

	public override void OnClientDisconnect(NetworkConnection conn) {
		Debug.Log("[BONSAI] OnClientDisconnect");

		NetworkClient.UnregisterHandler<SpotMessage>();
		NetworkClient.UnregisterHandler<ShouldDisconnectMessage>();

		switch (State) {
			case ConnectionState.ClientConnected:
				// this happens on client when the host exits rudely (power off, etc)
				// base method stops client with a delay so it can gracefully disconnct
				// since the client is getting booted here, we don't need to wait (which introduces bugs)

				// todo set fade mask 1
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

	private static void OnSpot(NetworkConnection conn, SpotMessage msg) {
		switch (msg.SpotId) {
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

	[Serializable]
	public class PlayerInfo {
		public int spot;

		public PlayerInfo(int spot) {
			this.spot = spot;
		}
	}

	private enum Work {
		Setup,
		Cleanup
	}

	private struct ShouldDisconnectMessage : NetworkMessage { }

	private struct SpotMessage : NetworkMessage {
		public int ColorIndex;
		public int SpotId;
	}
}