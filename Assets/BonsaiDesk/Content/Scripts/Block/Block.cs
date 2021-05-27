using UnityEngine;

public class Block
{
    // public const int xTextures = 9;
    // public const float textureWidth = 1f / xTextures;
    public const float BreakTextureWidth = 1f / 11f; //hard coded in shader "Block.shader"

    public int topTextureIndex;
    public int sideTextureIndex;
    public int bottomTextureIndex;

    // public GameObject blockObject;

    public enum BlockType
    {
        Normal,
        Bearing
    }

    public BlockType blockType;

    public Block(string topTextureName, string sideTextureName, string bottomTextureName, BlockType blockType)
    {
        topTextureIndex = BlockUtility.BlockTextureNameToTextureArrayIndex[topTextureName];
        sideTextureIndex = BlockUtility.BlockTextureNameToTextureArrayIndex[sideTextureName];
        bottomTextureIndex = BlockUtility.BlockTextureNameToTextureArrayIndex[bottomTextureName];
        this.blockType = blockType;
    }

    public Block(string textureName)
    {
        topTextureIndex = BlockUtility.BlockTextureNameToTextureArrayIndex[textureName];
        sideTextureIndex = topTextureIndex;
        bottomTextureIndex = topTextureIndex;
        blockType = BlockType.Normal;
    }

    // public Block(int textureIndex)
    // {
    //     topTextureIndex = textureIndex;
    //     sideTextureIndex = textureIndex;
    //     bottomTextureIndex = textureIndex;
    //     blockObject = null;
    //     blockType = BlockType.normal;
    // }
    //
    // public Block(int topTextureIndex, int sideTextureIndex, int bottomTextureIndex)
    // {
    //     this.topTextureIndex = topTextureIndex;
    //     this.sideTextureIndex = sideTextureIndex;
    //     this.bottomTextureIndex = bottomTextureIndex;
    //     blockObject = null;
    //     blockType = BlockType.normal;
    // }
    //
    // public Block(string blockObjectName)
    // {
    //     topTextureIndex = 0;
    //     sideTextureIndex = 0;
    //     bottomTextureIndex = 0;
    //     blockObject = Resources.Load("BlockObjects/" + blockObjectName) as GameObject;
    //     blockType = BlockType.normal;
    // }
    //
    // public Block(string blockObjectName, BlockType blockType)
    // {
    //     topTextureIndex = 0;
    //     sideTextureIndex = 0;
    //     bottomTextureIndex = 0;
    //     blockObject = Resources.Load("BlockObjects/" + blockObjectName) as GameObject;
    //     this.blockType = blockType;
    // }
}