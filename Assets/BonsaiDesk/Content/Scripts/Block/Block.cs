using UnityEngine;

public class Block
{
    public const int xTextures = 9;
    public const float textureWidth = 1f / xTextures;
    public const float breakTextureWidth = 1f / 11f; //hard coded in shader "Block.shader"

    public int topTextureIndex;
    public int sideTextureIndex;
    public int bottomTextureIndex;

    public GameObject blockObject;

    public enum BlockType
    {
        normal,
        bearing
    }

    public BlockType blockType;

    public Block(int textureIndex)
    {
        topTextureIndex = textureIndex;
        sideTextureIndex = textureIndex;
        bottomTextureIndex = textureIndex;
        blockObject = null;
        blockType = BlockType.normal;
    }

    public Block(int topTextureIndex, int sideTextureIndex, int bottomTextureIndex)
    {
        this.topTextureIndex = topTextureIndex;
        this.sideTextureIndex = sideTextureIndex;
        this.bottomTextureIndex = bottomTextureIndex;
        blockObject = null;
        blockType = BlockType.normal;
    }

    public Block(string blockObjectName)
    {
        topTextureIndex = 0;
        sideTextureIndex = 0;
        bottomTextureIndex = 0;
        blockObject = Resources.Load("BlockObjects/" + blockObjectName) as GameObject;
        blockType = BlockType.normal;
    }

    public Block(string blockObjectName, BlockType blockType)
    {
        topTextureIndex = 0;
        sideTextureIndex = 0;
        bottomTextureIndex = 0;
        blockObject = Resources.Load("BlockObjects/" + blockObjectName) as GameObject;
        this.blockType = blockType;
    }
}