using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCrash : MonoBehaviour
{
    public void Crash()
    {
        var test = GetComponent<Rigidbody>();
        test.isKinematic = true;
    }
}