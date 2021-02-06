using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

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

    //contains all of the BlockObjects in the scene
    private static HashSet<AutoAuthority> _blockObjectAuthorities = new HashSet<AutoAuthority>();

    public readonly SyncDictionary<Vector3Int, SyncBlock> Blocks = new SyncDictionary<Vector3Int, SyncBlock>();

    //public inspector variables
    public Material blockObjectMaterial;
    public PhysicMaterial blockPhysicMaterial;
    public PhysicMaterial spherePhysicMaterial;

    //contains all the information required to create this block object
    // [HideInInspector] public BlockObjectData blockObjectData = new BlockObjectData();

    //contains the information about the local state of the mesh. The structure of the mesh can be slightly
    //different depending on the order of block add/remove even though the final result looks the same
    private Dictionary<Vector3Int, MeshBlock> _meshBlocks = new Dictionary<Vector3Int, MeshBlock>();

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

    private void Start()
    {
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

        Blocks.Add(Vector3Int.zero, new SyncBlock(0, BlockUtility.QuaternionToByte(Quaternion.identity)));

        CreateInitialMesh();
    }

    private void FixedUpdate()
    {
        PhysicsFixedUpdate();

        if (_resetCoM)
        {
            _resetCoM = false;
            _body.ResetInertiaTensor();
            _body.ResetCenterOfMass();
        }
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

    private void AddBlock(byte id, Vector3Int coord, Quaternion rotation, bool updateTheMesh)
    {
        Blocks.Add(coord, new SyncBlock(id, BlockUtility.QuaternionToByte(rotation)));
        AddBlockToMesh(id, coord, rotation, updateTheMesh);
    }

    private void AddBlockToMesh(byte id, Vector3Int coord, Quaternion rotation, bool updateTheMesh)
    {
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

        if (updateTheMesh)
        {
            UpdateMesh(UpdateType.AddBlock);
        }

        // if (blocks.Count == 1)
        //     blockPhysics.enabled = true;
        // else
        //     blockPhysics.enabled = false;
    }

    enum UpdateType
    {
        AddBlock,
        RemoveBlock
    }

    private void UpdateMesh(UpdateType updateType)
    {
        //the order of updating the mesh matters depending on if adding or removing parts of the mesh
        switch (updateType)
        {
            case UpdateType.AddBlock:
                _mesh.vertices = _vertices.ToArray();
                _mesh.triangles = _triangles.ToArray();
                break;
            case UpdateType.RemoveBlock:
                _mesh.triangles = _triangles.ToArray();
                _mesh.vertices = _vertices.ToArray();
                break;
        }

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
            AddBlockToMesh(block.Value.id, block.Key, BlockUtility.ByteToQuaternion(block.Value.rotation), false);
        }

        UpdateMesh(UpdateType.AddBlock);
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