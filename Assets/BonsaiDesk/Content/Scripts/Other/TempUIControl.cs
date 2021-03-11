using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TempUIControl : MonoBehaviour
{
    public GameObject defaultUI;
    public GameObject alternateUI;

    private void Start()
    {
        defaultUI.SetActive(true);
        alternateUI.SetActive(false);
    }

    public void ToggleUI()
    {
        defaultUI.SetActive(!defaultUI.activeSelf);
        alternateUI.SetActive(!alternateUI.activeSelf);
    }
}