using System.Collections.Generic;
using UnityEngine;

public class DrawingMesh : MonoBehaviour
{
    public static DrawingMesh singleton;

    public Transform canvas;

    private bool[] tipDownLastFrame = new bool[10];
    private Vector3[] tipPositionLastFrame = new Vector3[10];
    private Vector3?[] tipLeftLastFrame = new Vector3?[10];
    private Vector3?[] tipRightLastFrame = new Vector3?[10];
    private Vector3?[] uvLeftLastFrame = new Vector3?[10];
    private Vector3?[] uvRightLastFrame = new Vector3?[10];
    private float?[] lastWidth = new float?[10];

    private Mesh mesh;
    private List<Vector3> vertices = new List<Vector3>();
    private List<Vector3> normals = new List<Vector3>();
    private List<Vector2> uv2 = new List<Vector2>(); //used as vertices that point in
    private List<Vector2> uv3 = new List<Vector2>(); //used to store creation time
    private List<int> triangles = new List<int>();

    private void Start()
    {
        if (singleton == null)
            singleton = this;

        for (int i = 0; i < 10; i++)
        {
            tipDownLastFrame[i] = false;
            tipLeftLastFrame[i] = null;
            tipRightLastFrame[i] = null;
            uvLeftLastFrame[i] = null;
            uvRightLastFrame[i] = null;
            lastWidth[i] = null;
        }

        mesh = new Mesh();
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    public void reset()
    {
        triangles.Clear();
        vertices.Clear();
        normals.Clear();
        uv2.Clear();
        uv3.Clear();

        mesh.triangles = triangles.ToArray();
        mesh.vertices = vertices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv2 = uv2.ToArray();
        mesh.uv3 = uv3.ToArray();
    }

    private void Update()
    {
        for (int i = 0; i < 10; i++)
        {
            if (i != 1 && i != 6)
                continue;

            Vector3 p = InputManager.Hands.targetFingerTipPositions[i];
            Vector3 pp = InputManager.Hands.physicsFingerTipPositions[i];
            //TODO physics pad positions?
            // Vector3 ppp = InputManager.Hands.physicsFingerPadPositions[i];
            Vector3 ppp = pp;
            if ((Mathf.Abs(p.x) < canvas.transform.localScale.x / 2f && p.z > 0.01f && p.z < canvas.transform.localScale.y + 0.01f) &&
                (Mathf.Abs(pp.x) < canvas.transform.localScale.x / 2f && pp.z > 0.01f && pp.z < canvas.transform.localScale.y + 0.01f) &&
                (Mathf.Abs(p.y) < 0.015f && Mathf.Abs(pp.y) < 0.02f || Mathf.Abs(ppp.y) < 0.015f))
            {
                if (tipDownLastFrame[i])
                {
                    if (tipPositionLastFrame[i] != InputManager.Hands.targetFingerTipPositions[i])
                        addLineSegment(i);
                }
                tipDownLastFrame[i] = true;
                tipPositionLastFrame[i] = InputManager.Hands.targetFingerTipPositions[i];
            }
            else
            {
                tipDownLastFrame[i] = false;
                tipLeftLastFrame[i] = null;
                tipRightLastFrame[i] = null;
                uvLeftLastFrame[i] = null;
                uvRightLastFrame[i] = null;
                lastWidth[i] = null;
            }
        }
    }

    private void addLineSegment(int index)
    {
        float width = 0.01f;
        float widthVariance = 0f;

        float frontWidth = width;
        frontWidth += Random.value * widthVariance - (widthVariance / 2f);
        float fwo2 = frontWidth / 2f;

        float backWidth = width;
        if (!lastWidth[index].HasValue)
        {
            backWidth += Random.value * widthVariance - (widthVariance / 2f);
        }
        else
        {
            backWidth = lastWidth[index].Value;
        }
        float bwo2 = backWidth / 2f;

        Vector3 start = tipPositionLastFrame[index];
        Vector3 end = InputManager.Hands.targetFingerTipPositions[index];
        start.y = 0;
        end.y = 0;

        Quaternion rotation = Quaternion.LookRotation(end - start);

        int startIndex = vertices.Count;

        vertices.Add(end + rotation * Vector3.left * fwo2);
        vertices.Add(end + rotation * Vector3.right * fwo2);

        if (!tipLeftLastFrame[index].HasValue || !tipLeftLastFrame[index].HasValue)
        {
            vertices.Add(start + rotation * Vector3.right * bwo2);
            vertices.Add(start + rotation * Vector3.left * bwo2);
        }
        else
        {
            vertices.Add(tipRightLastFrame[index].Value);
            vertices.Add(tipLeftLastFrame[index].Value);
        }

        tipLeftLastFrame[index] = vertices[startIndex + 0];
        tipRightLastFrame[index] = vertices[startIndex + 1];

        for (int i = 0; i < 4; i++)
            normals.Add(Vector3.up);

        uv2.Add((rotation * Vector3.left).xz() * fwo2);
        uv2.Add((rotation * Vector3.right).xz() * fwo2);
        if (!uvLeftLastFrame[index].HasValue || !uvRightLastFrame[index].HasValue)
        {
            uv2.Add((rotation * Vector3.right).xz() * bwo2);
            uv2.Add((rotation * Vector3.left).xz() * bwo2);
        }
        else
        {
            uv2.Add(uvRightLastFrame[index].Value);
            uv2.Add(uvLeftLastFrame[index].Value);
        }

        uvLeftLastFrame[index] = uv2[startIndex + 0];
        uvRightLastFrame[index] = uv2[startIndex + 1];

        for (int i = 0; i < 4; i++)
            uv3.Add(new Vector2(Time.time, 0));

        triangles.Add(startIndex + 0);
        triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 0);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 3);

        mesh.vertices = vertices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv2 = uv2.ToArray();
        mesh.uv3 = uv3.ToArray();
        mesh.triangles = triangles.ToArray();
    }
}