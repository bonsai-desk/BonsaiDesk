﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GeneratePhysicsHand : MonoBehaviour
{
    public OVRSkeleton oVRSkeleton;
    public GameObject handMeshPrefab;

    private OVRPlugin.Skeleton2 _skeleton;
    private bool initialized = false;

    private Queue<Rigidbody> bodiesToReset = new Queue<Rigidbody>();

    private void Update()
    {
        if (!Application.isEditor)
            return;

        if (!initialized && oVRSkeleton.BindPoses.Count == 24 &&
            OVRPlugin.GetSkeleton2((OVRPlugin.SkeletonType) oVRSkeleton.GetSkeletonType(), ref _skeleton))
        {
            initialized = true;
            var physicsHand = CreateCapsules();
            if (physicsHand != null)
            {
                var handMeshObject = Instantiate(handMeshPrefab, physicsHand.transform);
                var mapper = handMeshObject.AddComponent<OVRHandTransformMapper>();
                mapper._skeletonType = oVRSkeleton.GetSkeletonType();
                mapper.TryAutoMapBonesByName();
                mapper.capsulesParent = physicsHand.transform;
                mapper.TryAutoMapBoneTargets();

                var follow = new GameObject("Follow");
                follow.transform.SetParent(transform, false);
                follow.AddComponent<Rigidbody>().isKinematic = true;

                var joint = physicsHand.AddComponent<ConfigurableJoint>();
                
                joint.rotationDriveMode = RotationDriveMode.Slerp;
                joint.slerpDrive = new JointDrive()
                {
                    positionSpring = 1000000f,
                    positionDamper = 10000f,
                    maximumForce = 15f
                };

                var drive = new JointDrive()
                {
                    positionSpring = 1000000f,
                    positionDamper = 1f,
                    maximumForce = 50f
                };

                joint.xDrive = drive;
                joint.yDrive = drive;
                joint.zDrive = drive;
                
                joint.autoConfigureConnectedAnchor = false;
                joint.connectedAnchor = Vector3.zero;
                joint.targetRotation = Quaternion.identity;
                joint.connectedBody = follow.GetComponent<Rigidbody>();

                while (bodiesToReset.Count > 0)
                {
                    var rb = bodiesToReset.Dequeue();
                    rb.ResetInertiaTensor();
                    rb.ResetCenterOfMass();
                    // print(rb.inertiaTensor.ToString("F20"));
                }

                // print(physicsHand.GetComponent<Rigidbody>().inertiaTensor);

                // var followPhysics = physicsHand.AddComponent<ObjectFollowPhysics>();
                // followPhysics.lbsTorque = 0;
                // followPhysics.target = follow.transform;

                // EditorApplication.isPaused = true;
            }
        }
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

    private void AddJoint(Rigidbody body, Rigidbody connectedBody, bool lockRotation = false)
    {
        var joint = body.gameObject.AddComponent<ConfigurableJoint>();

        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;

        if (lockRotation)
        {
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;
        }
        else
        {
            joint.rotationDriveMode = RotationDriveMode.Slerp;
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
        
        var physicsLayer = oVRSkeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.HandLeft
            ? LayerMask.NameToLayer("LeftHand")
            : LayerMask.NameToLayer("RightHand");

        var handName = oVRSkeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.HandLeft ? "Left" : "Right";
        var _capsulesGO = new GameObject(handName + "_Physics_Hand");
        _capsulesGO.AddComponent<SphereCollider>().radius = 0.005f;
        _capsulesGO.layer = physicsLayer;
        _capsulesGO.transform.SetParent(transform, false);
        _capsulesGO.transform.localPosition = Vector3.zero;
        _capsulesGO.transform.localRotation = Quaternion.identity;
        var rb = _capsulesGO.AddComponent<Rigidbody>();
        rb.drag = 10f;
        rb.useGravity = false;
        rb.mass = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        bodiesToReset.Enqueue(rb);

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
                if (boneIndex == (short) OVRSkeleton.BoneId.Hand_Pinky1 ||
                    boneIndex == (short) OVRSkeleton.BoneId.Hand_Thumb1)
                {
                    var name = (OVRSkeleton.BoneId) parentBoneIndex;
                    var parent = new GameObject(name.ToString()).transform;
                    parent.gameObject.layer = physicsLayer;
                    parent.localPosition = oVRSkeleton.BindPoses[parentBoneIndex].Transform.localPosition;
                    parent.localRotation = oVRSkeleton.BindPoses[parentBoneIndex].Transform.localRotation;
                    parent.SetParent(_capsulesGO.transform, false);
                    parentBoneTransform = parent;
                    var prb = AddBody(parent.gameObject);
                    prb.gameObject.AddComponent<SphereCollider>().radius = 0.005f;
                    bodiesToReset.Enqueue(prb);
                }
                else
                {
                    parentBoneTransform = _capsulesGO.transform;
                }
            }

            OVRBoneCapsule capsule = _capsules[i] ?? (_capsules[i] = new OVRBoneCapsule());
            capsule.BoneIndex = boneIndex;

            var capsuleGO = new GameObject(bone.Id + "_CapsuleRigidBody");
            capsuleGO.layer = physicsLayer;
            
            capsuleGO.transform.SetParent(parentBoneTransform, false);
            capsuleGO.transform.localPosition = bone.Transform.localPosition;
            capsuleGO.transform.localRotation = bone.Transform.localRotation;

            if (boneIndex != 0)
            {
                capsule.CapsuleRigidbody = AddBody(capsuleGO);
                bodiesToReset.Enqueue(capsule.CapsuleRigidbody);

                if (boneIndex == (short) OVRSkeleton.BoneId.Hand_Pinky1 ||
                    boneIndex == (short) OVRSkeleton.BoneId.Hand_Thumb1)
                {
                    AddJoint(parentBoneTransform.GetComponent<Rigidbody>(), _capsulesGO.GetComponent<Rigidbody>(), true);
                }

                AddJoint(capsule.CapsuleRigidbody, parentBoneTransform.GetComponent<Rigidbody>(), true);
            }

            capsule.CapsuleCollider = new GameObject((bone.Id).ToString() + "_CapsuleCollider")
                .AddComponent<CapsuleCollider>();
            capsule.CapsuleCollider.gameObject.layer = physicsLayer;
            capsule.CapsuleCollider.isTrigger = false;

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