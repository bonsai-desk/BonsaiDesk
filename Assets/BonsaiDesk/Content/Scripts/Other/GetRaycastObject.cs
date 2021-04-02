using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class GetRaycastObject : MonoBehaviour
{
    public LayerMask raycastLayerMask;

    [HideInInspector] public Transform hitObject;

    void Update()
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 100f, raycastLayerMask, QueryTriggerInteraction.Collide))
        {
            hitObject = hit.transform;
        }
        else
        {
            hitObject = null;
        }
    }
}