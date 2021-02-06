using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static partial class BlockUtility
{
    /// <summary>
    /// Snaps rotation to nearest rotation where all axes are aligned with world axes.
    /// </summary>
    /// <param name="currentRotation">The rotation to be snapped</param>
    /// <returns>The snapped reotation</returns>
    public static Quaternion SnapToNearestRightAngle(Quaternion currentRotation)
    {
        Vector3 closestToForward = SnappedToNearestAxis(currentRotation * Vector3.forward);
        Vector3 closestToUp = SnappedToNearestAxis(currentRotation * Vector3.up);
        return Quaternion.LookRotation(closestToForward, closestToUp);
    }

    /// <summary>
    /// Find the world axis that is closest to the inputted direction
    /// </summary>
    /// <param name="direction">Vector to be snapped</param>
    /// <returns>World axis, up, left, right, etc.</returns>
    private static Vector3 SnappedToNearestAxis(Vector3 direction)
    {
        float x = Mathf.Abs(direction.x);
        float y = Mathf.Abs(direction.y);
        float z = Mathf.Abs(direction.z);
        if (x > y && x > z)
        {
            return new Vector3(Mathf.Sign(direction.x), 0, 0);
        }
        else if (y > x && y > z)
        {
            return new Vector3(0, Mathf.Sign(direction.y), 0);
        }
        else
        {
            return new Vector3(0, 0, Mathf.Sign(direction.z));
        }
    }

    /// <summary>
    /// Convert a quaternion which already has its axes aligned with world axes to a byte
    /// </summary>
    /// <param name="rotation"></param>
    /// <returns></returns>
    public static byte QuaternionToByte(Quaternion rotation)
    {
        var forward = rotation * Vector3.forward;
        var up = rotation * Vector3.up;

        var forwardRounded = new Vector3Int(Mathf.RoundToInt(forward.x), Mathf.RoundToInt(forward.y),
            Mathf.RoundToInt(forward.z));
        var upRounded = new Vector3Int(Mathf.RoundToInt(up.x), Mathf.RoundToInt(up.y), Mathf.RoundToInt(up.z));

        if (!DirectionToByte.TryGetValue(forwardRounded, out var forwardByte) ||
            !DirectionToByte.TryGetValue(upRounded, out var upByte))
        {
            Debug.LogError("Invalid direction. Maybe snap the angle before using this function?");
            return 0;
        }

        byte rotationByte = upByte;
        rotationByte <<= 4;
        rotationByte |= forwardByte;

        return rotationByte;
    }

    /// <summary>
    /// Convert a byte back to a quaternion. The byte must be in the form generated from QuaternionToByte
    /// </summary>
    /// <param name="rotationByte"></param>
    /// <returns></returns>
    public static Quaternion ByteToQuaternion(byte rotationByte)
    {
        return Quaternion.LookRotation(ByteToDirection[(byte) (rotationByte & 0b_1111)],
            ByteToDirection[(byte) ((rotationByte >> 4) & 0b_1111)]);
    }

    //these two dictionaries allow you to convert a world axis direction to a byte and vice versa for light weight storage
    private static readonly IReadOnlyDictionary<byte, Vector3> ByteToDirection = new Dictionary<byte, Vector3>
    {
        {0, new Vector3(1, 0, 0)},
        {1, new Vector3(-1, 0, 0)},
        {2, new Vector3(0, 1, 0)},
        {3, new Vector3(0, -1, 0)},
        {4, new Vector3(0, 0, 1)},
        {5, new Vector3(0, 0, -1)}
    };

    private static readonly IReadOnlyDictionary<Vector3Int, byte> DirectionToByte = new Dictionary<Vector3Int, byte>
    {
        {new Vector3Int(1, 0, 0), 0},
        {new Vector3Int(-1, 0, 0), 1},
        {new Vector3Int(0, 1, 0), 2},
        {new Vector3Int(0, -1, 0), 3},
        {new Vector3Int(0, 0, 1), 4},
        {new Vector3Int(0, 0, -1), 5}
    };

    //the 6 world axes in an iterable form
    public static readonly Vector3Int[] Directions = new Vector3Int[]
    {
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 0, -1)
    };

    public static (Vector3[] vertices, Vector2[] uv, int[] triangles, Vector2[] uv2) GetBlockMesh(int id,
        Vector3Int coord, Quaternion rotation, float texturePadding)
    {
        if (id < 0)
        {
            id = 0;
            Debug.LogError("Attempted to getBlockMesh with id of " + id);
        }

        Vector3[] vertices = new Vector3[6 * 4];
        Vector2[] uv = new Vector2[6 * 4];
        Vector2[] uv2 = new Vector2[6 * 4];
        int[] triangles = new int[6 * 6];

        Vector2 topBlockuv = GetBlockuv(Blocks.blocks[id].topTextureIndex);
        Vector2 sideBlockuv = GetBlockuv(Blocks.blocks[id].sideTextureIndex);
        Vector2 bottomBlockuv = GetBlockuv(Blocks.blocks[id].bottomTextureIndex);

        for (int face = 0; face < 6; face++)
        {
            for (int v = 0; v < 4; v++)
            {
                vertices[face * 4 + v] = (rotation * _cubeVertices[face * 4 + v]) + coord;
            }

            Vector2 blockuv;

            if (face < 4)
                blockuv = sideBlockuv;
            else if (face < 5)
                blockuv = topBlockuv;
            else
                blockuv = bottomBlockuv;
            uv[face * 4 + 0] = blockuv + new Vector2(texturePadding, texturePadding);
            uv[face * 4 + 1] = blockuv + new Vector2(Block.textureWidth, 0) +
                               new Vector2(-texturePadding, texturePadding);
            uv[face * 4 + 2] = blockuv + new Vector2(Block.textureWidth, Block.textureWidth) +
                               new Vector2(-texturePadding, -texturePadding);
            uv[face * 4 + 3] = blockuv + new Vector2(0, Block.textureWidth) +
                               new Vector2(texturePadding, -texturePadding);

            uv2[face * 4 + 0] = new Vector2(0, 0);
            uv2[face * 4 + 1] = new Vector2(Block.breakTextureWidth, 0);
            uv2[face * 4 + 2] = new Vector2(Block.breakTextureWidth, 1);
            uv2[face * 4 + 3] = new Vector2(0, 1);

            if (Blocks.blocks[id].blockObject == null)
            {
                int v0 = face * 4;
                triangles[face * 6 + 0] = v0 + 0;
                triangles[face * 6 + 1] = v0 + 3;
                triangles[face * 6 + 2] = v0 + 2;
                triangles[face * 6 + 3] = v0 + 0;
                triangles[face * 6 + 4] = v0 + 2;
                triangles[face * 6 + 5] = v0 + 1;
            }
        }

        if (Blocks.blocks[id].blockObject != null)
            for (int i = 0; i < triangles.Length; i++)
                triangles[i] = 0;

        return (vertices, uv, triangles, uv2);
    }

    private static Vector2 GetBlockuv(int textureId)
    {
        int xTexture = textureId % Block.xTextures;
        int yTexture = textureId / Block.xTextures;
        return new Vector2(xTexture * Block.textureWidth, 1 - Block.textureWidth - (yTexture * Block.textureWidth));
    }

    private static readonly Vector3[] _cubeVertices = new Vector3[]
    {
        //front
        new Vector3(-0.5f, -0.5f, -0.5f),
        new Vector3(0.5f, -0.5f, -0.5f),
        new Vector3(0.5f, 0.5f, -0.5f),
        new Vector3(-0.5f, 0.5f, -0.5f),

        //right
        new Vector3(0.5f, -0.5f, -0.5f),
        new Vector3(0.5f, -0.5f, 0.5f),
        new Vector3(0.5f, 0.5f, 0.5f),
        new Vector3(0.5f, 0.5f, -0.5f),

        //back
        new Vector3(0.5f, -0.5f, 0.5f),
        new Vector3(-0.5f, -0.5f, 0.5f),
        new Vector3(-0.5f, 0.5f, 0.5f),
        new Vector3(0.5f, 0.5f, 0.5f),

        //left
        new Vector3(-0.5f, -0.5f, 0.5f),
        new Vector3(-0.5f, -0.5f, -0.5f),
        new Vector3(-0.5f, 0.5f, -0.5f),
        new Vector3(-0.5f, 0.5f, 0.5f),

        //top
        new Vector3(-0.5f, 0.5f, -0.5f),
        new Vector3(0.5f, 0.5f, -0.5f),
        new Vector3(0.5f, 0.5f, 0.5f),
        new Vector3(-0.5f, 0.5f, 0.5f),

        //bottom
        new Vector3(-0.5f, -0.5f, 0.5f),
        new Vector3(0.5f, -0.5f, 0.5f),
        new Vector3(0.5f, -0.5f, -0.5f),
        new Vector3(-0.5f, -0.5f, -0.5f)
    };

    public static (Queue<BoxCollider> boxCollidersNotNeeded, float mass, bool destroySphere) UpdateHitBox(
        Dictionary<Vector3Int, (byte id, byte rotation)> blocks,
        Queue<BoxCollider> boxCollidersInUse, Transform boxesParent, Transform sphereObject,
        PhysicMaterial blockPhysicMaterial, PhysicMaterial spherePhysicMaterial)
    {
        if (blocks.Count < 1)
        {
            Debug.LogError("Cannot update hitbox with no blocks.");
            return (null, 1f, false);
        }

        HashSet<Vector3Int> assymilated = new HashSet<Vector3Int>();
        // if (blocks.Count == 1)
        //     assymilated = new HashSet<Vector3Int>();
        // else
        //     assymilated = new HashSet<Vector3Int>(blockObjects);
        Dictionary<Vector3Int, Vector2Int[]> boxes = new Dictionary<Vector3Int, Vector2Int[]>();
        foreach (var block in blocks)
        {
            if (!assymilated.Contains(block.Key))
            {
                assymilated.Add(block.Key);

                Vector2Int[] boxBounds = {new Vector2Int(), new Vector2Int(), new Vector2Int()};

                bool canSpreadRight = true;
                bool canSpreadLeft = true;
                bool canSpreadUp = true;
                bool canSpreadDown = true;
                bool canSpreadForward = true;
                bool canSpreadBackward = true;

                while (canSpreadRight || canSpreadLeft || canSpreadUp || canSpreadDown || canSpreadForward ||
                       canSpreadBackward)
                {
                    if (canSpreadRight)
                        canSpreadRight = expandBoxBoundsRight(block.Key, ref boxBounds, ref assymilated, ref blocks);
                    if (canSpreadLeft)
                        canSpreadLeft = expandBoxBoundsLeft(block.Key, ref boxBounds, ref assymilated, ref blocks);
                    if (canSpreadUp)
                        canSpreadUp = expandBoxBoundsUp(block.Key, ref boxBounds, ref assymilated, ref blocks);
                    if (canSpreadDown)
                        canSpreadDown = expandBoxBoundsDown(block.Key, ref boxBounds, ref assymilated, ref blocks);
                    if (canSpreadForward)
                        canSpreadForward =
                            expandBoxBoundsForward(block.Key, ref boxBounds, ref assymilated, ref blocks);
                    if (canSpreadBackward)
                        canSpreadBackward =
                            expandBoxBoundsBackward(block.Key, ref boxBounds, ref assymilated, ref blocks);
                }

                boxes.Add(block.Key, boxBounds);
            }
        }

        Queue<BoxCollider> boxCollidersNotNeeded = new Queue<BoxCollider>();
        while (boxCollidersInUse.Count > 0)
        {
            BoxCollider boxCollider = boxCollidersInUse.Dequeue();
            boxCollidersNotNeeded.Enqueue(boxCollider);
        }

        foreach (var box in boxes)
        {
            float xScale = 1f + (1f * (-box.Value[0][0] + box.Value[0][1]));
            float yScale = 1f + (1f * (-box.Value[1][0] + box.Value[1][1]));
            float zScale = 1f + (1f * (-box.Value[2][0] + box.Value[2][1]));

            float xPosition = -(1f / 2f) - (1f * -box.Value[0][0]) + (xScale / 2f);
            float yPosition = -(1f / 2f) - (1f * -box.Value[1][0]) + (yScale / 2f);
            float zPosition = -(1f / 2f) - (1f * -box.Value[2][0]) + (zScale / 2f);

            if (boxCollidersNotNeeded.Count > 0)
            {
                BoxCollider boxCollider = boxCollidersNotNeeded.Dequeue();
                boxCollider.size = new Vector3(xScale, yScale, zScale);
                boxCollider.center = box.Key + new Vector3(xPosition, yPosition, zPosition);
                boxCollidersInUse.Enqueue(boxCollider);
            }
            else
            {
                BoxCollider boxCollider = boxesParent.gameObject.AddComponent<BoxCollider>();
                boxCollider.sharedMaterial = blockPhysicMaterial;
                boxCollider.size = new Vector3(xScale, yScale, zScale);
                boxCollider.center = box.Key + new Vector3(xPosition, yPosition, zPosition);
                boxCollidersInUse.Enqueue(boxCollider);
            }
        }

        bool destroySphere = false;
        if (blocks.Count == 1)
        {
            KeyValuePair<Vector3Int, (byte id, byte rotation)> block;
            foreach (var nextBlock in blocks)
                block = nextBlock;

            var s = sphereObject.gameObject.AddComponent<SphereCollider>();
            s.sharedMaterial = spherePhysicMaterial;
            s.center = block.Key;
            s.radius = 0.475f;
        }
        else
        {
            destroySphere = true;
        }

        float mass = Mathf.Clamp((BlockObject.CubeMass * blocks.Count) - (BlockObject.CubeMass * blocks.Count),
            BlockObject.CubeMass, Mathf.Infinity);
        return (boxCollidersNotNeeded, mass, destroySphere);
    }

    private static bool ContainsBlock(BlockObjectData blockObjectData, Vector3Int testPosition)
    {
        return blockObjectData.Blocks.ContainsKey(testPosition);
    }

    public static (bool isInCubeArea, bool isNearHole) InCubeArea(BlockObjectData blockObjectData,
        Vector3Int testPosition, int id)
    {
        if (ContainsBlock(blockObjectData, testPosition))
        {
            return (false, false);
        }

        //don't @ me
        bool isNearHole =
            (!ContainsBlock(blockObjectData, testPosition + new Vector3Int(1, 0, 0)) &&
             (ContainsBlock(blockObjectData, testPosition + new Vector3Int(1, -1, 0)) &&
              ContainsBlock(blockObjectData, testPosition + new Vector3Int(1, 1, 0)) ||
              ContainsBlock(blockObjectData, testPosition + new Vector3Int(1, 0, -1)) &&
              ContainsBlock(blockObjectData, testPosition + new Vector3Int(1, 0, 1)))) ||
            (!ContainsBlock(blockObjectData, testPosition + new Vector3Int(-1, 0, 0)) &&
             (ContainsBlock(blockObjectData, testPosition + new Vector3Int(-1, -1, 0)) &&
              ContainsBlock(blockObjectData, testPosition + new Vector3Int(-1, 1, 0)) ||
              ContainsBlock(blockObjectData, testPosition + new Vector3Int(-1, 0, -1)) &&
              ContainsBlock(blockObjectData, testPosition + new Vector3Int(-1, 0, 1)))) ||
            (!ContainsBlock(blockObjectData, testPosition + new Vector3Int(0, 1, 0)) &&
             (ContainsBlock(blockObjectData, testPosition + new Vector3Int(-1, 1, 0)) &&
              ContainsBlock(blockObjectData, testPosition + new Vector3Int(1, 1, 0)) ||
              ContainsBlock(blockObjectData, testPosition + new Vector3Int(0, 1, -1)) &&
              ContainsBlock(blockObjectData, testPosition + new Vector3Int(0, 1, 1)))) ||
            (!ContainsBlock(blockObjectData, testPosition + new Vector3Int(0, -1, 0)) &&
             (ContainsBlock(blockObjectData, testPosition + new Vector3Int(-1, -1, 0)) &&
              ContainsBlock(blockObjectData, testPosition + new Vector3Int(1, -1, 0)) ||
              ContainsBlock(blockObjectData, testPosition + new Vector3Int(0, -1, -1)) &&
              ContainsBlock(blockObjectData, testPosition + new Vector3Int(0, -1, 1)))) ||
            (!ContainsBlock(blockObjectData, testPosition + new Vector3Int(0, 0, 1)) &&
             (ContainsBlock(blockObjectData, testPosition + new Vector3Int(-1, 0, 1)) &&
              ContainsBlock(blockObjectData, testPosition + new Vector3Int(1, 0, 1)) ||
              ContainsBlock(blockObjectData, testPosition + new Vector3Int(0, -1, 1)) &&
              ContainsBlock(blockObjectData, testPosition + new Vector3Int(0, 1, 1)))) ||
            (!ContainsBlock(blockObjectData, testPosition + new Vector3Int(0, 0, -1)) &&
             (ContainsBlock(blockObjectData, testPosition + new Vector3Int(-1, 0, -1)) &&
              ContainsBlock(blockObjectData, testPosition + new Vector3Int(1, 0, -1)) ||
              ContainsBlock(blockObjectData, testPosition + new Vector3Int(0, -1, -1)) &&
              ContainsBlock(blockObjectData, testPosition + new Vector3Int(0, 1, -1)))) ||
            (ContainsBlock(blockObjectData, testPosition + new Vector3Int(-1, 0, 0)) &&
             ContainsBlock(blockObjectData, testPosition + new Vector3Int(1, 0, 0)) ||
             ContainsBlock(blockObjectData, testPosition + new Vector3Int(0, -1, 0)) &&
             ContainsBlock(blockObjectData, testPosition + new Vector3Int(0, 1, 0)) ||
             ContainsBlock(blockObjectData, testPosition + new Vector3Int(0, 0, -1)) &&
             ContainsBlock(blockObjectData, testPosition + new Vector3Int(0, 0, 1)));

        bool inCubeAreaBearing = true;
        // if (Blocks.blocks[id].blockType == Block.BlockType.bearing)
        // {
        //     if (blocks.TryGetValue(testPosition, out MeshBlock block))
        //     {
        //         if (Blocks.blocks[block.id].blockType == Block.BlockType.bearing)
        //             inCubeAreaBearing = false;
        //     }
        // }

        bool isInCubeArea = inCubeAreaBearing && (
            ContainsBlock(blockObjectData, testPosition + new Vector3Int(1, 0, 0)) ||
            ContainsBlock(blockObjectData, testPosition + new Vector3Int(-1, 0, 0)) ||
            ContainsBlock(blockObjectData, testPosition + new Vector3Int(0, 1, 0)) ||
            ContainsBlock(blockObjectData, testPosition + new Vector3Int(0, -1, 0)) ||
            ContainsBlock(blockObjectData, testPosition + new Vector3Int(0, 0, 1)) ||
            ContainsBlock(blockObjectData, testPosition + new Vector3Int(0, 0, -1)));

        return (isInCubeArea, isNearHole);
    }

    public static bool AboutEquals(Vector3 v1, Vector3 v2)
    {
        //+ or - 1mm
        float tolerance = 0.001f;
        float distance = (v2.x - v1.x) * (v2.x - v1.x) + (v2.y - v1.y) * (v2.y - v1.y) + (v2.z - v1.z) * (v2.z - v1.z);
        return distance < tolerance * tolerance;
    }

    public static bool AboutEquals(Quaternion q1, Quaternion q2)
    {
        //+ or - 1 degree
        return Mathf.Abs(Quaternion.Dot(q1, q2)) > 0.98888889f;
    }
}