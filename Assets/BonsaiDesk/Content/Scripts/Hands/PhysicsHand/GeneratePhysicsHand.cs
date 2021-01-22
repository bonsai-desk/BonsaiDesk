using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GeneratePhysicsHand : MonoBehaviour
{
    // public GameObject prefabSaveLocation;

    public OVRSkeleton oVRSkeleton;
    public GameObject handMeshPrefab;
    public GameObject handPalmPrefab;
    public PhysicMaterial physicMaterial;
    public Material targetMaterial;
    public Material physicsHandMaterial;

    private OVRPlugin.Skeleton2 _skeleton;
    private bool initialized = false;

    private Queue<Rigidbody> bodiesToReset = new Queue<Rigidbody>();

    private GameObject hand;

    private void Start()
    {
        if (!Application.isEditor)
        {
            Debug.LogError(
                "This script is intended to be used to generate the hands which should be saved as a prefab.");
            Destroy(this);
            return;
        }

        hand = new GameObject(HandName() + "_Hand");
        hand.transform.SetParent(transform, false);
    }

    private void Update()
    {
        if (!Application.isEditor)
        {
            return;
        }

        if (!initialized && oVRSkeleton.IsInitialized && oVRSkeleton.BindPoses.Count == 24 &&
            OVRPlugin.GetSkeleton2((OVRPlugin.SkeletonType) oVRSkeleton.GetSkeletonType(), ref _skeleton))
        {
            initialized = true;

            var physicsHand = CreateCapsules();
            if (physicsHand != null)
            {
                var handMeshObject = Instantiate(handMeshPrefab, physicsHand.transform);
                DestroyImmediate(handMeshObject.GetComponent<Animator>());
                handMeshObject.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial = physicsHandMaterial;
                var mapper = handMeshObject.AddComponent<OVRHandTransformMapper>();
                mapper.useLocalRotation = false;
                mapper._skeletonType = oVRSkeleton.GetSkeletonType();
                mapper.TryAutoMapBonesByName();
                mapper.capsulesParent = physicsHand.transform;
                mapper.TryAutoMapBoneTargets();

                while (bodiesToReset.Count > 0)
                {
                    var rb = bodiesToReset.Dequeue();
                    rb.ResetInertiaTensor();
                    rb.ResetCenterOfMass();
                }

                var handTarget = Instantiate(handMeshPrefab, hand.transform);
                DestroyImmediate(handTarget.GetComponent<Animator>());
                handTarget.name = HandName() + "_Physics_Hand_Target";
                var targetMapper = handTarget.AddComponent<OVRHandTransformMapper>();
                targetMapper.moveObjectToTarget = false;
                targetMapper.moveBonesToTargets = false;
                targetMapper._skeletonType = oVRSkeleton.GetSkeletonType();
                targetMapper.TryAutoMapBonesByName();
                targetMapper.targetObject = oVRSkeleton.transform;
                targetMapper.TryAutoMapBoneTargetsAPIHand();
                var renderer = handTarget.GetComponentInChildren<SkinnedMeshRenderer>();
                renderer.sharedMaterial = targetMaterial;
                renderer.enabled = false;

                var physicsHandController = physicsHand.AddComponent<PhysicsHandController>();
                physicsHandController.physicsMapper = mapper;
                physicsHandController.targetMapper = targetMapper;
                physicsHandController.skeletonType = oVRSkeleton.GetSkeletonType();
                physicsHandController.Init();
            }

#if UNITY_EDITOR
            PrefabUtility.SaveAsPrefabAsset(hand,
                "Assets/BonsaiDesk/Content/Prefabs/Hand/Resources/" + hand.name + ".prefab");
            print("Finished generating hands. Exiting play mode.");
            EditorApplication.isPlaying = false;
#endif
        }
    }

    private string HandName()
    {
        return oVRSkeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.HandLeft ? "Left" : "Right";
    }

    private Rigidbody AddBody(GameObject go)
    {
        var rb = go.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.mass = 0.1f;
        rb.angularDrag = 0f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        return rb;
    }

    private void AddJoint(Rigidbody body, Rigidbody connectedBody, bool stiff = false)
    {
        var joint = body.gameObject.AddComponent<ConfigurableJoint>();

        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;

        joint.rotationDriveMode = RotationDriveMode.Slerp;
        if (stiff)
        {
            joint.slerpDrive = new JointDrive()
            {
                positionSpring = 500f,
                positionDamper = 1f,
                maximumForce = 10f
            };
        }
        else
        {
            joint.slerpDrive = new JointDrive()
            {
                positionSpring = 500f,
                positionDamper = 1f,
                maximumForce = 5f
            };
        }

        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = body.transform.localPosition;
        joint.connectedBody = connectedBody;
    }

    private GameObject CreateCapsules()
    {
        if (oVRSkeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.None)
        {
            Debug.LogError("Cannot create capsules for skeleton type None");
            return null;
        }

        var _capsulesGO = new GameObject(HandName() + "_Physics_Hand");
        _capsulesGO.transform.SetParent(hand.transform, false);
        _capsulesGO.transform.localPosition = Vector3.zero;
        _capsulesGO.transform.localRotation = Quaternion.identity;
        var rb = _capsulesGO.AddComponent<Rigidbody>();
        rb.drag = 10f;
        rb.useGravity = false;
        rb.mass = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        bodiesToReset.Enqueue(rb);

        var palm = Instantiate(handPalmPrefab);
        palm.transform.SetParent(_capsulesGO.transform, false);

        var _capsules = new List<OVRBoneCapsule>(new OVRBoneCapsule[_skeleton.NumBoneCapsules]);

        var boneIndexToCapsuleIndex = new Dictionary<short, int>();

        for (int i = 0; i < _skeleton.NumBoneCapsules; ++i)
        {
            short boneIndex = _skeleton.BoneCapsules[i].BoneIndex;
            OVRBone bone = oVRSkeleton.BindPoses[boneIndex];
            if (boneIndex > 0)
                boneIndexToCapsuleIndex.Add(boneIndex, i);

            bool pinkyOrThumbStart = false;
            bool thumb = boneIndex == (short) OVRSkeleton.BoneId.Hand_Thumb0 ||
                         boneIndex == (short) OVRSkeleton.BoneId.Hand_Thumb1 ||
                         boneIndex == (short) OVRSkeleton.BoneId.Hand_Thumb2 ||
                         boneIndex == (short) OVRSkeleton.BoneId.Hand_Thumb3;
            Transform parentBoneTransform;
            short parentBoneIndex = bone.ParentBoneIndex;
            if (parentBoneIndex >= 0 && boneIndexToCapsuleIndex.TryGetValue(parentBoneIndex, out var value))
            {
                parentBoneTransform = _capsules[value].CapsuleRigidbody.transform;
            }
            else
            {
                if (boneIndex == (short) OVRSkeleton.BoneId.Hand_Pinky1 ||
                    boneIndex == (short) OVRSkeleton.BoneId.Hand_Thumb1)
                {
                    var name = (OVRSkeleton.BoneId) parentBoneIndex;
                    var parent = new GameObject(name.ToString()).transform;
                    parent.localPosition = oVRSkeleton.BindPoses[parentBoneIndex].Transform.localPosition;
                    parent.localRotation = oVRSkeleton.BindPoses[parentBoneIndex].Transform.localRotation;
                    parent.SetParent(_capsulesGO.transform, false);
                    parentBoneTransform = parent;
                    pinkyOrThumbStart = true;
                }
                else
                {
                    parentBoneTransform = _capsulesGO.transform;
                }
            }

            OVRBoneCapsule capsule = _capsules[i] ?? (_capsules[i] = new OVRBoneCapsule());
            capsule.BoneIndex = boneIndex;

            var capsuleGO = new GameObject(bone.Id + "_CapsuleRigidBody");

            if (pinkyOrThumbStart)
            {
                capsuleGO.transform.SetParent(_capsulesGO.transform, false);

                // capsuleGO.transform.localPosition = parentBoneTransform.TransformPoint(bone.Transform.localPosition);
                capsuleGO.transform.localPosition = parentBoneTransform.localPosition +
                                                    parentBoneTransform.localRotation * bone.Transform.localPosition;
                capsuleGO.transform.localRotation = parentBoneTransform.localRotation * bone.Transform.localRotation;
            }
            else
            {
                capsuleGO.transform.SetParent(parentBoneTransform, false);

                capsuleGO.transform.localPosition = bone.Transform.localPosition;
                capsuleGO.transform.localRotation = bone.Transform.localRotation;
            }

            if (boneIndex != 0)
            {
                capsule.CapsuleRigidbody = AddBody(capsuleGO);
                bodiesToReset.Enqueue(capsule.CapsuleRigidbody);

                if (pinkyOrThumbStart)
                {
                    // AddJoint(parentBoneTransform.GetComponent<Rigidbody>(), _capsulesGO.GetComponent<Rigidbody>(),
                    //     true, parentBoneTransform);
                    AddJoint(capsule.CapsuleRigidbody, _capsulesGO.GetComponent<Rigidbody>(), thumb);
                }
                else
                {
                    AddJoint(capsule.CapsuleRigidbody, parentBoneTransform.GetComponent<Rigidbody>(), thumb);
                }
            }

            capsule.CapsuleCollider = new GameObject((bone.Id).ToString() + "_CapsuleCollider")
                .AddComponent<CapsuleCollider>();
            capsule.CapsuleCollider.sharedMaterial = physicMaterial;

            var p0 = _skeleton.BoneCapsules[i].StartPoint.FromFlippedXVector3f();
            var p1 = _skeleton.BoneCapsules[i].EndPoint.FromFlippedXVector3f();
            var delta = p1 - p0;
            var mag = delta.magnitude;
            var rot = Quaternion.FromToRotation(Vector3.right, delta);
            capsule.CapsuleCollider.radius = _skeleton.BoneCapsules[i].Radius;
            capsule.CapsuleCollider.height = mag + _skeleton.BoneCapsules[i].Radius * 2.0f;
            capsule.CapsuleCollider.direction = 0;
            capsule.CapsuleCollider.center = Vector3.right * mag * 0.5f;

            GameObject ccGO = capsule.CapsuleCollider.gameObject;
            ccGO.transform.SetParent(capsuleGO.transform, false);
            ccGO.transform.localPosition = p0;
            ccGO.transform.localRotation = rot;
        }

        return _capsulesGO;
    }
}