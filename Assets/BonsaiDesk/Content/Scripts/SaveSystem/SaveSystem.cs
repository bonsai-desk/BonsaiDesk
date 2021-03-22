using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    public Dictionary<string, bool> BoolPairs = new Dictionary<string, bool>();
    public Dictionary<string, int> IntPairs = new Dictionary<string, int>();
    public Dictionary<string, float> FloatPairs = new Dictionary<string, float>();
    public Dictionary<string, string> StringPairs = new Dictionary<string, string>();

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
            BoolPairs = (Dictionary<string, bool>) value;
        }
        
        if (dictionaries.TryGetValue(typeof(int), out value))
        {
            IntPairs = (Dictionary<string, int>) value;
        }
        
        if (dictionaries.TryGetValue(typeof(float), out value))
        {
            FloatPairs = (Dictionary<string, float>) value;
        }
        
        if (dictionaries.TryGetValue(typeof(string), out value))
        {
            StringPairs = (Dictionary<string, string>) value;
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
        dictionaries.Add(typeof(bool), BoolPairs);
        dictionaries.Add(typeof(int), IntPairs);
        dictionaries.Add(typeof(float), FloatPairs);
        dictionaries.Add(typeof(string), StringPairs);

        var bf = new BinaryFormatter();
        bf.Serialize(file, dictionaries);
        file.Close();
    }
}