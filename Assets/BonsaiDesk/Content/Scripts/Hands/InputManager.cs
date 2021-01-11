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

    private HandComponents _left;
    private HandComponents _right;

    private void Start()
    {
        if (leftHandAnchor && leftHandObject)
        {
            _left = new HandComponents(leftHandAnchor, leftHandObject);
        }

        if (rightHandAnchor && rightHandObject)
        {
            _right = new HandComponents(rightHandAnchor, rightHandObject);
        }
    }
    
    private void Update()
    {
        if (_left != null)
        {
            UpdateHandTarget(_left, LeftControllerOffset, LeftControllerRotationOffset);
            HandComponentsUpdate(_left);
        }
        
        if (_right != null)
        {
            UpdateHandTarget(_right, RightControllerOffset, RightControllerRotationOffset);
            HandComponentsUpdate(_right);
        }
    }

    private void HandComponentsUpdate(HandComponents handComponents)
    {
        if (!handComponents.MapperTargetsInitialized && handComponents.OVRSkeleton.IsInitialized)
        {
            handComponents.MapperTargetsInitialized = true;
            handComponents.TargetMapper.TryAutoMapBoneTargetsAPIHand();
        }
    }

    private void UpdateHandTarget(HandComponents handComponents, Vector3 controllerOffset, Quaternion rotationOffset)
    {
        var controller = OVRInput.GetConnectedControllers();
        if (controller == OVRInput.Controller.Hands)
        {
            handComponents.HandTarget.position = handComponents.HandAnchor.position;
            handComponents.HandTarget.rotation = handComponents.HandAnchor.rotation * HandRotationOffset;
            if (handComponents.MapperTargetsInitialized)
            {
                handComponents.TargetMapper.UpdateBonesToTargets();
            }
        }
        else if (controller == OVRInput.Controller.Touch)
        {
            handComponents.HandTarget.position = handComponents.HandAnchor.TransformPoint(controllerOffset);
            handComponents.HandTarget.rotation = handComponents.HandAnchor.rotation * rotationOffset;
        }
    }

    private class HandComponents
    {
        public readonly Transform HandAnchor;
        public readonly Transform HandObject;
        public readonly Transform PhysicsHand;
        public readonly Transform HandTarget;
        public readonly OVRHandTransformMapper TargetMapper;
        public readonly OVRSkeleton OVRSkeleton;

        public bool MapperTargetsInitialized = false;

        public HandComponents(Transform handAnchor, Transform handObject)
        {
            HandAnchor = handAnchor;
            HandObject = handObject;
            PhysicsHand = handObject.GetChild(0);
            HandTarget = handObject.GetChild(1);

            TargetMapper = HandTarget.GetComponent<OVRHandTransformMapper>();
            TargetMapper.targetObject = handAnchor;

            OVRSkeleton = handAnchor.GetComponentInChildren<OVRSkeleton>();
        }
    }

    private static Quaternion FlipRotationX(Quaternion rotation)
    {
        rotation.y *= -1f;
        rotation.z *= -1f;
        rotation *= Quaternion.AngleAxis(180f, Vector3.forward);
        return rotation;
    }
}