using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestAnimationHand : MonoBehaviour
{
    public Animator animator;

    private void Update()
    {
        var value = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch);
        animator.SetFloat("DefaultFistBlend", value);
    }
}