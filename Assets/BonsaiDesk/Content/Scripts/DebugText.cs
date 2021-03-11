using System;
using TMPro;
using UnityEngine;

public class DebugText : MonoBehaviour
{
    private static DebugText _instance;
    
    public GameObject panelObject;
    public TextMeshProUGUI textMesh;
    
    public static string TextString
    {
        get => _instance.textMesh.text;
        set
        {
            _instance.textMesh.text = value;
            _instance.panelObject.SetActive(!string.IsNullOrEmpty(value));
        }
    }

    private void Awake()
    {
        _instance = this;
    }
}