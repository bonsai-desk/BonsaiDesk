using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MoveToDeskTutorialSwap : MonoBehaviour
{
    public GameObject leftHand;
    public GameObject rightHand;
    public GameObject leftController;
    public GameObject rightController;

    public GameObject tutorial;
    public GameObject useHandsPopup;

    public Image progressImage;
    private float _progress = 0;

    private bool _lastUseHandsPopupShowing = false;

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

        var lThumb = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);
        var rThumb = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch);
        var A = OVRInput.Get(OVRInput.RawButton.A);
        var B = OVRInput.Get(OVRInput.RawButton.B);
        var X = OVRInput.Get(OVRInput.RawButton.X);
        var Y = OVRInput.Get(OVRInput.RawButton.Y);
        var lTrig = OVRInput.Get(OVRInput.RawButton.LIndexTrigger);
        var rTrig = OVRInput.Get(OVRInput.RawButton.RIndexTrigger);
        var lHand = OVRInput.Get(OVRInput.RawButton.LHandTrigger);
        var rHand = OVRInput.Get(OVRInput.RawButton.RHandTrigger);
        var anyButton = lThumb || rThumb || A || B || X || Y || lHand || rHand || lTrig || rTrig;

        var useHandsPopupShowing = useHandsPopup.activeInHierarchy;

        if (!_popupDismissed && !handTracking && useHandsPopupShowing && _lastUseHandsPopupShowing && anyButton)
        {
            _progress += Time.deltaTime;
            if (_progress > 0.75f)
            {
                _popupDismissed = true;
                SaveSystem.Instance.BoolPairs["HidePopup"] = true;
                SaveSystem.Instance.Save();
            }
        }
        else
        {
            _progress = 0;
        }

        progressImage.fillAmount = Mathf.Clamp01(_progress / 0.75f);

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

        _lastUseHandsPopupShowing = useHandsPopupShowing;
    }
}