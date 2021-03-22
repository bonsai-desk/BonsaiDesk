using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;
using BlockDictOp = Mirror.SyncDictionary<UnityEngine.Vector3Int, SyncBlock>.Operation;

public struct SyncBlock
{
    public byte id;
    public byte rotation;

    public SyncBlock(byte id, byte rotation)
    {
        this.id = id;
        this.rotation = rotation;
    }
}

[RequireComponent(typeof(Rigidbody))]
public partial class BlockObject : NetworkBehaviour
{
    public const float CubeScale = 0.05f;

    //contains all of the AutoAuthority of all BlockObjects in the scene
    private static HashSet<AutoAuthority> _blockObjectAuthorities = new HashSet<AutoAuthority>();

    //contains all of the information required to construct this BlockObject
    public readonly SyncDictionary<Vector3Int, SyncBlock> Blocks = new SyncDictionary<Vector3Int, SyncBlock>();

    //caches changes made to the sync dictionary so all block changes can be made at once with a single mesh update
    public readonly Queue<(Vector3Int coord, SyncBlock syncBlock, BlockDictOp op)> BlockChanges =
        new Queue<(Vector3Int coord, SyncBlock syncBlock, BlockDictOp op)>();

    //public inspector variables
    public Material blockObjectMaterial;
    public PhysicMaterial blockPhysicMaterial;
    public PhysicMaterial spherePhysicMaterial;

    //contains the information about the local state of the mesh. The structure of the mesh can be slightly
    //different depending on the order of block add/remove even though the final result looks the same
    private Dictionary<Vector3Int, MeshBlock> _meshBlocks = new Dictionary<Vector3Int, MeshBlock>();

    //contains the keys for entries in _meshBlocks which are damages. This saves having to loop through
    //the entire _meshBlocks to check for damaged blocks
    private HashSet<Vector3Int> _damagedBlocks = new HashSet<Vector3Int>();

    //mesh stuff
    private MeshFilter _meshFilter;
    private Mesh _mesh;
    private List<Vector3> _vertices = new List<Vector3>();
    private List<Vector2> _uv = new List<Vector2>();
    private List<Vector2> _uv2 = new List<Vector2>();
    private List<int> _triangles = new List<int>();
    private float _texturePadding = 0f;

    //physics
    private Rigidbody _body;
    private Transform _physicsBoxesObject;
    private Queue<BoxCollider> _boxCollidersInUse = new Queue<BoxCollider>();
    private bool _resetCoM = false; //flag to reset CoM on the next physics update
    private AutoAuthority _autoAuthority;
    private Transform _sphereObject;

    //this object is the parent of any BlockObject with 1 block trying to attach to another BlockObject
    [HideInInspector] public Transform potentialBlocksParent;

    //used to make sure Init is only called once
    private bool _isInit = false;

    public bool debug = false;

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (debug)
        {
            Blocks.Add(Vector3Int.zero, new SyncBlock(0, BlockUtility.QuaternionToByte(Quaternion.identity)));

            for (int i = 1; i < 6; i++)
            {
                Blocks.Add(new Vector3Int(0, 0, i),
                    new SyncBlock(0, BlockUtility.QuaternionToByte(Quaternion.identity)));
            }

            // Blocks.Add(new Vector3Int(0, 0, 1), new SyncBlock(0, BlockUtility.QuaternionToByte(Quaternion.identity)));
            // Blocks.Add(new Vector3Int(1, 0, 0), new SyncBlock(0, BlockUtility.QuaternionToByte(Quaternion.identity)));
            // Blocks.Add(new Vector3Int(1, 0, 1), new SyncBlock(0, BlockUtility.QuaternionToByte(Quaternion.identity)));
            // Blocks.Add(new Vector3Int(0, 1, 0), new SyncBlock(0, BlockUtility.QuaternionToByte(Quaternion.identity)));
            // Blocks.Add(new Vector3Int(0, 1, 1), new SyncBlock(0, BlockUtility.QuaternionToByte(Quaternion.identity)));
            // Blocks.Add(new Vector3Int(1, 1, 0), new SyncBlock(0, BlockUtility.QuaternionToByte(Quaternion.identity)));
            // Blocks.Add(new Vector3Int(1, 1, 1), new SyncBlock(0, BlockUtility.QuaternionToByte(Quaternion.identity)));
        }

