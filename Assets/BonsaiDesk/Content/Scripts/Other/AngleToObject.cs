using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AngleToObject : MonoBehaviour
{
    public Transform targetObject;
    public float angleThreshold;

    private float cachedAngle;
    private int cachedOnFrame = -1;

    public float Angle()
    {
        if (cachedOnFrame != Time.frameCount)
        {
            cachedOnFrame = Time.frameCount;
            Vector3 targetVector = targetObject.position - transform.position;
            cachedAngle = Vector3.Angle(transform.forward, targetVector);
            ;
        }

        return cachedAngle;
    }

    public bool AngleBelowThreshold()
    {
        return Angle() < angleThreshold;
    }
}