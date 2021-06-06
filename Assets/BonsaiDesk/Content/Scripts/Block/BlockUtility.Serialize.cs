using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public static partial class BlockUtility
{
    public static string SerializeBlocksFromRoot(BlockObject rootBlockObject)
    {
        const string blockVersion = "0";
        var data = "Block version v" + blockVersion;
        data += "\nFile created by: app version " + NetworkManagerGame.Singleton.FullVersion;
        data += "\nSerialized on date (UTC): " + DateTime.UtcNow;

        var blockObjects = GetBlockObjectsFromRoot(rootBlockObject);
        var blockObjectToIndex = new Dictionary<BlockObject, int>();
        for (int i = 0; i < blockObjects.Count; i++)
        {
            blockObjectToIndex.Add(blockObjects[i], i);
        }

        var processedRoot = false;
        for (int i = 0; i < blockObjects.Count; i++)
        {
            var result = SerializeBlockObject(blockObjects[i], blockObjectToIndex);
            if (!result.isAttached)
            {
                if (processedRoot)
                {
                    return "";
                }
                processedRoot = true;
            }
            data += result.data;
        }

        return data;
    }

    private static (string data, bool isAttached) SerializeBlockObject(BlockObject blockObject, Dictionary<BlockObject, int> blockObjectToIndex)
    {
        var data = "\n\nId: " + blockObjectToIndex[blockObject];
        var isAttached = false;

        var attachedToString = "none";
        if (blockObject.SyncJoint.connected && blockObject.SyncJoint.attachedTo != null && blockObject.SyncJoint.attachedTo.Value)
        {
            isAttached = true;
            var attachedToBlockObject = blockObject.SyncJoint.attachedTo.Value.GetComponent<BlockObject>();
            var attachedToId = blockObjectToIndex[attachedToBlockObject];
            attachedToString = attachedToId.ToString();
        }

        data += "\nAttached to: " + attachedToString;

        if (attachedToString != "none")
        {
            var localPosition = blockObject.SyncJoint.attachedTo.Value.transform.InverseTransformPoint(blockObject.transform.position);
            var localRotation = Quaternion.Inverse(blockObject.SyncJoint.attachedTo.Value.transform.rotation) * blockObject.transform.rotation;

            data += "\n" + localPosition.ToString("F5");
            data += "\n" + localRotation.ToString("F5");

            data += "\n" + blockObject.SyncJoint.localRotation;
            data += "\n" + blockObject.SyncJoint.bearingLocalRotation;
            data += "\n" + blockObject.SyncJoint.attachedToMeAtCoord;
            data += "\n" + blockObject.SyncJoint.otherBearingCoord;
        }

        foreach (var pair in blockObject.Blocks)
        {
            data += $"\n{pair.Key.x} {pair.Key.y} {pair.Key.z} {pair.Value.name} {pair.Value.rotation}";
        }

        return (data, isAttached);
    }

    public static Queue<KeyValuePair<Vector3Int, SyncBlock>> DeserializeBlocks(string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return null;
        }

        var allLines = data.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None);
        var lines = new List<string>();
        for (int i = 0; i < allLines.Length; i++)
        {
            if (!string.IsNullOrEmpty(allLines[i]))
            {
                lines.Add(allLines[i]);
            }
        }

        if (lines.Count <= 0)
        {
            Debug.LogError("Block data missing block version");
            return null;
        }

        var blockVersion = lines[0];
        var validBlockVersion = "Block version v";
        if (string.Compare(blockVersion, 0, validBlockVersion, 0, validBlockVersion.Length) != 0)
        {
            Debug.LogError("Could not parse block version");
            return null;
        }

        var blockVersionNumber = blockVersion.Substring(validBlockVersion.Length);
        if (!int.TryParse(blockVersionNumber, out var blockVersionNumberInt))
        {
            Debug.LogError("Could not parse block version Integer: " + blockVersionNumber);
            return null;
        }

        if (lines.Count <= 1)
        {
            Debug.LogError("Block data missing created by line");
            return null;
        }

        var fileCreatedBy = lines[1];
        var validCreatedBy = "File created by: ";
        if (string.Compare(fileCreatedBy, 0, validCreatedBy, 0, validCreatedBy.Length) != 0)
        {
            Debug.LogError("Could not parse created by line");
            return null;
        }

        var createdBy = fileCreatedBy.Substring(validCreatedBy.Length);

        var fileSerializedOn = lines[2];
        var validSerializedOn = "Serialized on date (UTC): ";
        if (string.Compare(fileSerializedOn, 0, validSerializedOn, 0, validSerializedOn.Length) != 0)
        {
            Debug.LogError("Could not parse serialized on line");
            return null;
        }

        var serializedOn = fileCreatedBy.Substring(validSerializedOn.Length);

        if (lines.Count <= 3)
        {
            Debug.LogError($"Block file with version v{blockVersionNumberInt} created by {createdBy} on {serializedOn} has no data");
            return null;
        }

        switch (blockVersionNumberInt)
        {
            case 0:
                return DeserializeBlocksVersion0(lines);
            default:
                Debug.LogError("Unknown block version number: " + blockVersionNumberInt);
                break;
        }

        return null;
    }

    private static Queue<KeyValuePair<Vector3Int, SyncBlock>> DeserializeBlocksVersion0(List<string> lines)
    {
        var blocks = new Queue<KeyValuePair<Vector3Int, SyncBlock>>();
        try
        {
            for (int i = 3; i < lines.Count; i++)
            {
                var line = lines[i].Split(' ');
                if (line.Length != 5)
                {
                    Debug.LogError("Invalid line");
                    return null;
                }

                var coord = new Vector3Int(int.Parse(line[0]), int.Parse(line[1]), int.Parse(line[2]));
                var name = line[3];
                var rotation = byte.Parse(line[4]);
                var pair = new KeyValuePair<Vector3Int, SyncBlock>(coord, new SyncBlock(name, rotation));
                blocks.Enqueue(pair);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to parse block file");
            Debug.LogError(e);
            return null;
        }

        return blocks;
    }
}