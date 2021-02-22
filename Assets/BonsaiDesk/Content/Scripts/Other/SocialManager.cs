using Mirror;
using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine;
using Application = UnityEngine.Application;

public class SocialManager : NetworkBehaviour {
	public static SocialManager Singleton;
	private float _postInfoLast;
	private bool reportWhenReady;
	public User User;

	// Start is called before the first frame update
	private void Start() {
		if (Singleton == null) {
			Singleton = this;
		}

		NetworkManagerGame.Singleton.ServerAddPlayer -= HandleServerAddPlayer;
		NetworkManagerGame.Singleton.ServerAddPlayer += HandleServerAddPlayer;

		Core.AsyncInitialize().OnComplete(InitCallback);
	}

	// Update is called once per frame
	private void Update() {
		if (reportWhenReady && User != null) {
			Users.GetLoggedInUser().OnComplete(msg =>
			{
				if (msg.IsError) {
					TerminateWithError(msg);
					return;
				}

				CmdSetUserInfo(new NetworkManagerGame.UserInfo(msg.Data.OculusID));
			});
			reportWhenReady = false;
		}
	}

	private void HandleGetLoggedInUser(Message<User> msg) {
		Debug.Log("");
		if (msg.IsError) {
			TerminateWithError(msg);
			return;
		}

		User = msg.Data;
	}

	private void InitCallback(Message<PlatformInitialize> msg) {
		if (msg.IsError) {
			TerminateWithError(msg);
			return;
		}

		Users.GetLoggedInUser().OnComplete(HandleGetLoggedInUser);
	}

	private static void TerminateWithError(Message msg) {
		Debug.LogError($"[BONSAI] Error {msg.GetError().Message}");
		Application.Quit();
	}

	[TargetRpc]
	private void TargetReportUserInfo(NetworkConnection conn) {
		var id = NetworkClient.connection.identity.netId;
		if (User != null) {
			CmdSetUserInfo(new NetworkManagerGame.UserInfo(User.DisplayName));
		}
		else {
			reportWhenReady = true;
		}
	}

	[Command(ignoreAuthority = true)]
	private void CmdSetUserInfo(NetworkManagerGame.UserInfo user, NetworkConnectionToClient sender = null) {
		var id = sender.identity.netId;
		NetworkManagerGame.Singleton.UpdateUserInfo(id, user);
		Debug.Log($"[BONSAI] Recieved user info (id={id}) ({user.DisplayName})");
	}

	private void HandleServerAddPlayer(NetworkConnection conn) {
		TargetReportUserInfo(conn);
	}
}