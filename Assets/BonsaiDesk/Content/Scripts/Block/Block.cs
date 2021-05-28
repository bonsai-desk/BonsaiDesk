using UnityEngine;

public class Block
{
    public const float BreakTextureWidth = 1f / 11f; //hard coded in shader "Block.shader"

    public readonly int TopTextureIndex;
    public readonly int SideTextureIndex;
    public readonly int BottomTextureIndex;

    public readonly bool AllowRotation;

    public enum BlockType
    {
        Normal,
        SurfaceMounted
    }

    public readonly BlockType blockType;

    public Block(string topTextureName, string sideTextureName, string bottomTextureName, BlockType blockType, bool allowRotation)
    {
        TopTextureIndex = BlockUtility.BlockTextureNameToTextureArrayIndex[topTextureName];
        SideTextureIndex = BlockUtility.BlockTextureNameToTextureArrayIndex[sideTextureName];
        BottomTextureIndex = BlockUtility.BlockTextureNameToTextureArrayIndex[bottomTextureName];
        this.blockType = blockType;
        AllowRotation = allowRotation;
    }

    public Block(string textureName, bool allowRotation = true)
    {
        TopTextureIndex = BlockUtility.BlockTextureNameToTextureArrayIndex[textureName];
        SideTextureIndex = TopTextureIndex;
        BottomTextureIndex = TopTextureIndex;
        blockType = BlockType.Normal;
        AllowRotation = allowRotation;
    }
    
    public Block(BlockType blockType)
    {
        if (blockType != BlockType.SurfaceMounted)
        {
            Debug.LogError("This constructor is only valid for surface mounted blocks (currently only bearings)");
        }
        
        this.blockType = blockType;
        AllowRotation = true;
    }
}