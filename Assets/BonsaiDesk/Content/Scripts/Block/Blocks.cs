using System.Collections.Generic;

public static class Blocks
{
    // public static readonly Dictionary<int, Block> blocks = new Dictionary<int, Block> {
    //     {0, new Block(0, 3, 2)},
    //     {1, new Block(2)},
    //     {2, new Block(1)},
    //     {3, new Block(4)},
    // };

    // public static readonly Block[] blocks = new Block[]
    // {
    //     // new Block(0, 3, 2),         //0, grass
    //     // new Block(2),               //1, dirt
    //     // new Block(1),               //2, stone
    //     // new Block(4),               //3, wood
    //     // new Block(6, 5, 6),         //4, slab stack
    //     // new Block(7),               //5, bricks
    //     // new Block(9, 8, 10),        //6, tnt
    //     // new Block(16),              //7, cobblestone
    //     new Block(24),
    //     new Block(1),
    //     new Block(2),
    //     new Block(3),
    //     new Block(4),
    //     new Block(10, 9, 11),
    //     new Block(6),
    //     new Block(7),
    //
    //     new Block("Bearing", Block.BlockType.bearing), //8, bearing
    //
    //     // new Block(17),
    //     // new Block(18),
    //     // new Block(19),
    //     // new Block(21, 20, 21),
    //     // new Block(22),
    //     // new Block(23),
    //     // new Block(24),
    //     // new Block(32),
    //     // new Block(33),
    //     // new Block(34),
    //     // new Block(4, 35, 4),
    //     // new Block(36),
    //     // new Block(37),
    //     // new Block(48),
    //     // new Block(50),
    //     // new Block(51),
    //     // new Block(64),
    //     new Block("Axle"),
    // };

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