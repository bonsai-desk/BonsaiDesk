using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public static partial class BlockUtility
{
    public static string SerializeBlocksFromRoot(BlockObject rootBlockObject)
    {
        const int blockVersion = 1;
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

    public static BlockObjectStringData DeserializeBlocks(string data)
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
            case 1:
                return DeserializeBlocksVersion1(lines);
            default:
                Debug.LogError("Unknown block version number: " + blockVersionNumberInt);
                break;
        }

        return null;
    }

    public class BlockObjectStringEntry
    {
        public int id;
        public int attachedTo;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public byte jointLocalRotation;
        public byte jointBearingLocalRotation;
        public Vector3Int jointAttachedToMeAtCoord;
        public Vector3Int jointOtherBearingCoord;
        public Queue<KeyValuePair<Vector3Int, SyncBlock>> blocks;
    }

    private static bool TryParseVector3(List<string> lines, ref int atLine, out Vector3 result)
    {
        if (atLine >= lines.Count)
        {
            Debug.LogError("Could not parse Vector3 because atLine is out of range");
            result = Vector3.zero;
            return false;
        }

        var s = lines[atLine];
        atLine++;
        // Remove the parentheses
        if (s.StartsWith("(") && s.EndsWith(")"))
        {
            s = s.Substring(1, s.Length - 2);
        }

        // split the items
        var sArray = s.Split(',');
        if (sArray.Length != 3)
        {
            result = Vector3.zero;
            return false;
        }

        try
        {
            result = new Vector3(float.Parse(sArray[0]), float.Parse(sArray[1]), float.Parse(sArray[2]));
        }
        catch
        {
            result = Vector3.zero;
            return false;
        }

        return true;
    }

    private static bool TryParseVector3Int(List<string> lines, ref int atLine, out Vector3Int result)
    {
        if (atLine >= lines.Count)
        {
            Debug.LogError("Could not parse Vector3 because atLine is out of range");
            result = Vector3Int.zero;
            return false;
        }

        var s = lines[atLine];
        atLine++;
        // Remove the parentheses
        if (s.StartsWith("(") && s.EndsWith(")"))
        {
            s = s.Substring(1, s.Length - 2);
        }

        // split the items
        var sArray = s.Split(',');
        if (sArray.Length != 3)
        {
            result = Vector3Int.zero;
            return false;
        }

        try
        {
            result = new Vector3Int(int.Parse(sArray[0]), int.Parse(sArray[1]), int.Parse(sArray[2]));
        }
        catch
        {
            result = Vector3Int.zero;
            return false;
        }

        return true;
    }

    private static bool TryParseQuaternion(List<string> lines, ref int atLine, out Quaternion result)
    {
        if (atLine >= lines.Count)
        {
            Debug.LogError("Could not parse Quaternion because atLine is out of range");
            result = Quaternion.identity;
            return false;
        }

        var s = lines[atLine];
        atLine++;
        // Remove the parentheses
        if (s.StartsWith("(") && s.EndsWith(")"))
        {
            s = s.Substring(1, s.Length - 2);
        }

        // split the items
        var sArray = s.Split(',');
        if (sArray.Length != 4)
        {
            result = Quaternion.identity;
            return false;
        }

        try
        {
            result = new Quaternion(float.Parse(sArray[0]), float.Parse(sArray[1]), float.Parse(sArray[2]), float.Parse(sArray[3]));
        }
        catch
        {
            result = Quaternion.identity;
            return false;
        }

        return true;
    }

    private static bool TryParseByte(List<string> lines, ref int atLine, out byte result)
    {
        if (atLine >= lines.Count)
        {
            Debug.LogError("Could not parse int because atLine is out of range");
            result = 0;
            return false;
        }

        var s = lines[atLine];
        atLine++;

        return byte.TryParse(s, out result);
    }

    private static bool TryParseInfoLine(List<string> lines, ref int atLine, string heading, out string info)
    {
        if (atLine >= lines.Count)
        {
            Debug.LogError("Could not parse heading: \"" + heading + "\" because atLine is out of range");
            info = "";
            return false;
        }

        var lineString = lines[atLine];
        atLine++;
        if (string.Compare(lineString, 0, heading, 0, heading.Length) != 0)
        {
            Debug.LogError("Could not parse heading: \"" + heading + "\" in lineString: \"" + lineString + "\"");
            info = "";
            return false;
        }

        info = lineString.Substring(heading.Length);
        return true;
    }

    private static bool ReadBlockObjectStringEntry(List<string> lines, ref int atLine, out BlockObjectStringEntry entry)
    {
        entry = new BlockObjectStringEntry();

        if (!TryParseInfoLine(lines, ref atLine, "Id: ", out var idString) || !int.TryParse(idString, out entry.id))
        {
            return false;
        }

        if (!TryParseInfoLine(lines, ref atLine, "Attached to: ", out var attachedToIdString))
        {
            return false;
        }

        if (attachedToIdString == "none")
        {
            entry.attachedTo = -1;
        }
        else
        {
            if (!int.TryParse(attachedToIdString, out entry.attachedTo))
            {
                Debug.LogError("Failed to parse attached to id: " + attachedToIdString);
                return false;
            }

            if (!TryParseVector3(lines, ref atLine, out entry.localPosition))
            {
                return false;
            }

            if (!TryParseQuaternion(lines, ref atLine, out entry.localRotation))
            {
                return false;
            }

            if (!TryParseByte(lines, ref atLine, out entry.jointLocalRotation))
            {
                return false;
            }

            if (!TryParseByte(lines, ref atLine, out entry.jointBearingLocalRotation))
            {
                return false;
            }

            if (!TryParseVector3Int(lines, ref atLine, out entry.jointAttachedToMeAtCoord))
            {
                return false;
            }

            if (!TryParseVector3Int(lines, ref atLine, out entry.jointOtherBearingCoord))
            {
                return false;
            }
        }

        entry.blocks = new Queue<KeyValuePair<Vector3Int, SyncBlock>>();
        try
        {
            for (; atLine < lines.Count; atLine++)
            {
                if (lines[atLine].Substring(0, 4) == "Id: ")
                {
                    break;
                }

                var line = lines[atLine].Split(' ');
                if (line.Length != 5)
                {
                    Debug.LogError("Block line does not have exactly 5 pieces: " + lines[atLine]);
                    return false;
                }

                var coord = new Vector3Int(int.Parse(line[0]), int.Parse(line[1]), int.Parse(line[2]));
                var name = line[3];
                var rotation = byte.Parse(line[4]);
                var pair = new KeyValuePair<Vector3Int, SyncBlock>(coord, new SyncBlock(name, rotation));
                entry.blocks.Enqueue(pair);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to parse block line: " + e);
            return false;
        }

        return true;
    }

    public class BlockObjectStringData
    {
        public Dictionary<int, BlockObjectStringEntry> idToEntry;
        public SortedDictionary<int, List<BlockObjectStringEntry>> entriesByAttachedTo;
    }

    private static BlockObjectStringData DeserializeBlocksVersion1(List<string> lines)
    {
        var idToEntry = new Dictionary<int, BlockObjectStringEntry>();
        var entriesByAttachedTo = new SortedDictionary<int, List<BlockObjectStringEntry>>();

        var atLine = 3;
        while (atLine < lines.Count)
        {
            if (!ReadBlockObjectStringEntry(lines, ref atLine, out var entry))
            {
                Debug.LogError("Failed to parse. Returning null.");
                return null;
            }

            if (idToEntry.ContainsKey(entry.id))
            {
                Debug.LogError("Multiple entries with id: " + entry.id);
                return null;
            }

            idToEntry.Add(entry.id, entry);
            if (entriesByAttachedTo.TryGetValue(entry.attachedTo, out var existingList))
            {
                existingList.Add(entry);
            }
            else
            {
                var newList = new List<BlockObjectStringEntry>();
                newList.Add(entry);
                entriesByAttachedTo.Add(entry.attachedTo, newList);
            }
        }

        var data = new BlockObjectStringData();
        data.idToEntry = idToEntry;
        data.entriesByAttachedTo = entriesByAttachedTo;

        var foundRoot = false;
        foreach (var pair in data.entriesByAttachedTo)
        {
            var list = pair.Value;
            for (int i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                if (entry.attachedTo >= 0)
                {
                    if (!data.idToEntry.ContainsKey(entry.attachedTo))
                    {
                        Debug.LogError($"id {entry.id} is connected to id {entry.attachedTo} which does not exist");
                        return null;
                    }
                }
                else
                {
                    if (foundRoot)
                    {
                        Debug.LogError("multiple root objects");
                        return null;
                    }
                    foundRoot = true;
                }
            }
        }
        
        return data;
    }
}