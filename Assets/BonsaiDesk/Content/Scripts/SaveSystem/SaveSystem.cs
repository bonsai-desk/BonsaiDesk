using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    private bool _possiblyUnsavedData;

    private Dictionary<string, bool> _boolPairs = new Dictionary<string, bool>();
    private Dictionary<string, int> _intPairs = new Dictionary<string, int>();
    private Dictionary<string, float> _floatPairs = new Dictionary<string, float>();
    private Dictionary<string, string> _stringPairs = new Dictionary<string, string>();

    public Dictionary<string, bool> BoolPairs
    {
        get
        {
            _possiblyUnsavedData = true;
            return _boolPairs;
        }
    }

    public Dictionary<string, int> IntPairs
    {
        get
        {
            _possiblyUnsavedData = true;
            return _intPairs;
        }
    }

    public Dictionary<string, float> FloatPairs
    {
        get
        {
            _possiblyUnsavedData = true;
            return _floatPairs;
        }
    }

    public Dictionary<string, string> StringPairs
    {
        get
        {
            _possiblyUnsavedData = true;
            return _stringPairs;
        }
    }

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

        if (dictionaries.TryGetValue(typeof(int), out value))
        {
            _intPairs = (Dictionary<string, int>) value;
        }

        if (dictionaries.TryGetValue(typeof(float), out value))
        {
            _floatPairs = (Dictionary<string, float>) value;
        }

        if (dictionaries.TryGetValue(typeof(string), out value))
        {
            _stringPairs = (Dictionary<string, string>) value;
        }
    }

    public void Save()
    {
        if (!_possiblyUnsavedData)
        {
            return;
        }

        string destination = Path.Combine(Application.persistentDataPath, "save.dat");
        print("Saving file to " + destination);

        FileStream file;
        if (File.Exists(destination)) file = File.OpenWrite(destination);
        else file = File.Create(destination);

        var dictionaries = new Dictionary<Type, IDictionary>();
        dictionaries.Add(typeof(bool), _boolPairs);
        dictionaries.Add(typeof(int), _intPairs);
        dictionaries.Add(typeof(float), _floatPairs);
        dictionaries.Add(typeof(string), _stringPairs);

        var bf = new BinaryFormatter();
        bf.Serialize(file, dictionaries);
        file.Close();
    }

    private void OnDestroy()
    {
        Save();
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            Save();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Save();
        }
    }
}