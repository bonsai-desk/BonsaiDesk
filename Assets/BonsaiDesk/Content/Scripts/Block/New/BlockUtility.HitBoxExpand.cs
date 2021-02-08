using System.Collections.Generic;
using Mirror;
using UnityEngine;

public static partial class BlockUtility
{
    //left/right
    public static bool expandBoxBoundsRight(Vector3Int center, ref Vector2Int[] boxBounds,
        ref HashSet<Vector3Int> assymilated, ref SyncDictionary<Vector3Int, SyncBlock> blocks)
    {
        int x = center.x + boxBounds[0][1] + 1;

        bool expand = true;
        HashSet<Vector3Int> toBeassymilated = new HashSet<Vector3Int>();
        // for (int x = center.x + boxBounds[0][0]; x <= center.x + boxBounds[0][1]; x++)
        // {
        for (int y = center.y + boxBounds[1][0]; y <= center.y + boxBounds[1][1]; y++)
        {
            for (int z = center.z + boxBounds[2][0]; z <= center.z + boxBounds[2][1]; z++)
            {
                Vector3Int check = new Vector3Int(x, y, z);
                toBeassymilated.Add(check);
                if (!blocks.ContainsKey(check) || assymilated.Contains(check))
                    expand = false;
            }
        }

        // }
        if (expand)
        {
            boxBounds[0][1]++;
            assymilated.UnionWith(toBeassymilated);
        }

        return expand;
    }

    public static bool expandBoxBoundsLeft(Vector3Int center, ref Vector2Int[] boxBounds,
        ref HashSet<Vector3Int> assymilated, ref SyncDictionary<Vector3Int, SyncBlock> blocks)
    {
        int x = center.x + boxBounds[0][0] - 1;

        bool expand = true;
        HashSet<Vector3Int> toBeassymilated = new HashSet<Vector3Int>();
        // for (int x = center.x + boxBounds[0][0]; x <= center.x + boxBounds[0][1]; x++)
        // {
        for (int y = center.y + boxBounds[1][0]; y <= center.y + boxBounds[1][1]; y++)
        {
            for (int z = center.z + boxBounds[2][0]; z <= center.z + boxBounds[2][1]; z++)
            {
                Vector3Int check = new Vector3Int(x, y, z);
                toBeassymilated.Add(check);
                if (!blocks.ContainsKey(check) || assymilated.Contains(check))
                    expand = false;
            }
        }

        // }
        if (expand)
        {
            boxBounds[0][0]--;
            assymilated.UnionWith(toBeassymilated);
        }

        return expand;
    }

    //up/down
    public static bool expandBoxBoundsUp(Vector3Int center, ref Vector2Int[] boxBounds,
        ref HashSet<Vector3Int> assymilated, ref SyncDictionary<Vector3Int, SyncBlock> blocks)
    {
        int y = center.y + boxBounds[1][1] + 1;

        bool expand = true;
        HashSet<Vector3Int> toBeassymilated = new HashSet<Vector3Int>();
        for (int x = center.x + boxBounds[0][0]; x <= center.x + boxBounds[0][1]; x++)
        {
            // for (int y = center.y + boxBounds[1][0]; y <= center.y + boxBounds[1][1]; y++)
            // {
            for (int z = center.z + boxBounds[2][0]; z <= center.z + boxBounds[2][1]; z++)
            {
                Vector3Int check = new Vector3Int(x, y, z);
                toBeassymilated.Add(check);
                if (!blocks.ContainsKey(check) || assymilated.Contains(check))
                    expand = false;
            }

            // }
        }

        if (expand)
        {
            boxBounds[1][1]++;
            assymilated.UnionWith(toBeassymilated);
        }

        return expand;
    }

    public static bool expandBoxBoundsDown(Vector3Int center, ref Vector2Int[] boxBounds,
        ref HashSet<Vector3Int> assymilated, ref SyncDictionary<Vector3Int, SyncBlock> blocks)
    {
        int y = center.y + boxBounds[1][0] - 1;

        bool expand = true;
        HashSet<Vector3Int> toBeassymilated = new HashSet<Vector3Int>();
        for (int x = center.x + boxBounds[0][0]; x <= center.x + boxBounds[0][1]; x++)
        {
            // for (int y = center.y + boxBounds[1][0]; y <= center.y + boxBounds[1][1]; y++)
            // {
            for (int z = center.z + boxBounds[2][0]; z <= center.z + boxBounds[2][1]; z++)
            {
                Vector3Int check = new Vector3Int(x, y, z);
                toBeassymilated.Add(check);
                if (!blocks.ContainsKey(check) || assymilated.Contains(check))
                    expand = false;
            }

            // }
        }

        if (expand)
        {
            boxBounds[1][0]--;
            assymilated.UnionWith(toBeassymilated);
        }

        return expand;
    }

    //forward/backward
    public static bool expandBoxBoundsForward(Vector3Int center, ref Vector2Int[] boxBounds,
        ref HashSet<Vector3Int> assymilated, ref SyncDictionary<Vector3Int, SyncBlock> blocks)
    {
        int z = center.z + boxBounds[2][1] + 1;

        bool expand = true;
        HashSet<Vector3Int> toBeassymilated = new HashSet<Vector3Int>();
        for (int x = center.x + boxBounds[0][0]; x <= center.x + boxBounds[0][1]; x++)
        {
            for (int y = center.y + boxBounds[1][0]; y <= center.y + boxBounds[1][1]; y++)
            {
                // for (int z = center.z + boxBounds[2][0]; z <= center.z + boxBounds[2][1]; z++)
                // {
                Vector3Int check = new Vector3Int(x, y, z);
                toBeassymilated.Add(check);
                if (!blocks.ContainsKey(check) || assymilated.Contains(check))
                    expand = false;
                // }
            }
        }

        if (expand)
        {
            boxBounds[2][1]++;
            assymilated.UnionWith(toBeassymilated);
        }

        return expand;
    }

    public static bool expandBoxBoundsBackward(Vector3Int center, ref Vector2Int[] boxBounds,
        ref HashSet<Vector3Int> assymilated, ref SyncDictionary<Vector3Int, SyncBlock> blocks)
    {
        int z = center.z + boxBounds[2][0] - 1;

        bool expand = true;
        HashSet<Vector3Int> toBeassymilated = new HashSet<Vector3Int>();
        for (int x = center.x + boxBounds[0][0]; x <= center.x + boxBounds[0][1]; x++)
        {
            for (int y = center.y + boxBounds[1][0]; y <= center.y + boxBounds[1][1]; y++)
            {
                // for (int z = center.z + boxBounds[2][0]; z <= center.z + boxBounds[2][1]; z++)
                // {
                Vector3Int check = new Vector3Int(x, y, z);
                toBeassymilated.Add(check);
                if (!blocks.ContainsKey(check) || assymilated.Contains(check))
                    expand = false;
                // }
            }
        }

        if (expand)
        {
            boxBounds[2][0]--;
            assymilated.UnionWith(toBeassymilated);
        }

        return expand;
    }
}