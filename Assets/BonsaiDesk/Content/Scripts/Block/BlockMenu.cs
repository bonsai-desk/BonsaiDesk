using UnityEngine;

public class BlockMenu : MonoBehaviour
{
    public Material blockMaterial;

    private void Start()
    {
        float gap = 0.025f;
        Vector3 localPosition = new Vector3((BlockArea.cubeScale + gap) * -2f, (BlockArea.cubeScale + gap) * 2f, 0);
        for (int i = 0; i < Blocks.blocks.Length; i++)
        {
            if (i != 0)
            {
                localPosition.x += BlockArea.cubeScale + gap;
                if (i % 5 == 0)
                {
                    localPosition.x = (BlockArea.cubeScale + gap) * -2f;
                    localPosition.y -= BlockArea.cubeScale + gap;
                }
            }

            CreateBlock(i, localPosition);
        }
    }

    //   private void Update()
    //   {
    //   }

    private GameObject CreateBlock(int id, Vector3 localPosition)
    {
        GameObject block = new GameObject
        {
            tag = "BlockSelect",
            name = id.ToString()
        };
        block.transform.parent = transform;
        block.transform.localPosition = localPosition;
        block.transform.localScale = new Vector3(BlockArea.cubeScale, BlockArea.cubeScale, BlockArea.cubeScale);
        block.transform.localRotation = Quaternion.identity;

        MeshFilter meshFilter = block.AddComponent<MeshFilter>();

        Mesh mesh = new Mesh();
        var (vertices, uv, triangles, uv2) = BlockArea.GetBlockMesh(id);
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.uv2 = uv2;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        meshFilter.sharedMesh = mesh;

        MeshRenderer meshRenderer = block.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = blockMaterial;

        BoxCollider boxCollider = block.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;

        return block;
    }
}