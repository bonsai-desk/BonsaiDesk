using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GrabbableObject : MonoBehaviour
{
    public static List<Transform> grabbableObjects = new List<Transform>();

    private void Start()
    {
        grabbableObjects.Add(transform);
    }

    public static Transform withinDistance(Vector3 position, float distance)
    {
        if (grabbableObjects.Count > 0)
        {
            Transform closest = grabbableObjects[0];
            float closestDistance = float.MaxValue;
            foreach (Transform grabbable in grabbableObjects)
            {
                float d = Vector3.Distance(grabbable.position, position);
                if (d < closestDistance)
                {
                    closestDistance = d;
                    closest = grabbable;
                }
            }
            if (closestDistance < distance)
                return closest;
            else
                return null;
        }
        else
            return null;
    }
}