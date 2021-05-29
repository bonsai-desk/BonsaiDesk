﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mirror;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;
using BlockDictOp = Mirror.SyncDictionary<UnityEngine.Vector3Int, SyncBlock>.Operation;

public struct SyncBlock
{
    public string name;
    public byte rotation;

    public SyncBlock(string name, byte rotation)
    {
        this.name = name;
        this.rotation = rotation;
    }
}

[RequireComponent(typeof(Rigidbody))]
public partial class BlockObject : NetworkBehaviour
{
    public const float CubeScale = 0.05f;

    //contains all of the AutoAuthority of all BlockObjects in the scene
    private static HashSet<AutoAuthority> _blockObjectAuthorities = new HashSet<AutoAuthority>();

    //all of the data required to reconstruct this block object
    public readonly SyncDictionary<Vector3Int, SyncBlock> Blocks = new SyncDictionary<Vector3Int, SyncBlock>();

    //caches changes made to the sync dictionary so all block changes can be made at once with a single mesh update
    public readonly Queue<(Vector3Int coord, SyncBlock syncBlock, BlockDictOp op)> BlockChanges =
        new Queue<(Vector3Int coord, SyncBlock syncBlock, BlockDictOp op)>();

    //public inspector variables
    public Material blockObjectMaterial;
    public PhysicMaterial blockPhysicMaterial;
    public PhysicMaterial spherePhysicMaterial;
    public GameObject saveDialogPrefab;
    public GameObject breakWholeDialogPrefab;

    //contains the information about the local state of the mesh. The structure of the mesh can be slightly
    //different depending on the order of block add/remove even though the final result looks the same
    private readonly Dictionary<Vector3Int, MeshBlock> _meshBlocks = new Dictionary<Vector3Int, MeshBlock>();

    //contains the keys for entries in _meshBlocks which have blockGameObjects. This saves having to loop through
    //the entire _meshBlocks to check for blockGameObject
    private readonly HashSet<Vector3Int> _blockGameObjects = new HashSet<Vector3Int>();

    //contains the keys for entries in _meshBlocks which are damages. This saves having to loop through
    //the entire _meshBlocks to check for damaged blocks
    private readonly HashSet<Vector3Int> _damagedBlocks = new HashSet<Vector3Int>();

    public class WholeEffectMode
    {
        public float progress;
        public int framesSinceLastDamage;
        public BlockBreakHand.BreakMode mode;
        public bool activated;

        public WholeEffectMode(BlockBreakHand.BreakMode mode)
        {
            progress = 0;
            framesSinceLastDamage = 100;
            this.mode = mode;
            activated = false;
        }
    }

    //used to keep track of any full block effect such as duplicate or delete all
    private WholeEffectMode _activeWholeEffect = null;

    //stores any active dialog. otherwise is null
    private GameObject _activeDialog;
    private Vector3 _dialogLocalPositionRoot;

    //mesh stuff
    private MeshFilter _meshFilter;
    private Mesh _mesh;
    private List<Vector3> _vertices = new List<Vector3>();
    private List<Vector3> _uv = new List<Vector3>();
    private List<Vector2> _uv2 = new List<Vector2>();
    private List<int> _triangles = new List<int>();
    private float _texturePadding = 0f;

    //holds blockGameObjects
    private Transform _blockGameObjectsParent;

    //is active if there is only one block and it has a blockGameObject
    private GameObject _transparentCube;
    public GameObject transparentCubePrefab;

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

    //if this string contains a valid block file string when OnStartServer is called, it will be used to construct the block object
    [TextArea]
    public string initialBlocksString;

    //for testing purposes
    public bool debug = false;

    public override void OnStartServer()
    {
        var blocks = BlockUtility.DeserializeBlocks(initialBlocksString);
        if (blocks != null)
        {
            while (blocks.Count > 0)
            {
                Blocks.Add(blocks.Dequeue());
            }
        }

        if (Blocks.Count <= 0)
        {
            Debug.LogError("Cannot have block object with no blocks");
            NetworkServer.Destroy(gameObject);
            return;
        }

        Init();
    }

