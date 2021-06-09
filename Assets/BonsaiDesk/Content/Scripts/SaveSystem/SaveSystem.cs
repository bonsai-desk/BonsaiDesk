﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    private bool _possiblyUnsavedData;

    private Dictionary<string, bool> _boolPairs = new Dictionary<string, bool>();
    private Dictionary<string, int> _intPairs = new Dictionary<string, int>();
    private Dictionary<string, long> _longPairs = new Dictionary<string, long>();
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

    public Dictionary<string, long> LongPairs
    {
        get
        {
            _possiblyUnsavedData = true;
            return _longPairs;
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

    private void DeleteSave()
    {
        try
        {
            string destination = Path.Combine(Application.persistentDataPath, "save.json");
            if (File.Exists(destination))
            {
                File.Delete(destination);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Delete error: " + e);
        }
    }

    private void Load()
    {
        string destination = Path.Combine(Application.persistentDataPath, "save.json");
        print("Attempting to load file from " + destination);

        if (!File.Exists(destination))
        {
            print("No load file found.");
            return;
        }

        try
        {
            var json = File.ReadAllText(destination);
            var dictionaries = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            if (dictionaries.TryGetValue("BoolPairs", out var boolPairsJson))
            {
                _boolPairs = JsonConvert.DeserializeObject<Dictionary<string, bool>>(boolPairsJson);
            }
            
            if (dictionaries.TryGetValue("IntPairs", out var intPairsJson))
            {
                _intPairs = JsonConvert.DeserializeObject<Dictionary<string, int>>(intPairsJson);
            }
            
            if (dictionaries.TryGetValue("LongPairs", out var longPairsJson))
            {
                _longPairs = JsonConvert.DeserializeObject<Dictionary<string, long>>(longPairsJson);
            }
            
            if (dictionaries.TryGetValue("FloatPairs", out var floatPairsJson))
            {
                _floatPairs = JsonConvert.DeserializeObject<Dictionary<string, float>>(floatPairsJson);
            }
            
            if (dictionaries.TryGetValue("StringPairs", out var stringPairsJson))
            {
                _stringPairs = JsonConvert.DeserializeObject<Dictionary<string, string>>(stringPairsJson);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Load error: " + e);
            Debug.LogError("Deleting save file.");
            DeleteSave();
        }
    }

    public void Save()
    {
        if (!_possiblyUnsavedData)
        {
            return;
        }

        _possiblyUnsavedData = false;

        try
        {
            string destination = Path.Combine(Application.persistentDataPath, "save.json");
            print("Saving file to " + destination);

            var dictionaries = new Dictionary<string, string>();
            dictionaries.Add("BoolPairs", JsonConvert.SerializeObject(_boolPairs));
            dictionaries.Add("IntPairs", JsonConvert.SerializeObject(_intPairs));
            dictionaries.Add("LongPairs", JsonConvert.SerializeObject(_longPairs));
            dictionaries.Add("FloatPairs", JsonConvert.SerializeObject(_floatPairs));
            dictionaries.Add("StringPairs", JsonConvert.SerializeObject(_stringPairs));

            var json = JsonConvert.SerializeObject(dictionaries, Formatting.Indented);
            File.WriteAllText(destination, json);
        }
        catch (Exception e)
        {
            Debug.LogError("Save error: " + e);
        }
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