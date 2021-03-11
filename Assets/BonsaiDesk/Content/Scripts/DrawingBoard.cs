using System.Collections.Generic;
using UnityEngine;

public class DrawingBoard : MonoBehaviour
{
    private LineRenderer lineRenderer;

    private Queue<Vector3> positions = new Queue<Vector3>();
    private Queue<float> times = new Queue<float>();

    public bool fade = true;

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.alignment = LineAlignment.TransformZ;
    }

    private void Update()
    {
        bool updateLine = false;
        while (times.Count > 0 && Time.time - times.Peek() > 2.5f && fade)
        {
            positions.Dequeue();
            times.Dequeue();
            updateLine = true;
        }

        Vector3 checkPosition = InputManager.Hands.physicsFingerTipPositions[6];
        if (Mathf.Abs(checkPosition.x) < transform.localScale.x / 2f && checkPosition.z > 0.01f && checkPosition.z < transform.localScale.y + 0.01f)
        {
            float difference = checkPosition.y - transform.position.y;
            if (difference < 0.01f)
            {
                positions.Enqueue(new Vector3(checkPosition.x, transform.position.y + 0.0001f, checkPosition.z));
                times.Enqueue(Time.time);
                updateLine = true;
            }
        }

        if (updateLine)
        {
            lineRenderer.positionCount = positions.Count;
            lineRenderer.SetPositions(positions.ToArray());
        }
    }
}