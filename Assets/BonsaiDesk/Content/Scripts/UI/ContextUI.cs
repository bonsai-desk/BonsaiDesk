﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContextUI : MonoBehaviour
{
    public GameObject uiObject;

    private void Start()
    {
        // CloseUI();
    }

    public void ToggleUI()
    {
        return;
        
        if (uiObject.activeSelf)
        {
            CloseUI();
        }
        else
        {
            OpenUI();
        }
    }

    public void OpenUI()
    {
        uiObject.SetActive(true);
        InputManager.Hands.Left.ZTestOverlay();
        InputManager.Hands.Right.ZTestOverlay();
        InputManager.Hands.Left.SetPhysicsLayerForTouchScreen();
        InputManager.Hands.Right.SetPhysicsLayerForTouchScreen();
    }

    public void CloseUI()
    {
        uiObject.SetActive(false);
        InputManager.Hands.Left.ZTestRegular();
        InputManager.Hands.Right.ZTestRegular();
        InputManager.Hands.Left.SetPhysicsLayerRegular();
        InputManager.Hands.Right.SetPhysicsLayerRegular();
    }
}