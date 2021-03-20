using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dissonance;
using Mirror;
using NobleConnect.Mirror;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.XR.Management;

public class NetworkManagerGame : NobleNetworkManager {
	public static NetworkManagerGame Singleton;
	public bool serverOnlyIfEditor;

	public readonly Dictionary<NetworkConnection, PlayerInfo> PlayerInfos =
		new Dictionary<NetworkConnection, PlayerInfo>();

	private DissonanceComms _comms;

	public EventHandler<NetworkConnection> ServerAddPlayer;
	public EventHandler<NetworkConnection> ServerDisconnect;

	public override void Awake() {
		base.Awake();
		if (Singleton == null) {
			Singleton = this;
		}
	}

	public override void Start() {
		base.Start();

		// todo make these into EventHandler
		TableBrowserMenu.JoinRoom         += HandleJoinRoom;
		TableBrowserMenu.LeaveRoom        += HandleLeaveRoom;
		TableBrowserMenu.KickConnectionId += HandleKickConnectionId;
		TableBrowserMenu.OpenRoom         += HandleOpenRoom;
		TableBrowserMenu.CloseRoom        += HandleCloseRoom;

		// todo make this into EventHandler
		MoveToDesk.OrientationChanged += HandleOrientationChanged;

		_comms = GetComponent<DissonanceComms>();
		SetCommsActive( false);

		if (Application.isEditor && !serverOnlyIfEditor) {
			StartCoroutine(StartXR());
		}
	}

	private void HandleOrientationChanged(bool oriented) {
		if (!oriented) {
			SetCommsActive( false);
		}
		throw new NotImplementedException();
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
		throw new NotImplementedException();
	}

	private void HandleLeaveRoom() {
		throw new NotImplementedException();
	}

	private void HandleJoinRoom(TableBrowserMenu.RoomData roomData) {
		throw new NotImplementedException();
	}

	private void HandleCloseRoom() {
		throw new NotImplementedException();
	}

	private void HandleOpenRoom() {
		throw new NotImplementedException();
	}

	public override void OnServerAddPlayer(NetworkConnection conn) {
		base.OnServerAddPlayer(conn);

		TargetSetSpot(conn, PlayerInfos[conn].spot);

		// todo send an open spot message

		// todo move into hosting mode if not already

		ServerAddPlayer?.Invoke(this, conn);

		throw new NotImplementedException();
	}

	public override void OnServerDisconnect(NetworkConnection conn) {
		base.OnServerDisconnect(conn);
		ServerDisconnect?.Invoke(this, conn);
		throw new NotImplementedException();
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

	[TargetRpc]
	private void TargetSetSpot(NetworkConnection conn, int spot) {
		switch (spot) {
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

	[Serializable]
	public class PlayerInfo {
		public int spot;
		public UserInfo User;

		// todo add the proper username
		public PlayerInfo() {
			User = new UserInfo("NoName");
		}
	}

	public readonly struct UserInfo {
		public readonly string DisplayName;

		public UserInfo(string displayName) {
			DisplayName = displayName;
		}
	}

	private int OpenSpotId() {
		var spots = new List<int>{0, 1};
		foreach (var info in PlayerInfos.Values) {
			spots.Remove(info.spot);
		}
		if (spots.Count > 0) {
			return spots[0];
		}
		Debug.LogError("[BONSAI] No open spot");
		return 0;
	}
}