using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockBreakHand : MonoBehaviour, IHandTick
{
    public PlayerHand playerHand { get; set; }
    private bool _init = false;

    public bool BreakModeActive { get; private set; }

    public GameObject particlePrefab;

    private GameObject _particleObject;

    public void Tick()
    {
        if (!_init)
        {
            _init = true;
            Init();
        }

        _particleObject.SetActive(playerHand.HandComponents.TrackingRecently && BreakModeActive);
    }

    private void Init()
    {
        _particleObject = Instantiate(particlePrefab);
        _particleObject.transform.SetParent(playerHand.HandComponents.PhysicsFingerTips[1], false);
        //SetBreakMode(playerHand.skeletonType == OVRSkeleton.SkeletonType.HandRight);
        SetBreakMode(false);
    }

    public void SetBreakMode(bool active)
    {
        _particleObject.SetActive(active);
        BreakModeActive = active;
    }
}