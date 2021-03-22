using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    private Dictionary<string, bool> _boolPairs = new Dictionary<string, bool>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }
    }

    private void Load()
    {
        string destination = Path.Combine(Application.persistentDataPath, "save.dat");
        print("Attempting to load file from " + destination);

        FileStream file;
        if (File.Exists(destination)) file = File.OpenRead(destination);
        else
        {
            print("No load file found.");
            return;
        }

        BinaryFormatter bf = new BinaryFormatter();
        var dictionaries = (Dictionary<Type, IDictionary>) bf.Deserialize(file);
        file.Close();

        if (dictionaries.TryGetValue(typeof(bool), out var value))
        {
            _boolPairs = (Dictionary<string, bool>) value;
        }
    }

    public void Save()
    {
        string destination = Path.Combine(Application.persistentDataPath, "save.dat");
        print("Saving file to " + destination);

        FileStream file;
        if (File.Exists(destination)) file = File.OpenWrite(destination);
        else file = File.Create(destination);

        var dictionaries = new Dictionary<Type, IDictionary>();
        dictionaries.Add(typeof(bool), _boolPairs);

        var bf = new BinaryFormatter();
        bf.Serialize(file, dictionaries);
        file.Close();
    }

    public bool GetBool(string key)
    {
        if (_boolPairs.TryGetValue(key, out var value))
        {
            return value;
        }

        return false;
    }

    public void SetBool(string key, bool value)
    {
        _boolPairs[key] = value;
    }
}