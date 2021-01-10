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
    public Rigidbody[] fingerJointBodies;
    public CapsuleCollider[] fingerCapsuleColliders;
    public CapsuleCollider[] palmCapsuleColliders;
    public Quaternion[] fingerJointStartLocalRotations;
    public Transform[] fingerTargets;

    public Vector3 jointOffset = new Vector3(0.035f, 0, 0);

    private float _snapBackDistanceThresholdSquared = 0.2f * 0.2f;
    
    // private readonly Quaternion _fixRotation = Quaternion.AngleAxis(180f, Vector3.up);

    private void Awake()
    {
        IgnoreCollisions();
        _rigidbody = GetComponent<Rigidbody>();
        if (!_initialized && targetMapper)
            Init();
    }

    private void Update()
    {
        CheckHit();
    }

    private void FixedUpdate()
    {
        if (!_initialized)
            return;

        bool notTracking = playerHand && !playerHand.Tracking();
        if (!notTracking)
        {
            if (Vector3.SqrMagnitude(transform.position - targetMapper.transform.position) >
                _snapBackDistanceThresholdSquared)
            {
                ResetFingerJoints();
            }

            UpdateJoint();
            UpdateFingerJoints();
            UpdateVelocity();
            _rigidbody.WakeUp();
        }
    }

    private void CheckHit()
    {
        _toDraw.Clear();
        for (int i = 0; i < fingerCapsuleColliders.Length; i++)
        {
            var capsule = fingerCapsuleColliders[i];
            var target = fingerTargets[i];

            //if thumb or pinky
            if (i == 0 || i == 12)
            {
                // ResetTransform(fingerJointBodies[i], joint.connectedAnchor,
                //     target.parent.localRotation * target.localRotation, true);
            }
            else
            {
                var pos = fingerTargets[i].position;
                _toDraw.Add((pos, pos, 0.01f));
            }
        }
    }

    private List<(Vector3, Vector3, float)> _toDraw = new List<(Vector3, Vector3, float)>();

    private void OnDrawGizmos()
    {
        foreach (var capsule in _toDraw)
        {
            DebugExtension.DrawCapsule(capsule.Item1, capsule.Item2 + new Vector3(0, 1f, 0), Color.blue, capsule.Item3);
        }
    }

    private void UpdateVelocity()
    {
        float velocityMagnitude = _rigidbody.velocity.magnitude;
        Vector3 direction = targetMapper.transform.position - transform.position;
        _rigidbody.velocity = direction.normalized * velocityMagnitude;
    }

    private void ResetFingerJoints()
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

        _joint.connectedAnchor = targetMapper.transform.position + targetMapper.transform.rotation * jointOffset;
        _joint.SetTargetRotationLocal(targetMapper.transform.localRotation, _startRotation);
    }

    public void Init()
    {
        if (_initialized)
            return;
        _initialized = true;

        SetupJoint();
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
            palmCapsuleColliders[i] = transform.GetChild(i).GetChild(0).GetComponent<CapsuleCollider>();
        }
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
        
        if (playerHand.skeletonType == OVRSkeleton.SkeletonType.HandLeft)
        {
            jointOffset = -jointOffset;
            // _joint.anchor = _jointOffset;
        }
        // else
        // {
        //     _joint.anchor = -_jointOffset;
        // }
        
        _joint.anchor = jointOffset;
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