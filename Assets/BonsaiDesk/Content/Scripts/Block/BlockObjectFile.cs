using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class BlockObjectFile
{
    public static string[] ListFiles()
    {
        try
        {
            var folderPath = System.IO.Path.Combine(Application.persistentDataPath, "blocks");
            System.IO.Directory.CreateDirectory(folderPath);

            return Directory.GetFiles(folderPath);
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePath">Full path to the file</param>
    /// <returns></returns>
    public static string LoadFile(string filePath)
    {
        try
        {
            var folderPath = System.IO.Path.Combine(Application.persistentDataPath, "blocks");
            System.IO.Directory.CreateDirectory(folderPath);

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