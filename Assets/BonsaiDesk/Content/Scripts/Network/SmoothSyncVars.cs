using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(AutoAuthority))]
public class SmoothSyncVars : NetworkBehaviour
{
    public string[] variables;
    private HashSet<string> _variablesHashSet;

    private class BoolValue
    {
        public bool Value;
        public float LastSendTime;
    }

    private readonly SyncDictionary<int, bool> _boolSyncDictionary = new SyncDictionary<int, bool>();
    private readonly Dictionary<int, BoolValue> _boolLocalDictionary = new Dictionary<int, BoolValue>();

    private AutoAuthority _autoAuthority;

    private void Start()
    {
        _autoAuthority = GetComponent<AutoAuthority>();
        _variablesHashSet = new HashSet<string>(variables);
    }

    public override void OnStartServer()
    {
        for (int i = 0; i < variables.Length; i++)
        {
            var hashCode = variables[i].GetHashCode();
            if (_boolSyncDictionary.ContainsKey(hashCode))
            {
                Debug.LogError("Duplicate variable names or hash code collision.");
            }
            else
            {
                _boolSyncDictionary.Add(hashCode, false);
            }
        }

        foreach (var pair in _boolSyncDictionary)
        {
            _boolLocalDictionary[pair.Key] = new BoolValue() {Value = pair.Value, LastSendTime = 0};
        }
    }

    public override void OnStartClient()
    {
        foreach (var pair in _boolSyncDictionary)
        {
            _boolLocalDictionary[pair.Key] = new BoolValue() {Value = pair.Value, LastSendTime = 0};
        }
    }

    private void Update()
    {
        if (_autoAuthority.HasAuthority())
        {
            foreach (var pair in _boolLocalDictionary)
            {
                if (_boolSyncDictionary[pair.Key] != pair.Value.Value && Time.unscaledTime - pair.Value.LastSendTime > syncInterval)
                {
                    _boolLocalDictionary[pair.Key].Value = pair.Value.Value;
                    _boolLocalDictionary[pair.Key].LastSendTime = Time.unscaledTime;
                    CmdUpdateDictionary(pair.Key, pair.Value.Value);
                }
            }
        }
        else
        {
            foreach (var pair in _boolSyncDictionary)
            {
                _boolLocalDictionary[pair.Key].Value = pair.Value;
            }
        }
    }

    [Command(ignoreAuthority = true)]
    private void CmdUpdateDictionary(int variable, bool value)
    {
        _boolSyncDictionary[variable] = value;
    }

    public bool HasAuthority()
    {
        return _autoAuthority.HasAuthority();
    }

    public void RequestAuthority()
    {
        _autoAuthority.Interact();
    }

    public bool Get(string variable)
    {
        if (!_variablesHashSet.Contains(variable))
        {
            Debug.LogError($"[Bonsai Desk] Variable \"{variable}\" not present in variables list.");
            return false;
        }

        return _boolLocalDictionary[variable.GetHashCode()].Value;
    }

    public void Set(string variable, bool value)
    {
        if (!_variablesHashSet.Contains(variable))
        {
            Debug.LogError($"[Bonsai Desk] Variable \"{variable}\" not present in variables list.");
            return;
        }

        if (!HasAuthority())
        {
            Debug.LogError($"[Bonsai Desk] Attempting to change variable \"{variable}\" without authority.");
            return;
        }
        
        _boolLocalDictionary[variable.GetHashCode()].Value = value;
    }
}