using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandAnimatorController : MonoBehaviour
{
    public OVRInput.Controller controller;
    public Animator animator;
    
    void Update()
    {
        animator.enabled = !InputManager.Hands.UsingHandTracking;

        var value = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controller);
        animator.SetFloat("DefaultFistBlend", value);
    }
}