using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PhysicsHandController : MonoBehaviour
{
    public OVRSkeleton.SkeletonType skeletonType = OVRSkeleton.SkeletonType.None;
    public OVRHandTransformMapper physicsMapper;
    public OVRHandTransformMapper targetMapper;

    public ConfigurableJoint[] fingerJoints;
    public Rigidbody[] fingerJointBodies;
    public CapsuleCollider[] fingerCapsuleColliders;
    public CapsuleCollider[] palmCapsuleColliders;
    public MeshCollider palmCollider;
    public Quaternion[] fingerJointStartLocalRotations;
    public Transform[] fingerTargets;

    public Vector3 jointOffset = new Vector3(0.035f, 0, 0);

    private bool _initialized = false;
    private ConfigurableJoint _joint;
    private Rigidbody _rigidbody;
    private Quaternion _startRotation;

    private const float SnapBackDistanceThresholdSquared = 0.2f * 0.2f;

    private bool _capsulesActive = false;
    private bool _capsulesActiveTarget = false;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (!_initialized && targetMapper && skeletonType != OVRSkeleton.SkeletonType.None)
            Init();
    }

    private void FixedUpdate()
    {
        if (!_initialized)
            return;

        TrySetCapsulesActiveToTarget();

        if ((float.IsNaN(transform.position.x) ||
             float.IsNaN(transform.position.y) ||
             float.IsNaN(transform.position.z) ||
             Vector3.SqrMagnitude(transform.position - targetMapper.transform.position) >
             SnapBackDistanceThresholdSquared)
            && !CheckHit())
        {
            ResetFingerJoints();
        }

        UpdateJoint();
        UpdateFingerJoints();
        UpdateVelocity();
        _rigidbody.WakeUp();
    }

    // public void SetCapsulesOffThenTryOn()
    // {
    //     if (!CheckHit())
    //         return;
    //
    //     SetCapsulesActiveTarget(false);
    //     SetCapsulesActiveTarget(true);
    // }

    public void SetCapsulesActiveTarget(bool active)
    {
        _capsulesActiveTarget = active;
        TrySetCapsulesActiveToTarget();
    }

    private void TrySetCapsulesActiveToTarget()
    {
        if (_capsulesActive == _capsulesActiveTarget)
            return;

        if (_capsulesActiveTarget && CheckHit())
            return;

        SetCapsulesActive(_capsulesActiveTarget);
    }

    private void SetCapsulesActive(bool active)
    {
        _capsulesActive = active;
        _capsulesActiveTarget = active;

        //only turn palm capsule off, never on
        if (!active)
        {
            for (int i = 0; i < palmCapsuleColliders.Length; i++)
            {
                palmCapsuleColliders[i].isTrigger = !active;
            }
        }

        for (int i = 0; i < fingerCapsuleColliders.Length; i++)
        {
            fingerCapsuleColliders[i].isTrigger = !active;
        }

        palmCollider.isTrigger = !active;
    }

    private bool CheckHit()
    {
        for (int i = 0; i < palmCapsuleColliders.Length; i++)
        {
            var capsule = palmCapsuleColliders[i];

            var start = targetMapper.transform.TransformPoint(capsule.transform.localPosition);
            var direction = targetMapper.transform.rotation * capsule.transform.localRotation * Vector3.right;
            var end = start + direction.normalized * (capsule.height - (capsule.radius * 2f));

            if (Physics.CheckCapsule(start, end, capsule.radius, ~0, QueryTriggerInteraction.Ignore))
                return true;
        }

        for (int i = 0; i < fingerCapsuleColliders.Length; i++)
        {
            var capsule = fingerCapsuleColliders[i];

            var start = fingerTargets[i].TransformPoint(capsule.transform.localPosition);
            var direction = fingerTargets[i].rotation * capsule.transform.localRotation * Vector3.right;
            var end = start + direction.normalized * (capsule.height - (capsule.radius * 2f));

            if (Physics.CheckCapsule(start, end, capsule.radius, ~0, QueryTriggerInteraction.Ignore))
                return true;
        }

        return false;
    }

    private void UpdateVelocity()
    {
        float velocityMagnitude = _rigidbody.velocity.magnitude;
        Vector3 direction = targetMapper.transform.position - transform.position;
        _rigidbody.velocity = direction.normalized * velocityMagnitude;
    }

    public void ResetFingerJoints()
    {
        ResetTransform(_rigidbody, targetMapper.transform.position, targetMapper.transform.rotation, false);
        for (int i = 0; i < fingerJoints.Length; i++)
        {
            var joint = fingerJoints[i];
            var target = fingerTargets[i];

            //if thumb or pinky
            if (i == 0 || i == 12)
            {
                ResetTransform(fingerJointBodies[i], joint.connectedAnchor,
                    target.parent.localRotation * target.localRotation, true);
            }
            else
            {
                ResetTransform(fingerJointBodies[i], joint.connectedAnchor, target.localRotation, true);
            }
        }
    }

    private void ResetTransform(Rigidbody body, Vector3 position, Quaternion rotation, bool useLocal)
    {
        if (useLocal)
        {
            body.transform.localPosition = position;
            body.transform.localRotation = rotation;
        }
        else
        {
            body.transform.position = position;
            body.transform.rotation = rotation;
        }

        body.MovePosition(body.transform.position);
        body.MoveRotation(body.transform.rotation);
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
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
                if (!_capsulesActive)
                {
                    fingerJointBodies[i].transform.localRotation = target.parent.localRotation * target.localRotation;
                }
            }
            else
            {
                joint.SetTargetRotationLocal(target.localRotation, jointStartRotation);
                if (!_capsulesActive)
                {
                    fingerJointBodies[i].transform.localRotation = target.localRotation;
                }
            }
        }
    }

    private void UpdateJoint()
    {
        if (!_joint)
            return;

        _joint.connectedAnchor = targetMapper.transform.position + targetMapper.transform.rotation * jointOffset;
        _joint.SetTargetRotationLocal(targetMapper.transform.localRotation, _startRotation);

        if (!_capsulesActive)
        {
            _joint.transform.position = targetMapper.transform.position;
            _joint.transform.localRotation = targetMapper.transform.localRotation;
        }
    }

    public void Init()
    {
        if (_initialized)
            return;
        _initialized = true;

        IgnoreCollisions();
        SetupJoint();
        GetJointsAndTargets();
        GetJointStartLocalRotations();

        SetCapsulesActive(false);
        SetCapsulesActiveTarget(true);
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

        fingerJointBodies = new Rigidbody[fingerJoints.Length];
        fingerCapsuleColliders = new CapsuleCollider[fingerJoints.Length];
        for (int i = 0; i < fingerJoints.Length; i++)
        {
            fingerJointBodies[i] = fingerJoints[i].GetComponent<Rigidbody>();
            fingerCapsuleColliders[i] = fingerJoints[i].transform.GetChild(0).GetComponent<CapsuleCollider>();
        }

        palmCapsuleColliders = new CapsuleCollider[4];
        for (int i = 0; i < palmCapsuleColliders.Length; i++)
        {
            //                                             +1 to get past the palm collider
            palmCapsuleColliders[i] = transform.GetChild(i + 1).GetChild(0).GetComponent<CapsuleCollider>();
        }

        palmCollider = GetComponentInChildren<MeshCollider>();
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
            positionSpring = float.MaxValue,
            positionDamper = 10000f,
            maximumForce = 50f
        };
        _joint.xDrive = drive;
        _joint.yDrive = drive;
        _joint.zDrive = drive;

        if (skeletonType == OVRSkeleton.SkeletonType.None)
        {
            Debug.LogError("Cannot determine joint offset without skeleton type.");
        }

        if (skeletonType == OVRSkeleton.SkeletonType.HandLeft)
        {
            jointOffset = -jointOffset;
        }

        _joint.anchor = jointOffset;
        _joint.autoConfigureConnectedAnchor = false;
        _joint.connectedAnchor = Vector3.zero;
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