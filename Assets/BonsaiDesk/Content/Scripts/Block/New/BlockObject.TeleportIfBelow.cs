using System.Collections;
using System.Collections.Generic;
using Mirror;
using Smooth;
using UnityEngine;

public partial class BlockObject
{
    private void TeleportIfBelow()
    {
        //if this is not the root object, return. The root object will loop through and check its children
        if (BlockUtility.GetRootBlockObject(this) != this)
        {
            return;
        }

        var blockObjects = BlockUtility.GetBlockObjectsFromRoot(this);

        for (int i = 0; i < blockObjects.Count; i++)
        {
            if (PhysicsHandController.InvalidTransform(blockObjects[i].transform))
            {
                Debug.LogError("BlockObject has invalid transform!");
                return;
            }
        }

        if (!BelowHeightOrFar(blockObjects))
        {
            return;
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

        TeleportToPosition(blockObjects, targetPosition, boundsCenter);
    }

    private static void TeleportToPosition(List<BlockObject> blockObjects, Vector3 position, Vector3 boundsCenter)
    {
        var offset = position - boundsCenter;
        for (int i = 0; i < blockObjects.Count; i++)
        {
            blockObjects[i]._autoAuthority.ServerForceNewOwner(uint.MaxValue, NetworkTime.time, false);
            var smoothSync = blockObjects[i].GetComponent<SmoothSyncMirror>();
            smoothSync.clearBuffer();
            blockObjects[i]._body.velocity = Vector3.zero;
            blockObjects[i]._body.angularVelocity = Vector3.zero;

            smoothSync.teleportAnyObjectFromServer(blockObjects[i].transform.position + offset, blockObjects[i].transform.rotation,
                blockObjects[i].transform.localScale);
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
}