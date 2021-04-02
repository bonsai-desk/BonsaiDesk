using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TogglePauseMorph : MonoBehaviour
{
    private Material material;
    private int pausedId;
    private int visibilityId;

    private void Awake()
    {
        material = GetComponent<MeshRenderer>().material;
        pausedId = Shader.PropertyToID("_Paused");
        visibilityId = Shader.PropertyToID("_Visibility");
    }

    void Start()
    {
        var meshFilter = GetComponent<MeshFilter>();

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
        };
        mesh.triangles = new[]
        {
            0, 1, 2,
            3, 0, 2,

            4, 5, 6,
            7, 4, 6
        };

        meshFilter.sharedMesh = mesh;
    }

    /// <summary>
    /// 0 is playing, 1 is paused
    /// </summary>
    public void SetPaused(float pauseLerp)
    {
        if (!material)
        {
            material = GetComponent<MeshRenderer>().material;
        }

        material.SetFloat(pausedId, Mathf.Clamp01(pauseLerp));
    }

    /// <summary>
    /// 0 is fully transparent, 1 is fully opaque
    /// </summary>
    public void SetVisibility(float visibility)
    {
        if (!material)
        {
            material = GetComponent<MeshRenderer>().material;
        }

        material.SetFloat(visibilityId, Mathf.Clamp01(visibility));
    }
}