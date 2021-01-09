using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PhysicsHandController : MonoBehaviour
{
    public OVRHandTransformMapper physicsMapper;
    public OVRHandTransformMapper targetMapper;
    public PlayerHand playerHand;

    private bool _initialized = false;
    private ConfigurableJoint _joint;
    private Rigidbody _rigidbody;
    private Quaternion _startRotation;

    public ConfigurableJoint[] fingerJoints;
    public Quaternion[] fingerJointStartLocalRotations;
    public Transform[] fingerTargets;

    private void Awake()
    {
        IgnoreCollisions();
        _rigidbody = GetComponent<Rigidbody>();
        if (!_initialized && targetMapper)
            Init();
    }

    private void FixedUpdate()
    {
        if (!_initialized)
            return;

        bool notTracking = playerHand && !playerHand.Tracking();
        if (!notTracking)
        {
            UpdateJoint();
            UpdateFingerJoints();
            _rigidbody.WakeUp();
        }
    }

    private void UpdateFingerJoints()
    {
        for (int i = 0; i < fingerJoints.Length; i++)
        {
            var joint = fingerJoints[i];
            var jointStartRotation = fingerJointStartLocalRotations[i];
            var target = fingerTargets[i];

            //if thumb or pinky
            if (i == 0 || i == 12)
            {
                joint.SetTargetRotationLocal(target.parent.localRotation * target.localRotation, jointStartRotation);
            }
            else
            {
                joint.SetTargetRotationLocal(target.localRotation, jointStartRotation);
            }
        }
    }

    private void UpdateJoint()
    {
        if (!_joint)
            return;
        
        _joint.connectedAnchor = targetMapper.transform.position;
        _joint.SetTargetRotationLocal(targetMapper.transform.localRotation, _startRotation);
    }

    public void Init()
    {
        if (_initialized)
            return;
        _initialized = true;

        SetupJoint();
        SetupObjectFollowPhysics();
        GetJointsAndTargets();
        GetJointStartLocalRotations();
    }

    private void GetJointStartLocalRotations()
    {
        if (fingerJointStartLocalRotations != null && fingerJointStartLocalRotations.Length > 0)
            return;

        fingerJointStartLocalRotations = new Quaternion[fingerJoints.Length];
        for (int i = 0; i < fingerJointStartLocalRotations.Length; i++)
        {
            fingerJointStartLocalRotations[i] = fingerJoints[i].transform.localRotation;
        }
    }

    private void GetJointsAndTargets()
    {
        if (fingerJoints != null && fingerJoints.Length > 0 || fingerTargets != null && fingerTargets.Length > 0)
            return;

        var fingerJointsList = new List<ConfigurableJoint>();
        var fingerTargetsList = new List<Transform>();

        for (int i = 0; i < physicsMapper.BoneTargets.Count; i++)
        {
            var jointTransform = physicsMapper.BoneTargets[i];
            if (jointTransform)
            {
                var joint = jointTransform.GetComponent<ConfigurableJoint>();
                if (joint)
                {
                    fingerJointsList.Add(joint);

                    var target = targetMapper.CustomBones[i];
                    if (!target)
                    {
                        Debug.LogError("Joint has no target");
                    }

                    fingerTargetsList.Add(target);
                }
            }
        }

        fingerJoints = fingerJointsList.ToArray();
        fingerTargets = fingerTargetsList.ToArray();
    }

    private void SetupObjectFollowPhysics()
    {
        if (GetComponent<ObjectFollowPhysics>())
            return;

        var objectFollowPhysics = gameObject.AddComponent<ObjectFollowPhysics>();
        objectFollowPhysics.target = targetMapper.transform;
        objectFollowPhysics.setKinematicOnLowConfidence = false;
        if (playerHand)
            objectFollowPhysics.oVRSkeleton = playerHand.oVRSkeleton;
    }

    private void SetupJoint()
    {
        _joint = GetComponent<ConfigurableJoint>();
        _startRotation = transform.localRotation;
        if (_joint)
            return;

        _joint = gameObject.AddComponent<ConfigurableJoint>();

        _joint.rotationDriveMode = RotationDriveMode.Slerp;
        _joint.slerpDrive = new JointDrive()
        {
            positionSpring = 1000000f,
            positionDamper = 10000f,
            maximumForce = 15f
        };

        var drive = new JointDrive()
        {
            positionSpring = 1000000f,
            positionDamper = 10000f,
            maximumForce = 50f
        };

        _joint.xDrive = drive;
        _joint.yDrive = drive;
        _joint.zDrive = drive;

        _joint.anchor = Vector3.zero;
        _joint.autoConfigureConnectedAnchor = false;
        _joint.connectedAnchor = transform.position;
        _joint.SetTargetRotationLocal(transform.rotation, _startRotation);
    }

    private void IgnoreCollisions()
    {
        var colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            for (int n = i + 1; n < colliders.Length; n++)
            {
                Physics.IgnoreCollision(colliders[i], colliders[n]);
            }
        }
    }
}