using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHand : MonoBehaviour
{
    public OVRSkeleton.SkeletonType skeletonType;

    [HideInInspector] public NetworkHand networkHand;

    public OVRHand oVRHand;
    public OVRSkeleton oVRSkeleton;

    [HideInInspector] public OVRPhysicsHand oVRPhysicsHand;

    [HideInInspector] public Transform[] fingerTips;

    public Transform[] physicsFingerTips;
    public Transform[] physicsFingerPads;

    [HideInInspector] public Rigidbody body;

    public Transform holdPosition;

    [HideInInspector] public ConfigurableJoint heldJoint;

    [HideInInspector] public Rigidbody heldBody;

    private bool heldObjectGravity;
    private float heldObjectDrag;
    private float heldObjectAngularDrag;

    public static LayerMask AllButHands;

    public PinchSpawn pinchSpawn;

    [HideInInspector] public Material material;

    [HideInInspector] public bool deleteMode = false;

    [HideInInspector] public bool deleteAllMode = false;

    public GameObject menu;

    public Transform head;

    public Transform cameraRig;

    public Transform pointerPose;

    [HideInInspector] public Transform beamHold;

    private Color beamHoldOriginalColor;
    private MeshRenderer beamHoldRenderer;

    public GameObject beamHoldControl;

    [HideInInspector] public bool objectAttached = false;

    public ConfigurableJoint beamJoint;

    [HideInInspector] public Rigidbody beamJointBody;

    private Vector3 hitPoint;

    [HideInInspector] public float ropeLength;

    public OVRHandTransformMapper mapper;

    [HideInInspector] public float hitDistance = Mathf.Infinity;

    [HideInInspector] public Transform oPointerPose;

    public Camera mainCamera;
    private int handLayer;

    private IHandTick[] _handTicks;
    private Dictionary<Type, IHandTick> _handTicksDictionary;

    public enum Gesture
    {
        AnyPinching,
        IndexPinching,
        Fist,
        FlatFist,
        WeakFist,
        WeakFlatFist,
        WeakPalm
    }

    private Dictionary<Gesture, bool> _gestures = new Dictionary<Gesture, bool>();
    private Dictionary<Gesture, bool> _lastGestures = new Dictionary<Gesture, bool>();
    private Dictionary<Gesture, float> _lastGestureActiveTime = new Dictionary<Gesture, float>();

    public bool GetGesture(Gesture gesture)
    {
        if (_gestures.TryGetValue(gesture, out var value))
        {
            return value;
        }

        return false;
    }

    public bool GetLastGesture(Gesture gesture)
    {
        if (_lastGestures.TryGetValue(gesture, out var value))
        {
            return value;
        }

        return false;
    }

    public bool GetGestureStart(Gesture gesture)
    {
        return GetGesture(gesture) && !GetLastGesture(gesture);
    }

    public bool GetGestureActiveWithin(Gesture gesture, float time)
    {
        if (_lastGestureActiveTime.TryGetValue(gesture, out float value))
        {
            return Time.time - value <= time;
        }

        return false;
    }

    public void UpdateLastGestures()
    {
        foreach (Gesture gesture in (Gesture[]) Gesture.GetValues(typeof(Gesture)))
        {
            _lastGestures[gesture] = _gestures[gesture];
        }
    }

    public void ToggleDeleteMode()
    {
        deleteMode = !deleteMode;
        if (deleteMode)
            deleteAllMode = false;
        UpdateDeleteMaterial();
    }

    public void ActivateDeleteMode()
    {
        deleteMode = true;
        deleteAllMode = false;
        UpdateDeleteMaterial();
    }

    public void DeactivateDeleteMode()
    {
        deleteMode = false;
        UpdateDeleteMaterial();
    }

    public void ToggleDeleteAllMode()
    {
        deleteAllMode = !deleteAllMode;
        if (deleteAllMode)
            deleteMode = false;
        UpdateDeleteMaterial();
    }

    public void ActivateDeleteAllMode()
    {
        deleteAllMode = true;
        deleteMode = false;
        UpdateDeleteMaterial();
    }

    public void DeactivateDeleteAllMode()
    {
        deleteAllMode = false;
        UpdateDeleteMaterial();
    }

    private void UpdateDeleteMaterial()
    {
        if (deleteMode)
        {
            material.SetColor("_EmissionColor", Color.red);
        }
        else if (deleteAllMode)
        {
            material.SetColor("_EmissionColor", Color.blue);
        }
        else
        {
            material.SetColor("_EmissionColor", Color.black);
        }
    }

    private void Awake()
    {
        AllButHands = ~LayerMask.GetMask("LeftHand", "RightHand", "LeftHeldObject", "RightHeldObject");
    }

    private void Start()
    {
        body = GetComponent<Rigidbody>();
        oVRPhysicsHand = GetComponent<OVRPhysicsHand>();

        fingerTips = new Transform[0];
        GetFingerTips();

        UpdateDeleteMaterial();

        menu.SetActive(false);
        beamHoldControl.SetActive(false);
        beamJointBody = beamJoint.GetComponent<Rigidbody>();

        _handTicks = GetComponentsInChildren<IHandTick>();
        _handTicksDictionary = new Dictionary<Type, IHandTick>();
        foreach (var handTick in _handTicks)
        {
            _handTicksDictionary.Add(handTick.GetType(), handTick);
            handTick.playerHand = this;
        }
        
        foreach (Gesture gesture in (Gesture[]) Gesture.GetValues(typeof(Gesture)))
        {
            _gestures.Add(gesture, false);
            _lastGestures.Add(gesture, false);
        }

        GameObject oPointerPoseGO = new GameObject
        {
            name = "PointerPoseAdjusted"
        };
        oPointerPose = oPointerPoseGO.transform;

        if (skeletonType == OVRSkeleton.SkeletonType.HandLeft)
        {
            handLayer = LayerMask.NameToLayer("LeftHand");
        }
        else
        {
            handLayer = LayerMask.NameToLayer("RightHand");
        }
    }

    public T GetIHandTick<T>()
    {
        return (T) _handTicksDictionary[typeof(T)];
    }

    public void UpdateGestures()
    {
        float fistStrength = FistStrength();
        float flatFistStrength = FlatFistStrength();

        UpdateGesture(Gesture.AnyPinching, AnyPinching());
        UpdateGesture(Gesture.IndexPinching, IndexPinching());
        UpdateGesture(Gesture.Fist, fistStrength > 0.7f);
        UpdateGesture(Gesture.FlatFist, flatFistStrength > 0.7f);
        UpdateGesture(Gesture.WeakFist, fistStrength > 0.5f);
        UpdateGesture(Gesture.WeakFlatFist, flatFistStrength > 0.5f);
        UpdateGesture(Gesture.WeakPalm, fistStrength < 0.35f);
    }

    void UpdateGesture(Gesture gesture, bool active)
    {
        _gestures[gesture] = active;
        if (active)
            _lastGestureActiveTime[gesture] = Time.time;
    }

    public void RunHandTicks()
    {
        for (var i = 0; i < _handTicks.Length; i++)
        {
            _handTicks[i].Tick();
        }
    }

    private void Update()
    {
        if (Tracking())
        {
            mainCamera.cullingMask |= 1 << handLayer;
        }
        else
        {
            mainCamera.cullingMask &= ~(1 << handLayer);
        }

        oPointerPose.position = cameraRig.TransformPoint(oVRHand.PointerPose.position);
        oPointerPose.rotation = cameraRig.rotation * oVRHand.PointerPose.rotation;

        if (oVRHand.IsPointerPoseValid && Physics.Raycast(oPointerPose.position, oPointerPose.forward,
            out RaycastHit uiHit, 10f, LayerMask.GetMask("UI")))
        {
            hitDistance = uiHit.distance;
        }
        else
        {
            hitDistance = Mathf.Infinity;
        }

        PlayerHands.hands.SetHandGesturesReady(skeletonType);
    }

    public PlayerHand OtherHand()
    {
        if (skeletonType == OVRSkeleton.SkeletonType.HandLeft)
            return PlayerHands.hands.right;
        if (skeletonType == OVRSkeleton.SkeletonType.HandRight)
            return PlayerHands.hands.left;
        return null;
    }

    public void ConnectBody(Rigidbody bodyToConnect, bool changeDrag)
    {
        if (skeletonType == OVRSkeleton.SkeletonType.HandLeft && bodyToConnect == PlayerHands.hands.right.heldBody ||
            skeletonType == OVRSkeleton.SkeletonType.HandRight && bodyToConnect == PlayerHands.hands.left.heldBody)
            return;

        if (heldJoint != null)
        {
            heldBody.useGravity = heldObjectGravity;
            heldBody.drag = heldObjectDrag;
            heldBody.angularDrag = heldObjectAngularDrag;
            Destroy(heldJoint);
            heldJoint = null;
            heldBody = null;
        }

        heldBody = bodyToConnect;

        heldObjectGravity = heldBody.useGravity;
        heldObjectDrag = heldBody.drag;
        heldObjectAngularDrag = heldBody.angularDrag;

        heldBody.useGravity = false;
        if (changeDrag)
        {
            heldBody.drag = 10f;
            heldBody.angularDrag = 10f;
        }

        heldJoint = bodyToConnect.gameObject.AddComponent<ConfigurableJoint>();
        heldJoint.connectedBody = body;
        heldJoint.anchor = Vector3.zero;

        JointDrive positionDrive = new JointDrive
        {
            positionSpring = 2500f,
            positionDamper = 1f,
            maximumForce = Mathf.Infinity
        };

        heldJoint.xDrive = positionDrive;
        heldJoint.yDrive = positionDrive;
        heldJoint.zDrive = positionDrive;

        JointDrive rotationDrive = new JointDrive
        {
            positionSpring = 10f,
            positionDamper = 1f,
            maximumForce = Mathf.Infinity
        };

        heldJoint.angularXDrive = rotationDrive;
        heldJoint.angularYZDrive = rotationDrive;
    }

    //TODO fake is tracking for 1-2 seconds while it fades out. this includes locking gesture values
    private float lastTracking = 0;
    public bool TrackingRecently()
    {
        if (Tracking())
            lastTracking = Time.time;
        return Time.time - lastTracking <= 1f;
    }

    public bool Tracking()
    {
        if (fingerTips.Length == 0)
            GetFingerTips();
        return oVRHand.IsTracked && fingerTips.Length > 0 && physicsFingerTips.Length > 0 &&
               physicsFingerPads.Length > 0 && oVRHand.IsDataHighConfidence;
    }

    private void GetFingerTips()
    {
        if (oVRSkeleton.Bones.Count > 0)
        {
            fingerTips = new Transform[5];
            fingerTips[0] = oVRSkeleton.Bones[(int) OVRSkeleton.BoneId.Hand_ThumbTip].Transform;
            fingerTips[1] = oVRSkeleton.Bones[(int) OVRSkeleton.BoneId.Hand_IndexTip].Transform;
            fingerTips[2] = oVRSkeleton.Bones[(int) OVRSkeleton.BoneId.Hand_MiddleTip].Transform;
            fingerTips[3] = oVRSkeleton.Bones[(int) OVRSkeleton.BoneId.Hand_RingTip].Transform;
            fingerTips[4] = oVRSkeleton.Bones[(int) OVRSkeleton.BoneId.Hand_PinkyTip].Transform;
        }
    }

    public bool Pointing()
    {
        if (!Tracking())
            return false;
        if (FingerCloseStrength(OVRSkeleton.BoneId.Hand_Index1) > 0.25f ||
            FingerCloseStrength(OVRSkeleton.BoneId.Hand_Middle1) < 0.8f ||
            FingerCloseStrength(OVRSkeleton.BoneId.Hand_Ring1) < 0.8f ||
            FingerCloseStrength(OVRSkeleton.BoneId.Hand_Pinky1) < 0.8f ||
            transform.InverseTransformPoint(fingerTips[0].position).z > 0.02f)
            return false;
        return true;
    }

    public bool Pinching(OVRHand.HandFinger finger)
    {
        if (Tracking())
            return oVRHand.GetFingerIsPinching(finger);
        else
            return false;
    }

    public bool IndexPinching()
    {
        return Pinching(OVRHand.HandFinger.Index);
    }

    public float AnyPinchingStrength()
    {
        if (Tracking())
            return oVRHand.GetFingerPinchStrength(OVRHand.HandFinger.Thumb);
        else
            return 0;
    }

    public bool AnyPinching()
    {
        if (Tracking())
            return oVRHand.GetFingerIsPinching(OVRHand.HandFinger.Thumb);
        else
            return false;
    }

    public Vector3 PinchPosition()
    {
        if (Tracking())
            return Vector3.Lerp(fingerTips[0].position, fingerTips[1].position, 0.5f);
        else
            return Vector3.zero;
    }

    public Vector3 PhysicsPinchPosition()
    {
        if (Tracking())
            return Vector3.Lerp(physicsFingerTips[0].position, physicsFingerTips[1].position, 0.5f);
        else
            return Vector3.zero;
    }

    public float FingerCloseStrength(OVRSkeleton.BoneId boneId)
    {
        if (!Tracking())
            return 0;

        float r1 = Vector3.Angle(-oVRSkeleton.transform.right, oVRSkeleton.Bones[(int) boneId].Transform.right);
        float r2 = Vector3.Angle(oVRSkeleton.Bones[(int) boneId].Transform.right,
            oVRSkeleton.Bones[(int) boneId + 2].Transform.right);

        r1 /= 60f;
        r2 /= 175f;

        return Mathf.Clamp01((r1 + r2) / 2f);
    }

    public float FlatFingerStrength(OVRSkeleton.BoneId boneId)
    {
        if (!Tracking())
            return 0;

        float r1 = Vector3.Angle(-oVRSkeleton.transform.right, oVRSkeleton.Bones[(int) boneId].Transform.right);
        r1 /= 60f;
        return Mathf.Clamp01(r1);
    }

    public float FlatFistStrength()
    {
        float minStrength = FlatFingerStrength(OVRSkeleton.BoneId.Hand_Index1);
        float strength = FlatFingerStrength(OVRSkeleton.BoneId.Hand_Middle1);
        if (strength < minStrength)
            minStrength = strength;
        strength = FlatFingerStrength(OVRSkeleton.BoneId.Hand_Ring1);
        if (strength < minStrength)
            minStrength = strength;
        strength = FlatFingerStrength(OVRSkeleton.BoneId.Hand_Pinky1);
        if (strength < minStrength)
            minStrength = strength;
        return minStrength;
    }

    public float FistStrength()
    {
        float minStrength = FingerCloseStrength(OVRSkeleton.BoneId.Hand_Index1);
        float strength = FingerCloseStrength(OVRSkeleton.BoneId.Hand_Middle1);
        if (strength < minStrength)
            minStrength = strength;
        strength = FingerCloseStrength(OVRSkeleton.BoneId.Hand_Ring1);
        if (strength < minStrength)
            minStrength = strength;
        strength = FingerCloseStrength(OVRSkeleton.BoneId.Hand_Pinky1);
        if (strength < minStrength)
            minStrength = strength;
        return minStrength;
    }
}