using System.Collections;
using System.Collections.Generic;
using Mirror;
using Smooth;
using UnityEngine;

public partial class BlockObject
{
    private Queue<float> _resetTimes = new Queue<float>();

    private float _clientOnlyLastCmdResetTime;

    private void CheckTeleport()
    {
        //if this is not the root object, return. The root object will loop through and check its children
        if (BlockUtility.GetRootBlockObject(this) != this)
        {
            return;
        }

        var blockObjects = BlockUtility.GetBlockObjectsFromRoot(this);

        var tookAction = false;

        if (isServer)
        {
            tookAction = ServerCheckIfInvalidTransform(blockObjects);
        }

        if (!tookAction && _autoAuthority.HasAuthority())
        {
            tookAction = CheckForInvalidJointConfiguration(blockObjects);
        }

        if (!tookAction && isServer)
        {
            ServerCheckIfBelowOrFar(blockObjects);
        }
    }

    private bool CheckForInvalidJointConfiguration(List<BlockObject> blockObjects)
    {
        for (int i = 0; i < blockObjects.Count; i++)
        {
            if (InvalidJointConfiguration(blockObjects[i]))
            {
                if (isClient && !isServer)
                {
                    if (Time.time - _clientOnlyLastCmdResetTime > 1f)
                    {
                        CmdFixInvalidJoints();
                        _clientOnlyLastCmdResetTime = Time.time;
                    }
                }
                else if (isServer)
                {
                    CmdFixInvalidJoints();
                }

                return true;
            }
        }

        return false;
    }

    [Server]
    private bool ServerCheckIfInvalidTransform(List<BlockObject> blockObjects)
    {
        for (int i = 0; i < blockObjects.Count; i++)
        {
            if (PhysicsHandController.InvalidTransform(blockObjects[i].transform))
            {
                Debug.LogWarning("BlockObject has invalid transform!");
                if (blockObjects.Count == 1 && blockObjects[0].Blocks.Count <= 4)
                {
                    _autoAuthority.CmdDestroy();
                }
                else
                {
                    if (ServerResetToValidOrientation(this))
                    {
                        ServerTeleportToDeskSurface(blockObjects);
                    }
                }

                return true;
            }
        }

        return false;
    }

    [Server]
    private bool ServerCheckIfBelowOrFar(List<BlockObject> blockObjects)
    {
        if (blockObjects.Count == 1 && blockObjects[0].Blocks.Count <= 4)
        {
            if (blockObjects[0].transform.position.y < -1f || Vector3.SqrMagnitude(blockObjects[0].transform.position) > 25f * 25f ||
                blockObjects[0].transform.position.y > 10f)
            {
                _autoAuthority.ServerStripOwnerAndDestroy();
                return true;
            }

            return false;
        }

        if (!BelowHeightOrFar(blockObjects))
        {
            return false;
        }

        ServerTeleportToDeskSurface(blockObjects);
        return true;
    }

