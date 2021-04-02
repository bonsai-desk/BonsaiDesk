using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpotManager : MonoBehaviour
{
    public static SpotManager Instance;

    public SpotInfo[] spotInfo;

    [Serializable]
    public struct SpotInfo
    {
        public Transform tableEdge;
        public Texture handTexture;
        public Texture headTexture;
    }

    private void Awake()
    {
        Instance = this;
    }
}