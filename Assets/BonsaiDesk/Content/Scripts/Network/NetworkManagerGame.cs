using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dissonance;
using Mirror;
using NobleConnect.Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Networking;
using UnityEngine.XR.Management;

public class NetworkManagerGame : NobleNetworkManager {
	public enum ConnectionState {
		RelayError,
		Loading,
		Neutral,
		HostCreating,
		HostWaiting,
		Hosting,
		ClientConnecting,
		ClientConnected
	}

	public static NetworkManagerGame Singleton;

	public static int AssignedColorIndex;
	public TableBrowser tableBrowser;

	public bool serverOnlyIfEditor;
	public bool visualizeAuthority;

	public MoveToDesk moveToDesk;

	public TogglePause togglePause;

	public TextMeshProUGUI textMesh;
	public BonsaiScreenFade fader;

	public float waitBeforeSpawnButton = 0.4f;

	public GameObject relayFailedButtons;

	private readonly bool[] _spotInUse = new bool[2];

	public readonly Dictionary<NetworkConnection, PlayerInfo> PlayerInfos =
		new Dictionary<NetworkConnection, PlayerInfo>();

	private readonly float postRoomInfoEvery = 1f;

	private Camera _camera;
	private DissonanceComms _comms;

	private ConnectionState _connectionState;
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

		tableBrowser.BrowserReady += () => { tableBrowser.ToggleHidden(); };

		tableBrowser.JoinRoom  += HandleJoinRoom;
		tableBrowser.LeaveRoom += HandleLeaveRoom;
		tableBrowser.KickAll   += HandleKickAll;

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

	public override void Update() {
		base.Update();
		if (Time.time - _postRoomInfoLast > postRoomInfoEvery) {
			_postRoomInfoLast = Time.time;
			tableBrowser.PostNetworkState(State.ToString());
			if (HostEndPoint != null) {
				tableBrowser.PostRoomInfo(HostEndPoint.Address.ToString(), HostEndPoint.Port.ToString());
			}
			else {
				tableBrowser.PostRoomInfo("", "");
			}
		}
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

	private IEnumerator StopHostFadeReturnToLoading() {
		fader.FadeOut();
		yield return new WaitForSeconds(fader.fadeTime);
		StopHost();
		yield return new WaitForSeconds(0.5f);
		State = ConnectionState.Loading;
		fader.FadeIn();
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
				if (work == Work.Setup) {
					isLANOnly     = true;
					textMesh.text = "Internet Disconnected!\n\n\n\n(reconnect)";
					Debug.Log("[BONSAI] RelayError Setup");
					ActivateButtons(relayFailedButtons, waitBeforeSpawnButton);
				}
				else {
					textMesh.text = "...";
					DisableButtons(relayFailedButtons);
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

			// Has a HostEndPoint and now can open room or join a room
			case ConnectionState.Neutral:
				if (work == Work.Setup) {
					if (fader.currentAlpha != 0) {
						fader.FadeIn();
					}

					if (serverOnlyIfEditor && Application.isEditor) {
						State = ConnectionState.HostWaiting;
					}
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
					fader.FadeIn();
					SetCommsActive(_comms, true);
				}
				else {
					client?.Disconnect();
					StopClient();
				}

				break;

			default:
				Debug.LogError($"[BONSAI] HandleState not handled {State}");
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
			State = ConnectionState.Neutral;
		}

		if (fader.currentAlpha != 0) {
			fader.FadeIn();
		}
	}

	private IEnumerator SmoothStartClient() {
		fader.FadeOut();
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
		fader.FadeOut();
		yield return new WaitForSeconds(fader.fadeTime);
		State = ConnectionState.Loading;
	}

	private void ActivateButtons(GameObject buttons, float delayTime) {
		StartCoroutine(DelayActivateButtons(buttons, delayTime));
	}

	private static void DisableButtons(GameObject buttons) {
		foreach (Transform child in buttons.transform) {
			var button = child.GetComponent<HoleButton>();
			if (button != null) {
				button.DisableButton();
			}
		}
	}

	private static IEnumerator DelayActivateButtons(GameObject buttons, float seconds) {
		yield return new WaitForSeconds(seconds);
		foreach (Transform child in buttons.transform) {
			var button = child.gameObject;

			if (button != null) {
				button.SetActive(true);
			}
		}
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
		Debug.Log("[BONSAI] OnServerPrepared isLanOnly: " + isLANOnly);
		Debug.Log("[BONSAI] OnServerPrepared: " + hostAddress + ":" + hostPort);

		// triggers on startup
		State = !isLANOnly ? ConnectionState.Neutral : ConnectionState.RelayError;
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
		if (NetworkServer.connections.Count > 1) {
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

	public void ClickRelayFailed() {
		StartCoroutine(StopHostFadeReturnToLoading());
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