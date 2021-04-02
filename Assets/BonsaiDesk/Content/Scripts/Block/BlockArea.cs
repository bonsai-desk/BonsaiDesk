using Mirror;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct NetworkBlockInfo
{
    public byte id;
    public byte rotation;
}

[System.Serializable]
public class SyncDictionaryVector3IntNetworkBlockInfo : SyncDictionary<Vector3Int, NetworkBlockInfo>
{
}

public class BlockArea : NetworkBehaviour
{
    public static float cubeScale = 0.05f;

    public static HashSet<NetworkIdentity> blockAreaIdentities = new HashSet<NetworkIdentity>();

    public readonly SyncDictionaryVector3IntNetworkBlockInfo networkBlocks = new SyncDictionaryVector3IntNetworkBlockInfo();

    // [SyncVar] public int startBlockId = 0;
    [SyncVar] public bool active = true;

    public GameObject blockBreakPrefab;

    public OVR.SoundFXRef breakSound;
    public OVR.SoundFXRef breakingSound;

    // public OVR.SoundFXRef lockInPlaceSound;
    public OVR.SoundFXRef bearingAttachSound;

    [HideInInspector]
    public Dictionary<Vector3Int, MeshBlock> startBlocks = null;

    [HideInInspector]
    public Dictionary<Vector3Int, Joint> startBearingJoints = null;

    //private Joint startJoint = null;

    [HideInInspector]
    public Rigidbody body;

    //private Vector3 startPosition;

    private BlockPhysics blockPhysics;

    private float health = 1f;
    private int framesSinceLastDamage = 0;
    private bool damagedThisFrame = false;
    private int emitterId = -1;

    public PhysicMaterial blockPhysicMaterial;
    public PhysicMaterial spherePhysicMaterial;

    [HideInInspector]
    public Vector2Int[] bounds = {new Vector2Int(), new Vector2Int(), new Vector2Int()};

    public Dictionary<Vector3Int, MeshBlock> blocks = new Dictionary<Vector3Int, MeshBlock>();
    public Dictionary<Vector3Int, BlockBreak> damagedBlocks = new Dictionary<Vector3Int, BlockBreak>();
    public HashSet<Vector3Int> blockObjects = new HashSet<Vector3Int>();
    private Queue<BoxCollider> boxCollidersInUse = new Queue<BoxCollider>();

    public class MeshBlock
    {
        public int id;
        public byte forward;
        public byte up;
        public int positionInList;
        public bool damagedThisFrame;
        public int framesSinceLastDamage;
        public GameObject blockObject;
        public MeshRenderer meshRenderer;
        public Joint connected;

        public MeshBlock(int id, int positionInList, byte forward, byte up, GameObject blockObject, MeshRenderer meshRenderer, Joint connected)
        {
            this.id = id;
            this.positionInList = positionInList;
            this.damagedThisFrame = false;
            this.framesSinceLastDamage = 0;
            this.forward = forward;
            this.up = up;
            this.blockObject = blockObject;
            this.meshRenderer = meshRenderer;
            this.connected = connected;
        }
    }

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;
    private List<Vector3> vertices = new List<Vector3>();
    private List<Vector2> uv = new List<Vector2>();
    private List<Vector2> uv2 = new List<Vector2>();
    private List<int> triangles = new List<int>();

    private static float texturePadding = 0;

    private bool resetCoM = false;

    [HideInInspector]
    public Vector3Int? coordConnectedToBearing = null;

    private bool destroyJoints = true;

    private int wakeUp = 0;

