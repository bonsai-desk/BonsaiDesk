using System.Collections.Generic;
using UnityEngine;

public class DeskController : MonoBehaviour
{
    //table
    public Transform tableParent;

    private const float tableThickness = 0.03f;
    public Material tableMaterial;
    public Material canvasMaterial;

    //holes
    public GameObject holePrefab;

    private Vector4[] holePositions = new Vector4[100];
    private float[] holeRadii = new float[100];
    private List<Hole> holes = new List<Hole>();

    public GameObject buttonPrefab;

    public GameObject graphy;
    private bool graphyActive = false;
    private Vector3 graphPos = new Vector3(-0.411f, 0.151f, 0.61f);

    //   private void Start()
    //   {
    //       // for (int i = 0; i < 4; i++)
    //       //     createRandomButton();
    //
    //       // toggleGraphy();
    //   }

    public void CreateRandomButton()
    {
        int attempts = 0;
        Vector2 position;
        bool valid;
        do
        {
            attempts++;
            position = new Vector2(Random.value * 0.5f - 0.25f, -0.44f + Random.value * 0.15f + 0.5f + 0.05f);
            valid = true;
            for (int i = 0; i < holes.Count; i++)
                if (Vector2.Distance(position, new Vector2(holes[i].holeObject.localPosition.x, holes[i].holeObject.localPosition.z)) < 0.1f)
                    valid = false;
        } while (!valid && attempts < 100);

        CreateButton(position, 0);
    }

    public void ToggleGraphy()
    {
        graphyActive = !graphyActive;
        if (graphyActive)
            graphy.transform.position = graphPos;
        else
            graphy.transform.position = new Vector3(0, -20f, -20f);
    }

    public void CreateButton(Vector2 position, float radius)
    {
        GameObject button = Instantiate(buttonPrefab);
        button.transform.parent = tableParent;
        button.transform.localPosition = new Vector3(position.x, 0, position.y);
        HoleButton holeButton = button.GetComponent<HoleButton>();
        holeButton.action.AddListener(CreateRandomButton);
        holeButton.activateOnRelease = false;
        holeButton.Init();
    }

    public Hole CreateHole(Vector2 position, float radius)
    {
        Hole hole = new Hole();

        //holeRadii[numHoles] = radius;

        GameObject holeObject = Instantiate(holePrefab);
        holeObject.transform.parent = tableParent;
        holeObject.transform.localPosition = new Vector3(position.x, -tableThickness / 2f, position.y);
        holeObject.transform.localScale = new Vector3(radius * 2f, tableThickness, radius * 2f);
        //holeObjects.Add(holeObject.transform);
        hole.holeObject = holeObject.transform;

        hole.radius = radius;

        holes.Add(hole);

        tableMaterial.SetInt("numHoles", holes.Count);
        canvasMaterial.SetInt("numHoles", holes.Count);
        UpdateHolePositionsInShader();
        UpdateHoleRadiiInShader();

        return hole;
    }

    public void DestroyHole(Hole hole)
    {
        Destroy(hole.holeObject.gameObject);
        holes.Remove(hole);
        tableMaterial.SetInt("numHoles", holes.Count);
        canvasMaterial.SetInt("numHoles", holes.Count);
        UpdateHolePositionsInShader();
        UpdateHoleRadiiInShader();
    }

    public void UpdateHolePositionsInShader()
    {
        for (int i = 0; i < holes.Count; i++)
        {
            holePositions[i].x = holes[i].holeObject.position.x;
            holePositions[i].y = holes[i].holeObject.position.z;
        }

        tableMaterial.SetVectorArray("holePositions", holePositions);
        canvasMaterial.SetVectorArray("holePositions", holePositions);
    }

    public void UpdateHoleRadiiInShader()
    {
        for (int i = 0; i < holes.Count; i++)
            holeRadii[i] = holes[i].radius;
        tableMaterial.SetFloatArray("holeRadii", holeRadii);
        canvasMaterial.SetFloatArray("holeRadii", holeRadii);
    }

    private void OnApplicationQuit()
    {
        tableMaterial.SetInt("numHoles", 0);
        canvasMaterial.SetInt("numHoles", 0);
    }

    //private struct TableOrientation
    //{
    //    public Vector3 position;
    //    public float angle;
    //    public float handDistance;
    //}
}