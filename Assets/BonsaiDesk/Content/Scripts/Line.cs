using UnityEngine;

public class Line : MonoBehaviour
{
    private LineRenderer lineRenderer;

    public static Transform from;
    public static Transform to;

    public static Vector3 fromPoint;
    public static Vector3 toPoint;

    public static bool render = true;

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        if (from != null)
            fromPoint = from.position;
        if (to != null)
            toPoint = to.position;
        if (render)
        {
            lineRenderer.SetPosition(0, fromPoint);
            lineRenderer.SetPosition(1, toPoint);
        }
        else
        {
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, Vector3.zero);
        }
    }
}