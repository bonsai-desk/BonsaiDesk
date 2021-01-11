using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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
                handComponents.HandTarget.position = handComponents.HandAnchor.position;
                handComponents.HandTarget.rotation = handComponents.HandAnchor.rotation * HandRotationOffset;
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
                handComponents.HandTarget.position = handComponents.HandAnchor.TransformPoint(controllerOffset);
                handComponents.HandTarget.rotation = handComponents.HandAnchor.rotation * rotationOffset;
                handComponents.TargetMapper.UpdateBonesToStartPose();
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
        public readonly Transform HandObject;
        public readonly Transform PhysicsHand;
        public readonly Transform HandTarget;
        public readonly OVRHandTransformMapper TargetMapper;
        public readonly OVRSkeleton OVRSkeleton;
        public readonly OVRHand OVRHand;

        public bool MapperTargetsInitialized = false;
        public bool Tracking { get; private set; } = false;

        public HandComponents(PlayerHand playerHand, Transform handAnchor, Transform handObject)
        {
            PlayerHand = playerHand;
            HandAnchor = handAnchor;
            HandObject = handObject;
            PhysicsHand = handObject.GetChild(0);
            HandTarget = handObject.GetChild(1);

            TargetMapper = HandTarget.GetComponent<OVRHandTransformMapper>();
            TargetMapper.targetObject = handAnchor;

            OVRSkeleton = handAnchor.GetComponentInChildren<OVRSkeleton>();
            OVRHand = handAnchor.GetComponentInChildren<OVRHand>();
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