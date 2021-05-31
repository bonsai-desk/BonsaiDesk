using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class BlockObjectNullTest : NetworkBehaviour
{
    public MessageStack messageStack;

    [Command(ignoreAuthority = true)]
    private void CmdMsg(string msg)
    {
        RpcMsg(msg);
    }

    [ClientRpc]
    private void RpcMsg(string msg)
    {
        messageStack.AddMessage(msg);
    }

    public void Test()
    {
        var allGood = true;

        var blockObjects = GameObject.FindObjectsOfType<BlockObject>();

        for (int i = 0; i < blockObjects.Length; i++)
        {
            if (blockObjects[i].syncJoint.connected)
            {
                var badJoint = !blockObjects[i].syncJoint.attachedTo || !blockObjects[i].syncJoint.attachedTo.gameObject ||
                               !blockObjects[i].syncJoint.attachedTo.GetComponent<BlockObject>() || !blockObjects[i].Joint;
                if (badJoint)
                {
                    CmdMsg("Connected but bad joint.");
                    allGood = false;
                }
            }

            foreach (var pair in blockObjects[i].ConnectedToSelf)
            {
                var badConnection = !pair.Value || !pair.Value.gameObject || !pair.Value.GetComponent<BlockObject>() ||
                                    !pair.Value.GetComponent<BlockObject>().syncJoint.connected ||
                                    !pair.Value.GetComponent<BlockObject>().syncJoint.attachedTo ||
                                    !pair.Value.GetComponent<BlockObject>().syncJoint.attachedTo.gameObject ||
                                    !pair.Value.GetComponent<BlockObject>().syncJoint.attachedTo.GetComponent<BlockObject>();
                if (badConnection)
                {
                    CmdMsg("Bad joint connection.");
                    allGood = false;
                }
            }
        }

        if (allGood)
        {
            CmdMsg("All good");
        }
    }
}