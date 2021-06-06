using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class BlockObjectFileReader
{
    public struct BlockObjectFile
    {
        public string fileName;
        public string content;
        public string displayName;

        public override string ToString()
        {
            return $"fileName: {fileName} content: {!string.IsNullOrEmpty(content)} displayName: {displayName}";
        }
    }

    public static BlockObjectFile[] GetBlockObjectFiles()
    {
        var fileNames = ListFiles();
        if (fileNames == null)
        {
            return null;
        }

        var blockObjectFiles = new SortedList<long, List<BlockObjectFile>>();

        var fileName = string.Empty;
        var content = string.Empty;
        var displayName = string.Empty;
        for (int i = 0; i < fileNames.Length; i++)
        {
            var name = fileNames[i];
            if (name.Length < 5) //name must at least have .txt and 1 character
            {
                continue;
            }

            if (string.Compare(name, name.Length - 4, ".txt", 0, 4) != 0) //missing extension
            {
                continue;
            }

            name = name.Substring(0, name.Length - 4); //take off extension
            
            fileName = fileNames[i]; //set these now in case the following parsing fails
            displayName = name;

            long unixTimestamp = 0;

            var dashIndex = name.IndexOf('-');
            if (dashIndex != -1 && dashIndex > 0 && name[dashIndex - 1] == ' ' && dashIndex + 2 < name.Length)
            {
                var unixTimestampString = name.Substring(0, dashIndex - 1);
                if (long.TryParse(unixTimestampString, out var num))
                {
                    unixTimestamp = num;
                }

                displayName = name.Substring(dashIndex + 2, name.Length - (dashIndex + 2));
            }

            var newBlockObjectFile = new BlockObjectFile() {fileName = fileName, content = content, displayName = displayName};
            if (blockObjectFiles.TryGetValue(unixTimestamp, out var list))
            {
                list.Add(newBlockObjectFile);
            }
            else
            {
                var newList = new List<BlockObjectFile>();
                newList.Add(newBlockObjectFile);
                blockObjectFiles.Add(unixTimestamp, newList);
            }
        }

        var outputList = new List<BlockObjectFile>();
        foreach (var pair in blockObjectFiles)
        {
            for (int i = 0; i < pair.Value.Count; i++)
            {
                outputList.Add(pair.Value[i]);
            }
        }

        return outputList.ToArray();
    }

    private static string[] ListFiles()
    {
        try
        {
            var folderPath = System.IO.Path.Combine(Application.persistentDataPath, "blocks");
            System.IO.Directory.CreateDirectory(folderPath);

            var filePaths = Directory.GetFiles(folderPath);
            var fileNames = new string[filePaths.Length];
            for (int i = 0; i < filePaths.Length; i++)
            {
                var fileName = Path.GetFileName(filePaths[i]);
                if (string.IsNullOrEmpty(fileName))
                {
                    return null;
                }

                fileNames[i] = fileName;
            }

            return fileNames;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        return null;
    }

    public static void SaveFile(string fileName, string content, bool prependUnixTimestamp = false)
    {
        try
        {
            var folderPath = System.IO.Path.Combine(Application.persistentDataPath, "blocks");
            System.IO.Directory.CreateDirectory(folderPath);

            var saveName = fileName;
            if (prependUnixTimestamp)
            {
                var now = DateTimeOffset.Now.ToUnixTimeSeconds();
                saveName = now + " - " + saveName;
            }

            var filePath = System.IO.Path.Combine(folderPath, saveName);
            int suffix = 0;
            while (File.Exists(filePath))
            {
                suffix++;
                saveName = fileName + $" ({suffix})";
                filePath = System.IO.Path.Combine(folderPath, saveName);
            }

            filePath += ".txt";

            File.WriteAllText(filePath, content);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    public static string LoadFile(string fileName)
    {
        try
        {
            var folderPath = System.IO.Path.Combine(Application.persistentDataPath, "blocks");
            System.IO.Directory.CreateDirectory(folderPath);

            var filePath = System.IO.Path.Combine(folderPath, fileName);

            if (!File.Exists(filePath))
            {
                Debug.LogError("Could not find file: " + filePath);
                return "";
            }

            return File.ReadAllText(filePath);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        return "";
    }
}