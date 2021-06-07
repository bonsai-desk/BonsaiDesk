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
        public string FileName;
        public string Content;
        public string DisplayName;

        public override string ToString()
        {
            return $"fileName: {FileName} content: {!string.IsNullOrEmpty(Content)} displayName: {DisplayName}";
        }
    }
    
    class DescComparer<T> : IComparer<T>
    {
        public int Compare(T x, T y)
        {
            if(x == null) return -1;
            if(y == null) return 1;
            return Comparer<T>.Default.Compare(y, x);
        }
    }

    public static BlockObjectFile[] GetBlockObjectFiles()
    {
        var fileNames = ListFiles();
        if (fileNames == null)
        {
            return null;
        }

        var blockObjectFiles = new SortedList<long, List<BlockObjectFile>>(new DescComparer<long>());

        var fileName = string.Empty;
        var displayName = string.Empty;
        for (int i = 0; i < fileNames.Length; i++)
        {
            if (!TryParseFileName(fileNames[i], out var newBlockObjectFile, out var unixTimestamp))
            {
                return null;
            }

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

    private static bool TryParseFileName(string inputFileName, out BlockObjectFile blockObjectFile, out long unixTimestamp)
    {
        var fileName = string.Empty;
        var displayName = string.Empty;
        unixTimestamp = long.MaxValue;

        var name = inputFileName;
        if (name.Length < 5) //name must at least have .txt and 1 character
        {
            blockObjectFile = new BlockObjectFile();
            return false;
        }

        if (string.Compare(name, name.Length - 4, ".txt", 0, 4) != 0) //missing extension
        {
            blockObjectFile = new BlockObjectFile();
            return false;
        }

        name = name.Substring(0, name.Length - 4); //take off extension

        fileName = inputFileName; //set these now in case the following parsing fails
        displayName = name;

        var dashIndex = name.IndexOf('-');
        if (dashIndex != -1 && dashIndex + 1 < name.Length)
        {
            var unixTimestampString = name.Substring(0, dashIndex);
            if (long.TryParse(unixTimestampString, out var num))
            {
                unixTimestamp = num;
            }

            displayName = name.Substring(dashIndex + 1, name.Length - (dashIndex + 1));
        }

        blockObjectFile = new BlockObjectFile() {FileName = fileName, Content = string.Empty, DisplayName = displayName};
        return true;
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

    public static bool SaveStagedBlockObject(string displayName)
    {
        if (string.IsNullOrEmpty(BlockObject.StagedSaveData))
        {
            return false;
        }

        var success = SaveFile(displayName, BlockObject.StagedSaveData, true);
        BlockObject.StagedSaveData = string.Empty;
        return success;
    }

    public static bool DeleteFile(string fileName)
    {
        try
        {
            var folderPath = System.IO.Path.Combine(Application.persistentDataPath, "blocks");
            System.IO.Directory.CreateDirectory(folderPath);

            var filePath = System.IO.Path.Combine(folderPath, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        return false;
    }

    public static bool SaveFile(string fileName, string content, bool prependUnixTimestamp = false)
    {
        try
        {
            var folderPath = System.IO.Path.Combine(Application.persistentDataPath, "blocks");
            System.IO.Directory.CreateDirectory(folderPath);

            var saveName = fileName;
            if (prependUnixTimestamp)
            {
                var now = DateTimeOffset.Now.ToUnixTimeSeconds();
                saveName = now + "-" + saveName;
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

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        return false;
    }

    public static BlockObjectFile LoadFileIntoBlockObjectFile(BlockObjectFile blockObjectFile)
    {
        return new BlockObjectFile()
            {FileName = blockObjectFile.FileName, Content = LoadFile(blockObjectFile.FileName), DisplayName = blockObjectFile.DisplayName};
    }

    public static BlockObjectFile LoadFileIntoBlockObjectFile(string fileName)
    {
        if (!TryParseFileName(fileName, out BlockObjectFile blockObjectFile, out var _))
        {
            return new BlockObjectFile();
        }

        return LoadFileIntoBlockObjectFile(blockObjectFile);
    }

    private static string LoadFile(string fileName)
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