        Init();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        Init();
    }

    private void Init()
    {
        if (_isInit)
        {
            return;
        }

        _isInit = true;

        //make copy of material so material asset is not changed
        blockObjectMaterial = new Material(blockObjectMaterial);

        PhysicsStart();

        _autoAuthority = GetComponent<AutoAuthority>();

        if (!_blockObjectAuthorities.Contains(_autoAuthority))
        {
            _blockObjectAuthorities.Add(_autoAuthority);
        }

        _body = GetComponent<Rigidbody>();
        _body.maxAngularVelocity = float.MaxValue;

        transform.localScale = new Vector3(CubeScale, CubeScale, CubeScale);

        SetupChildren();

        _texturePadding = 1f / blockObjectMaterial.mainTexture.width / 2f;

        _mesh = new Mesh();
        _meshFilter.mesh = _mesh;

        CreateInitialMesh();
        Blocks.Callback -= OnBlocksDictionaryChange;
        Blocks.Callback += OnBlocksDictionaryChange;
    }

    private void Update()
    {
        ProcessBlockChanges();
        UpdateDamagedBlocks();

        if (Input.GetKeyDown(KeyCode.Space) && debug)
        {
            print("--- " + Blocks.Count);
            foreach (var pair in Blocks)
            {
                print(pair.Key);
            }
        }
    }

    private void FixedUpdate()
    {
        if (!_isInit)
        {
            return;
        }

        if (!(isClient && NetworkClient.connection != null && NetworkClient.connection.identity))
        {
            return;
        }

        if (_resetCoM)
        {
            _resetCoM = false;
            _body.ResetInertiaTensor();
            _body.ResetCenterOfMass();
        }

        PhysicsFixedUpdate();
    }

    private void OnDestroy()
    {
        if (_blockObjectAuthorities.Contains(_autoAuthority))
        {
            _blockObjectAuthorities.Remove(_autoAuthority);
        }
    }

    private void SetupChildren()
    {
        var meshObject = new GameObject("Mesh");
        meshObject.transform.SetParent(transform, false);
        var meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = blockObjectMaterial;
        _autoAuthority.SetCachedMaterial(blockObjectMaterial);
        _autoAuthority.meshRenderer = meshRenderer;
        _meshFilter = meshObject.AddComponent<MeshFilter>();

        var physicsBoxes = new GameObject("PhysicsBoxes");
        _physicsBoxesObject = physicsBoxes.transform;
        physicsBoxes.transform.SetParent(transform, false);

        potentialBlocksParent = new GameObject("PotentialBlocksParent").transform;
        const float inverseScale = 1f / CubeScale;
        potentialBlocksParent.localScale = new Vector3(inverseScale, inverseScale, inverseScale);
        potentialBlocksParent.SetParent(transform, false);

        var sphereObject = new GameObject("Sphere");
        _sphereObject = sphereObject.transform;
        sphereObject.transform.SetParent(transform, false);
        sphereObject.layer = LayerMask.NameToLayer("sphere");
    }

    private void OnBlocksDictionaryChange(BlockDictOp op, Vector3Int key,
        SyncBlock value)
    {
        BlockChanges.Enqueue((key, value, op));
    }

    //loops through any blocks in BlockChanges and adds/removes blocks from the mesh
    private void ProcessBlockChanges()
    {
        if (BlockChanges.Count == 0)
        {
            return;
        }

        while (BlockChanges.Count > 0)
        {
            var (coord, syncBlock, op) = BlockChanges.Dequeue();

            switch (op)
            {
                case BlockDictOp.OP_ADD:
                    if (!_meshBlocks.ContainsKey(coord))
                    {
                        AddBlockToMesh(syncBlock.id, coord, BlockUtility.ByteToQuaternion(syncBlock.rotation));
                    }

                    break;
                case BlockDictOp.OP_REMOVE:
                    if (_meshBlocks.ContainsKey(coord))
                    {
                        RemoveBlockFromMesh(coord);
                    }

                    break;
                default:
                    Debug.LogError("Unknown dictionary operation.");
                    break;
            }
        }

        UpdateMesh();
    }

    [Command(ignoreAuthority = true)]
    private void CmdAddBlock(byte id, Vector3Int coord, Quaternion rotation, NetworkIdentity blockToDestroy)
    {
        blockToDestroy.GetComponent<AutoAuthority>().ServerStripOwnerAndDestroy();

        if (Blocks.ContainsKey(coord))
        {
            Debug.LogError("Command: Attempted to add block which already exists");
            return;
        }

        Blocks.Add(coord, new SyncBlock(id, BlockUtility.QuaternionToByte(rotation)));
    }

    private void AddBlockToMesh(byte id, Vector3Int coord, Quaternion rotation)
    {
        if (_meshBlocks.ContainsKey(coord))
        {
            Debug.LogError("Attempted to add block to mesh which already exists in _meshBlocks");
            return;
        }

        var blockMesh = BlockUtility.GetBlockMesh(id, coord, rotation, _texturePadding);
        _vertices.AddRange(blockMesh.vertices);
        _uv.AddRange(blockMesh.uv);
        _uv2.AddRange(blockMesh.uv2);
        for (int i = 0; i < blockMesh.triangles.Length; i++)
            blockMesh.triangles[i] += _vertices.Count - (6 * 4);
        _triangles.AddRange(blockMesh.triangles);

        // GameObject blockObject = null;
        // MeshRenderer meshRenderer = null;
        // if (Blocks.blocks[id].blockObject != null)
        // {
        //     blockObject = Instantiate(Blocks.blocks[id].blockObject, transform.GetChild(3));
        //     blockObject.transform.localPosition = coord;
        //     blockObject.transform.localRotation = rotation;
        //     blockObjects.Add(coord);
        //
        //     meshRenderer = blockObject.GetComponentInChildren<MeshRenderer>();
        // }

        _meshBlocks.Add(coord, new MeshBlock(_meshBlocks.Count));
    }

    [Command(ignoreAuthority = true)]
    private void CmdRemoveBlock(Vector3Int coord)
    {
        if (!Blocks.ContainsKey(coord))
        {
            Debug.LogError("Command: Attempted to remove block which does not exist in Blocks.");
            return;
        }

        if (Blocks.Count <= 1)
        {
            _autoAuthority.ServerStripOwnerAndDestroy();
        }
        else
        {
            var filledBlocksGroups = new List<Dictionary<Vector3Int, SyncBlock>>();

            var surroundingBlocks = BlockUtility.GetSurroundingBlocks(coord, Blocks);
            foreach (var block in surroundingBlocks)
            {
                bool blockAlreadyFilled = false;
                for (int i = 0; i < filledBlocksGroups.Count; i++)
                {
                    if (filledBlocksGroups[i].ContainsKey(block))
                    {
                        blockAlreadyFilled = true;
                        break;
                    }
                }

                if (!blockAlreadyFilled)
                {
                    filledBlocksGroups.Add(BlockUtility.FloodFill(block, coord, Blocks));
                }
            }

            //if there is only one group of filled blocks, just remove the block
            if (filledBlocksGroups.Count <= 1)
            {
                Blocks.Remove(coord);
            }
            else //if there are 2 or more groups of filled blocks, we must delete this object and create 2 or more new objects
            {
                _autoAuthority.ServerStripOwnerAndDestroy();

                foreach (var filledBlocks in filledBlocksGroups)
                {
                    var newBlockObject = Instantiate(StaticPrefabs.instance.blockObjectPrefab, transform.position,
                        transform.rotation);

                    var newBlockObjectScript = newBlockObject.GetComponent<BlockObject>();
                    foreach (var pair in filledBlocks)
                    {
                        newBlockObjectScript.Blocks.Add(pair.Key, pair.Value);
                    }

                    NetworkServer.Spawn(newBlockObject);
                }
            }
        }
    }

    private void RemoveBlockFromMesh(Vector3Int coord)
    {
        if (!_meshBlocks.ContainsKey(coord))
        {
            Debug.LogError("Attempted to remove block which does not exist in _meshBlocks.");
            return;
        }

        int vStart = _meshBlocks[coord].positionInList * 6 * 4;
        int tStart = _meshBlocks[coord].positionInList * 6 * 6;

        int vLastStart = _meshBlocks.Count * 6 * 4 - (6 * 4);
        int tLastStart = _meshBlocks.Count * 6 * 6 - (6 * 6);

        //move the last block mesh into where the block you want to remove is
        for (int i = 0; i < 6 * 6; i++)
        {
            _triangles[tStart + i] = _triangles[tLastStart + i] -
                                     ((_meshBlocks.Count - 1 - _meshBlocks[coord].positionInList) * 6 * 4);
        }

        _triangles.RemoveRange(tLastStart, 6 * 6);

        for (int i = 0; i < 6 * 4; i++)
        {
            _vertices[vStart + i] = _vertices[vLastStart + i];
            _uv[vStart + i] = _uv[vLastStart + i];
            _uv2[vStart + i] = _uv2[vLastStart + i];
        }

        //remove the last block because it has been moved somewhere else
        _vertices.RemoveRange(vLastStart, 6 * 4);
        _uv.RemoveRange(vLastStart, 6 * 4);
        _uv2.RemoveRange(vLastStart, 6 * 4);

        //find the last block. its not a list, so you must loop through
        //this block is no longer the last block, but the positionInList has not been updated yet, so we can find it like this
        int max = -1;
        Vector3Int key = Vector3Int.zero;
        foreach (var entry in _meshBlocks)
        {
            if (entry.Value.positionInList > max)
            {
                max = entry.Value.positionInList;
                key = entry.Key;
            }
        }

        //update the positionInList for the block which moved
        _meshBlocks[key].positionInList = _meshBlocks[coord].positionInList;

        //finally remove the block from the mesh blocks
        _meshBlocks.Remove(coord);
    }

    private void DamageBlock(Vector3Int coord)
    {
        if (!Blocks.ContainsKey(coord) || !_meshBlocks.ContainsKey(coord))
        {
            return;
        }

        var meshBlock = _meshBlocks[coord];

        //if health is already below 0, then we have probably already sent a command to remove the block
        //we don't want to send a duplicate command, so just return
        if (meshBlock.health < 0)
        {
            return;
        }

        const float BreakTime = 0.225f;
        meshBlock.health -= Time.deltaTime / BreakTime;
        meshBlock.framesSinceLastDamage = 0;

        if (meshBlock.health < 0)
        {
            _damagedBlocks.Remove(coord);
            CmdRemoveBlock(coord);

            //client side prediction - remove block locally
            if (_meshBlocks.ContainsKey(coord))
            {
                BlockChanges.Enqueue((coord, new SyncBlock(), BlockDictOp.OP_REMOVE));
            }

            return;
        }

        if (!_damagedBlocks.Contains(coord))
        {
            _damagedBlocks.Add(coord);
        }
    }

    private void UpdateDamagedBlocks()
    {
        var damagedBlocksForShader = new Vector4[10];
        var noLongerDamagedBlocks = new Queue<Vector3Int>();

        int i = 0;
        foreach (var block in _damagedBlocks)
        {
            if (i >= damagedBlocksForShader.Length)
            {
                break;
            }

            if (_meshBlocks.TryGetValue(block, out var meshBlock))
            {
                meshBlock.framesSinceLastDamage++;
                if (meshBlock.framesSinceLastDamage >= 3)
                {
                    meshBlock.health = 1;
                    noLongerDamagedBlocks.Enqueue(block);
                }

                damagedBlocksForShader[i] = new Vector4(block.x, block.y, block.z, meshBlock.health);
                i++;
            }
        }

        while (noLongerDamagedBlocks.Count > 0)
        {
            _damagedBlocks.Remove(noLongerDamagedBlocks.Dequeue());
        }

        blockObjectMaterial.SetVectorArray("damagedBlocks", damagedBlocksForShader);
        blockObjectMaterial.SetInt("numDamagedBlocks", i);
    }

    enum UpdateType
    {
        AddBlock,
        RemoveBlock
    }

    private void UpdateMesh()
    {
        //the order of updating the mesh matters depending on if adding or removing parts of the mesh
        // switch (updateType)
        // {
        //     case UpdateType.AddBlock:
        //         _mesh.vertices = _vertices.ToArray();
        //         _mesh.triangles = _triangles.ToArray();
        //         break;
        //     case UpdateType.RemoveBlock:
        //         _mesh.triangles = _triangles.ToArray();
        //         _mesh.vertices = _vertices.ToArray();
        //         //_mesh.SetVertices()
        //         break;
        // }

        //first set triangles to empty so we can update the vertices without triangles referencing (now) invalid vertices
        _mesh.triangles = new int[0];
        _mesh.vertices = _vertices.ToArray();
        _mesh.triangles = _triangles.ToArray();

        _mesh.uv = _uv.ToArray();
        _mesh.uv2 = _uv2.ToArray();

        _mesh.RecalculateNormals();
        _mesh.RecalculateTangents();
        _mesh.RecalculateBounds();

        // if (blocks.Count == 1)
        // {
        //     if (blocks[OnlyBlock()].blockObject != null)
        //     {
        //         transform.GetChild(5).localPosition = (Vector3) OnlyBlock() * CubeScale;
        //         transform.GetChild(5).gameObject.SetActive(true);
        //     }
        // }

        var (boxCollidersNotNeeded, mass, destroySphere) = BlockUtility.UpdateHitBox(Blocks,
            _boxCollidersInUse,
            _physicsBoxesObject, _sphereObject, blockPhysicMaterial, spherePhysicMaterial);
        while (boxCollidersNotNeeded.Count > 0)
        {
            Destroy(boxCollidersNotNeeded.Dequeue());
        }

        if (destroySphere)
        {
            var s = _sphereObject.gameObject.GetComponent<SphereCollider>();
            if (s)
            {
                Destroy(s);
            }
        }

        _body.mass = mass;
        _resetCoM = true;
    }

    private void CreateInitialMesh()
    {
        foreach (var block in Blocks)
        {
            AddBlockToMesh(block.Value.id, block.Key, BlockUtility.ByteToQuaternion(block.Value.rotation));
        }

        UpdateMesh();
    }

    private Vector3Int GetOnlyBlockCoord()
    {
        if (Blocks.Count != 1)
        {
            Debug.LogError("GetOnlyBlockCoord is only valid when there is only 1 block");
        }

        foreach (var block in Blocks)
        {
            return block.Key;
        }

        return Vector3Int.zero;
    }

    public Quaternion GetTargetRotation(Quaternion blockRotation, Vector3Int coord, Block.BlockType blockType)
    {
        // if (blockType == Block.BlockType.bearing)
        // {
        //     List<Vector3> upCheckAxes = new List<Vector3>();
        //     for (var i = 0; i < 6; i++)
        //     {
        //         Vector3Int testBlock = coord + BlockUtility.Directions[i];
        //         if (blocks.TryGetValue(testBlock, out MeshBlock block))
        //         {
        //             if (Blocks.blocks[block.id].blockType == Block.BlockType.normal)
        //             {
        //                 upCheckAxes.Add(-BlockUtility.Directions[i]);
        //             }
        //         }
        //     }
        //
        //     Quaternion rotationLocalToArea = Quaternion.Inverse(transform.rotation) * blockRotation;
        //     Vector3 up = ClosestToAxis(rotationLocalToArea, Vector3.up, upCheckAxes.ToArray());
        //     Vector3[] forwardCheckAxes = new Vector3[4];
        //     int n = 0;
        //     Vector3Int upInt = Vector3Int.RoundToInt(up);
        //     for (int i = 0; i < directions.Length; i++)
        //     {
        //         if (!(directions[i] == upInt || directions[i] == -upInt))
        //         {
        //             forwardCheckAxes[n] = directions[i];
        //             n++;
        //         }
        //     }
        //
        //     Vector3 forward = ClosestToAxis(rotationLocalToArea, Vector3.forward, forwardCheckAxes);
        //     return transform.rotation * Quaternion.LookRotation(forward, up);
        // }
        // else
        // {
        return transform.rotation *
               BlockUtility.SnapToNearestRightAngle(Quaternion.Inverse(transform.rotation) * blockRotation);
        // }
    }
}