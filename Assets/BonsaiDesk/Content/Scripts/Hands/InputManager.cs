using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Hands;

    public bool renderTargetHands = false;

    [Header("Camera Rig Anchors")]
    public Transform leftHandAnchor;

    public Transform rightHandAnchor;
    public GameObject leftControllerModel;
    public GameObject rightControllerModel;

    [Header("PlayHand Scripts")]
    public PlayerHand leftPlayerHand;

    public PlayerHand rightPlayerHand;
    public RuntimeAnimatorController leftAnimationController;
    public RuntimeAnimatorController rightAnimationController;

    [Header("")]
    public Transform cameraRig;

    private static readonly Quaternion HandRotationOffset = Quaternion.AngleAxis(180f, Vector3.up);

    private static readonly Vector3 RightControllerOffset = new Vector3(0.02288249f, -0.03249159f, -0.11621020f);

    private static readonly Quaternion RightControllerRotationOffset = new Quaternion(0.55690630f, 0.41798240f, 0.49228620f, -0.52230300f);

    private static readonly Vector3 LeftControllerOffset = new Vector3(-RightControllerOffset.x, RightControllerOffset.y, RightControllerOffset.z);

    private static readonly Quaternion LeftControllerRotationOffset = FlipRotationX(RightControllerRotationOffset) * Quaternion.AngleAxis(180f, Vector3.up);

    public HandComponents Left { get; private set; }
    public HandComponents Right { get; private set; }

    //0-4 thumb to pinky on left hand, 5-9 thumb to pinky right hand.
    [HideInInspector] public Vector3[] physicsFingerTipPositions = new Vector3[10];
    [HideInInspector] public Vector3[] targetFingerTipPositions = new Vector3[10];

    private IHandsTick[] _handsTicks;

    public bool UsingHandTracking => OVRInput.GetConnectedControllers() == OVRInput.Controller.Hands;

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
        foreach (var handTick in _handsTicks)
        {
            handTick.leftPlayerHand = leftPlayerHand;
            handTick.rightPlayerHand = rightPlayerHand;
        }

        Transform leftHandObject = Instantiate(Resources.Load<GameObject>("Left_Hand"), transform).transform;
        leftHandObject.name = "Left_Hand";
        Transform rightHandObject = Instantiate(Resources.Load<GameObject>("Right_Hand"), transform).transform;
        rightHandObject.name = "Right_Hand";

        Left = new HandComponents(leftPlayerHand, leftHandAnchor, leftHandObject, leftAnimationController);
        Right = new HandComponents(rightPlayerHand, rightHandAnchor, rightHandObject, rightAnimationController);

        Left.SetHandColliderActiveForScreen(false);
        Right.SetHandColliderActiveForScreen(false);
    }

    private void Update()
    {
        UpdateHandTargets();

        HandComponentsUpdate(Left);
        HandComponentsUpdate(Right);

        CalculateFingerTipPositions();

        Left.PlayerHand.UpdateGestures();
        Right.PlayerHand.UpdateGestures();

        Left.PlayerHand.RunHandTicks();
        Right.PlayerHand.RunHandTicks();

        for (var i = 0; i < _handsTicks.Length; i++)
        {
            _handsTicks[i].Tick();
        }

        Left.PlayerHand.UpdateLastGestures();
        Right.PlayerHand.UpdateLastGestures();

        //set controllers on/off
        var controllersActive = !UsingHandTracking && !MoveToDesk.Singleton.oriented;
        var playing = Application.isFocused && Application.isPlaying || Application.isEditor;
        leftControllerModel.SetActive(controllersActive);
        rightControllerModel.SetActive(controllersActive);
    }

    public void UpdateHandTargets(bool updateTracking = true)
    {
        UpdateHandTarget(Left, LeftControllerOffset, LeftControllerRotationOffset, updateTracking);
        UpdateHandTarget(Right, RightControllerOffset, RightControllerRotationOffset, updateTracking);
    }

    private void HandComponentsUpdate(HandComponents handComponents, bool updateTracking = true)
    {
        if (!handComponents.MapperTargetsInitialized && handComponents.OVRSkeleton.IsInitialized)
        {
            handComponents.MapperTargetsInitialized = true;
            handComponents.TargetMapper.TryAutoMapBoneTargetsAPIHand();
        }
    }

    private void UpdateHandTarget(HandComponents handComponents, Vector3 controllerOffset, Quaternion rotationOffset, bool updateTracking = true)
    {
        var controller = OVRInput.GetConnectedControllers();
        if (controller == OVRInput.Controller.Hands)
        {
            bool tracking = handComponents.OVRSkeleton.IsInitialized && handComponents.OVRSkeleton.IsDataValid &&
                            handComponents.OVRSkeleton.IsDataHighConfidence;
            if (tracking)
            {
                handComponents.TargetHand.position = handComponents.HandAnchor.position;
                handComponents.TargetHand.rotation = handComponents.HandAnchor.rotation * HandRotationOffset;
                if (handComponents.MapperTargetsInitialized)
                {
                    handComponents.TargetMapper.UpdateBonesToTargets();
                }

                handComponents.PhysicsHandController.SetHandScale(handComponents.OVRSkeleton.transform.localScale.x);
            }

            if (updateTracking)
            {
                handComponents.SetTracking(tracking);
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

                handComponents.PhysicsHandController.SetHandScale(1f);
            }
        }
    }

    public HandComponents GetHand(OVRSkeleton.SkeletonType skeletonType)
    {
        if (skeletonType == OVRSkeleton.SkeletonType.HandLeft)
            return Left;
        if (skeletonType == OVRSkeleton.SkeletonType.HandRight)
            return Right;
        return null;
    }

    public HandComponents GetOtherHand(OVRSkeleton.SkeletonType skeletonType)
    {
        if (skeletonType == OVRSkeleton.SkeletonType.HandLeft)
            return Right;
        if (skeletonType == OVRSkeleton.SkeletonType.HandRight)
            return Left;
        return null;
    }

    public bool Tracking()
    {
        return Left.Tracking && Right.Tracking;
    }

    public bool TrackingRecently()
    {
        return Left.TrackingRecently && Right.TrackingRecently;
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
        Debug.LogError("Not implemented.");
        return (Vector3.zero, Quaternion.identity);
        // if (OVRInput.GetConnectedControllers() == OVRInput.Controller.Hands)
        // {
        //     return (cameraRig.TransformPoint(handComponents.OVRHand.PointerPose.localPosition),
        //         cameraRig.rotation * handComponents.OVRHand.PointerPose.rotation);
        // }
        // else
        // {
        //     Debug.LogError("PointerPose not implemented for controllers.");
        //     return (Vector3.zero, Quaternion.identity);
        // }
    }

    private static Quaternion FlipRotationX(Quaternion rotation)
    {
        rotation.y *= -1f;
        rotation.z *= -1f;
        rotation *= Quaternion.AngleAxis(180f, Vector3.forward);
        return rotation;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            Left.TurnOffHandForPause();
            Right.TurnOffHandForPause();

            var leftMenu = Left.PlayerHand.GetIHandTick<MenuHand>();
            if (leftMenu)
            {
                leftMenu.TurnOffMenus();
            }

            var rightMenu = Right.PlayerHand.GetIHandTick<MenuHand>();
            if (rightMenu)
            {
                rightMenu.TurnOffMenus();
            }
            
            leftControllerModel.SetActive(false);
            rightControllerModel.SetActive(false);
            
            Left.PlayerHand.stylus.parent.gameObject.SetActive(false);
            Right.PlayerHand.stylus.parent.gameObject.SetActive(false);
        }
    }
}