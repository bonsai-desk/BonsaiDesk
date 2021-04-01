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
        BonsaiLog("OnStartServer");
	}

	/// <summary>
	///     Called on server from OnServerAuthenticateInternal when a client needs to authenticate
	/// </summary>
	/// <param name="conn">Connection to client.</param>
	public override void OnServerAuthenticate(NetworkConnection conn) {
		if (NetworkManagerGame.Singleton.roomOpen ||
		    conn.connectionId == NetworkServer.localConnection.connectionId) {
			var authResponseMessage = new AuthResponseMessage {
				Code = 100
			};
            BonsaiLog("Server Accept");
			conn.Send(authResponseMessage);
			ServerAccept(conn);
		}
		else {
			var authResponseMessage = new AuthResponseMessage {
				Code = 200
			};
            BonsaiLog("Server Reject");
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
        BonsaiLog("OnStartClient");
		// register a handler for the authentication response we expect from server
		NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponseMessage, false);
	}

	/// <summary>
	///     Called on client from OnClientAuthenticateInternal when a client needs to authenticate
	/// </summary>
	/// <param name="conn">Connection of the client.</param>
	public override void OnClientAuthenticate(NetworkConnection conn) {
        BonsaiLog("OnClientAuthenticate");
		// do nothing just wait for AuthMessageResponse
	}

	private void OnAuthResponseMessage(NetworkConnection conn, AuthResponseMessage msg) {
		switch (msg.Code) {
			// Invoke the event to complete a successful authentication
			case 100:
                BonsaiLog("Authenticator Accepted");
				ClientAccept(conn);
				break;
			case 200:
                BonsaiLog("Authenticator Rejected");
				ClientReject(conn);
				break;
		}
	}

	private struct AuthResponseMessage : NetworkMessage {
		// 100 : good
		// 200 : reject
		public byte Code;
	}
    
    private void BonsaiLog(string msg)
    {
        Debug.Log("<color=orange>BonsaiAuth: </color>: " + msg);
    }
    private void BonsaiLogWarning(string msg)
    {
        Debug.LogWarning("<color=orange>BonsaiAuth: </color>: " + msg);
    }
    private void BonsaiLogError(string msg)
    {
        Debug.LogError("<color=orange>BonsaiAuth: </color>: " + msg);
    }
}