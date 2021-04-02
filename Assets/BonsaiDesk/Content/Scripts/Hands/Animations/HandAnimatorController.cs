using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandAnimatorController : MonoBehaviour
{
    public OVRInput.Controller controller;
    public Animator animator;

    private float _grab = 0;
    private float _pinch = 0;
    private const float GrabAnimationTime = 0.1f;
    private const float PinchAnimationTime = 0.075f;

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
        _grab = Mathf.MoveTowards(_grab, grab, Time.deltaTime / GrabAnimationTime);
        animator.SetFloat(_grabBlendHash, _grab);

        var pinch = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controller);
        if (OVRInput.Get(OVRInput.Touch.PrimaryIndexTrigger, controller))
        {
            pinch = Mathf.Max(pinch, grab);
        }
        _pinch = Mathf.MoveTowards(_pinch, pinch, Time.deltaTime / PinchAnimationTime);
        animator.SetFloat(_pinchBlendHash, _pinch);
    }
}