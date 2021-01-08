using Mirror;
using Smooth;
using System.Collections.Generic;
using UnityEngine;

public class ResetPositionFall : MonoBehaviour
{
    private NetworkIdentity networkIdentity;

    private BlockArea blockArea;
    private Rigidbody body;

    // Start is called before the first frame update
    private void Start()
    {
        networkIdentity = GetComponent<NetworkIdentity>();
        blockArea = GetComponent<BlockArea>();
        body = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        if (!(networkIdentity.isServer && networkIdentity.connectionToClient == null || networkIdentity.hasAuthority))
            return;

        if (body.worldCenterOfMass.y < -1f)
        {
            if (blockArea.blocks.Count > 0 || blockArea.coordConnectedToBearing != null)
            {
                Queue<BlockArea> connectedBlockAreas = new Queue<BlockArea>();
                Stack<BlockArea> blockAreasToCheck = new Stack<BlockArea>();

                BlockArea rootBlockArea = blockArea;
                while (rootBlockArea.coordConnectedToBearing != null)
                {
                    Joint joint = rootBlockArea.GetComponent<Joint>();
                    if (joint != null)
                    {
                        rootBlockArea = joint.connectedBody.GetComponent<BlockArea>();
                    }
                    else
                    {
                        Debug.LogError("No joint even though coordConnectedToBearing is not null");
                        rootBlockArea.coordConnectedToBearing = null;
                        break;
                    }
                }
                blockAreasToCheck.Push(rootBlockArea);
                while (blockAreasToCheck.Count > 0)
                {
                    BlockArea check = blockAreasToCheck.Pop();
                    connectedBlockAreas.Enqueue(check);
                    foreach (var block in check.blockObjects)
                    {
                        if (Blocks.blocks[check.blocks[block].id].blockType == Block.BlockType.bearing)
                        {
                            if (check.blocks[block].connected != null)
                            {
                                blockAreasToCheck.Push(check.blocks[block].connected.GetComponent<BlockArea>());
                            }
                        }
                    }
                }
                Vector3 CoM = Vector3.zero;
                float c = 0f;
                foreach (var area in connectedBlockAreas)
                {
                    CoM += area.body.worldCenterOfMass * area.body.mass;
                    c += area.body.mass;
                }
                CoM /= c;

                Vector3 upperBounds = CoM;
                Vector3 lowerBounds = CoM;
                foreach (var area in connectedBlockAreas)
                {
                    foreach (var block in area.blocks)
                    {
                        Vector3 blockPosition = area.transform.TransformPoint((Vector3)block.Key * BlockArea.cubeScale);
                        for (int i = 0; i < 3; i++)
                        {
                            if (blockPosition[i] > upperBounds[i])
                                upperBounds[i] = blockPosition[i];
                            if (blockPosition[i] < lowerBounds[i])
                                lowerBounds[i] = blockPosition[i];
                        }
                    }
                }

                float padding = (BlockArea.cubeScale / 2f) * 1.41421f;

                upperBounds -= CoM;
                lowerBounds -= CoM;

                for (int i = 0; i < 3; i++)
                {
                    upperBounds[i] += padding;
                    lowerBounds[i] -= padding;
                }

                Vector3 targetPosition = new Vector3(0, 0, 0.375f);
                float additionalHeight = 0;
                Vector3 boxPosition = targetPosition + new Vector3(0, upperBounds.y + additionalHeight + 0.005f, 0);
                Vector3 boxHalfExtends = new Vector3((upperBounds.x - lowerBounds.x) / 2f, (upperBounds.y - lowerBounds.y) / 2f, (upperBounds.z - lowerBounds.z) / 2f);

                while (Physics.CheckBox(boxPosition, boxHalfExtends, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                {
                    additionalHeight += BlockArea.cubeScale;
                    boxPosition.y += BlockArea.cubeScale;
                }

                targetPosition.y += additionalHeight + (-lowerBounds.y);
                Vector3 offset = CoM - targetPosition;

                while (connectedBlockAreas.Count > 0)
                {
                    BlockArea area = connectedBlockAreas.Dequeue();
                    area.body.velocity = Vector3.zero;
                    area.body.angularVelocity = Vector3.zero;
                    area.body.MovePosition(area.transform.position - offset);
                    SmoothSyncMirror smooth = GetComponent<SmoothSyncMirror>();
                    if (smooth != null)
                        smooth.teleport();
                }
            }
            else
            {
                Debug.LogError("Block area with no blocks deleted");
                Destroy(gameObject);
            }
        }
    }
}