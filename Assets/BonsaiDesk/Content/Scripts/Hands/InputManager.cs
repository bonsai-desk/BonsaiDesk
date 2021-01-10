using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    void Update()
    {
        var controller = OVRInput.GetConnectedControllers();
        if (controller == OVRInput.Controller.Hands)
        {
            // print("Hand Tracking.");
        }
        else if (controller == OVRInput.Controller.Touch)
        {
            // print("Controllers.");
        }
        else
        {
            // print("Neither.");
        }
    }
}