    public override void OnStartClient()
    {
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
        blockObjectMaterial.SetTexture("_TextureArray", BlockUtility.BlockTextureArray);

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
        _mesh.MarkDynamic();
        _meshFilter.mesh = _mesh;

        CreateInitialMesh();
        Blocks.Callback -= OnBlocksDictionaryChange;
        Blocks.Callback += OnBlocksDictionaryChange;
    }

    private void Update()
    {
        ProcessBlockChanges();
        UpdateDamagedBlocks();
        UpdateWholeEffects();
        UpdateDialogPosition();

        if (debug && Input.GetKeyDown(KeyCode.S))
        {
            Save();
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
        CloseDialog();

        if (_blockObjectAuthorities.Contains(_autoAuthority))
        {
            _blockObjectAuthorities.Remove(_autoAuthority);
        }

        Destroy(blockObjectMaterial); //auto authority will also destroy this material

        foreach (var coord in _blockGameObjects)
        {
            Destroy(_meshBlocks[coord].material);
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

        potentialBlocksParent = new GameObject("PotentialBlocks").transform;
        const float inverseScale = 1f / CubeScale;
        potentialBlocksParent.localScale = new Vector3(inverseScale, inverseScale, inverseScale);
        potentialBlocksParent.SetParent(transform, false);

        var sphereObject = new GameObject("Sphere");
        _sphereObject = sphereObject.transform;
        sphereObject.transform.SetParent(transform, false);
        sphereObject.layer = LayerMask.NameToLayer("sphere");

        _blockGameObjectsParent = new GameObject("BlockGameObjects").transform;
        _blockGameObjectsParent.SetParent(transform, false);

        _transparentCube = Instantiate(transparentCubePrefab, transform);
        _transparentCube.name = "TransparentCube";
        _transparentCube.SetActive(false);
    }

    private void OnBlocksDictionaryChange(BlockDictOp op, Vector3Int key, SyncBlock value)
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
                        AddBlockToMesh(syncBlock.name, coord, BlockUtility.ByteToQuaternion(syncBlock.rotation));
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
    private void CmdAddBlock(string blockName, Vector3Int coord, Quaternion rotation, NetworkIdentity blockToDestroy)
    {
        blockToDestroy.GetComponent<AutoAuthority>().ServerStripOwnerAndDestroy();

        if (Blocks.ContainsKey(coord))
        {
            Debug.LogError("Command: Attempted to add block which already exists");
            return;
        }

        Blocks.Add(coord, new SyncBlock(blockName, BlockUtility.QuaternionToByte(rotation)));
    }

    private void AddBlockToMesh(string blockName, Vector3Int coord, Quaternion rotation)
    {
        if (_meshBlocks.ContainsKey(coord))
        {
            Debug.LogError("Attempted to add block to mesh which already exists in _meshBlocks");
            return;
        }

        var blockMesh = BlockUtility.GetBlockMesh(blockName, coord, rotation, _texturePadding);
        _vertices.AddRange(blockMesh.vertices);
        _uv.AddRange(blockMesh.uv);
        _uv2.AddRange(blockMesh.uv2);
        for (int i = 0; i < blockMesh.triangles.Length; i++)
        {
            blockMesh.triangles[i] += _vertices.Count - (6 * 4);
        }

        _triangles.AddRange(blockMesh.triangles);

        GameObject blockGameObject = null;
        Material blockGameObjectMaterial = null;
        Block block = global::Blocks.GetBlock(blockName);
        if (block.blockGameObjectPrefab)
        {
            blockGameObject = Instantiate(block.blockGameObjectPrefab, _blockGameObjectsParent);
            blockGameObject.transform.localPosition = coord;
            blockGameObject.transform.localRotation = rotation;
            _blockGameObjects.Add(coord);

            blockGameObjectMaterial = blockGameObject.GetComponentInChildren<MeshRenderer>().material;
        }

        _meshBlocks.Add(coord, new MeshBlock(_meshBlocks.Count, blockGameObject, blockGameObjectMaterial));
    }

    [Command(ignoreAuthority = true)]
    private void CmdRemoveBlock(Vector3Int coord, uint identityId)
    {
        if (!Blocks.ContainsKey(coord))
        {
            Debug.LogError("Command: Attempted to remove block which does not exist in Blocks.");
            return;
        }

        if (Blocks.Count <= 1)
        {
            _autoAuthority.ServerStripOwnerAndDestroy();
            return;
        }

        //flood fill blocks surrounding the block which will be removed so we can know if removing this block
        //should result in the block object splitting into 2 or more block objects
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
        else //if there are 2 or more groups of filled blocks, we must split this object into 2 or more objects
        {
            //find the largest block group
            int indexOfLargest = 0;
            for (int i = 1; i < filledBlocksGroups.Count; i++)
            {
                if (filledBlocksGroups[i].Count > filledBlocksGroups[indexOfLargest].Count)
                {
                    indexOfLargest = i;
                }
            }

            //remove largest block group from the list
            var largestGroup = filledBlocksGroups[indexOfLargest];
            filledBlocksGroups.RemoveAt(indexOfLargest);

            //every block that is not a part of the largest group will be removed and spawned as a new block
            //this way the largest group of blocks can be reused instead of regenerated
            Blocks.Remove(coord);
            foreach (var filledBlocks in filledBlocksGroups)
            {
                foreach (var pair in filledBlocks)
                {
                    Blocks.Remove(pair.Key);
                }
            }

            //immediately remove the blocks so the newly spawned blocks to not clip
            //this will only happen for the server/host, so hopefully it does not glitch on the client
            ProcessBlockChanges();

            //generate the new block objects from the remaining blocks groups
            foreach (var filledBlocks in filledBlocksGroups)
            {
                var newBlockObject = Instantiate(StaticPrefabs.instance.blockObjectPrefab, transform.position, transform.rotation);

                var newBlockObjectScript = newBlockObject.GetComponent<BlockObject>();
                foreach (var pair in filledBlocks)
                {
                    newBlockObjectScript.Blocks.Add(pair.Key, pair.Value);
                }

                NetworkServer.Spawn(newBlockObject);
                newBlockObject.GetComponent<AutoAuthority>().ServerForceNewOwner(identityId, NetworkTime.time, false);
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
            _triangles[tStart + i] = _triangles[tLastStart + i] - ((_meshBlocks.Count - 1 - _meshBlocks[coord].positionInList) * 6 * 4);
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

        if (_meshBlocks[coord].blockGameObject)
        {
            Destroy(_meshBlocks[coord].material);
            Destroy(_meshBlocks[coord].blockGameObject);
            _blockGameObjects.Remove(coord);
        }

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

    private void IncrementWholeEffect(BlockBreakHand.BreakMode mode, Vector3 contactPoint)
    {
        if (_activeWholeEffect == null)
        {
            _activeWholeEffect = new WholeEffectMode(mode);
        }

        if (_activeWholeEffect.mode != mode)
        {
            return;
        }

        const float ActiveTime = 1f;
        _activeWholeEffect.progress += Time.deltaTime / ActiveTime;
        _activeWholeEffect.framesSinceLastDamage = 0;

        if (_activeWholeEffect.progress > 1 && !_activeWholeEffect.activated)
        {
            switch (_activeWholeEffect.mode)
            {
                case BlockBreakHand.BreakMode.Whole:
                    if (_activeDialog)
                    {
                        Destroy(_activeDialog);
                        _activeDialog = null;
                    }

                    _dialogLocalPositionRoot = transform.InverseTransformPoint(contactPoint);
                    _activeDialog = Instantiate(breakWholeDialogPrefab);
                    UpdateDialogPosition();
                    _activeDialog.transform.GetChild(0).GetComponent<HoverButton>().action.AddListener(CloseDialog);
                    _activeDialog.transform.GetChild(1).GetComponent<HoverButton>().action.AddListener(Delete);
                    break;
                case BlockBreakHand.BreakMode.Duplicate:
                    if (NetworkClient.connection != null && NetworkClient.connection.identity)
                    {
                        CmdDuplicate(NetworkClient.connection.identity.netId);
                    }

                    break;
                case BlockBreakHand.BreakMode.Save:
                    if (_activeDialog)
                    {
                        Destroy(_activeDialog);
                        _activeDialog = null;
                    }

                    _dialogLocalPositionRoot = transform.InverseTransformPoint(contactPoint);
                    _activeDialog = Instantiate(saveDialogPrefab);
                    UpdateDialogPosition();
                    _activeDialog.transform.GetChild(0).GetComponent<HoverButton>().action.AddListener(CloseDialog);
                    _activeDialog.transform.GetChild(1).GetComponent<HoverButton>().action.AddListener(Save);

                    break;
                default:
                    Debug.LogError("Unknown mode: " + _activeWholeEffect.mode);
                    break;
            }

            _activeWholeEffect.activated = true;
        }
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

            var nId = uint.MaxValue;
            if (NetworkClient.connection != null && NetworkClient.connection.identity)
            {
                nId = NetworkClient.connection.identity.netId;
            }

            CmdRemoveBlock(coord, nId);

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

    private void UpdateWholeEffects()
    {
        blockObjectMaterial.SetFloat("_EffectProgress", 0);
        blockObjectMaterial.SetColor("_EffectColor", Color.red);
        foreach (var coord in _blockGameObjects)
        {
            _meshBlocks[coord].material.SetFloat("_EffectProgress", 1);
            _meshBlocks[coord].material.SetColor("_EffectColor", Color.red);
        }

        if (_activeWholeEffect == null)
        {
            return;
        }

        _activeWholeEffect.framesSinceLastDamage++;
        if (_activeWholeEffect.framesSinceLastDamage >= 3)
        {
            _activeWholeEffect.progress = 0;
            _activeWholeEffect = null;
            return;
        }

        var color = Color.red;
        switch (_activeWholeEffect.mode)
        {
            case BlockBreakHand.BreakMode.Whole:
                color = Color.red;
                break;
            case BlockBreakHand.BreakMode.Duplicate:
                color = Color.blue;
                break;
            case BlockBreakHand.BreakMode.Save:
                color = Color.yellow;
                break;
            default:
                Debug.LogError("Unknown case: " + _activeWholeEffect.mode);
                break;
        }
        
        blockObjectMaterial.SetFloat("_EffectProgress", Mathf.Clamp01(_activeWholeEffect.progress));
        blockObjectMaterial.SetColor("_EffectColor", color);
        foreach (var coord in _blockGameObjects)
        {
            _meshBlocks[coord].material.SetFloat("_EffectProgress", 1 - Mathf.Clamp01(_activeWholeEffect.progress));
            _meshBlocks[coord].material.SetColor("_EffectColor", color);
        }
    }

    private void UpdateDamagedBlocks()
    {
        var damagedBlocksForShader = new Vector4[10];
        var noLongerDamagedBlocks = new Queue<Vector3Int>();

        int i = 0;
        foreach (var block in _damagedBlocks)
        {
            if (_meshBlocks.TryGetValue(block, out var meshBlock))
            {
                meshBlock.framesSinceLastDamage++;
                if (meshBlock.framesSinceLastDamage >= 3)
                {
                    meshBlock.health = 1;
                    noLongerDamagedBlocks.Enqueue(block);
                }

                if (meshBlock.material)
                {
                    meshBlock.material.SetFloat("_Health", meshBlock.health);
                }
                else
                {
                    if (i < damagedBlocksForShader.Length)
                    {
                        damagedBlocksForShader[i] = new Vector4(block.x, block.y, block.z, meshBlock.health);
                        i++;
                    }
                }
            }
        }

        while (noLongerDamagedBlocks.Count > 0)
        {
            _damagedBlocks.Remove(noLongerDamagedBlocks.Dequeue());
        }

        blockObjectMaterial.SetVectorArray("damagedBlocks", damagedBlocksForShader);
        blockObjectMaterial.SetInt("numDamagedBlocks", i);
    }

    private void UpdateMesh()
    {
        //first set triangles to empty so we can update the vertices without triangles referencing (now) invalid vertices
        _mesh.triangles = new int[0];
        _mesh.vertices = _vertices.ToArray();
        _mesh.triangles = _triangles.ToArray();

        _mesh.SetUVs(0, _uv);
        _mesh.SetUVs(1, _uv2);

        _mesh.RecalculateNormals();
        _mesh.RecalculateTangents();
        _mesh.RecalculateBounds();

        var (boxCollidersNotNeeded, mass, destroySphere) = BlockUtility.UpdateHitBox(Blocks, _boxCollidersInUse, _physicsBoxesObject, _sphereObject,
            blockPhysicMaterial, spherePhysicMaterial);
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

        if (_meshBlocks.Count == 1 && GetOnlyBlock().blockGameObjectPrefab)
        {
            _transparentCube.transform.localPosition = GetOnlyBlockCoord();
            _transparentCube.SetActive(true);
        }
        else
        {
            _transparentCube.SetActive(false);
        }

        _body.mass = mass;
        _resetCoM = true;

        //if any blocks are added or removed, close the save dialog if it is up
        CloseDialog();
    }

    private void CreateInitialMesh()
    {
        foreach (var block in Blocks)
        {
            AddBlockToMesh(block.Value.name, block.Key, BlockUtility.ByteToQuaternion(block.Value.rotation));
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

    private Block GetOnlyBlock()
    {
        if (Blocks.Count != 1)
        {
            Debug.LogError("GetOnlyBlock is only valid when there is only 1 block");
        }

        return global::Blocks.GetBlock(Blocks[GetOnlyBlockCoord()].name);
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
        return transform.rotation * BlockUtility.SnapToNearestRightAngle(Quaternion.Inverse(transform.rotation) * blockRotation);
        // }
    }

    [Command]
    private void CmdDuplicate(uint ownerId)
    {
        var blockObjectGameObject = Instantiate(StaticPrefabs.instance.blockObjectPrefab, new Vector3(0, 1.5f, 0), Quaternion.identity);
        var blockObject = blockObjectGameObject.GetComponent<BlockObject>();
        foreach (var pair in Blocks)
        {
            blockObject.Blocks.Add(pair);
        }

        NetworkServer.Spawn(blockObjectGameObject);
        blockObjectGameObject.GetComponent<AutoAuthority>().ServerForceNewOwner(ownerId, NetworkTime.time, false);
    }

    private void UpdateDialogPosition()
    {
        if (!_activeDialog)
        {
            return;
        }

        _activeDialog.transform.position = transform.TransformPoint(_dialogLocalPositionRoot) + new Vector3(0, 0.1f, 0);

        var forwardFlat = _activeDialog.transform.position - InputManager.Hands.head.position;
        forwardFlat.y = 0;
        _activeDialog.transform.rotation = Quaternion.LookRotation(forwardFlat);
    }

    private void CloseDialog()
    {
        if (_activeDialog)
        {
            Destroy(_activeDialog);
            _activeDialog = null;
        }
    }

    private void Save()
    {
        CloseDialog();
        print(BlockUtility.SerializeBlocks(Blocks));
    }

    private void Delete()
    {
        CloseDialog();
        _autoAuthority.CmdDestroy();
    }
}