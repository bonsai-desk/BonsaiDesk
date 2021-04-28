using Mirror;
using UnityEngine;

/*
	Authenticators: https://mirror-networking.com/docs/Components/Authenticators/
	Documentation: https://mirror-networking.com/docs/Guides/Authentication.html
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkAuthenticator.html
*/

public class NetworkOpenAuthenticator : NetworkAuthenticator
{
    /// <summary>
    ///     Called on server from StartServer to initialize the Authenticator
    ///     <para>Server message handlers should be registered in this method.</para>
    /// </summary>
    public override void OnStartServer()
    {
        NetworkServer.RegisterHandler<AuthRequestMessage>(OnAuthRequestMessage, false);
    }

    private void SendSuccess(NetworkConnection conn)
    {
        var authResponseMessage = new AuthResponseMessage
        {
            Code = 100,
            Message = "Success"
        };
        conn.Send(authResponseMessage);
        ServerAccept(conn);
    }

    private void SendReject(NetworkConnection conn, string message)
    {
        var authResponseMessage = new AuthResponseMessage
        {
            Code = 200,
            Message = message
        };
        BonsaiLog($"Server Reject: {message}");
        conn.Send(authResponseMessage);
        conn.isAuthenticated = false;
        ServerReject(conn);
    }

    private void OnAuthRequestMessage(NetworkConnection conn, AuthRequestMessage msg)
    {
        var roomOpen = NetworkManagerGame.Singleton.roomOpen;

        var hostVersion = NetworkManagerGame.Singleton.FullVersion;
        var clientVersion = msg.version;

        if (conn.connectionId == NetworkServer.localConnection.connectionId)
        {
            SendSuccess(conn);
            return;
        }

        if (!roomOpen)
        {
            SendReject(conn, "Room is closed");
        }
        else if (clientVersion != hostVersion)
        {
            SendReject(conn, $"Client version ({clientVersion}) does not match host ({hostVersion})");
        }
        else
        {
            SendSuccess(conn);
        }
    }

    /// <summary>
    ///     Called on server from OnServerAuthenticateInternal when a client needs to authenticate
    /// </summary>
    /// <param name="conn">Connection to client.</param>
    public override void OnServerAuthenticate(NetworkConnection conn)
    {
        // do nothing...wait for AuthRequestMessage from client
    }

    /// <summary>
    ///     Called on client from StartClient to initialize the Authenticator
    ///     <para>Client message handlers should be registered in this method.</para>
    /// </summary>
    public override void OnStartClient()
    {
        NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponseMessage, false);
    }

    /// <summary>
    ///     Called on client from OnClientAuthenticateInternal when a client needs to authenticate
    /// </summary>
    /// <param name="conn">Connection of the client.</param>
    public override void OnClientAuthenticate(NetworkConnection conn)
    {
        var authRequestMessage = new AuthRequestMessage
        {
            version = NetworkManagerGame.Singleton.FullVersion
        };
        conn.Send(authRequestMessage);
    }

    private void OnAuthResponseMessage(NetworkConnection conn, AuthResponseMessage msg)
    {
        switch (msg.Code)
        {
            // Invoke the event to complete a successful authentication
            case 100:
                ClientAccept(conn);
                break;
            case 200:
                BonsaiLog($"Authenticator Rejected: {msg.Message}");
                ClientReject(conn);
                break;
        }
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

    private struct AuthRequestMessage : NetworkMessage
    {
        public string version;
    }

    private struct AuthResponseMessage : NetworkMessage
    {
        // 100 : good
        // 200 : reject
        public byte Code;
        public string Message;
    }
}