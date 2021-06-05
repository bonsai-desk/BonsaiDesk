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
        print("CmdMsg: " + msg);
        RpcMsg(msg);
    }

    [ClientRpc]
    private void RpcMsg(string msg)
    {
        print("RpcMsg: " + msg);
        messageStack.AddMessage(msg);
    }

    public void TestAll()
    {
        var allGood = true;

        var blockObjects = GameObject.FindObjectsOfType<BlockObject>();

        for (int i = 0; i < blockObjects.Length; i++)
        {
            if (!AllConnectedCheckGood(blockObjects[i]))
            {
                var msg = "Failed BFS: " + blockObjects[i].name;
                Debug.LogError(msg);
                CmdMsg(msg);
                allGood = false;
            }

            if (blockObjects[i].SyncJoint.connected)
            {
                var badJoint = !blockObjects[i].SyncJoint.attachedTo.Value || !blockObjects[i].SyncJoint.attachedTo.Value.GetComponent<BlockObject>();
                if (badJoint)
                {
                    var msg = "Connected but bad joint: " + blockObjects[i].name;
                    Debug.LogError(msg);
                    CmdMsg(msg);
                    allGood = false;
                }

                var nullHingeJoint = !blockObjects[i].Joint;
                if (nullHingeJoint)
                {
                    var msg = "Hinge joint is null: " + blockObjects[i].name;
                    Debug.LogError(msg);
                    CmdMsg(msg);
                    allGood = false;
                }
            }

            foreach (var pair in blockObjects[i].ConnectedToSelf)
            {
                var badConnection = !pair.Value.Value || !pair.Value.Value.GetComponent<BlockObject>().SyncJoint.connected ||
                                    !pair.Value.Value.GetComponent<BlockObject>().SyncJoint.attachedTo.Value || !pair.Value.Value.GetComponent<BlockObject>()
                                        .SyncJoint.attachedTo.Value.GetComponent<BlockObject>();
                if (badConnection)
                {
                    var msg = "Bad joint connection: " + blockObjects[i].name;
                    Debug.LogError(msg);
                    CmdMsg(msg);
                    allGood = false;
                }
            }
        }

        if (allGood)
        {
            CmdMsg("All good");
        }
    }

    public static (bool syncConnectionProblem, bool hingeJointProblem) CheckBlockObjectForProblems(BlockObject blockObject)
    {
        var syncConnectionProblem = !AllConnectedCheckGood(blockObject);
        var hingeJointProblem = false;

        if (blockObject.SyncJoint.connected)
        {
            var badJoint = !blockObject.SyncJoint.attachedTo.Value || !blockObject.SyncJoint.attachedTo.Value.GetComponent<BlockObject>();
            if (badJoint)
            {
                syncConnectionProblem = true;
            }

            var nullHingeJoint = !blockObject.Joint;
            if (nullHingeJoint)
            {
                hingeJointProblem = true;
            }
        }

        foreach (var pair in blockObject.ConnectedToSelf)
        {
            var badConnection = !pair.Value.Value || !pair.Value.Value.GetComponent<BlockObject>().SyncJoint.connected ||
                                !pair.Value.Value.GetComponent<BlockObject>().SyncJoint.attachedTo.Value || !pair.Value.Value.GetComponent<BlockObject>()
                                    .SyncJoint.attachedTo.Value.GetComponent<BlockObject>();
            if (badConnection)
            {
                syncConnectionProblem = true;
            }
        }

        return (syncConnectionProblem, hingeJointProblem);
    }

    private static bool AllConnectedCheckGood(BlockObject blockObject)
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