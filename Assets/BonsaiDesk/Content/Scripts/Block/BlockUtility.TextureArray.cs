using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static partial class BlockUtility
{
    //2DArray for block textures. value is calculated once then cached
    private static Texture2DArray _blockTextureArray = null;

    public static Texture2DArray BlockTextureArray
    {
        get
        {
            if (_blockTextureArray == null)
            {
                _blockTextureArray = GenerateBlockTextureArray();
            }

            return _blockTextureArray;
        }
    }

    private static Dictionary<string, int> _blockTextureNameToTextureArrayIndex = null;

    public static Dictionary<string, int> BlockTextureNameToTextureArrayIndex
    {
        get
        {
            if (_blockTextureNameToTextureArrayIndex == null)
            {
                _blockTextureArray = GenerateBlockTextureArray();
            }

            return _blockTextureNameToTextureArrayIndex;
        }
    }

    private static Texture2DArray GenerateBlockTextureArray()
    {
        var objects = Resources.LoadAll("Blocks", typeof(Texture2D));
        if (objects.Length == 0)
        {
            Debug.LogError("Found no textures in Blocks folder");
            return null;
        }

        //convert object array to Texture2D list where the first element is the invalid texture
        var textures = new List<Texture2D>();
        for (int i = 0; i < objects.Length; i++)
        {
            if (String.Compare(objects[i].name, "invalid", StringComparison.Ordinal) == 0)
            {
                textures.Add((Texture2D) objects[i]);
                break;
            }
        }

        for (int i = 0; i < objects.Length; i++)
        {
            if (String.Compare(objects[i].name, "invalid", StringComparison.Ordinal) != 0)
            {
                textures.Add((Texture2D) objects[i]);
            }
        }

        var sampleTexture = (Texture2D) textures[0];

        if (sampleTexture.width != 128 || sampleTexture.height != 128)
        {
            Debug.LogError($"Sample texture {sampleTexture.name} of size {sampleTexture.width}x{sampleTexture.height} should be 128x128");
            return null;
        }

        if (sampleTexture.format != TextureFormat.RGBA32)
        {
            Debug.LogError($"Sample texture {sampleTexture.name} should have format RGBA32");
            return null;
        }

        if (sampleTexture.filterMode != FilterMode.Trilinear)
        {
            Debug.LogError($"Sample texture {sampleTexture.name} should have filter mode trilinear");
            return null;
        }

        var texture2DArray = new Texture2DArray(sampleTexture.width, sampleTexture.height, textures.Count, sampleTexture.format, true);

        texture2DArray.filterMode = sampleTexture.filterMode;
        texture2DArray.wrapMode = sampleTexture.wrapMode;

        _blockTextureNameToTextureArrayIndex = new Dictionary<string, int>();

        for (int i = 0; i < textures.Count; i++)
        {
            var texture = textures[i];
            if (texture.width != sampleTexture.width || texture.height != sampleTexture.height || texture.format != sampleTexture.format ||
                texture.filterMode != sampleTexture.filterMode || texture.wrapMode != sampleTexture.wrapMode)
            {
                Debug.LogError($"Texture {texture.name} does not match parameters of sample texture {sampleTexture.name}");
                return null;
            }

            texture2DArray.SetPixels(texture.GetPixels(), i);
            _blockTextureNameToTextureArrayIndex.Add(texture.name, i);
        }

        texture2DArray.Apply();

        return texture2DArray;
    }
}