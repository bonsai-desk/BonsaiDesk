using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            if (!AllConnectedCheckGood(blockObjects[i]))
            {
                CmdMsg("Failed BFS: " + blockObjects[i].name);
                allGood = false;
            }
            
            if (blockObjects[i].SyncJoint.connected)
            {
                var badJoint = !blockObjects[i].SyncJoint.attachedTo.Value ||
                               !blockObjects[i].SyncJoint.attachedTo.Value.GetComponent<BlockObject>() || !blockObjects[i].Joint;
                if (badJoint)
                {
                    CmdMsg("Connected but bad joint: " + blockObjects[i].name);
                    allGood = false;
                }
            }

            foreach (var pair in blockObjects[i].ConnectedToSelf)
            {
                var badConnection = !pair.Value.Value ||
                                    !pair.Value.Value.GetComponent<BlockObject>().SyncJoint.connected ||
                                    !pair.Value.Value.GetComponent<BlockObject>().SyncJoint.attachedTo.Value ||
                                    !pair.Value.Value.GetComponent<BlockObject>().SyncJoint.attachedTo.Value.GetComponent<BlockObject>();
                if (badConnection)
                {
                    CmdMsg("Bad joint connection: " + blockObjects[i].name);
                    allGood = false;
                }
            }
        }

        if (allGood)
        {
            CmdMsg("All good");
        }
    }

    private bool AllConnectedCheckGood(BlockObject blockObject)
    {
        var blockObjects = BlockUtility.BreadthFirstSearch(blockObject);
        foreach (var foundBlockObject in blockObjects)
        {
            if (BlockUtility.BreadthFirstSearch(foundBlockObject).Count != blockObjects.Count)
            {
                return false;
            }
        }
        return true;
    }

    
}