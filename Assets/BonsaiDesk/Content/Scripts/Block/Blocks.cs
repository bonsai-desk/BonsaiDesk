using System.Collections.Generic;

public static class Blocks
{
    private static Dictionary<string, Block> _blocks = new Dictionary<string, Block>()
    {
        {"wood1", new Block("wood1")},
        {"wood2", new Block("wood2")},
        {"wood3", new Block("wood3")},
        {"wood4", new Block("wood4")},
        {"wood5", new Block("wood5")},
        {"wood6", new Block("wood6")},
    };

    public static Block GetBlock(string blockName)
    {
        if (_blocks.TryGetValue(blockName, out var block))
        {
            return block;
        }
        return null;
    }
}