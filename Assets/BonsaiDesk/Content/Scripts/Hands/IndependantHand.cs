using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IndependantHand : MonoBehaviour
{
    void Start()
    {
        foreach (var body in GetComponentsInChildren<Rigidbody>())
        {
            body.useGravity = true;
        }
        
        Destroy(GetComponent<ConfigurableJoint>());
        Destroy(GetComponent<ObjectFollowPhysics>());
        GetComponent<Rigidbody>().useGravity = true;
    }
}