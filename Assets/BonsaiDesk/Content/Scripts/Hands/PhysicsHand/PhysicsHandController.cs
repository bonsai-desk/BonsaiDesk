using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsHandController : MonoBehaviour
{
    public OVRHandTransformMapper targetMapper;
    public PlayerHand playerHand;

    private bool _initialized = false;
    private ConfigurableJoint _joint;
    private Rigidbody _rigidbody;
    private Quaternion _startRotation;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (!_initialized && targetMapper)
            Init();
    }

    private void Update()
    {
        if (!_initialized && targetMapper)
            Init();
    }

    private void FixedUpdate()
    {
        if (!_initialized)
            return;

        if (playerHand && !playerHand.Tracking())
        {
            _rigidbody.WakeUp();
        }
        else
        {
            _joint.connectedAnchor = targetMapper.transform.position;
            _joint.SetTargetRotationLocal(targetMapper.transform.localRotation, _startRotation);
            _rigidbody.WakeUp();
        }
    }

    public void Init()
    {
        if (_initialized)
            return;
        _initialized = true;

        SetupJoint();
        SetupObjectFollowPhysics();
    }

    private void SetupObjectFollowPhysics()
    {
        var objectFollowPhysics = gameObject.AddComponent<ObjectFollowPhysics>();
        objectFollowPhysics.target = targetMapper.transform;
        objectFollowPhysics.setKinematicOnLowConfidence = false;
        if (playerHand)
            objectFollowPhysics.oVRSkeleton = playerHand.oVRSkeleton;
    }

    private void SetupJoint()
    {
        _startRotation = transform.localRotation;
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
        _joint.connectedAnchor = Vector3.zero;
        _joint.targetRotation = Quaternion.identity;
    }
}