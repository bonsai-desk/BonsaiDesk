using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BlockObjectData
{
    public Dictionary<Vector3Int, (byte id, byte rotation)> Blocks;

    public BlockObjectData()
    {
        Blocks = new Dictionary<Vector3Int, (byte id, byte rotation)>();
    }
}