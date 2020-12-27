using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TogglePauseMorph : MonoBehaviour
{
    private Material material;

    private void Awake()
    {
        material = GetComponent<MeshRenderer>().material;
    }

    void Start()
    {
        var iconColor = Color.white;
        // var backgroundColor = Color.black;

        var meshFilter = GetComponent<MeshFilter>();

        // float backgroundOffset = 0.00001f;
        var mesh = new Mesh();
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, 0.5f, 0),
            new Vector3(-0.1f, 0.5f, 0),
            new Vector3(-0.1f, -0.5f, 0),
            new Vector3(-0.5f, -0.5f, 0),
            
            new Vector3(0.1f, 0.5f, 0),
            new Vector3(0.5f, 0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(0.1f, -0.5f, 0)
            
            // new Vector3(-0.6f, 0.6f, backgroundOffset),
            // new Vector3(0.6f, 0.6f, backgroundOffset),
            // new Vector3(0.6f, -0.6f, backgroundOffset),
            // new Vector3(-0.6f, -0.6f, backgroundOffset)
        };
        mesh.tangents = new[]
        {
            new Vector4(-0.5f, 0.5f, 0, 1),
            new Vector4(0, 0.25f, 0, 1),
            new Vector4(0, -0.25f, 0, 1),
            new Vector4(-0.5f, -0.5f, 0, 1),
            
            new Vector4(0, 0.25f, 0, 1),
            new Vector4(0.5f, 0, 0, 1),
            new Vector4(0.5f, 0, 0, 1),
            new Vector4(0, -0.25f, 0, 1)
            
            // new Vector4(-0.6f, 0.6f, backgroundOffset, 1),
            // new Vector4(0.6f, 0, backgroundOffset, 1),
            // new Vector4(0.6f, 0, backgroundOffset, 1),
            // new Vector4(-0.6f, -0.6f, backgroundOffset, 1)
        };
        mesh.colors = new[]
        {
            iconColor,
            iconColor,
            iconColor,
            iconColor,
            
            iconColor,
            iconColor,
            iconColor,
            iconColor
            
            // backgroundColor,
            // backgroundColor,
            // backgroundColor,
            // backgroundColor
        };
        mesh.triangles = new[]
        {
            0, 1, 2,
            3, 0, 2,
            
            4, 5, 6,
            7, 4, 6
            
            // 8, 9, 10,
            // 10, 11, 8
        };

        meshFilter.sharedMesh = mesh;
    }

    public void SetLerp(float lerp)
    {
        material.SetFloat("_Lerp", Mathf.Clamp01(lerp));
    }

    public void SetFade(float fade)
    {
        material.SetFloat("_Alpha", Mathf.Clamp01(fade));
    }
}