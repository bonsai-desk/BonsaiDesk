using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AngleToObject : MonoBehaviour
{
    public Transform targetObject;
    public float angleThreshold;

    private bool cachedValue;
    private int cachedOnFrame = -1;

    public bool angleBelowThreshold()
    {
        if (cachedOnFrame != Time.frameCount)
        {
            cachedOnFrame = Time.frameCount;
            Vector3 targetVector = targetObject.position - transform.position;
            float angle = Vector3.Angle(transform.forward, targetVector);
            cachedValue = angle < angleThreshold;
        }

        return cachedValue;
    }
}   