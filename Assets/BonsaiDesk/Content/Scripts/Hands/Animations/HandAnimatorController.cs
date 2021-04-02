using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandAnimatorController : MonoBehaviour
{
    public OVRInput.Controller controller;
    public Animator animator;

    private int _grabBlendHash;
    private int _pinchBlendHash;

    private void Start()
    {
        _grabBlendHash = Animator.StringToHash("GrabBlend");
        _pinchBlendHash = Animator.StringToHash("PinchBlend");
    }

    private void Update()
    {
        animator.enabled = !InputManager.Hands.UsingHandTracking;

        var grab = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controller);
        animator.SetFloat(_grabBlendHash, grab);

        var pinch = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controller);
        animator.SetFloat(_pinchBlendHash, pinch);
    }
}