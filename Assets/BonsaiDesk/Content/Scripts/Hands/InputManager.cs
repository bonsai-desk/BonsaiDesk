using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    [Header("Left Hand Object Targets")]
    public Transform leftHandRotationFix;
    public Transform leftControllerRotationFix;

    [Header("Right Hand Object Targets")]
    public Transform rightHandRotationFix;
    public Transform rightControllerRotationFix;
    
    [Header("Scene Hand Objects")]
    public Transform leftHandObject;
    public Transform rightHandObject;

    void Update()
    {
        var controller = OVRInput.GetConnectedControllers();
        if (controller == OVRInput.Controller.Hands)
        {
            leftHandObject.GetChild(1).transform.position = leftHandRotationFix.position;
            leftHandObject.GetChild(1).transform.rotation = leftHandRotationFix.rotation;
            
            rightHandObject.GetChild(1).transform.position = rightHandRotationFix.position;
            rightHandObject.GetChild(1).transform.rotation = rightHandRotationFix.rotation;
        }
        else if (controller == OVRInput.Controller.Touch)
        {
            leftHandObject.GetChild(1).transform.position = leftControllerRotationFix.position;
            leftHandObject.GetChild(1).transform.rotation = leftControllerRotationFix.rotation;
            
            rightHandObject.GetChild(1).transform.position = rightControllerRotationFix.position;
            rightHandObject.GetChild(1).transform.rotation = rightControllerRotationFix.rotation;
        }
    }
}