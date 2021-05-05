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
        var orientWithControllers = SaveSystem.Instance.BoolPairs.TryGetValue("OrientWithControllers", out var value) && value;
        var hidePopup = SaveSystem.Instance.BoolPairs.TryGetValue("HidePopup", out value) && value;
        _popupDismissed = orientWithControllers || hidePopup;
    }

    private void Update()
    {
        var handTracking = InputManager.Hands.UsingHandTracking;
        leftHand.SetActive(handTracking);
        rightHand.SetActive(handTracking);
        leftController.SetActive(!handTracking);
        rightController.SetActive(!handTracking);

        var lThumb = OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
        var rThumb = OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch);
        var A = OVRInput.GetDown(OVRInput.RawButton.A);
        var B = OVRInput.GetDown(OVRInput.RawButton.B);
        var X = OVRInput.GetDown(OVRInput.RawButton.X);
        var Y = OVRInput.GetDown(OVRInput.RawButton.Y);
        var lTrig = OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger);
        var rTrig = OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger);
        var lHand = OVRInput.GetDown(OVRInput.RawButton.LHandTrigger);
        var rHand = OVRInput.GetDown(OVRInput.RawButton.RHandTrigger);

        if (!_popupDismissed && !handTracking && (lThumb || rThumb || A || B || X || Y || lHand || rHand || lTrig || rTrig))
        {
            _popupDismissed = true;
            SaveSystem.Instance.BoolPairs["HidePopup"] = true;
            SaveSystem.Instance.Save();
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