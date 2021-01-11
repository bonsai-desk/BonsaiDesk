using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    [Header("Camera Rig Anchors")]
    public Transform leftHandAnchor;

    public Transform rightHandAnchor;

    [Header("Scene Hand Objects")]
    public Transform leftHandObject;

    public Transform rightHandObject;

    private static readonly Quaternion HandRotationOffset = Quaternion.AngleAxis(180f, Vector3.up);

    private static readonly Vector3 RightControllerOffset = new Vector3(0.02288249f, -0.03249159f, -0.11621020f);

    private static readonly Quaternion RightControllerRotationOffset =
        new Quaternion(0.55690630f, 0.41798240f, 0.49228620f, -0.52230300f);

    private static readonly Vector3 LeftControllerOffset =
        new Vector3(-RightControllerOffset.x, RightControllerOffset.y, RightControllerOffset.z);

    private static readonly Quaternion LeftControllerRotationOffset =
        FlipRotationX(RightControllerRotationOffset) * Quaternion.AngleAxis(180f, Vector3.up);

    private static Quaternion FlipRotationX(Quaternion rotation)
    {
        rotation.y *= -1f;
        rotation.z *= -1f;
        rotation *= Quaternion.AngleAxis(180f, Vector3.forward);
        return rotation;
    }

    void Update()
    {
        var controller = OVRInput.GetConnectedControllers();
        if (controller == OVRInput.Controller.Hands)
        {
            leftHandObject.GetChild(1).transform.position = leftHandAnchor.position;
            leftHandObject.GetChild(1).transform.rotation = leftHandAnchor.rotation * HandRotationOffset;

            rightHandObject.GetChild(1).transform.position = rightHandAnchor.position;
            rightHandObject.GetChild(1).transform.rotation = rightHandAnchor.rotation * HandRotationOffset;
        }
        else if (controller == OVRInput.Controller.Touch)
        {
            leftHandObject.GetChild(1).transform.position = leftHandAnchor.TransformPoint(LeftControllerOffset);
            leftHandObject.GetChild(1).transform.rotation = leftHandAnchor.rotation * LeftControllerRotationOffset;

            rightHandObject.GetChild(1).transform.position = rightHandAnchor.TransformPoint(RightControllerOffset);
            rightHandObject.GetChild(1).transform.rotation = rightHandAnchor.rotation * RightControllerRotationOffset;
        }
    }
}