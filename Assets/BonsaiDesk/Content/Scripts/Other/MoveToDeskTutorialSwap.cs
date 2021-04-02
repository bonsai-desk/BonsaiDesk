using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveToDeskTutorialSwap : MonoBehaviour
{
    public GameObject leftHand;
    public GameObject rightHand;
    public GameObject leftController;
    public GameObject rightController;

    public GameObject tutorial;
    public GameObject useHandsPopup;

    private bool _popupDismissed = false;
    public bool PopupDismissed => _popupDismissed;

    private void Start()
    {
        if (SaveSystem.Instance.BoolPairs.TryGetValue("OrientWithControllers", out var value))
        {
            _popupDismissed = value;
        }
    }

    private void Update()
    {
        var handTracking = InputManager.Hands.UsingHandTracking;
        leftHand.SetActive(handTracking);
        rightHand.SetActive(handTracking);
        leftController.SetActive(!handTracking);
        rightController.SetActive(!handTracking);

        if (!_popupDismissed && !handTracking && (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch) ||
                                                  OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch)))
        {
            _popupDismissed = true;
        }

        if (!handTracking && !_popupDismissed)
        {
            tutorial.SetActive(false);
            useHandsPopup.SetActive(true);
        }
        else
        {
            tutorial.SetActive(true);
            useHandsPopup.SetActive(false);
        }
    }
}