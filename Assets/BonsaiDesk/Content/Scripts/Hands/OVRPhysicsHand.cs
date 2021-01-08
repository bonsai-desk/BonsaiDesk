using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class OVRPhysicsHand : MonoBehaviour
{
    public OVRSkeleton.SkeletonType _skeletonType = OVRSkeleton.SkeletonType.None;

    public OVRSkeleton oVRSkeleton;

    public PhysicMaterial physicMaterial;

    [SerializeField] private bool createPhysicalBones = true;

    [SerializeField] private bool createPhysicalSkin = true;

    [SerializeField] private bool renderPhysicalBones = true;

    [SerializeField] private bool renderPhysicalSkin = true;

    [SerializeField] private float mass = 0.46f;

    [SerializeField] private float lbsForce = 70f;

    [SerializeField] private float lbsTorque = 10f;

    [SerializeField] private float snapBackDistance = 0.2f;

    private float bonesToSkinRatio = 0.5f;

    public SkinnedMeshRenderer meshRenderer;
    public Material bonesMaterial;
    public Material skinMaterial;

    private OVRSkeleton.IOVRSkeletonDataProvider _dataProvider;

    private GameObject _capsulesGO;
    private List<OVRBoneCapsule> _capsules;
    private GameObject _capsulesFollowGO;

    public bool IsInitialized { get; private set; }
    public bool IsDataValid { get; private set; }
    public bool IsDataHighConfidence { get; private set; }

    private float lastHandScale = 1f;

    private Transform target;

    private Rigidbody body;

    [HideInInspector] public Transform thumbTipTarget;

    [HideInInspector] public Transform[] fingerTips;

    //finger tips and thumb
    public static readonly short[] skipBones = {5, 8, 11, 14, 18, 3, 4};

    //all fingers (no palm)
    //public static readonly short[] skipBones = { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 };

    //not fingers
    public static readonly short[] notFingers = {0, 1, 2, 15};

    private List<Joint> joints = new List<Joint>();

    private void Awake()
    {
        if (!createPhysicalBones)
            bonesToSkinRatio = 0f;
        if (!createPhysicalSkin)
            bonesToSkinRatio = 1f;

        body = GetComponent<Rigidbody>();
        body.mass = mass * bonesToSkinRatio;
        body.useGravity = false;
        body.angularDrag = 0;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        GameObject targetObject = new GameObject();
        targetObject.transform.parent = oVRSkeleton.transform;
        targetObject.transform.localEulerAngles = new Vector3(0, -180, 0);
        targetObject.transform.localPosition = Vector3.zero;
        target = targetObject.transform;

        ObjectFollowPhysics follow = gameObject.AddComponent<ObjectFollowPhysics>();
        follow.target = target;
        follow.lbsForce = lbsForce * bonesToSkinRatio;
        follow.lbsTorque = lbsTorque * bonesToSkinRatio;
        follow.snapBackDistance = snapBackDistance;
        follow.oVRSkeleton = oVRSkeleton;
        Material handMaterialCopy = Instantiate(meshRenderer.sharedMaterial);
        // handMaterialCopy.SetFloat("_Glossiness", 0);
        meshRenderer.sharedMaterial = handMaterialCopy;
        follow.material = handMaterialCopy;

        // if (_skeletonType == OVRSkeleton.SkeletonType.HandLeft)
        //     PlayerHands.hands.left.material = handMaterialCopy;
        // if (_skeletonType == OVRSkeleton.SkeletonType.HandRight)
        //     PlayerHands.hands.right.material = handMaterialCopy;

        fingerTips = new Transform[5];
        var customBones = GetComponent<OVRHandTransformMapper>().CustomBones;
        for (int i = 0; i < fingerTips.Length; i++)
            fingerTips[i] = customBones[customBones.Count - fingerTips.Length + i];

        if (_dataProvider == null)
        {
            _dataProvider = oVRSkeleton.GetComponent<OVRSkeleton.IOVRSkeletonDataProvider>();
        }

        _capsules = new List<OVRBoneCapsule>();
        //Capsules = _capsules.AsReadOnly();
    }

    private void Start()
    {
        if (_skeletonType != OVRSkeleton.SkeletonType.None && oVRSkeleton.IsInitialized)
        {
            Initialize();
        }
    }

    private void Initialize()
    {
        var skeleton = new OVRPlugin.Skeleton();
        if (OVRPlugin.GetSkeleton((OVRPlugin.SkeletonType) _skeletonType, out skeleton))
        {
            InitializeCapsules(skeleton);

            IsInitialized = true;

            OVRHandTransformMapper mapper = GetComponent<OVRHandTransformMapper>();
            if (mapper != null)
            {
                mapper.capsulesParent = _capsulesFollowGO.transform;
                mapper.bonesParent = _capsulesGO.transform;
                mapper.TryAutoMapBoneTargets();
            }
        }
    }

    private void InitializeCapsules(OVRPlugin.Skeleton skeleton)
    {
        transform.position = target.position;
        transform.rotation = target.rotation;

        _capsules = new List<OVRBoneCapsule>(new OVRBoneCapsule[skeleton.NumBoneCapsules]);
        //Capsules = _capsules.AsReadOnly();

        if (!_capsulesGO)
        {
            _capsulesGO = new GameObject(transform.name + "Capsules");
            _capsulesGO.transform.position = transform.position;
            _capsulesGO.transform.rotation = transform.rotation;
            _capsulesGO.transform.parent = transform;
            _capsulesGO.transform.localPosition = Vector3.zero;
            _capsulesGO.transform.localRotation = Quaternion.identity;
            _capsulesFollowGO = new GameObject(transform.name + "FollowCapsules");
            _capsulesFollowGO.transform.position = transform.position;
            _capsulesFollowGO.transform.rotation = transform.rotation;
            _capsulesGO.transform.localPosition = Vector3.zero;
            _capsulesGO.transform.localRotation = Quaternion.identity;
        }

        _capsules = new List<OVRBoneCapsule>(new OVRBoneCapsule[skeleton.NumBoneCapsules]);
        //Capsules = _capsules.AsReadOnly();

        Rigidbody lastThumbJoint = body;

        Vector3 lastLocalPosition = Vector3.zero;
        Quaternion lastLocalRotation = Quaternion.identity;

        //GameObject lastBone = null;
        //int startBoneIndex = -1;

        // Time.timeScale = 0;

        for (int n = 0; n < 2; n++)
        {
            for (int i = 0; i < skeleton.NumBoneCapsules; ++i)
            {
                if (!((n == 0 && !createPhysicalBones) || (n == 1 && !createPhysicalSkin)))
                {
                    var capsule = skeleton.BoneCapsules[i];

                    Transform bindBone = oVRSkeleton.BindPoses[capsule.BoneIndex].Transform;
                    Transform boneObject = oVRSkeleton.Bones[capsule.BoneIndex].Transform;

                    var capsuleRigidBodyGO =
                        new GameObject((oVRSkeleton.Bones[capsule.BoneIndex].Id).ToString() + "_CapsuleRigidBody");
                    if (n == 0)
                    {
                        capsuleRigidBodyGO.transform.parent = _capsulesGO.transform;
                        capsuleRigidBodyGO.transform.localPosition = Vector3.zero;
                        capsuleRigidBodyGO.transform.localRotation = Quaternion.identity;
                    }
                    else
                    {
                        capsuleRigidBodyGO.transform.parent = _capsulesFollowGO.transform;
                        capsuleRigidBodyGO.transform.localPosition = Vector3.zero;
                        capsuleRigidBodyGO.transform.localRotation = Quaternion.identity;
                    }

                    if (capsule.BoneIndex == 3 || capsule.BoneIndex == 16)
                    {
                        lastLocalPosition = oVRSkeleton.BindPoses[capsule.BoneIndex - 1].Transform.localPosition;
                        lastLocalRotation = oVRSkeleton.BindPoses[capsule.BoneIndex - 1].Transform.localRotation;
                    }

                    if (capsule.BoneIndex == 6 || capsule.BoneIndex == 9 || capsule.BoneIndex == 12)
                    {
                        lastLocalPosition = Vector3.zero;
                        lastLocalRotation = Quaternion.identity;
                    }

                    Vector3 localPosition = lastLocalPosition + (lastLocalRotation * bindBone.localPosition);
                    Quaternion localRotation = lastLocalRotation * bindBone.localRotation;

                    capsuleRigidBodyGO.transform.localPosition = localPosition;
                    capsuleRigidBodyGO.transform.localRotation = localRotation;

                    lastLocalPosition = localPosition;
                    lastLocalRotation = localRotation;

                    // if (n == 1)
                    // {
                    //     print("---");
                    //     if (_skeletonType == OVRSkeleton.SkeletonType.HandLeft)
                    //         print("left");
                    //     else
                    //         print("right");
                    //     print(capsule.BoneIndex);
                    //     print(oVRSkeleton.transform.position.ToString("F4"));
                    //     print(bindBone.position.ToString("F4"));
                    //     print(bindBone.localPosition.ToString("F4"));
                    //     print(bindBone.localEulerAngles.ToString("F4"));
                    //     print(oVRSkeleton.transform.InverseTransformPoint(bindBone.position).ToString("F4"));
                    //     print("---");
                    // }

                    if (n == 0)
                    {
                        if (capsule.BoneIndex == (int) OVRSkeleton.BoneId.Hand_Thumb3)
                        {
                            GameObject thumbTip = new GameObject();
                            thumbTip.name = "ThumbTip";
                            thumbTip.transform.parent = capsuleRigidBodyGO.transform;
                            thumbTip.transform.localRotation = Quaternion.identity;
                            thumbTip.transform.localPosition = new Vector3(0.02459077f, 0.001026971f, -0.0006703722f);
                            if (_skeletonType == OVRSkeleton.SkeletonType.HandLeft)
                                thumbTip.transform.localPosition = -thumbTip.transform.localPosition;
                            thumbTipTarget = thumbTip.transform;
                        }
                    }

                    if (n == 1)
                    {
                        var capsuleRigidBody = capsuleRigidBodyGO.AddComponent<Rigidbody>();
                        capsuleRigidBodyGO.AddComponent<HandAuthority>();
                        capsuleRigidBody.mass = mass * (1f - bonesToSkinRatio) / 19f;
                        capsuleRigidBody.useGravity = false;
                        capsuleRigidBody.angularDrag = 0;
                        capsuleRigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                        if (capsule.BoneIndex == 8)
                            capsuleRigidBody.gameObject.tag = "PointerTip";
                        if (capsule.BoneIndex == 5 || capsule.BoneIndex == 11 || capsule.BoneIndex == 14 ||
                            capsule.BoneIndex == 18)
                            capsuleRigidBody.gameObject.tag = "FingerTip";

                        if (capsule.BoneIndex == 3 || capsule.BoneIndex == 6 || capsule.BoneIndex == 9 ||
                            capsule.BoneIndex == 12 || capsule.BoneIndex == 16)
                        {
                            lastThumbJoint = body;
                        }

                        bool notFinger = false;
                        for (int j = 0; j < notFingers.Length; j++)
                            if (capsule.BoneIndex == notFingers[j])
                                notFinger = true;
                        bool finger = !notFinger;

                        if (finger)
                        {
                            var capsuleJoint = capsuleRigidBodyGO.AddComponent<ConfigurableJoint>();

                            capsuleJoint.autoConfigureConnectedAnchor = false;
                            capsuleJoint.anchor = Vector3.zero;
                            capsuleJoint.connectedAnchor =
                                lastThumbJoint.transform.InverseTransformPoint(capsuleRigidBody.transform.position);

                            capsuleJoint.xMotion = ConfigurableJointMotion.Locked;
                            capsuleJoint.yMotion = ConfigurableJointMotion.Locked;
                            capsuleJoint.zMotion = ConfigurableJointMotion.Locked;
                            capsuleJoint.angularXMotion = ConfigurableJointMotion.Locked;

                            capsuleJoint.connectedBody = lastThumbJoint;

                            joints.Add(capsuleJoint);

                            lastThumbJoint = capsuleRigidBody;
                        }

                        var objectFollow = capsuleRigidBodyGO.AddComponent<ObjectFollowPhysics>();
                        if (createPhysicalBones)
                            objectFollow.target = _capsules[i].CapsuleCollider.transform.parent;
                        else
                            objectFollow.target = boneObject;
                        objectFollow.lbsForce = lbsForce * (1f - bonesToSkinRatio) / 19f;
                        objectFollow.lbsTorque = lbsTorque * (1f - bonesToSkinRatio) / 19f;
                        objectFollow.snapBackDistance = 0.05f;
                        //objectFollow.oVRSkeleton = oVRSkeleton;
                    }

                    var capsuleColliderGO =
                        new GameObject((oVRSkeleton.Bones[capsule.BoneIndex].Id).ToString() + "_CapsuleCollider");
                    if (_skeletonType == OVRSkeleton.SkeletonType.HandLeft)
                    {
                        int layer = LayerMask.NameToLayer("LeftHand");
                        capsuleRigidBodyGO.layer = layer;
                        capsuleColliderGO.layer = layer;
                    }

                    if (_skeletonType == OVRSkeleton.SkeletonType.HandRight)
                    {
                        int layer = LayerMask.NameToLayer("RightHand");
                        capsuleRigidBodyGO.layer = layer;
                        capsuleColliderGO.layer = layer;
                    }

                    capsuleColliderGO.transform.parent = capsuleRigidBodyGO.transform;
                    capsuleColliderGO.transform.localPosition = Vector3.zero;
                    capsuleColliderGO.transform.localRotation = Quaternion.identity;
                    var capsuleCollider = capsuleColliderGO.AddComponent<CapsuleCollider>();
                    if (physicMaterial != null)
                        capsuleCollider.material = physicMaterial;
                    // var p0 = capsule.Points[0].FromFlippedXVector3f();
                    // var p1 = capsule.Points[1].FromFlippedXVector3f();
                    var p0 = Vector3.zero;
                    var p1 = Vector3.zero;
                    var delta = p1 - p0;
                    var mag = delta.magnitude;
                    var rot = Quaternion.FromToRotation(Vector3.right, delta);
                    if (n == 0)
                    {
                        capsuleCollider.radius = 0.001f;
                        capsuleCollider.height = mag + capsule.Radius;
                    }
                    else
                    {
                        capsuleCollider.radius = capsule.Radius;
                        capsuleCollider.height = mag + capsule.Radius * 2.0f;
                    }

                    bool skipBone = false;
                    if (n == 0)
                    {
                        for (int j = 0; j < skipBones.Length; j++)
                            if (capsule.BoneIndex == skipBones[j])
                                skipBone = true;
                    }

                    if (skipBone)
                        capsuleCollider.isTrigger = true;
                    else
                        capsuleCollider.isTrigger = false;
                    capsuleCollider.direction = 0;
                    capsuleColliderGO.transform.localPosition = p0;
                    capsuleColliderGO.transform.localRotation = rot;
                    capsuleCollider.center = Vector3.right * mag * 0.5f;

                    if (((n == 0 && renderPhysicalBones) || (n == 1 && renderPhysicalSkin)) && !skipBone)
                    {
                        var capsuleRenderer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        Destroy(capsuleRenderer.GetComponent<CapsuleCollider>());
                        capsuleRenderer.transform.parent = capsuleColliderGO.transform;
                        capsuleRenderer.transform.localPosition = capsuleCollider.center;
                        capsuleRenderer.transform.localEulerAngles = new Vector3(0, 0, 90);
                        capsuleRenderer.transform.localScale = new Vector3(capsuleCollider.radius * 2f,
                            capsuleCollider.height / 2f, capsuleCollider.radius * 2f);
                        if (n == 0 && bonesMaterial != null)
                            capsuleRenderer.GetComponent<MeshRenderer>().sharedMaterial = bonesMaterial;
                        if (n == 1 && skinMaterial != null)
                            capsuleRenderer.GetComponent<MeshRenderer>().sharedMaterial = skinMaterial;
                    }

                    if (n == 0)
                        _capsules[i] = new OVRBoneCapsule(capsule.BoneIndex, null, capsuleCollider);
                }
            }
        }
    }

    public float scale = 1f;

    private void Update()
    {
#if UNITY_EDITOR
        if (OVRInput.IsControllerConnected(OVRInput.Controller.Hands) && !IsInitialized && oVRSkeleton.IsInitialized)
        {
            if (_skeletonType != OVRSkeleton.SkeletonType.None)
            {
                Initialize();
            }
        }
#endif
        float handScale = oVRSkeleton.transform.localScale.x;
        if (IsInitialized && !Mathf.Approximately(handScale, lastHandScale))
        {
            Vector3 localScale = new Vector3(handScale, handScale, handScale);
            transform.localScale = localScale;
            _capsulesFollowGO.transform.localScale = localScale;
            for (int i = 0; i < joints.Count; i++)
            {
                joints[i].connectedAnchor = joints[i].connectedAnchor;
            }
        }

        lastHandScale = handScale;
    }

    private void FixedUpdate()
    {
        if (!IsInitialized || _dataProvider == null)
        {
            IsDataValid = false;
            IsDataHighConfidence = false;

            return;
        }

        var data = _dataProvider.GetSkeletonPoseData();

        IsDataValid = data.IsDataValid;
        IsDataHighConfidence = data.IsDataHighConfidence;

        if (createPhysicalBones)
        {
            for (int i = 0; i < _capsules.Count; ++i)
            {
                OVRBoneCapsule capsule = _capsules[i];

                if (data.IsDataValid && data.IsDataHighConfidence)
                {
                    //capsule.CapsuleCollider.transform.parent.gameObject.SetActive(true);

                    Transform bone = oVRSkeleton.Bones[(int) capsule.BoneIndex].Transform;

                    capsule.CapsuleCollider.transform.parent.localRotation =
                        Quaternion.Inverse(target.rotation) * bone.rotation;
                    capsule.CapsuleCollider.transform.parent.localPosition =
                        target.InverseTransformPoint(bone.position);
                }
                else
                {
                    //capsule.CapsuleCollider.transform.parent.gameObject.SetActive(false);
                }
            }
        }
    }
}