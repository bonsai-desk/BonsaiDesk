using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHand : MonoBehaviour
{
    public OVRSkeleton.SkeletonType skeletonType;

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

    private LayerMask allButHands;

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

    public AngleToObject angleToObject;
    public AngleToObject headAngleToObject;

    public TogglePause togglePause;

    public Transform test;

    private IHandTick[] _handTicks;

    // [HideInInspector] public bool indexPinching;
    // [HideInInspector] public bool pinch;
    // [HideInInspector] public float fistMinStrength;
    // [HideInInspector] public bool fist;
    // [HideInInspector] public bool weakFist;

    private bool lastIndexPinching;

    private bool lastPinkyPinch = false;

    // private bool lastPointingAtScreen = false;
    private bool lastFist;
    private bool lastWeakFist;

    public enum Gesture
    {
        AnyPinching,
        IndexPinching,
        Fist,
        WeakFist,
        WeakPalm
    }

    private Dictionary<Gesture, bool> _gestures = new Dictionary<Gesture, bool>();
    private Dictionary<Gesture, bool> _lastGestures = new Dictionary<Gesture, bool>();

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

    public void AttachObject()
    {
        if (!objectAttached)
        {
            objectAttached = true;
            beamHoldControl.SetActive(false);

            beamJoint.connectedAnchor = beamHold.InverseTransformPoint(hitPoint);

            float jointLimit = Vector3.Distance(beamJoint.transform.position, hitPoint);
            SoftJointLimit softJointLimit = new SoftJointLimit
            {
                limit = jointLimit
            };
            beamJoint.linearLimit = softJointLimit;

            ropeLength = jointLimit + Vector3.Distance(beamJointBody.transform.position,
                OtherHand().beamJointBody.transform.position);

            beamJoint.connectedBody = beamHold.GetComponent<Rigidbody>();

            var nid = beamHold.GetComponent<NetworkIdentity>();
            if (nid != null)
                NetworkVRPlayer.self.CmdReceiveOwnershipOfObject(nid);

            BackToOriginalColor();

            // lastFingerDistance = -1;
            // otherHand().lastFingerDistance = -1;
        }
    }

    public void DetachObject()
    {
        if (objectAttached)
        {
            objectAttached = false;
            StopBeam();
            beamJoint.connectedBody = null;
        }
    }

    private void Start()
    {
        body = GetComponent<Rigidbody>();
        oVRPhysicsHand = GetComponent<OVRPhysicsHand>();

        fingerTips = new Transform[0];
        GetFingerTips();

        allButHands = LayerMask.GetMask("LeftHand", "RightHand", "LeftHeldObject", "RightHeldObject");
        allButHands = ~allButHands;

        UpdateDeleteMaterial();

        menu.SetActive(false);
        beamHoldControl.SetActive(false);
        beamJointBody = beamJoint.GetComponent<Rigidbody>();

        _handTicks = GetComponentsInChildren<IHandTick>();

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

    public void BackToOriginalColor()
    {
        beamHoldRenderer.material.SetColor("_Color", beamHoldOriginalColor);
    }

    private void StopBeam()
    {
        beamHold = null;
        if (beamHoldRenderer != null)
        {
            BackToOriginalColor();
            // beamHoldRenderer.material.SetColor("_Color", Color.white);
        }

        beamHoldRenderer = null;
        beamHoldControl.SetActive(false);
    }

    public float FixedUpdateExternal(float fingerDistance)
    {
        float difference = 0;
        bool indexPinching = IndexPinching();
        if (indexPinching)
        {
            if (OtherHand().beamHold != null)
            {
                var limit = OtherHand().beamJoint.linearLimit;
                // if (Mathf.Approximately(lastFingerDistance, -1))
                //     lastFingerDistance = fingerDistance;

                // float difference = fingerDistance - lastFingerDistance;

                // limit.limit = Mathf.Clamp(limit.limit - difference, 0, otherHand().ropeLength);

                float previousLimit = limit.limit;

                limit.limit = Mathf.Clamp(OtherHand().ropeLength - fingerDistance, 0, OtherHand().ropeLength);

                difference = Mathf.Abs(previousLimit - limit.limit);

                OtherHand().beamJoint.linearLimit = limit;
            }
        }
        else
        {
            OtherHand().DetachObject();
            DetachObject();
        }

        // lastFingerDistance = fingerDistance;
        return difference;
    }

    private void Update()
    {
        // if(test != null)
        // {
        //     if (Input.GetKeyDown(KeyCode.Space))
        //     {
        //         test.parent = null;
        //     }
        //
        //     if (Input.GetKeyUp(KeyCode.Space))
        //     {
        //         test.parent = transform;
        //     }
        // }

        bool indexPinching = IndexPinching();
        bool pinch = AnyPinching();
        float fistMinStrength = FistStrength();
        bool fist = fistMinStrength > 0.7f;
        bool weakFist = fistMinStrength > 0.5f;

        _gestures[Gesture.AnyPinching] = AnyPinching();
        _gestures[Gesture.IndexPinching] = IndexPinching();
        _gestures[Gesture.Fist] = fistMinStrength > 0.7f;
        _gestures[Gesture.WeakFist] = fistMinStrength > 0.5f;
        _gestures[Gesture.WeakPalm] = fistMinStrength < 0.35f;

        for (var i = 0; i < _handTicks.Length; i++)
        {
            _handTicks[i].Tick(this);
        }

        PlayerHands.hands.SetHandGesturesReady(skeletonType);

        if (!weakFist)
            togglePause.StopToggleGesture(skeletonType, holdPosition.position);

        if (oVRPhysicsHand.IsDataValid && oVRPhysicsHand.IsDataHighConfidence)
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

        float pinkyStrength = oVRHand.GetFingerPinchStrength(OVRHand.HandFinger.Pinky);
        bool pinkyPinch = pinkyStrength > 0.8f;
        if (pinkyPinch && !lastPinkyPinch && transform.position.y > head.position.y)
        {
            GameObject.Find("GameManager").GetComponent<MoveToDesk>().ResetPosition();

            if (skeletonType == OVRSkeleton.SkeletonType.HandRight)
            {
            }
            else
            {
            }
        }

        lastPinkyPinch = pinkyPinch;

        bool hitPullBox = false;
        if (indexPinching && !lastIndexPinching)
        {
            if (oVRPhysicsHand.thumbTipTarget != null)
            {
                var hits = Physics.OverlapSphere(oVRPhysicsHand.thumbTipTarget.position, 0, allButHands,
                    QueryTriggerInteraction.Collide);
                foreach (var hit in hits)
                {
                    if (hit.CompareTag("BeamPinch"))
                    {
                        OtherHand().AttachObject();
                        hitPullBox = true;
                        break;
                    }
                }
            }
        }

        bool hitObject = false;
        if (!objectAttached && !hitPullBox && !OtherHand().objectAttached && OtherHand().beamHold == null &&
            heldJoint == null)
        {
            if (indexPinching)
            {
                float r = 0.15f;
                float length = 1f;
                float loops = 5;
                for (float t = 0; t < 2f * Mathf.PI; t += Mathf.PI * 2f / 15.25744f / loops)
                {
                    Vector3 posOnCircle = new Vector3(Mathf.Cos(t * loops) * t / (Mathf.PI * 2f) * r * 2f,
                        Mathf.Sin(t * loops) * t / (Mathf.PI * 2f) * r * 2f, length);
                    Vector3 origin = pointerPose.position;
                    Vector3 end = pointerPose.TransformPoint(posOnCircle);
                    // pointerPose.tran

                    // if (Physics.Raycast(origin, direction, out RaycastHit hit, 1f, allButHands, QueryTriggerInteraction.Ignore))
                    if (Physics.Linecast(origin, end, out RaycastHit hit, allButHands, QueryTriggerInteraction.Ignore))
                    {
                        Transform check = hit.transform;
                        Rigidbody hitBody = check.GetComponent<Rigidbody>();
                        while (hitBody == null && check.parent != null)
                        {
                            check = check.parent;
                            hitBody = check.GetComponent<Rigidbody>();
                        }

                        if (hitBody != null /* && !hitBody.isKinematic*/)
                        {
                            //found valid object

                            if (hit.distance < 0.2f)
                                break;

                            if (check != beamHold)
                            {
                                if (beamHoldRenderer != null)
                                    BackToOriginalColor();

                                MeshRenderer meshRenderer = check.GetComponent<MeshRenderer>();
                                if (meshRenderer == null)
                                    meshRenderer = check.GetComponentInChildren<MeshRenderer>();
                                if (meshRenderer != null)
                                {
                                    beamHoldRenderer = meshRenderer;
                                    beamHoldOriginalColor = meshRenderer.material.GetColor("_Color");
                                    meshRenderer.material.SetColor("_Color", Color.yellow);
                                }

                                beamHoldControl.SetActive(true);

                                // if (beamHold == null)
                                // {
                                beamHold = check;
                                // }
                            }

                            hitPoint = hit.point;

                            hitObject = true;
                            break;
                        }
                    }
                }
            }

            if (!hitObject)
            {
                StopBeam();
            }
        }

        if ((pinch || fist) && !objectAttached)
        {
            if (heldJoint == null && !menu.activeInHierarchy)
            {
                Collider[] pinchHits = new Collider[0];
                if (pinch && oVRPhysicsHand.thumbTipTarget != null)
                    pinchHits = Physics.OverlapSphere(oVRPhysicsHand.thumbTipTarget.position, 0, allButHands,
                        QueryTriggerInteraction.Ignore);
                Collider[] fistHits = new Collider[0];
                if (fistMinStrength > 0.7f)
                    fistHits = Physics.OverlapSphere(holdPosition.position, 0.02f, allButHands,
                        QueryTriggerInteraction.Ignore);
                Collider[] hits = new Collider[pinchHits.Length + fistHits.Length];
                pinchHits.CopyTo(hits, 0);
                fistHits.CopyTo(hits, pinchHits.Length);
                for (int i = 0; i < hits.Length; i++)
                {
                    Transform check = hits[i].transform;
                    Rigidbody hitBody = check.GetComponent<Rigidbody>();
                    while (hitBody == null && check.parent != null)
                    {
                        check = check.parent;
                        hitBody = check.GetComponent<Rigidbody>();
                    }

                    if (hitBody != null /* && !hitBody.isKinematic*/)
                    {
                        BlockArea ba = hitBody.GetComponent<BlockArea>();
                        if (ba != null && ba.blocks.Count > 4)
                            ConnectBody(hitBody, true);
                        else
                            ConnectBody(hitBody, false);
                        break;
                    }
                }
            }
        }
        else
        {
            if (Tracking() && heldJoint != null)
            {
                heldBody.useGravity = heldObjectGravity;
                heldBody.drag = heldObjectDrag;
                heldBody.angularDrag = heldObjectAngularDrag;
                Destroy(heldJoint);
                heldJoint = null;
                heldBody = null;
            }
        }

        lastFist = fist;
        lastWeakFist = weakFist;
        lastIndexPinching = indexPinching;
        // lastPointingAtScreen = pointingAtScreen;
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