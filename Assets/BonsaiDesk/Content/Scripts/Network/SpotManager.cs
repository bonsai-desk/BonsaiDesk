using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class SpotManager : NetworkBehaviour
{
    public enum Layout
    {
        Solo = 0,
        Opposite = 1,
        Side = 2
    }

    public static SpotManager Instance;

    public AutoBrowser autoBrowser;

    public Transform autoBrowserPositions;
        
    public Transform playerSpotSceneObjects;

    [SyncVar(hook = nameof(OnLayoutChange))]
    public Layout layout;

    public ColorInfo[] colorInfo;

    private Transform[][] playerSpots;

    public int TotalSpots => playerSpots.Length;

    private void Awake()
    {
        Instance = this;
        
        playerSpots = new Transform[playerSpotSceneObjects.GetChild(0).childCount][];
        for (var i = 0; i < playerSpots.Length; i++)
        {
            playerSpots[i] = new Transform[playerSpotSceneObjects.childCount];
        }

        for (var i = 0; i < playerSpotSceneObjects.childCount; i++)
        {
            for (var j = 0; j < playerSpotSceneObjects.GetChild(i).childCount; j++)
            {
                playerSpots[j][i] = playerSpotSceneObjects.GetChild(i).GetChild(j);
            }
        }
    }

    private void Start()
    {
        NetworkManagerGame.Singleton.ServerAddPlayer += HandleServerAddPlayer;
        NetworkManagerGame.ServerDisconnect += HandleServerDisconnect;

    }

    public override void OnStartServer()
    {
        layout = Layout.Solo;
    }

    public override void OnStartClient()
    {
        TableBrowserMenu.Singleton.LayoutChange -= HandleLayoutChange;
        TableBrowserMenu.Singleton.LayoutChange += HandleLayoutChange;
    }

    private void HandleLayoutChange(object sender, Layout newLayout)
    {
        CmdSetLayout(newLayout);
    }

    [Command(ignoreAuthority = true)]
    public void CmdSetLayout(Layout newLayout)
    {
        if (isServer && !isClient)
        {
            OnLayoutChange(layout, newLayout);
        }
        layout = newLayout;
    }

    public void OnLayoutChange(Layout oldLayout, Layout newLayout)
    {
        autoBrowser.transform.parent.position = autoBrowserPositions.GetChild((int) newLayout).position;
        autoBrowser.transform.parent.rotation = autoBrowserPositions.GetChild((int) newLayout).rotation;
        autoBrowser.screenRigidBody.MovePosition(autoBrowser.transform.parent.position);
        autoBrowser.screenRigidBody.MoveRotation(autoBrowser.transform.parent.rotation);
        if (NetworkVRPlayer.localPlayer != null)
        {
            NetworkVRPlayer.localPlayer.LayoutChange(newLayout);
        }
    }

    private void HandleServerDisconnect(object sender, NetworkConnection e)
    {
        if (NetworkManagerGame.Singleton.PlayerInfos.Count == 2)
        {
            HandleLayoutChange(this, Layout.Solo);
        }
    }

    private void HandleServerAddPlayer(NetworkConnection conn, bool islanonly)
    {
        if (NetworkManagerGame.Singleton.PlayerInfos.Count == 1)
        {
            HandleLayoutChange(this, Layout.Solo);
        }
        else
        {
            HandleLayoutChange(this, Layout.Opposite);
        }
    }

    [Server]
    public void SetLayout(Layout layout)
    {
        this.layout = layout;
    }

    public ColorInfo GetColorInfo(int spotId)
    {
        return colorInfo[spotId];
    }

    public Transform GetSpotTransform(int spotId, Layout targetLayout)
    {
        return playerSpots[spotId][(int) targetLayout];
    }

    public Transform GetSpotTransform(int spotId)
    {
        return GetSpotTransform(spotId, layout);
    }

    private void BonsaiLog(string msg)
    {
        Debug.Log("<color=orange>BonsaiSpot: </color>: " + msg);
    }

    private void BonsaiLogWarning(string msg)
    {
        Debug.LogWarning("<color=orange>BonsaiSpot: </color>: " + msg);
    }

    private void BonsaiLogError(string msg)
    {
        Debug.LogError("<color=orange>BonsaiSpot: </color>: " + msg);
    }

    [Serializable]
    public struct ColorInfo
    {
        public Texture handTexture;
        public Texture headTexture;
    }

    public struct LocationInfo
    {
        public Texture handTexture;
        public Texture headTexture;
    }
}