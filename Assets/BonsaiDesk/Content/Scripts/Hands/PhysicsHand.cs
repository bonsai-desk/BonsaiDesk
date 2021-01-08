using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsHand : MonoBehaviour
{
    public OVRSkeleton oVRSkeleton;
    private OVRPlugin.Skeleton2 _skeleton;

    private bool initialized = false;

    private void Update()
    {
        if (!initialized && oVRSkeleton.BindPoses.Count == 24 &&
            OVRPlugin.GetSkeleton2((OVRPlugin.SkeletonType) oVRSkeleton.GetSkeletonType(), ref _skeleton))
        {
            CreateCapsules();
            initialized = true;
        }
    }

    private void CreateCapsules()
    {
        if (oVRSkeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.None)
        {
            Debug.LogError("Cannot create capsules for skeleton type None");
            return;
        }

        var _capsulesGO = new GameObject("Capsules");
        _capsulesGO.transform.SetParent(transform, false);
        _capsulesGO.transform.localPosition = Vector3.zero;
        _capsulesGO.transform.localRotation = Quaternion.identity;

        var _capsules = new List<OVRBoneCapsule>(new OVRBoneCapsule[_skeleton.NumBoneCapsules]);

        var boneIndexToCapsuleIndex = new Dictionary<short, int>();

        for (int i = 0; i < _skeleton.NumBoneCapsules; ++i)
        {
            short boneIndex = _skeleton.BoneCapsules[i].BoneIndex;
            OVRBone bone = oVRSkeleton.BindPoses[boneIndex];
            if (boneIndex > 0)
                boneIndexToCapsuleIndex.Add(boneIndex, i);

            Transform parentBoneTransform;
            short parentBoneIndex = bone.ParentBoneIndex;
            if (parentBoneIndex >= 0 && boneIndexToCapsuleIndex.TryGetValue(parentBoneIndex, out var value))
            {
                parentBoneTransform = _capsules[value].CapsuleRigidbody.transform;
            }
            else
            {
                if (boneIndex == (short) OVRSkeleton.BoneId.Hand_Pinky1 || boneIndex == (short) OVRSkeleton.BoneId.Hand_Thumb1)
                {
                    var name = (OVRSkeleton.BoneId) parentBoneIndex;
                    var parent = new GameObject(name.ToString()).transform;
                    parent.localPosition = oVRSkeleton.BindPoses[parentBoneIndex].Transform.localPosition;
                    parent.localRotation = oVRSkeleton.BindPoses[parentBoneIndex].Transform.localRotation;
                    parent.SetParent(_capsulesGO.transform, false);
                    parentBoneTransform = parent;
                }
                else
                {
                    parentBoneTransform = _capsulesGO.transform;
                }
            }

            OVRBoneCapsule capsule = _capsules[i] ?? (_capsules[i] = new OVRBoneCapsule());
            capsule.BoneIndex = boneIndex;

            if (capsule.CapsuleRigidbody == null)
            {
                capsule.CapsuleRigidbody = new GameObject(bone.Id.ToString())
                    .AddComponent<Rigidbody>();
                capsule.CapsuleRigidbody.mass = 1.0f;
                capsule.CapsuleRigidbody.isKinematic = true;
                capsule.CapsuleRigidbody.useGravity = false;
                capsule.CapsuleRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            }

            GameObject rbGO = capsule.CapsuleRigidbody.gameObject;
            rbGO.transform.SetParent(parentBoneTransform, false);
            rbGO.transform.localPosition = bone.Transform.localPosition;
            rbGO.transform.localRotation = bone.Transform.localRotation;

            if (capsule.CapsuleCollider == null)
            {
                capsule.CapsuleCollider = new GameObject((bone.Id).ToString() + "_CapsuleCollider")
                    .AddComponent<CapsuleCollider>();
                capsule.CapsuleCollider.isTrigger = false;
            }

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
            ccGO.transform.SetParent(rbGO.transform, false);
            ccGO.transform.localPosition = p0;
            ccGO.transform.localRotation = rot;
        }
    }
}