using UnityEngine;

public class RandomMotion : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 startRotation;
    private float startTime;

    private void Start()
    {
        startPosition = transform.localPosition;
        startRotation = new Vector3(Random.value * 360f, Random.value * 360f, Random.value * 360f);
        startTime = Random.value * 1000f;
    }

    private void Update()
    {
        transform.localPosition = startPosition + new Vector3(Mathf.Sin(Time.time / 2f + startTime) * 0.1f, Mathf.Sin(Time.time / 3f + startTime) * 0.1f,
            Mathf.Sin(Time.time + startTime) * 0.1f);
        transform.eulerAngles = startRotation + new Vector3(Mathf.Sin(Time.time / 2f) * 180f, Mathf.Sin(Time.time / 3f) * 180f, Mathf.Sin(Time.time) * 180f);
    }
}