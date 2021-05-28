// using UnityEngine;
//
// public class BlockBreak : MonoBehaviour
// {
//     public int id = 0;
//     public byte forward = 4;
//     public byte up = 2;
//
//     [HideInInspector]
//     public int emitterId = -1;
//
//     [Range(0f, 1f)]
//     public float health = 1f;
//
//     public Material materialAsset;
//
//     private MeshRenderer meshRenderer;
//     private Material material;
//
//     private void Start()
//     {
//         // transform.localScale = new Vector3(BlockArea.cubeScale, BlockArea.cubeScale, BlockArea.cubeScale);
//         // transform.localScale = new Vector3(0.04f, 0.04f, 0.04f);
//         // transform.localScale = new Vector3(0.0501f, 0.0501f, 0.0501f);
//         transform.localScale = new Vector3(1.0001f, 1.0001f, 1.0001f);
//
//         meshRenderer = GetComponent<MeshRenderer>();
//         material = Instantiate(materialAsset);
//         meshRenderer.sharedMaterial = material;
//
//         Mesh mesh = new Mesh();
//         var (vertices, uv, triangles, uv2) = BlockArea.GetBlockMesh(id, forward, up);
//         mesh.vertices = vertices;
//         mesh.uv = uv;
//         mesh.uv2 = uv2;
//         mesh.triangles = triangles;
//         mesh.RecalculateNormals();
//         mesh.RecalculateTangents();
//         GetComponent<MeshFilter>().sharedMesh = mesh;
//     }
//
//     private void Update()
//     {
//         material.SetFloat("_Health", health);
//     }
//
//     private void OnDestroy()
//     {
//         Destroy(material);
//         if (emitterId > -1)
//         {
//             OVR.AudioManager.StopSound(emitterId, false);
//         }
//     }
// }