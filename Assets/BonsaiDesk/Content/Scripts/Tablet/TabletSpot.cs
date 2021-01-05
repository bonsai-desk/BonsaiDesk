using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class TabletSpot : NetworkBehaviour
{
    public static TabletSpot instance;

    public override void OnStartServer()
    {
        base.OnStartServer();
        instance = this;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        instance = this;
    }

    [Command(ignoreAuthority = true)]
    public void CmdSetNewVideo(NetworkIdentity tabletIdentity)
    {
        print(tabletIdentity.netId);
    }
}