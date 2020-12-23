using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TogglePauseMorph : MonoBehaviour
{
    private Material material;
    
    void Start()
    {
        var meshFilter = GetComponent<MeshFilter>();
        material = GetComponent<MeshRenderer>().material;
        material.SetFloat("_Lerp", 0f);

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
}