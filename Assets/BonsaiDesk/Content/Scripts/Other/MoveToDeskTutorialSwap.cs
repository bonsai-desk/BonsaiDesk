using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveToDeskTutorialSwap : MonoBehaviour
{
    public GameObject leftHand;
    public GameObject rightHand;
    public GameObject leftController;
    public GameObject rightController;
    
    void Update()
    {
        var handTracking = InputManager.Hands.UsingHandTracking;
        leftHand.SetActive(handTracking);
        rightHand.SetActive(handTracking);
        leftController.SetActive(!handTracking);
        rightController.SetActive(!handTracking);
    }
}
