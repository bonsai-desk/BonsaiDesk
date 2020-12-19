using Mirror;
using NobleConnect.Mirror;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManagerGame : NobleNetworkManager
{
    #region properties

    public static new NetworkManagerGame singleton;

    public Dictionary<NetworkConnection, PlayerInfo> playerInfo = new Dictionary<NetworkConnection, PlayerInfo>();

    public enum VideoState
    {
        none,
        cued,
        playing
    }

    public VideoState videoState = VideoState.none;

    [System.Serializable]
    public class PlayerInfo
    {
        public int spot;
        public int youtubePlayerState;
        public float youtubePlayerCurrentTime;

        public PlayerInfo(int spot)
        {
            this.spot = spot;
            youtubePlayerState = -1;
            youtubePlayerCurrentTime = 0;
        }
    }

    public void ResetPlayerInfoTime()
    {
        foreach (var player in playerInfo)
        {
            player.Value.youtubePlayerCurrentTime = 0;
        }
    }

    private bool[] spotInUse = new bool[2];

    public static int colorIndex = 0;

    #endregion properties

    #region overrides

    public override void Awake()
    {
        base.Awake();

        if (singleton == null)
            singleton = this;
    }

    public override void Start()
    {
        base.Start();

        for (int i = 0; i < spotInUse.Length; i++)
            spotInUse[i] = false;
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        Debug.Log("[BONSAI] OnClientConnect");
        base.OnClientConnect(conn);

        NetworkClient.RegisterHandler<SpotMessage>(OnSpot);
        NetworkClient.RegisterHandler<ActionMessage>(OnAction);
    }

    public override void OnServerConnect(NetworkConnection conn)
    {
        Debug.Log("[BONSAI] OnServerConnect");
        base.OnServerConnect(conn);
        int openSpotId = -1;
        for (int i = 0; i < spotInUse.Length; i++)
        {
            if (!spotInUse[i])
            {
                openSpotId = i;
                break;
            }
        }

        if (openSpotId == -1)
        {
            Debug.LogError("No open spot.");
            openSpotId = 0;
        }
        spotInUse[openSpotId] = true;
        playerInfo.Add(conn, new PlayerInfo(openSpotId));
    }

    public override void OnServerDisconnect(NetworkConnection conn)
    {
        Debug.Log("[BONSAI] OnServerDisconnect");
        base.OnServerDisconnect(conn);
        int spotId = playerInfo[conn].spot;

        int spotUsedCount = 0;
        foreach (var player in playerInfo)
        {
            if (player.Value.spot == spotId)
                spotUsedCount++;
        }
        if (spotUsedCount <= 1)
        {
            spotInUse[spotId] = false;
        }
        playerInfo.Remove(conn);

        HashSet<NetworkIdentity> tmp = new HashSet<NetworkIdentity>(conn.clientOwnedObjects);
        foreach (NetworkIdentity netIdentity in tmp)
        {
            if (netIdentity != null && (netIdentity.gameObject.CompareTag("KeepOnDisconnect") || netIdentity.gameObject.CompareTag("BlockArea")))
            {
                netIdentity.RemoveClientAuthority();
            }
        }

        NetworkServer.DestroyPlayerForConnection(conn);
    }

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        Debug.Log("[BONSAI] OnServerAddPlayer");
        conn.Send(new SpotMessage()
        {
            spotId = playerInfo[conn].spot,
            colorIndex = playerInfo[conn].spot
        });

        base.OnServerAddPlayer(conn);
    }

    #endregion overrides

    #region spot messages

    public class SpotMessage : NetworkMessage
    {
        public int spotId;
        public int colorIndex;
    }

    public class ActionMessage : NetworkMessage
    {
        public int actionId;
    }

    private void OnSpot(NetworkConnection conn, SpotMessage msg)
    {
        if (msg.spotId == 0)
            GameObject.Find("GameManager").GetComponent<MoveToDesk>().SetTableEdge(GameObject.Find("DefaultEdge").transform);
        if (msg.spotId == 1)
            GameObject.Find("GameManager").GetComponent<MoveToDesk>().SetTableEdge(GameObject.Find("AcrossEdge").transform);

        colorIndex = msg.colorIndex;
    }

    private void OnAction(NetworkConnection conn, ActionMessage msg)
    {
        switch (msg.actionId)
        {
            case 0: //play video
                BrowserManager.instance.StartVideo();
                break;

            case 1: //resume video
                BrowserManager.instance.ResumeVideo();
                break;

            case 2: //pause video
                BrowserManager.instance.PauseVideo();
                break;

            default:
                break;
        }
    }

    #endregion spot messages
}