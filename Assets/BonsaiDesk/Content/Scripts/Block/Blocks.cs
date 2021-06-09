using System.Collections.Generic;
using UnityEngine;

public static class Blocks
{
    private static Dictionary<string, Block> _blocks = new Dictionary<string, Block>()
    {
        {"invalid", new Block("invalid")},
        {"wood1", new Block("wood1")},
        {"wood2", new Block("wood2")},
        {"wood3", new Block("wood3")},
        {"wood4", new Block("wood4", false)},
        {"wood5", new Block("wood6", false)},
        {"bearing", new Block(Block.BlockType.SurfaceMounted, "Bearing")},
    };

    public static Block GetBlock(string blockName)
    {
        if (_blocks.TryGetValue(blockName, out var block))
        {
            return block;
        }

        if (_blocks.TryGetValue("invalid", out block))
        {
            Debug.LogError($"Could not find block {blockName}. Returning invalid block.");
            return block;
        }

        Debug.LogError($"Could not find block {blockName} and could not find invalid block. Returning null.");
        return null;
    }
}