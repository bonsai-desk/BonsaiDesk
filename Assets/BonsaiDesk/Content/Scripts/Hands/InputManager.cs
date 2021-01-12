﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Hands;

    [Header("Camera Rig Anchors")]
    public Transform leftHandAnchor;

    public Transform rightHandAnchor;

    [Header("Scene Hand Objects")]
    public Transform leftHandObject;

    public Transform rightHandObject;

    [Header("PlayHand Scripts")]
    public PlayerHand leftPlayerHand;

    public PlayerHand rightPlayerHand;

    [Header("")]
    public Transform cameraRig;

    private static readonly Quaternion HandRotationOffset = Quaternion.AngleAxis(180f, Vector3.up);

    private static readonly Vector3 RightControllerOffset = new Vector3(0.02288249f, -0.03249159f, -0.11621020f);

    private static readonly Quaternion RightControllerRotationOffset =
        new Quaternion(0.55690630f, 0.41798240f, 0.49228620f, -0.52230300f);

    private static readonly Vector3 LeftControllerOffset =
        new Vector3(-RightControllerOffset.x, RightControllerOffset.y, RightControllerOffset.z);

    private static readonly Quaternion LeftControllerRotationOffset =
        FlipRotationX(RightControllerRotationOffset) * Quaternion.AngleAxis(180f, Vector3.up);

    public HandComponents Left { get; private set; }
    public HandComponents Right { get; private set; }

    //0-4 thumb to pinky on left hand, 5-9 thumb to pinky right hand.
    public Vector3[] physicsFingerTipPositions = new Vector3[10];
    public Vector3[] targetFingerTipPositions = new Vector3[10];

    private IHandsTick[] _handsTicks;

    private void Awake()
    {
        if (Hands != null)
        {
            Debug.LogError("There should only be one input manager.");
        }

        Hands = this;
    }

    private void Start()
    {
        _handsTicks = GetComponentsInChildren<IHandsTick>();

        Left = new HandComponents(leftPlayerHand, leftHandAnchor, leftHandObject);
        Right = new HandComponents(rightPlayerHand, rightHandAnchor, rightHandObject);
    }

    private void Update()
    {
        CalculateFingerTipPositions();

        Left.PlayerHand.UpdateGestures();
        Right.PlayerHand.UpdateGestures();

        Left.PlayerHand.RunHandTicks();
        Right.PlayerHand.RunHandTicks();

        for (var i = 0; i < _handsTicks.Length; i++)
        {
            _handsTicks[i].Tick(Left.PlayerHand, Right.PlayerHand);
        }

        Left.PlayerHand.UpdateLastGestures();
        Right.PlayerHand.UpdateLastGestures();

        UpdateHandTarget(Left, LeftControllerOffset, LeftControllerRotationOffset);
        HandComponentsUpdate(Left);

        UpdateHandTarget(Right, RightControllerOffset, RightControllerRotationOffset);
        HandComponentsUpdate(Right);
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
            handComponents.SetTracking(handComponents.OVRSkeleton.IsInitialized &&
                                       handComponents.OVRSkeleton.IsDataValid &&
                                       handComponents.OVRSkeleton.IsDataHighConfidence);
            if (handComponents.Tracking)
            {
                handComponents.TargetHand.position = handComponents.HandAnchor.position;
                handComponents.TargetHand.rotation = handComponents.HandAnchor.rotation * HandRotationOffset;
                if (handComponents.MapperTargetsInitialized)
                {
                    handComponents.TargetMapper.UpdateBonesToTargets();
                }
            }
        }
        else if (controller == OVRInput.Controller.Touch)
        {
            handComponents.SetTracking(true);
            if (handComponents.Tracking)
            {
                handComponents.TargetHand.position = handComponents.HandAnchor.TransformPoint(controllerOffset);
                handComponents.TargetHand.rotation = handComponents.HandAnchor.rotation * rotationOffset;
                handComponents.TargetMapper.UpdateBonesToStartPose();
            }
        }
    }

    private void CalculateFingerTipPositions()
    {
        for (int i = 0; i < 10; i++)
        {
            if (i < 5)
            {
                physicsFingerTipPositions[i] = Left.PhysicsFingerTips[i].position;
                targetFingerTipPositions[i] = Left.TargetFingerTips[i].position;
            }
            else
            {
                physicsFingerTipPositions[i] = Right.PhysicsFingerTips[i - 5].position;
                targetFingerTipPositions[i] = Right.TargetFingerTips[i - 5].position;
            }
        }
    }

    public (Vector3 position, Quaternion rotation) PointerPose(HandComponents handComponents)
    {
        if (OVRInput.GetConnectedControllers() == OVRInput.Controller.Hands)
        {
            return (cameraRig.TransformPoint(handComponents.OVRHand.PointerPose.localPosition),
                cameraRig.rotation * handComponents.OVRHand.PointerPose.rotation);
        }
        else
        {
            Debug.LogError("PointerPose not implemented for controllers.");
            return (Vector3.zero, Quaternion.identity);
        }
    }

    public class HandComponents
    {
        public readonly PlayerHand PlayerHand;
        public readonly Transform HandAnchor;
        public readonly Transform PhysicsHand;
        public readonly Transform TargetHand;
        public readonly OVRHandTransformMapper PhysicsMapper;
        public readonly OVRHandTransformMapper TargetMapper;
        public readonly OVRSkeleton OVRSkeleton;
        public readonly OVRHand OVRHand;

        //0 - thumb, 1 - index, 2 - middle, 3 - ring, 4 - pinky
        public readonly Transform[] PhysicsFingerTips;
        public readonly Transform[] TargetFingerTips;

        public bool MapperTargetsInitialized = false;
        public bool Tracking { get; private set; } = false;

        public HandComponents(PlayerHand playerHand, Transform handAnchor, Transform handObject)
        {
            PlayerHand = playerHand;
            HandAnchor = handAnchor;

            PhysicsHand = handObject.GetChild(0);
            TargetHand = handObject.GetChild(1);

            PhysicsMapper = PhysicsHand.GetComponentInChildren<OVRHandTransformMapper>();

            TargetMapper = TargetHand.GetComponent<OVRHandTransformMapper>();
            TargetMapper.targetObject = handAnchor;

            OVRSkeleton = handAnchor.GetComponentInChildren<OVRSkeleton>();
            OVRHand = handAnchor.GetComponentInChildren<OVRHand>();

            PhysicsFingerTips = GetFingerTips(PhysicsMapper);
            TargetFingerTips = GetFingerTips(TargetMapper);
        }

        private static Transform[] GetFingerTips(OVRHandTransformMapper mapper)
        {
            var fingerTips = new Transform[5];
            fingerTips[0] = mapper.CustomBones[(int) OVRSkeleton.BoneId.Hand_ThumbTip];
            fingerTips[1] = mapper.CustomBones[(int) OVRSkeleton.BoneId.Hand_IndexTip];
            fingerTips[2] = mapper.CustomBones[(int) OVRSkeleton.BoneId.Hand_MiddleTip];
            fingerTips[3] = mapper.CustomBones[(int) OVRSkeleton.BoneId.Hand_RingTip];
            fingerTips[4] = mapper.CustomBones[(int) OVRSkeleton.BoneId.Hand_PinkyTip];
            return fingerTips;
        }

        public void SetTracking(bool tracking)
        {
            Tracking = tracking;
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