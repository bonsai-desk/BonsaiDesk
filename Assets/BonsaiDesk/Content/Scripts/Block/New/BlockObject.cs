using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BlockObject : MonoBehaviour
{
    public const float CubeScale = 0.05f;

    public Material blockObjectMaterial;

    //contains all the information required to create this block object
    private BlockObjectData _blockObjectData = new BlockObjectData();

    private Dictionary<Vector3Int, MeshBlock> _meshBlocks = new Dictionary<Vector3Int, MeshBlock>();

    //mesh stuff
    private MeshRenderer _meshRenderer;
    private MeshFilter _meshFilter;
    private Mesh _mesh;
    private List<Vector3> _vertices = new List<Vector3>();
    private List<Vector2> _uv = new List<Vector2>();
    private List<Vector2> _uv2 = new List<Vector2>();
    private List<int> _triangles = new List<int>();
    private float _texturePadding = 0f;

    //bounds of all blocks. used to check if new block is close enough to do checks with this block object
    private Vector2Int[] _bounds = {new Vector2Int(), new Vector2Int(), new Vector2Int()};

    private void Start()
    {
        transform.localScale = new Vector3(CubeScale, CubeScale, CubeScale);

        var meshObject = new GameObject("Mesh");
        meshObject.transform.SetParent(transform, false);
        _meshRenderer = meshObject.AddComponent<MeshRenderer>();
        _meshFilter = meshObject.AddComponent<MeshFilter>();

        _meshRenderer.sharedMaterial = blockObjectMaterial;
        _texturePadding = 1f / _meshRenderer.sharedMaterial.mainTexture.width / 2f;

        _mesh = new Mesh();
        _meshFilter.mesh = _mesh;

        _blockObjectData.Blocks.Add(Vector3Int.zero, (0, 0));

        for (int i = 0; i < 100; i++)
        {
            var coord = new Vector3Int(Random.Range(-10, 10), Random.Range(-10, 10), Random.Range(-10, 10));
            if (!_blockObjectData.Blocks.ContainsKey(coord))
            {
                _blockObjectData.Blocks.Add(coord, ((byte)Random.Range(0, 8), 0));
            }
        }

        CalculateInitialBounds();
        
        CreateInitialMesh();
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
        ExpandToFit(coord);

        if (updateTheMesh)
            UpdateMesh(UpdateType.AddBlock);

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

        // UpdateHitBox();
    }

    private void ExpandToFit(Vector3Int blockCoord)
    {
        for (int axis = 0; axis < 3; axis++)
        {
            if (blockCoord[axis] <= _bounds[axis][0])
                _bounds[axis][0] = blockCoord[axis] - 1;
            if (blockCoord[axis] >= _bounds[axis][1])
                _bounds[axis][1] = blockCoord[axis] + 1;
        }
    }

    private void CalculateInitialBounds()
    {
        foreach (var block in _blockObjectData.Blocks)
        {
            ExpandToFit(block.Key);
        }
    }

    private void CreateInitialMesh()
    {
        foreach (var block in _blockObjectData.Blocks)
        {
            AddBlockToMesh(block.Value.id, block.Key, BlockUtility.ByteToQuaternion(block.Value.rotation), false);
        }
        UpdateMesh(UpdateType.AddBlock);
    }
}