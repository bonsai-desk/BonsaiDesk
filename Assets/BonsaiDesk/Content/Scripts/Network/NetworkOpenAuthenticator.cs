using Mirror;
using UnityEngine;

/*
	Authenticators: https://mirror-networking.com/docs/Components/Authenticators/
	Documentation: https://mirror-networking.com/docs/Guides/Authentication.html
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkAuthenticator.html
*/

public class NetworkOpenAuthenticator : NetworkAuthenticator {
	/// <summary>
	///     Called on server from StartServer to initialize the Authenticator
	///     <para>Server message handlers should be registered in this method.</para>
	/// </summary>
	public override void OnStartServer() {
		// do nothing
	}

	/// <summary>
	///     Called on server from OnServerAuthenticateInternal when a client needs to authenticate
	/// </summary>
	/// <param name="conn">Connection to client.</param>
	public override void OnServerAuthenticate(NetworkConnection conn) {
		Debug.Log("[BONSAI] OnServerAuthenticate");
		if (NetworkManagerGame.Singleton.roomOpen ||
		    conn.connectionId == NetworkServer.localConnection.connectionId) {
			var authResponseMessage = new AuthResponseMessage {
				Code = 100
			};
			Debug.Log("[BONSAI] OnServerAuthenticate 100");
			conn.Send(authResponseMessage);
			ServerAccept(conn);
		}
		else {
			var authResponseMessage = new AuthResponseMessage {
				Code = 200
			};
			Debug.Log("[BONSAI] OnServerAuthenticate 200");
			conn.Send(authResponseMessage);
			conn.isAuthenticated = false;
			ServerReject(conn);
		}
	}

	/// <summary>
	///     Called on client from StartClient to initialize the Authenticator
	///     <para>Client message handlers should be registered in this method.</para>
	/// </summary>
	public override void OnStartClient() {
		// register a handler for the authentication response we expect from server
		NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponseMessage, false);
	}

	/// <summary>
	///     Called on client from OnClientAuthenticateInternal when a client needs to authenticate
	/// </summary>
	/// <param name="conn">Connection of the client.</param>
	public override void OnClientAuthenticate(NetworkConnection conn) {
		// do nothing just wait for AuthMessageResponse
	}

	private void OnAuthResponseMessage(NetworkConnection conn, AuthResponseMessage msg) {
		switch (msg.Code) {
			// Invoke the event to complete a successful authentication
			case 100:
				Debug.Log("[BONSAI] Authenticator Accepted");
				ClientAccept(conn);
				break;
			case 200:
				Debug.Log("[BONSAI] Authenticator Rejected");
				ClientReject(conn);
				NetworkManagerGame.Singleton.State = NetworkManagerGame.ConnectionState.Loading;
				break;
		}
	}

	private struct AuthResponseMessage : NetworkMessage {
		// 100 : good
		// 200 : reject
		public byte Code;
	}
}