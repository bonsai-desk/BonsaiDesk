using System.Collections;
using Mirror;
using UnityEngine;

/*
	Authenticators: https://mirror-networking.com/docs/Components/Authenticators/
	Documentation: https://mirror-networking.com/docs/Guides/Authentication.html
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkAuthenticator.html
*/

public class NetworkOpenAuthenticator : NetworkAuthenticator {
	#region Messages

	public struct AuthResponseMessage : NetworkMessage {
		public byte code;
	}

	#endregion

	#region Server

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
				code = 100
			};
			Debug.Log("[BONSAI] OnServerAuthenticate 100");
			conn.Send(authResponseMessage);
			ServerAccept(conn);
		}
		else {
			var authResponseMessage = new AuthResponseMessage {
				code = 200
			};
			Debug.Log("[BONSAI] OnServerAuthenticate 200");
			conn.isAuthenticated = false;
			ServerReject(conn);
		}
	}

	private IEnumerator DelayedDisconnect(NetworkConnection conn, float waitTime) {
		yield return new WaitForSeconds(waitTime);

		// Reject the unsuccessful authentication
		// The client should have disconnected by now
		// This will throw a warning 
		ServerReject(conn);
	}

	#endregion

	#region Client

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

	public void OnAuthResponseMessage(NetworkConnection conn, AuthResponseMessage msg) {
		// Invoke the event to complete a successful authentication
		if (msg.code == 100) {
			// Authentication has been accepted
			Debug.Log("[BONSAI] ClientAccept");
			ClientAccept(conn);
		}
		else if (msg.code == 200) {
			// Authentication has been rejected
			Debug.Log("[BONSAI] ClientReject");
			// TODO try both!
			//ClientReject(conn);
			NetworkManagerGame.Singleton.State = NetworkManagerGame.ConnectionState.Loading;
		}
	}

	#endregion
}