    [Server]
    private void ServerTeleportToDeskSurface(List<BlockObject> blockObjects = null)
    {
        if (blockObjects == null)
        {
            blockObjects = BlockUtility.GetBlockObjectsFromRoot(this);
        }

        var upperBounds = Vector3.zero;
        var lowerBounds = Vector3.zero;
        var first = true;

        for (int i = 0; i < blockObjects.Count; i++)
        {
            foreach (var pair in blockObjects[i].Blocks)
            {
                var blockPosition = blockObjects[i].transform.TransformPoint(pair.Key);
                if (first)
                {
                    first = false;
                    upperBounds = blockPosition;
                    lowerBounds = blockPosition;
                }
                else
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (blockPosition[j] > upperBounds[j])
                            upperBounds[j] = blockPosition[j];
                        if (blockPosition[j] < lowerBounds[j])
                            lowerBounds[j] = blockPosition[j];
                    }
                }
            }
        }

        //in the worst case where the block is rotated 45 degrees, its side sticks out ((root 2) / 2) units, so lets use this for the padding
        const float diagonal = 1.41421f / 2f;
        const float paddingFloat = diagonal * 2f * CubeScale; //times 2f since the bounds is on two sides
        var padding = new Vector3(paddingFloat, paddingFloat, paddingFloat);

        var boundsCenter = (upperBounds + lowerBounds) / 2f;
        var boundsSize = upperBounds - lowerBounds;
        boundsSize += padding;
        var halfExtends = boundsSize / 2f;

        const float tableSurfaceHeight = 0.726f;

        var targetPosition = new Vector3(0, tableSurfaceHeight + boundsSize.y / 2f, 0);
        while (Physics.CheckBox(targetPosition, halfExtends, Quaternion.identity, BlockUtility.DefaultLayerMask))
        {
            targetPosition.y += 0.025f;
        }

        ServerTeleportToPosition(blockObjects, targetPosition, boundsCenter);
    }

    [Server]
    private void ServerTeleportToPosition(List<BlockObject> blockObjects, Vector3 position, Vector3 boundsCenter)
    {
        var offset = position - boundsCenter;
        for (int i = 0; i < blockObjects.Count; i++)
        {
            blockObjects[i]._autoAuthority.ServerForceNewOwner(uint.MaxValue, NetworkTime.time, false);
            var smoothSync = blockObjects[i].GetComponent<SmoothSyncMirror>();
            smoothSync.clearBuffer();
            blockObjects[i]._body.velocity = Vector3.zero;
            blockObjects[i]._body.angularVelocity = Vector3.zero;

            var newPosition = blockObjects[i].transform.position + offset;
            var newRotation = blockObjects[i].transform.rotation;
            var newScale = blockObjects[i].transform.localScale;

            blockObjects[i].transform.position = newPosition;
            blockObjects[i]._body.MovePosition(newPosition);
            blockObjects[i]._body.MoveRotation(newRotation);

            smoothSync.teleportAnyObjectFromServer(newPosition, newRotation, newScale);
        }

        RpcTeleportEvent();
    }

    [ClientRpc]
    private void RpcTeleportEvent()
    {
        var leftPinchPull = InputManager.Hands.Left.PlayerHand.GetIHandTick<PinchPullHand>();
        if (leftPinchPull.ConnectedBlockObjectRoot() == this)
        {
            leftPinchPull.DetachObject();
        }

        var rightPinchPull = InputManager.Hands.Right.PlayerHand.GetIHandTick<PinchPullHand>();
        if (rightPinchPull.ConnectedBlockObjectRoot() == this)
        {
            rightPinchPull.DetachObject();
        }

        var rightLockObject = InputManager.Hands.Right.PlayerHand.GetIHandTick<LockObjectHand>();
        if (rightLockObject.ConnectedBlockObjectRoot() == this)
        {
            rightLockObject.DetachObject();
        }

        var leftLockObject = InputManager.Hands.Left.PlayerHand.GetIHandTick<LockObjectHand>();
        if (leftLockObject.ConnectedBlockObjectRoot() == this)
        {
            leftLockObject.DetachObject();
        }
    }

    private static bool BelowHeightOrFar(List<BlockObject> blockObjects)
    {
        const float diagonal = 1.41421f / 2f;
        const float teleportBelowHeight = -diagonal * CubeScale;

        var highest = blockObjects[0];
        for (int i = 1; i < blockObjects.Count; i++)
        {
            if (blockObjects[i]._body.worldCenterOfMass.y > highest._body.worldCenterOfMass.y)
            {
                highest = blockObjects[i];
            }

            if (Vector3.SqrMagnitude(blockObjects[i].transform.position) > 25f * 25f || blockObjects[i].transform.position.y > 10f)
            {
                return true;
            }
        }

        if (highest._body.worldCenterOfMass.y > teleportBelowHeight)
        {
            return false;
        }

        float highestBlock = float.NegativeInfinity;
        foreach (var pair in highest.Blocks)
        {
            var blockHeight = highest.transform.TransformPoint(pair.Key).y;
            if (blockHeight > highestBlock)
            {
                highestBlock = blockHeight;
            }
        }

        if (highestBlock > teleportBelowHeight)
        {
            return false;
        }

        return true;
    }

    private static bool InvalidJointConfiguration(BlockObject blockObject)
    {
        if (!blockObject.SyncJoint.connected)
        {
            return false;
        }

        if (blockObject.SyncJoint.attachedTo == null || !blockObject.SyncJoint.attachedTo.Value)
        {
            Debug.LogError("Joint connected but attachedTo is null.");
            return true;
        }

        var targetPosition = blockObject.SyncJoint.attachedTo.Value.transform.TransformPoint(blockObject.SyncJoint.otherBearingCoord);
        var currentPosition = blockObject.transform.TransformPoint(blockObject.SyncJoint.attachedToMeAtCoord);
        var distanceSquared = Vector3.SqrMagnitude(targetPosition - currentPosition);

        var invalid = distanceSquared > 0.045f * 0.045f;
        return invalid;
    }

    [Command(ignoreAuthority = true)]
    private void CmdFixInvalidJoints()
    {
        if (ServerResetToValidOrientation(this))
        {
            ServerTeleportToDeskSurface();
        }
    }

    [Server]
    private bool ServerResetToValidOrientation(BlockObject rootBlockObject)
    {
        _resetTimes.Enqueue(Time.time);

        while (Time.time - _resetTimes.Peek() > 2f)
        {
            _resetTimes.Dequeue();
        }

        if (_resetTimes.Count > 3)
        {
            _autoAuthority.CmdDestroy();
            return false;
        }

        var toUpdate = new Queue<BlockObject>();
        toUpdate.Enqueue(rootBlockObject);
        
        while (toUpdate.Count > 0)
        {
            var next = toUpdate.Dequeue();

            next._autoAuthority.ServerForceNewOwner(uint.MaxValue, NetworkTime.time, false);
            var smoothSync = next.GetComponent<SmoothSyncMirror>();
            smoothSync.clearBuffer();

            if (next == rootBlockObject && PhysicsHandController.InvalidTransform(next.transform))
            {
                next.transform.position = new Vector3(0, 5, 0);
                next.transform.rotation = Quaternion.identity;
                next._body.MovePosition(next.transform.position);
                next._body.MoveRotation(next.transform.rotation);
            }

            if (next.SyncJoint.attachedTo != null && next.SyncJoint.attachedTo.Value)
            {
                next.transform.position = next.SyncJoint.attachedTo.Value.transform.TransformPoint(next._validLocalPosition);
                next.transform.rotation = next.SyncJoint.attachedTo.Value.transform.rotation * next._validLocalRotation;
                next._body.MovePosition(next.transform.position);
                next._body.MoveRotation(next.transform.rotation);
            }

            foreach (var pair in next.ConnectedToSelf)
            {
                if (pair.Value != null && pair.Value.Value)
                {
                    var childBlockObject = pair.Value.Value.GetComponent<BlockObject>();
                    toUpdate.Enqueue(childBlockObject);
                }
            }
        }

        return true;
    }
}