    private void Start()
    {
        if (!blockAreaIdentities.Contains(GetComponent<NetworkIdentity>()))
            blockAreaIdentities.Add(GetComponent<NetworkIdentity>());
        // networkBlocks.Callback += OnNetworkBlocksChange;

        transform.name = Random.value.ToString();

        if (transform.childCount != 6)
            Debug.LogError("CubeArea object should have 6 child");

        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).localPosition = Vector3.zero;
            transform.GetChild(i).localRotation = Quaternion.identity;
            transform.GetChild(i).localScale = new Vector3(cubeScale, cubeScale, cubeScale);
        }

        transform.GetChild(5).gameObject.SetActive(false);

        meshFilter = transform.GetChild(0).GetComponent<MeshFilter>();
        meshRenderer = transform.GetChild(0).GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = Instantiate(meshRenderer.sharedMaterial);
        //meshCollider = transform.GetChild(0).GetComponent<MeshCollider>();
        mesh = new Mesh();
        meshFilter.sharedMesh = mesh;
        //meshCollider.sharedMesh = mesh;

        texturePadding = 1f / meshRenderer.sharedMaterial.mainTexture.width / 2f;

        body = GetComponent<Rigidbody>();
        //startPosition = transform.position;

        blockPhysics = GetComponent<BlockPhysics>();

        // reset();

        // if (startBlocks != null)
        // {
        if (startBlocks != null && false) //dissabled start blocks and start joints
        {
            foreach (var block in startBlocks)
            {
                CreateBlock(block.Value.id, block.Key, false, block.Value.forward, block.Value.up);
            }

            foreach (var bearingJoint in startBearingJoints)
            {
                if (!blocks.ContainsKey(bearingJoint.Key) || Blocks.blocks[blocks[bearingJoint.Key].id].blockType != Block.BlockType.bearing)
                    Debug.LogError("Invalid bearing joint");
                blocks[bearingJoint.Key].connected = bearingJoint.Value;
                blocks[bearingJoint.Key].connected.connectedBody = body;
            }
        }

        foreach (var block in networkBlocks)
        {
            CreateBlock(block.Value.id, block.Key, false, ByteRotationForward(block.Value.rotation), ByteRotationUp(block.Value.rotation));
        }

        UpdateMesh(true);
        // }
        // else
        // {
        //     createBlock(startBlockId, Vector3Int.zero);
        // }

        // blockAreas.Add(this);
    }

    private void FixedUpdate()
    {
        if (!(isServer && connectionToClient == null || hasAuthority))
            return;

        if (wakeUp > 0)
        {
            wakeUp--;
            body.WakeUp();
        }

        HashSet<Vector3Int> removeFromDamage = new HashSet<Vector3Int>();
        foreach (var block in damagedBlocks)
        {
            if (blocks[block.Key].framesSinceLastDamage >= 3)
            {
                // MeshBlock meshBlock = blocks[block.Key];
                // meshBlock.health = 1f;
                // blocks[block.Key] = meshBlock;
                if (blocks[block.Key].meshRenderer != null)
                {
                    blocks[block.Key].meshRenderer.material.SetFloat("_Health", 1f);
                }

                removeFromDamage.Add(block.Key);
            }
            else
            {
                MeshBlock meshBlock = blocks[block.Key];
                meshBlock.damagedThisFrame = false;
                meshBlock.framesSinceLastDamage++;
                blocks[block.Key] = meshBlock;
            }
        }

        foreach (var block in removeFromDamage)
        {
            //int id = damagedBlocks[block].id;
            Destroy(damagedBlocks[block].gameObject);
            damagedBlocks.Remove(block);
        }

        if (framesSinceLastDamage >= 3)
        {
            if (!Mathf.Approximately(health, 1))
            {
                meshRenderer.sharedMaterial.SetFloat("_Health", 1);
                foreach (var coord in blockObjects)
                {
                    blocks[coord].meshRenderer.material.SetFloat("_Health", 1);
                }
            }

            health = 1f;
            if (emitterId > -1)
            {
                OVR.AudioManager.StopSound(emitterId, false);
                emitterId = -1;
            }
        }
        else
        {
            framesSinceLastDamage++;
            damagedThisFrame = false;
        }

        if (resetCoM)
        {
            resetCoM = false;
            body.ResetInertiaTensor();
            body.ResetCenterOfMass();
        }
    }

    // void OnNetworkBlocksChange(SyncDictionaryVector3IntNetworkBlockInfo.Operation op, Vector3Int key, NetworkBlockInfo value)
    // {
    //     switch (op)
    //     {
    //         case SyncDictionaryVector3IntNetworkBlockInfo.Operation.OP_ADD:
    //             break;
    //         case SyncDictionaryVector3IntNetworkBlockInfo.Operation.OP_REMOVE:
    //             break;
    //         case SyncDictionaryVector3IntNetworkBlockInfo.Operation.OP_SET:
    //             break;
    //         case SyncDictionaryVector3IntNetworkBlockInfo.Operation.OP_CLEAR:
    //             break;
    //     }
    // }

    // [Command]
    // void CmdNetworkBlockAdd(Vector3Int key, NetworkBlockInfo value)
    // {
    //     networkBlocks.Add(key, value);
    // }

    // [Command]
    // void CmdNetworkBlockRemove(Vector3Int key)
    // {
    //     networkBlocks.Remove(key);
    // }

    [Command]
    public void CmdSetActive(bool active)
    {
        this.active = active;
    }

    public static byte IdentityByte()
    {
        return ForwardUpToByte(4, 2);
    }

    public static byte ByteRotationForward(byte rotation)
    {
        return (byte) (rotation & 0b_1111);
    }

    public static byte ByteRotationUp(byte rotation)
    {
        rotation >>= 4;
        return (byte) (rotation & 0b_1111);
    }

    public static byte ForwardUpToByte(int forward, int up)
    {
        byte rotation = (byte) up;
        rotation <<= 4;
        rotation |= (byte) forward;
        return rotation;
    }

    [Command]
    private void CmdDestroy(NetworkIdentity objId)
    {
        NetworkServer.Destroy(objId.gameObject);
    }

    public void LockBlock(GameObject physicsBlock)
    {
        Vector3 soundPosition = physicsBlock.transform.position;

        BlockArea blockArea = physicsBlock.GetComponent<BlockArea>();
        int id = -1;
        Vector3Int physicsCoord = Vector3Int.zero;
        foreach (var block in blockArea.blocks)
        {
            id = blockArea.blocks[block.Key].id;
            physicsCoord = block.Key;
            break;
        }

        Vector3 blockPosition = physicsBlock.transform.TransformPoint((Vector3) physicsCoord * cubeScale);
        Vector3 positionLocalToCubeArea = transform.InverseTransformPoint(blockPosition);
        Vector3Int coord = GetBlockCoord(positionLocalToCubeArea);

        Vector3 forward = transform.InverseTransformDirection(physicsBlock.transform.forward);
        Vector3 up = transform.InverseTransformDirection(physicsBlock.transform.up);
        byte forwardInt = directionToInt[new Vector3Int(Mathf.RoundToInt(forward.x), Mathf.RoundToInt(forward.y), Mathf.RoundToInt(forward.z))];
        byte upInt = directionToInt[new Vector3Int(Mathf.RoundToInt(up.x), Mathf.RoundToInt(up.y), Mathf.RoundToInt(up.z))];

        if (!blocks.ContainsKey(coord))
        {
            // lockInPlaceSound.PlaySoundAt(soundPosition);
            physicsBlock.SetActive(false);
            Destroy(physicsBlock);
            CmdDestroy(physicsBlock.GetComponent<NetworkIdentity>());

            CreateBlock(id, coord, true, forwardInt, upInt);
            CmdCreateBlock((byte) id, coord, ForwardUpToByte(forwardInt, upInt));
        }
        else if (Blocks.blocks[blocks[coord].id].blockType == Block.BlockType.bearing)
        {
            bearingAttachSound.PlaySoundAt(soundPosition);

            physicsBlock.GetComponent<BlockPhysics>().enabled = false;

            physicsBlock.transform.position = BlockCoordToPosition(coord) - (physicsBlock.transform.rotation * ((Vector3) physicsCoord * cubeScale));
            physicsBlock.transform.rotation = GetTargetRotation(physicsBlock.transform.rotation, coord,
                Blocks.blocks[blockArea.blocks[blockArea.OnlyBlock()].id].blockType);

            HingeJoint joint = physicsBlock.AddComponent<HingeJoint>();
            joint.autoConfigureConnectedAnchor = false;

            Vector3 axis = physicsBlock.transform.InverseTransformDirection(transform.rotation *
                                                                            Quaternion.LookRotation(intToDirection[blocks[coord].forward],
                                                                                intToDirection[blocks[coord].up]) * Vector3.up);
            axis = new Vector3(Mathf.Round(axis.x), Mathf.Round(axis.y), Mathf.Round(axis.z));
            axis = axis.normalized;
            joint.axis = axis;

            joint.anchor = (Vector3) physicsCoord * cubeScale - ((axis * 0.1f * cubeScale));

            joint.connectedAnchor = (Vector3) coord * cubeScale;

            joint.enableCollision = true;
            joint.connectedBody = body;

            blockArea.coordConnectedToBearing = physicsCoord;

            blocks[coord].connected = joint;
        }
        else
        {
            physicsBlock.SetActive(false);
            Destroy(physicsBlock);
            CmdDestroy(physicsBlock.GetComponent<NetworkIdentity>());
            Debug.LogError("Tried to place block on another block.");
        }
    }

    public void DamageAll(Vector3 position, float damage)
    {
        if (Mathf.Approximately(health, 1))
        {
            emitterId = breakingSound.PlaySoundAt(position);
        }

        if (!damagedThisFrame)
        {
            damagedThisFrame = true;
            framesSinceLastDamage = 0;
            health -= damage;
            if (health <= 0)
            {
                breakSound.PlaySoundAt(position);
                Destroy(gameObject);
                CmdDestroy(GetComponent<NetworkIdentity>());
            }

            meshRenderer.sharedMaterial.SetFloat("_Health", health);
            foreach (var coord in blockObjects)
            {
                blocks[coord].meshRenderer.material.SetFloat("_Health", health);
            }
        }
    }

    public void DamageBlock(Vector3Int coord, float damage)
    {
        if (blocks.ContainsKey(coord))
        {
            if (!damagedBlocks.ContainsKey(coord))
            {
                int id = blocks[coord].id;
                GameObject blockObject = Instantiate(blockBreakPrefab);
                blockObject.transform.parent = transform.GetChild(4);
                blockObject.transform.localPosition = coord;
                blockObject.transform.rotation = transform.rotation;
                BlockBreak blockBreak = blockObject.GetComponent<BlockBreak>();
                blockBreak.id = id;
                blockBreak.forward = blocks[coord].forward;
                blockBreak.up = blocks[coord].up;
                blockBreak.emitterId = breakingSound.PlaySoundAt(blockObject.transform.position);
                damagedBlocks.Add(coord, blockBreak);
            }

            if (!blocks[coord].damagedThisFrame)
            {
                MeshBlock meshBlock = blocks[coord];
                damagedBlocks[coord].health -= damage;
                meshBlock.damagedThisFrame = true;
                meshBlock.framesSinceLastDamage = 0;
                blocks[coord] = meshBlock;
                if (blocks[coord].meshRenderer != null)
                {
                    blocks[coord].meshRenderer.material.SetFloat("_Health", damagedBlocks[coord].health);
                }

                if (damagedBlocks[coord].health <= 0)
                {
                    breakSound.PlaySoundAt(damagedBlocks[coord].transform.position);
                    Destroy(damagedBlocks[coord].gameObject);
                    damagedBlocks.Remove(coord);

                    DeleteBlock(coord);
                    CmdDeleteBlock(coord);
                }
            }
        }
    }

    private readonly Vector3Int[] directions = new Vector3Int[]
    {
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 0, -1)
    };

    [Command]
    private void CmdCreateBlock(byte id, Vector3Int coord, byte rotation)
    {
        if (!(NetworkServer.active && NetworkClient.isConnected))
            CreateBlock(id, coord, true, ByteRotationForward(rotation), ByteRotationUp(rotation));
        networkBlocks.Add(coord, new NetworkBlockInfo() {id = id, rotation = rotation});
        RpcCreateBlock(id, coord, rotation);
    }

    [ClientRpc(excludeOwner = true)]
    private void RpcCreateBlock(byte id, Vector3Int coord, byte rotation)
    {
        CreateBlock(id, coord, true, ByteRotationForward(rotation), ByteRotationUp(rotation));
    }

    [Command]
    private void CmdDeleteBlock(Vector3Int coord)
    {
        if (!(NetworkServer.active && NetworkClient.isConnected))
        {
            if (!DeleteBlock(coord))
            {
                networkBlocks.Remove(coord);
                RpcDeleteBlock(coord);
            }
        }
        else
        {
            networkBlocks.Remove(coord);
            RpcDeleteBlock(coord);
        }
    }

    [ClientRpc(excludeOwner = true)]
    private void RpcDeleteBlock(Vector3Int coord)
    {
        DeleteBlock(coord);
    }

    public bool DeleteBlock(Vector3Int coord)
    {
        bool networkDestroy = false;
        if (blocks.ContainsKey(coord))
        {
            int vStart = blocks[coord].positionInList * 6 * 4;
            int tStart = blocks[coord].positionInList * 6 * 6;

            int vLastStart = blocks.Count * 6 * 4 - (6 * 4);
            int tLastStart = blocks.Count * 6 * 6 - (6 * 6);

            for (int i = 0; i < 6 * 6; i++)
            {
                triangles[tStart + i] = triangles[tLastStart + i] - ((blocks.Count - 1 - blocks[coord].positionInList) * 6 * 4);
            }

            triangles.RemoveRange(tLastStart, 6 * 6);

            for (int i = 0; i < 6 * 4; i++)
            {
                vertices[vStart + i] = vertices[vLastStart + i];
                uv[vStart + i] = uv[vLastStart + i];
                uv2[vStart + i] = uv2[vLastStart + i];
            }

            vertices.RemoveRange(vLastStart, 6 * 4);
            uv.RemoveRange(vLastStart, 6 * 4);
            uv2.RemoveRange(vLastStart, 6 * 4);

            int max = -1;
            Vector3Int key = Vector3Int.zero;
            foreach (var entry in blocks)
            {
                if (entry.Value.positionInList > max)
                {
                    max = entry.Value.positionInList;
                    key = entry.Key;
                }
            }

            blocks[key] = new MeshBlock(blocks[key].id, blocks[coord].positionInList, blocks[key].forward, blocks[key].up, blocks[key].blockObject,
                blocks[key].meshRenderer, blocks[key].connected);

            if (blocks[coord].blockObject != null)
            {
                Destroy(blocks[coord].blockObject);
                blockObjects.Remove(coord);
            }

            if (blocks[coord].connected != null)
            {
                if (blocks[coord].connected.GetComponent<BlockArea>().blocks.Count == 1)
                    blocks[coord].connected.GetComponent<BlockPhysics>().enabled = true;
                blocks[coord].connected.GetComponent<BlockArea>().wakeUp = 2;
                blocks[coord].connected.GetComponent<BlockArea>().coordConnectedToBearing = null;
                Destroy(blocks[coord].connected);
            }

            int id = blocks[coord].id;

            blocks.Remove(coord);

            if (coordConnectedToBearing != null)
            {
                if (coord == coordConnectedToBearing)
                {
                    coordConnectedToBearing = null;
                    Joint joint = GetComponent<Joint>();
                    if (joint != null)
                    {
                        joint.connectedBody.GetComponent<BlockArea>().wakeUp = 2;
                        Destroy(joint);
                    }
                }
            }

            foreach (var direction in directions)
            {
                Vector3Int testBlock = coord + direction;
                if (blocks.TryGetValue(testBlock, out MeshBlock block))
                {
                    if (Blocks.blocks[block.id].blockType == Block.BlockType.bearing)
                    {
                        Vector3Int connectedTo = testBlock + Vector3Int.RoundToInt(block.blockObject.transform.localRotation * Vector3.down);
                        if (connectedTo == coord)
                        {
                            DeleteBlock(testBlock);
                            if (isServer)
                                networkBlocks.Remove(testBlock);
                        }
                    }
                }
            }

            //split object into multiple objects if needed
            if (Blocks.blocks[id].blockType != Block.BlockType.bearing)
            {
                HashSet<Vector3Int> surroundingBlocks = new HashSet<Vector3Int>();
                foreach (var direction in directions)
                {
                    Vector3Int testPos = coord + direction;
                    if (blocks.TryGetValue(testPos, out MeshBlock block))
                    {
                        if (Blocks.blocks[block.id].blockType != Block.BlockType.bearing)
                        {
                            surroundingBlocks.Add(testPos);
                        }
                    }
                }

                List<Dictionary<Vector3Int, MeshBlock>> structures = new List<Dictionary<Vector3Int, MeshBlock>>();
                if (surroundingBlocks.Count > 1)
                {
                    foreach (var block in surroundingBlocks)
                    {
                        bool blockAssymilated = false;
                        for (int i = 0; i < structures.Count; i++)
                            if (structures[i].ContainsKey(block))
                                blockAssymilated = true;

                        if (!blockAssymilated)
                            structures.Add(FloodFill(block));
                    }
                }

                //split into more than one object
                if (structures.Count > 1 && isServer)
                {
                    gameObject.SetActive(false);

                    foreach (var structure in structures)
                    {
                        if (!(structure.Count == 1 && Blocks.blocks[structure[OnlyBlock(structure)].id].blockType == Block.BlockType.bearing))
                        {
                            GameObject blockArea = Instantiate(StaticPrefabs.instance.blockAreaPrefab);
                            blockArea.transform.parent = transform.parent;
                            blockArea.transform.position = transform.position;
                            blockArea.transform.rotation = transform.rotation;

                            Dictionary<Vector3Int, Joint> bearingJoints = new Dictionary<Vector3Int, Joint>();
                            foreach (var block in blockObjects)
                            {
                                if (structure.ContainsKey(block) && blocks[block].connected != null)
                                {
                                    bearingJoints.Add(block, blocks[block].connected);
                                }
                            }

                            BlockArea blockAreaBlockArea = blockArea.GetComponent<BlockArea>();
                            foreach (var block in structure)
                                blockAreaBlockArea.networkBlocks.Add(block.Key, new NetworkBlockInfo()
                                {
                                    id = (byte) block.Value.id,
                                    rotation = ForwardUpToByte(block.Value.forward, block.Value.up)
                                });
                            blockAreaBlockArea.startBlocks = structure;
                            blockAreaBlockArea.startBearingJoints = bearingJoints;

                            if (coordConnectedToBearing != null && structure.ContainsKey(coordConnectedToBearing.Value))
                            {
                                HingeJoint currentJoint = GetComponent<HingeJoint>();

                                HingeJoint joint = blockArea.AddComponent<HingeJoint>();

                                BlockArea otherArea = currentJoint.connectedBody.GetComponent<BlockArea>();
                                foreach (var block in otherArea.blocks)
                                {
                                    if (block.Value.connected != null && block.Value.connected == currentJoint)
                                    {
                                        otherArea.blocks[block.Key].connected = joint;
                                    }
                                }

                                joint.autoConfigureConnectedAnchor = false;

                                joint.axis = currentJoint.axis;
                                joint.anchor = currentJoint.anchor;
                                joint.connectedAnchor = currentJoint.connectedAnchor;

                                joint.enableCollision = currentJoint.enableCollision;
                                joint.connectedBody = currentJoint.connectedBody;

                                blockAreaBlockArea.coordConnectedToBearing = coordConnectedToBearing;
                            }

                            NetworkServer.Spawn(blockArea, connectionToClient);
                        }
                        else
                        {
                            Debug.LogError("structure only has a bearing");
                        }
                    }

                    destroyJoints = false;
                    if (isServer)
                    {
                        networkDestroy = true;
                        NetworkServer.Destroy(gameObject);
                    }
                    else
                    {
                        // Destroy(gameObject);
                    }

                    return networkDestroy;
                }
            }

            if (blocks.Count > 0)
                UpdateMesh(false);
            if (blocks.Count == 1)
            {
                blockPhysics.enabled = true;
            }
            else
            {
                blockPhysics.enabled = false;
            }

            if (blocks.Count == 0)
            {
                if (isServer)
                {
                    networkDestroy = true;
                    NetworkServer.Destroy(gameObject);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        return networkDestroy;
    }

    private Dictionary<Vector3Int, MeshBlock> FloodFill(Vector3Int fillPosition)
    {
        Dictionary<Vector3Int, MeshBlock> filledBlocks = new Dictionary<Vector3Int, MeshBlock>();
        Stack<Vector3Int> blocksToCheck = new Stack<Vector3Int>();

        blocksToCheck.Push(fillPosition);

        while (blocksToCheck.Count > 0)
        {
            Vector3Int checkPosition = blocksToCheck.Pop();
            if (!filledBlocks.ContainsKey(checkPosition) && blocks.ContainsKey(checkPosition))
            {
                filledBlocks.Add(checkPosition, blocks[checkPosition]);
                if (Blocks.blocks[blocks[checkPosition].id].blockType == Block.BlockType.normal)
                    for (int i = 0; i < 6; i++)
                        blocksToCheck.Push(checkPosition + directions[i]);
            }
        }

        return filledBlocks;
    }

    private void UpdateMesh(bool create)
    {
        if (create)
        {
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
        }
        else
        {
            mesh.triangles = triangles.ToArray();
            mesh.vertices = vertices.ToArray();
        }

        mesh.uv = uv.ToArray();
        mesh.uv2 = uv2.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        if (blocks.Count == 1)
        {
            if (blocks[OnlyBlock()].blockObject != null)
            {
                transform.GetChild(5).localPosition = (Vector3) OnlyBlock() * cubeScale;
                transform.GetChild(5).gameObject.SetActive(true);
            }
        }

        UpdateHitBox();
    }

    public Vector3Int OnlyBlock()
    {
        return OnlyBlock(blocks);
    }

    public Vector3Int OnlyBlock(Dictionary<Vector3Int, MeshBlock> structure)
    {
        if (structure.Count != 1)
            Debug.LogError("onlyBlock can only be called when there is only 1 block");

        foreach (var block in structure)
        {
            return block.Key;
        }

        Debug.LogError("No blocks. Returning Vector3Int.zero");
        return Vector3Int.zero;
    }

    private void UpdateHitBox()
    {
        resetCoM = true;
        if (blocks.Count < 1)
        {
            Debug.LogError("Cannot update hitbox with no blocks.");
            return;
        }

        HashSet<Vector3Int> assymilated;
        if (blocks.Count == 1)
            assymilated = new HashSet<Vector3Int>();
        else
            assymilated = new HashSet<Vector3Int>(blockObjects);
        Dictionary<Vector3Int, Vector2Int[]> boxes = new Dictionary<Vector3Int, Vector2Int[]>();
        // foreach (var block in blocks)
        // {
        //     if (!assymilated.Contains(block.Key))
        //     {
        //         assymilated.Add(block.Key);
        //
        //         Vector2Int[] boxBounds = {new Vector2Int(), new Vector2Int(), new Vector2Int()};
        //
        //         bool canSpreadRight = true;
        //         bool canSpreadLeft = true;
        //         bool canSpreadUp = true;
        //         bool canSpreadDown = true;
        //         bool canSpreadForward = true;
        //         bool canSpreadBackward = true;
        //
        //         while (canSpreadRight || canSpreadLeft || canSpreadUp || canSpreadDown || canSpreadForward ||
        //                canSpreadBackward)
        //         {
        //             if (canSpreadRight)
        //                 canSpreadRight =
        //                     HitBoxExpand.expandBoxBoundsRight(block.Key, ref boxBounds, ref assymilated, ref blocks);
        //             if (canSpreadLeft)
        //                 canSpreadLeft =
        //                     HitBoxExpand.expandBoxBoundsLeft(block.Key, ref boxBounds, ref assymilated, ref blocks);
        //             if (canSpreadUp)
        //                 canSpreadUp =
        //                     HitBoxExpand.expandBoxBoundsUp(block.Key, ref boxBounds, ref assymilated, ref blocks);
        //             if (canSpreadDown)
        //                 canSpreadDown =
        //                     HitBoxExpand.expandBoxBoundsDown(block.Key, ref boxBounds, ref assymilated, ref blocks);
        //             if (canSpreadForward)
        //                 canSpreadForward =
        //                     HitBoxExpand.expandBoxBoundsForward(block.Key, ref boxBounds, ref assymilated, ref blocks);
        //             if (canSpreadBackward)
        //                 canSpreadBackward =
        //                     HitBoxExpand.expandBoxBoundsBackward(block.Key, ref boxBounds, ref assymilated, ref blocks);
        //         }
        //
        //         boxes.Add(block.Key, boxBounds);
        //     }
        // }

        Queue<BoxCollider> boxCollidersNotNeeded = new Queue<BoxCollider>();
        while (boxCollidersInUse.Count > 0)
        {
            BoxCollider boxCollider = boxCollidersInUse.Dequeue();
            boxCollidersNotNeeded.Enqueue(boxCollider);
        }

        foreach (var box in boxes)
        {
            float xScale = 1f + (1f * (-box.Value[0][0] + box.Value[0][1]));
            float yScale = 1f + (1f * (-box.Value[1][0] + box.Value[1][1]));
            float zScale = 1f + (1f * (-box.Value[2][0] + box.Value[2][1]));

            float xPosition = -(1f / 2f) - (1f * -box.Value[0][0]) + (xScale / 2f);
            float yPosition = -(1f / 2f) - (1f * -box.Value[1][0]) + (yScale / 2f);
            float zPosition = -(1f / 2f) - (1f * -box.Value[2][0]) + (zScale / 2f);

            if (boxCollidersNotNeeded.Count > 0)
            {
                BoxCollider boxCollider = boxCollidersNotNeeded.Dequeue();
                boxCollider.size = new Vector3(xScale, yScale, zScale);
                boxCollider.center = box.Key + new Vector3(xPosition, yPosition, zPosition);
                boxCollidersInUse.Enqueue(boxCollider);
            }
            else
            {
                BoxCollider boxCollider = transform.GetChild(1).gameObject.AddComponent<BoxCollider>();
                boxCollider.material = blockPhysicMaterial;
                boxCollider.size = new Vector3(xScale, yScale, zScale);
                boxCollider.center = box.Key + new Vector3(xPosition, yPosition, zPosition);
                boxCollidersInUse.Enqueue(boxCollider);
            }
        }

        while (boxCollidersNotNeeded.Count > 0)
        {
            Destroy(boxCollidersNotNeeded.Dequeue());
        }

        if (blocks.Count == 1)
        {
            KeyValuePair<Vector3Int, MeshBlock> block;
            foreach (var nextBlock in blocks)
                block = nextBlock;

            var s = transform.GetChild(2).gameObject.AddComponent<SphereCollider>();
            s.material = spherePhysicMaterial;
            s.center = block.Key;
            s.radius = 0.475f;
        }
        else
        {
            var s = transform.GetChild(2).gameObject.GetComponent<SphereCollider>();
            if (s != null)
                Destroy(s);
        }

        body.mass = Mathf.Clamp((BlockObject.CubeMass * blocks.Count) - (BlockObject.CubeMass * blockObjects.Count), BlockObject.CubeMass, Mathf.Infinity);
    }

    private void CreateBlock(int id, Vector3Int coord, bool updateTheMesh, byte forward = 4, byte up = 2)
    {
        if (id < 0)
            Debug.LogError("Cannot create block with id less than 0");
        if (blocks.ContainsKey(coord))
            Debug.LogError("Block with coord already exists");

        var blockMesh = GetBlockMesh(id, coord, forward, up);
        vertices.AddRange(blockMesh.vertices);
        uv.AddRange(blockMesh.uv);
        uv2.AddRange(blockMesh.uv2);
        for (int i = 0; i < blockMesh.triangles.Length; i++)
            blockMesh.triangles[i] += vertices.Count - (6 * 4);
        triangles.AddRange(blockMesh.triangles);

        GameObject blockObject = null;
        MeshRenderer meshRenderer = null;
        if (Blocks.blocks[id].blockObject != null)
        {
            blockObject = Instantiate(Blocks.blocks[id].blockObject, transform.GetChild(3));
            blockObject.transform.localPosition = coord;
            blockObject.transform.localRotation = Quaternion.LookRotation(intToDirection[forward], intToDirection[up]);
            blockObjects.Add(coord);

            meshRenderer = blockObject.GetComponentInChildren<MeshRenderer>();
        }

        blocks.Add(coord, new MeshBlock(id, blocks.Count, forward, up, blockObject, meshRenderer, null));
        ExpandToFit(coord);

        if (updateTheMesh)
            UpdateMesh(true);

        if (blocks.Count == 1)
            blockPhysics.enabled = true;
        else
            blockPhysics.enabled = false;
    }

    public static (Vector3[] vertices, Vector2[] uv, int[] triangles, Vector2[] uv2) GetBlockMesh(int id, byte forward = 4, byte up = 2)
    {
        return GetBlockMesh(id, Vector3Int.zero, forward, up);
    }

    private static (Vector3[] vertices, Vector2[] uv, int[] triangles, Vector2[] uv2) GetBlockMesh(int id, Vector3Int coord, byte forward = 4, byte up = 2)
    {
        if (id == -1)
        {
            id = 0;
            Debug.LogError("Attempted to getBlockMesh with id of -1");
        }

        Vector3[] vertices = new Vector3[6 * 4];
        Vector2[] uv = new Vector2[6 * 4];
        Vector2[] uv2 = new Vector2[6 * 4];
        int[] triangles = new int[6 * 6];

        Vector2 topBlockuv = GetBlockuv(Blocks.blocks[id].topTextureIndex);
        Vector2 sideBlockuv = GetBlockuv(Blocks.blocks[id].sideTextureIndex);
        Vector2 bottomBlockuv = GetBlockuv(Blocks.blocks[id].bottomTextureIndex);
        Vector2 blockuv; // = Vector2.zero;

        Quaternion rotation = Quaternion.LookRotation(intToDirection[forward], intToDirection[up]);

        for (int face = 0; face < 6; face++)
        {
            for (int v = 0; v < 4; v++)
            {
                vertices[face * 4 + v] = (rotation * cubeVertices[face * 4 + v]) + coord;
            }

            if (face < 4)
                blockuv = sideBlockuv;
            else if (face < 5)
                blockuv = topBlockuv;
            else
                blockuv = bottomBlockuv;
            uv[face * 4 + 0] = blockuv + new Vector2(texturePadding, texturePadding);
            uv[face * 4 + 1] = blockuv + new Vector2(Block.textureWidth, 0) + new Vector2(-texturePadding, texturePadding);
            uv[face * 4 + 2] = blockuv + new Vector2(Block.textureWidth, Block.textureWidth) + new Vector2(-texturePadding, -texturePadding);
            uv[face * 4 + 3] = blockuv + new Vector2(0, Block.textureWidth) + new Vector2(texturePadding, -texturePadding);

            uv2[face * 4 + 0] = new Vector2(0, 0);
            uv2[face * 4 + 1] = new Vector2(Block.breakTextureWidth, 0);
            uv2[face * 4 + 2] = new Vector2(Block.breakTextureWidth, 1);
            uv2[face * 4 + 3] = new Vector2(0, 1);

            if (Blocks.blocks[id].blockObject == null)
            {
                int v0 = face * 4;
                triangles[face * 6 + 0] = v0 + 0;
                triangles[face * 6 + 1] = v0 + 3;
                triangles[face * 6 + 2] = v0 + 2;
                triangles[face * 6 + 3] = v0 + 0;
                triangles[face * 6 + 4] = v0 + 2;
                triangles[face * 6 + 5] = v0 + 1;
            }
        }

        if (Blocks.blocks[id].blockObject != null)
            for (int i = 0; i < triangles.Length; i++)
                triangles[i] = 0;

        return (vertices, uv, triangles, uv2);
    }

    public static Vector2 GetBlockuv(int textureId)
    {
        int xTexture = textureId % Block.xTextures;
        int yTexture = textureId / Block.xTextures;
        return new Vector2(xTexture * Block.textureWidth, 1 - Block.textureWidth - (yTexture * Block.textureWidth));
    }

    // public void setVisualSize()
    // {
    //     float xScale = cubeScale + (cubeScale * (-bounds[0][0] + bounds[0][1]));
    //     float yScale = cubeScale + (cubeScale * (-bounds[1][0] + bounds[1][1]));
    //     float zScale = cubeScale + (cubeScale * (-bounds[2][0] + bounds[2][1]));
    //     transform.GetChild(0).localScale = new Vector3(xScale, yScale, zScale);

    //     float xPosition = -(cubeScale / 2f) - (cubeScale * -bounds[0][0]) + (xScale / 2f);
    //     float yPosition = -(cubeScale / 2f) - (cubeScale * -bounds[1][0]) + (yScale / 2f);
    //     float zPosition = -(cubeScale / 2f) - (cubeScale * -bounds[2][0]) + (zScale / 2f);
    //     transform.GetChild(0).localPosition = new Vector3(xPosition, yPosition, zPosition);
    // }

    public void ResetBounds()
    {
        for (int axis = 0; axis < 3; axis++)
        for (int component = 0; component < 2; component++)
            bounds[axis][component] = 0;
    }

    public void ExpandToFit(Vector3Int blockCoord)
    {
        for (int axis = 0; axis < 3; axis++)
        {
            if (blockCoord[axis] <= bounds[axis][0])
                bounds[axis][0] = blockCoord[axis] - 1;
            if (blockCoord[axis] >= bounds[axis][1])
                bounds[axis][1] = blockCoord[axis] + 1;
        }
    }

    public (bool isInCubeArea, bool isNearHole) InCubeArea(Vector3Int testPosition, int id)
    {
        if (ContainsBlock(testPosition))
        {
            return (false, false);
        }

        //don't @ me
        bool isNearHole =
            (!ContainsBlock(testPosition + new Vector3Int(1, 0, 0)) &&
             (ContainsBlock(testPosition + new Vector3Int(1, -1, 0)) && ContainsBlock(testPosition + new Vector3Int(1, 1, 0)) ||
              ContainsBlock(testPosition + new Vector3Int(1, 0, -1)) && ContainsBlock(testPosition + new Vector3Int(1, 0, 1)))) ||
            (!ContainsBlock(testPosition + new Vector3Int(-1, 0, 0)) &&
             (ContainsBlock(testPosition + new Vector3Int(-1, -1, 0)) && ContainsBlock(testPosition + new Vector3Int(-1, 1, 0)) ||
              ContainsBlock(testPosition + new Vector3Int(-1, 0, -1)) && ContainsBlock(testPosition + new Vector3Int(-1, 0, 1)))) ||
            (!ContainsBlock(testPosition + new Vector3Int(0, 1, 0)) &&
             (ContainsBlock(testPosition + new Vector3Int(-1, 1, 0)) && ContainsBlock(testPosition + new Vector3Int(1, 1, 0)) ||
              ContainsBlock(testPosition + new Vector3Int(0, 1, -1)) && ContainsBlock(testPosition + new Vector3Int(0, 1, 1)))) ||
            (!ContainsBlock(testPosition + new Vector3Int(0, -1, 0)) &&
             (ContainsBlock(testPosition + new Vector3Int(-1, -1, 0)) && ContainsBlock(testPosition + new Vector3Int(1, -1, 0)) ||
              ContainsBlock(testPosition + new Vector3Int(0, -1, -1)) && ContainsBlock(testPosition + new Vector3Int(0, -1, 1)))) ||
            (!ContainsBlock(testPosition + new Vector3Int(0, 0, 1)) &&
             (ContainsBlock(testPosition + new Vector3Int(-1, 0, 1)) && ContainsBlock(testPosition + new Vector3Int(1, 0, 1)) ||
              ContainsBlock(testPosition + new Vector3Int(0, -1, 1)) && ContainsBlock(testPosition + new Vector3Int(0, 1, 1)))) ||
            (!ContainsBlock(testPosition + new Vector3Int(0, 0, -1)) &&
             (ContainsBlock(testPosition + new Vector3Int(-1, 0, -1)) && ContainsBlock(testPosition + new Vector3Int(1, 0, -1)) ||
              ContainsBlock(testPosition + new Vector3Int(0, -1, -1)) && ContainsBlock(testPosition + new Vector3Int(0, 1, -1)))) ||
            (ContainsBlock(testPosition + new Vector3Int(-1, 0, 0)) && ContainsBlock(testPosition + new Vector3Int(1, 0, 0)) ||
             ContainsBlock(testPosition + new Vector3Int(0, -1, 0)) && ContainsBlock(testPosition + new Vector3Int(0, 1, 0)) ||
             ContainsBlock(testPosition + new Vector3Int(0, 0, -1)) && ContainsBlock(testPosition + new Vector3Int(0, 0, 1)));

        bool inCubeAreaBearing = true;
        if (Blocks.blocks[id].blockType == Block.BlockType.bearing)
        {
            if (blocks.TryGetValue(testPosition, out MeshBlock block))
            {
                if (Blocks.blocks[block.id].blockType == Block.BlockType.bearing)
                    inCubeAreaBearing = false;
            }
        }

        bool isInCubeArea = inCubeAreaBearing && (ContainsBlock(testPosition + new Vector3Int(1, 0, 0)) ||
                                                  ContainsBlock(testPosition + new Vector3Int(-1, 0, 0)) ||
                                                  ContainsBlock(testPosition + new Vector3Int(0, 1, 0)) ||
                                                  ContainsBlock(testPosition + new Vector3Int(0, -1, 0)) ||
                                                  ContainsBlock(testPosition + new Vector3Int(0, 0, 1)) ||
                                                  ContainsBlock(testPosition + new Vector3Int(0, 0, -1)));

        return (isInCubeArea, isNearHole);
    }

    private bool ContainsBlock(Vector3Int coord)
    {
        if (blocks.TryGetValue(coord, out MeshBlock block))
        {
            return block.blockObject == null;
        }
        else
        {
            return false;
        }
    }

    public Vector3Int GetBlockCoord(Vector3 positionLocalToCubeArea)
    {
        Vector3 offset = new Vector3(BlockArea.cubeScale / 2f, BlockArea.cubeScale / 2f, BlockArea.cubeScale / 2f);
        Vector3 localPosition = SnapPosition(positionLocalToCubeArea - offset) + offset;
        return Vector3Int.RoundToInt(localPosition / BlockArea.cubeScale);
    }

    public Vector3 BlockCoordToPosition(Vector3Int blockCoord)
    {
        return transform.TransformPoint(new Vector3(blockCoord.x, blockCoord.y, blockCoord.z) * cubeScale);
    }

    private Vector3 SnapPosition(Vector3 currentPosition)
    {
        currentPosition = new Vector3(Mathf.Floor(currentPosition.x * (1f / BlockArea.cubeScale)) / (1f / BlockArea.cubeScale),
            Mathf.Floor(currentPosition.y * (1f / BlockArea.cubeScale)) / (1f / BlockArea.cubeScale),
            Mathf.Floor(currentPosition.z * (1f / BlockArea.cubeScale)) / (1f / BlockArea.cubeScale));
        currentPosition.x += BlockArea.cubeScale / 2f;
        currentPosition.y += BlockArea.cubeScale / 2f;
        currentPosition.z += BlockArea.cubeScale / 2f;
        return currentPosition;
    }

    public Quaternion GetTargetRotation(Quaternion blockRotation, Vector3Int coord, Block.BlockType blockType)
    {
        if (blockType == Block.BlockType.bearing)
        {
            List<Vector3> upCheckAxes = new List<Vector3>();
            foreach (var direction in directions)
            {
                Vector3Int testBlock = coord + direction;
                if (blocks.TryGetValue(testBlock, out MeshBlock block))
                {
                    if (Blocks.blocks[block.id].blockType == Block.BlockType.normal)
                    {
                        upCheckAxes.Add(-direction);
                    }
                }
            }

            Quaternion rotationLocalToArea = Quaternion.Inverse(transform.rotation) * blockRotation;
            Vector3 up = ClosestToAxis(rotationLocalToArea, Vector3.up, upCheckAxes.ToArray());
            Vector3[] forwardCheckAxes = new Vector3[4];
            int n = 0;
            Vector3Int upInt = Vector3Int.RoundToInt(up);
            for (int i = 0; i < directions.Length; i++)
            {
                if (!(directions[i] == upInt || directions[i] == -upInt))
                {
                    forwardCheckAxes[n] = directions[i];
                    n++;
                }
            }

            Vector3 forward = ClosestToAxis(rotationLocalToArea, Vector3.forward, forwardCheckAxes);
            return transform.rotation * Quaternion.LookRotation(forward, up);
        }
        else
        {
            return transform.rotation * SnapToNearestRightAngle(Quaternion.Inverse(transform.rotation) * blockRotation);
        }
    }

    // public static Quaternion getTargetRotation(Transform transform, Quaternion blockRotation)
    // {
    //     return transform.rotation * snapToNearestRightAngle(Quaternion.Inverse(transform.rotation) * blockRotation);
    // }

    //takes a rotation and returns the rotation that is the closest with all axes pointing at 90 degree intervals to the identity quaternion
    private static Quaternion SnapToNearestRightAngle(Quaternion currentRotation)
    {
        Vector3 closestToForward = SnappedToNearestAxis(currentRotation * Vector3.forward);
        Vector3 closestToUp = SnappedToNearestAxis(currentRotation * Vector3.up);
        return Quaternion.LookRotation(closestToForward, closestToUp);
    }

    //find the world axis that is closest to direction
    private static Vector3 SnappedToNearestAxis(Vector3 direction)
    {
        float x = Mathf.Abs(direction.x);
        float y = Mathf.Abs(direction.y);
        float z = Mathf.Abs(direction.z);
        if (x > y && x > z)
        {
            return new Vector3(Mathf.Sign(direction.x), 0, 0);
        }
        else if (y > x && y > z)
        {
            return new Vector3(0, Mathf.Sign(direction.y), 0);
        }
        else
        {
            return new Vector3(0, 0, Mathf.Sign(direction.z));
        }
    }

    // Quaternion snapToNearestRightAngleDot(Quaternion currentRotation)
    // {
    //     Vector3[] checkAxes = new Vector3[] { Vector3.forward, Vector3.right, Vector3.up, -Vector3.forward, -Vector3.right, -Vector3.up };
    //     Vector3 closestToForward = closestToAxis(currentRotation, Vector3.forward, checkAxes);
    //     Vector3 closestToUp = closestToAxis(currentRotation, Vector3.up, checkAxes);
    //     return Quaternion.LookRotation(closestToForward, closestToUp);
    // }

    //finds the axis that is closest to the currentRotations local axis
    private Vector3 ClosestToAxis(Quaternion currentRotation, Vector3 axis, Vector3[] checkAxes)
    {
        Vector3 closestToAxis = Vector3.forward;
        float highestDot = -1;
        foreach (Vector3 checkAxis in checkAxes)
            Check(ref highestDot, ref closestToAxis, currentRotation, axis, checkAxis);
        return closestToAxis;
    }

    //finds the closest axis to the input rotations specified axis
    private void Check(ref float highestDot, ref Vector3 closest, Quaternion currentRotation, Vector3 axis, Vector3 checkDir)
    {
        float dot = Vector3.Dot(currentRotation * axis, checkDir);
        if (dot > highestDot)
        {
            highestDot = dot;
            closest = checkDir;
        }
    }

    // public static Vector3 TransformPointUnscaled(Transform transformIn, Vector3 position)
    //  {
    //      var localToWorldMatrix = Matrix4x4.TRS(transformIn.position, transformIn.rotation, Vector3.one);
    //      return localToWorldMatrix.MultiplyPoint3x4(position);
    //  }

    //  public static Vector3 InverseTransformPointUnscaled(Transform transformIn, Vector3 position)
    //  {
    //      var worldToLocalMatrix = Matrix4x4.TRS(transformIn.position, transformIn.rotation, Vector3.one).inverse;
    //      return worldToLocalMatrix.MultiplyPoint3x4(position);
    //  }

    public static Quaternion IntToQuat(byte forward, byte up)
    {
        return Quaternion.LookRotation(intToDirection[forward], intToDirection[up]);
    }

    public static readonly Vector3[] cubeVertices = new Vector3[]
    {
        //front
        new Vector3(-0.5f, -0.5f, -0.5f),
        new Vector3(0.5f, -0.5f, -0.5f),
        new Vector3(0.5f, 0.5f, -0.5f),
        new Vector3(-0.5f, 0.5f, -0.5f),

        //right
        new Vector3(0.5f, -0.5f, -0.5f),
        new Vector3(0.5f, -0.5f, 0.5f),
        new Vector3(0.5f, 0.5f, 0.5f),
        new Vector3(0.5f, 0.5f, -0.5f),

        //back
        new Vector3(0.5f, -0.5f, 0.5f),
        new Vector3(-0.5f, -0.5f, 0.5f),
        new Vector3(-0.5f, 0.5f, 0.5f),
        new Vector3(0.5f, 0.5f, 0.5f),

        //left
        new Vector3(-0.5f, -0.5f, 0.5f),
        new Vector3(-0.5f, -0.5f, -0.5f),
        new Vector3(-0.5f, 0.5f, -0.5f),
        new Vector3(-0.5f, 0.5f, 0.5f),

        //top
        new Vector3(-0.5f, 0.5f, -0.5f),
        new Vector3(0.5f, 0.5f, -0.5f),
        new Vector3(0.5f, 0.5f, 0.5f),
        new Vector3(-0.5f, 0.5f, 0.5f),

        //bottom
        new Vector3(-0.5f, -0.5f, 0.5f),
        new Vector3(0.5f, -0.5f, 0.5f),
        new Vector3(0.5f, -0.5f, -0.5f),
        new Vector3(-0.5f, -0.5f, -0.5f),
    };

    public static readonly IReadOnlyDictionary<byte, Vector3> intToDirection = new Dictionary<byte, Vector3>
    {
        {0, new Vector3(1, 0, 0)},
        {1, new Vector3(-1, 0, 0)},
        {2, new Vector3(0, 1, 0)},
        {3, new Vector3(0, -1, 0)},
        {4, new Vector3(0, 0, 1)},
        {5, new Vector3(0, 0, -1)}
    };

    public static readonly IReadOnlyDictionary<Vector3Int, byte> directionToInt = new Dictionary<Vector3Int, byte>
    {
        {new Vector3Int(1, 0, 0), 0},
        {new Vector3Int(-1, 0, 0), 1},
        {new Vector3Int(0, 1, 0), 2},
        {new Vector3Int(0, -1, 0), 3},
        {new Vector3Int(0, 0, 1), 4},
        {new Vector3Int(0, 0, -1), 5}
    };

    private void OnDestroy()
    {
        foreach (var block in blockObjects)
        {
            if (blocks[block].connected != null)
            {
                if (destroyJoints)
                {
                    if (blocks[block].connected.GetComponent<BlockArea>().blocks.Count == 1)
                        blocks[block].connected.GetComponent<BlockPhysics>().enabled = true;
                    Destroy(blocks[block].connected);
                }
            }
        }

        // blockAreas.Remove(this);
        if (blockAreaIdentities.Contains(GetComponent<NetworkIdentity>()))
            blockAreaIdentities.Remove(GetComponent<NetworkIdentity>());
        if (emitterId > -1)
        {
            OVR.AudioManager.StopSound(emitterId, false);
            emitterId = -1;
        }
    }
}