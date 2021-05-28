using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockBreakHand : MonoBehaviour, IHandTick
{
    public PlayerHand playerHand { get; set; }
    private bool _init = false;

    public enum BreakMode
    {
        None = 0,
        Single = 1,
        Whole = 2,
        Duplicate = 3,
        Save = 4
    }

    private BreakMode _breakMode = BreakMode.None;

    public BreakMode HandBreakMode => _breakMode;

    public GameObject particlePrefab;

    private GameObject _particleObject;
    private ParticleSystem _particleSystem;
    private ParticleSystem.MainModule _mainModule;

    public void Tick()
    {
        if (!_init)
        {
            _init = true;
            Init();
        }

        var playing = Application.isFocused && Application.isPlaying || Application.isEditor;
        _particleObject.SetActive(playerHand.HandComponents.TrackingRecently && HandBreakMode != BreakMode.None && playing);
    }

    private void Init()
    {
        _particleObject = Instantiate(particlePrefab);
        _particleObject.transform.SetParent(playerHand.HandComponents.PhysicsFingerTips[1], false);
        _particleSystem = _particleObject.GetComponent<ParticleSystem>();
        _mainModule = _particleSystem.main;
        SetBreakMode(BreakMode.None);
    }

    public void SetBreakMode(BreakMode breakMode)
    {
        _breakMode = breakMode;
        switch (breakMode)
        {
            case BreakMode.None:
                break;
            case BreakMode.Single:
                _mainModule.startColor = new ParticleSystem.MinMaxGradient(Color.red);
                break;
            case BreakMode.Whole:
                _mainModule.startColor = new ParticleSystem.MinMaxGradient(new Color(1, 0.5f, 0));
                break;
            case BreakMode.Duplicate:
                _mainModule.startColor = new ParticleSystem.MinMaxGradient(Color.blue);
                break;
            case BreakMode.Save:
                _mainModule.startColor = new ParticleSystem.MinMaxGradient(Color.yellow);
                break;
            default:
                Debug.LogError("Unknown break mode: " + breakMode);
                break;
        }
    }
}