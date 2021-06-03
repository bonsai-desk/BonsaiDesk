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
                CmdMsg("Failed BFS.");
                allGood = false;
            }
            
            if (blockObjects[i].SyncJoint.connected)
            {
                var badJoint = !blockObjects[i].SyncJoint.attachedTo.Value ||
                               !blockObjects[i].SyncJoint.attachedTo.Value.GetComponent<BlockObject>() || !blockObjects[i].Joint;
                if (badJoint)
                {
                    CmdMsg("Connected but bad joint.");
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

    private bool AllConnectedCheckGood(BlockObject blockObject)
    {
        var blockObjects = BreadthFirstSearch(blockObject);
        foreach (var foundBlockObject in blockObjects)
        {
            if (BreadthFirstSearch(foundBlockObject).Count != blockObjects.Count)
            {
                return false;
            }
        }
        return true;
    }

    private HashSet<BlockObject> BreadthFirstSearch(BlockObject blockObject)
    {
        var visited = new HashSet<BlockObject>();
        visited.Add(blockObject);

        var queue = new Queue<BlockObject>();
        queue.Enqueue(blockObject);

        while (queue.Any())
        {
            var next = queue.Dequeue();
            if (next.SyncJoint.attachedTo != null && next.SyncJoint.attachedTo.Value)
            {
                var nextAttachedToBlockObject = next.SyncJoint.attachedTo.Value.GetComponent<BlockObject>();
                if (!visited.Contains(nextAttachedToBlockObject))
                {
                    visited.Add(nextAttachedToBlockObject);
                    queue.Enqueue(nextAttachedToBlockObject);
                }
            }

            foreach (var pair in next.ConnectedToSelf)
            {
                if (pair.Value != null && pair.Value.Value)
                {
                    var nextConnectedBlockObject = pair.Value.Value.GetComponent<BlockObject>();
                    if (!visited.Contains(nextConnectedBlockObject))
                    {
                        visited.Add(nextConnectedBlockObject);
                        queue.Enqueue(nextConnectedBlockObject);
                    }
                }
            }
        }

        return visited;
    }
}