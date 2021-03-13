﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuHand : MonoBehaviour, IHandTick
{
    public PlayerHand playerHand { get; set; }

    public GameObject menuPrefab;
    public AngleToObject angleToHead;
    public TableBrowserParent tableBrowserParent;

    private bool _isInit = false;
    private GameObject menuObject;

    private void Init()
    {
        menuObject = Instantiate(menuPrefab, transform);
        menuObject.SetActive(false);
    }

    public void Tick()
    {
        if (!_isInit)
        {
            _isInit = true;
            Init();
        }

        var angleBelowThreshold = angleToHead.AngleBelowThreshold();
        var menuOpen = tableBrowserParent && !tableBrowserParent.sleeped;
        menuObject.SetActive(angleBelowThreshold && playerHand.HandComponents.TrackingRecently && !menuOpen);
    }
}