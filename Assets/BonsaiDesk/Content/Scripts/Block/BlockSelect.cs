// using UnityEngine;
//
// public class BlockSelect : MonoBehaviour
// {
//     public Material blockMaterial;
//     public Material deselectMaterial;
//     private GameObject blocksParent;
//     public BlockSpawner blockSpawner;
//
//     private bool blockSelectedThisFrame = false;
//
//     private void Start()
//     {
//         GameObject blocksParentObject = new GameObject
//         {
//             name = "blocksParentObject"
//         };
//         blocksParentObject.transform.parent = transform;
//         blocksParentObject.transform.localPosition = Vector3.zero;
//         blocksParentObject.transform.localRotation = Quaternion.identity;
//         blocksParent = blocksParentObject;
//
//         const float gap = 0.025f;
//         const float circleRadius = 0.5f;
//         const int height = 3;
//         const int width = 3;
//         Vector3 startLocalPosition = new Vector3(0, 0.1f, 0.125f - circleRadius);
//         Vector3 localPosition = startLocalPosition;
//         for (int x = 0; x < width; x++)
//         {
//             float theta = -(x - (width / 2)) * (Mathf.PI / 16f);
//             localPosition.x = startLocalPosition.x + Mathf.Cos(theta + Mathf.PI / 2f) * circleRadius;
//             localPosition.z = startLocalPosition.z + Mathf.Sin(theta + Mathf.PI / 2f) * circleRadius;
//             localPosition.y = startLocalPosition.y;
//             for (int y = 0; y < height; y++)
//             {
//                 CreateBlock(x * height + y, localPosition, Quaternion.AngleAxis(-theta * Mathf.Rad2Deg, Vector3.up));
//                 localPosition.y += BlockArea.cubeScale + gap;
//             }
//
//             if (x == width / 2)
//             {
//                 CreateBlock(-1, localPosition, Quaternion.AngleAxis(-theta * Mathf.Rad2Deg, Vector3.up));
//             }
//         }
//
//         blocksParent.SetActive(false);
//     }
//
//     private void FixedUpdate()
//     {
//         blockSelectedThisFrame = false;
//     }
//
//     public void ToggleBlockMenu()
//     {
//         if (blocksParent.activeSelf)
//         {
//             CloseBlockMenu();
//         }
//         else
//         {
//             OpenBlockMenu();
//         }
//     }
//
//     public void OpenBlockMenu()
//     {
//         blocksParent.SetActive(true);
//     }
//
//     public void CloseBlockMenu()
//     {
//         blocksParent.SetActive(false);
//     }
//
//     public void SelectBlock(int id)
//     {
//         if (!blockSelectedThisFrame)
//         {
//             blockSelectedThisFrame = true;
//
//             CloseBlockMenu();
//
//             // int blockSpawner1Id = blockSpawner1.blockId;
//             // blockSpawner1.blockId = id;
//             // blockSpawner2.blockId = blockSpawner1Id;
//
//             blockSpawner.BlockId = id;
//         }
//     }
//
//     private GameObject CreateBlock(int id, Vector3 localPosition, Quaternion localRotation)
//     {
//         GameObject block = new GameObject
//         {
//             tag = "BlockSelect",
//             name = id.ToString()
//         };
//         block.transform.parent = blocksParent.transform;
//         block.transform.localPosition = localPosition;
//         block.transform.localScale = new Vector3(BlockArea.cubeScale, BlockArea.cubeScale, BlockArea.cubeScale);
//         block.transform.localRotation = localRotation;
//
//         MeshFilter meshFilter = block.AddComponent<MeshFilter>();
//         Mesh mesh = new Mesh();
//         int meshId = id;
//         if (id == -1)
//             meshId = 0;
//         var (vertices, uv, triangles, uv2) = BlockArea.GetBlockMesh(meshId);
//         mesh.vertices = vertices;
//         mesh.uv = uv;
//         mesh.uv2 = uv2;
//         mesh.triangles = triangles;
//         mesh.RecalculateNormals();
//         mesh.RecalculateTangents();
//         meshFilter.sharedMesh = mesh;
//
//         if (id > -1 && Blocks.blocks[id].blockObject != null)
//         {
//             GameObject blockObject = Instantiate(Blocks.blocks[id].blockObject, block.transform);
//             blockObject.transform.localPosition = Vector3.zero;
//             blockObject.transform.localRotation = Quaternion.Euler(90, 0, 0);
//         }
//
//         MeshRenderer meshRenderer = block.AddComponent<MeshRenderer>();
//         if (id == -1)
//             meshRenderer.sharedMaterial = deselectMaterial;
//         else
//             meshRenderer.sharedMaterial = blockMaterial;
//
//         //BoxCollider boxCollider = block.AddComponent<BoxCollider>();
//
//         BlockSelectTouch blockSelectTouch = block.AddComponent<BlockSelectTouch>();
//         blockSelectTouch.blockId = id;
//         blockSelectTouch.blockSelect = this;
//
//         return block;
//     }
// }