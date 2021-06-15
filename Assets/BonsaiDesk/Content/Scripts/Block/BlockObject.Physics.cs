using System.Collections;
using System.Collections.Generic;
using Mirror;
using mixpanel;
using UnityEngine;

public partial class BlockObject
{
    //force constants
    public const float CubeMass = 1f;
    private const float LbsForce = 0.1f;
    private const float LbsTorque = 0.01f;

    private const float LbsToKg = 0.45359237f;

    private const float MoveForce = LbsForce * LbsToKg * 9.81f;
    private const float RotationTorque = LbsTorque * LbsToKg * 9.81f;

    //private variables
    private bool _touchingHand;
    private float _lastTouchingHandTime;

    private bool TouchingHand
    {
        get => _touchingHand;
        set
        {
            _touchingHand = value;
            if (_touchingHand)
            {
                _lastTouchingHandTime = Time.time;
            }
        }
    }

    private bool _lastInCubeArea;

    //layers
    private int _blockLayer;
    private int _blockAreaLayer;

    private void PhysicsStart()
    {
        _blockLayer = LayerMask.NameToLayer("block");
        _blockAreaLayer = LayerMask.NameToLayer("blockArea");
    }

    private void CalculateBearingFriction()
    {
        var rootBlockObject = BlockUtility.GetRootBlockObject(this);
        if (rootBlockObject == this)
        {
            var blockObjects = BlockUtility.GetBlockObjectsFromRoot(rootBlockObject);
            for (int i = blockObjects.Count - 1; i >= 0; i--)
            {
                ApplyFriction(blockObjects[i]);
            }
        }
    }

    private static void ApplyFriction(BlockObject blockObject)
    {
        if (!blockObject._joint || !blockObject._joint.connectedBody)
        {
            return;
        }

        const float friction = 0.75f; //the percent of the energy which will be lost each second
        const float maxVelocityChange = 5f;
        const float forceRatio = 0.5f; //the percent of the force which goes to blockObject vs the thing it is attached to

        var worldAxis = blockObject.transform.TransformVector(blockObject._joint.axis).normalized;

        var relativeAngularVelocity = blockObject._body.angularVelocity - blockObject._joint.connectedBody.angularVelocity;
        var rotationSpeed = Vector3.Dot(worldAxis, relativeAngularVelocity);
        var rotationDirection = rotationSpeed > 0 ? 1f : -1f;

        var fixedSlowDown = Mathf.Min(maxVelocityChange * Time.fixedDeltaTime, Mathf.Abs(rotationSpeed));
        var proportionalSlowDown = Mathf.Abs(friction * Time.deltaTime * rotationSpeed);
        var slowDown = Mathf.Max(fixedSlowDown, proportionalSlowDown);

        var resistTorque = slowDown * rotationDirection * -worldAxis;
        var angularVelocityChange = resistTorque;

        blockObject._body.AddTorque(angularVelocityChange * forceRatio, ForceMode.VelocityChange);
        blockObject._joint.connectedBody.AddTorque(-angularVelocityChange * (1f - forceRatio), ForceMode.VelocityChange);
    }

    private void PhysicsFixedUpdate()
    {
        if (Joint && !Joint.connectedBody)
        {
            //this is not an error because it will probably correct itself
            print("Joint exists but is not connected to anything. Destroying joint.");
            Destroy(_joint);
            _joint = null;

            if (SyncJoint.connected)
            {
                print("Attempting to reconnect joint");
                if (SyncJoint.attachedTo == null)
                {
                    Debug.LogError("SyncJoint connected, but attachedTo is null");
                }
                else if (SyncJoint.attachedTo.Value)
                {
                    Debug.LogError("SyncJoint connected, but attachedTo.Value is null");
                }

                ConnectJoint(SyncJoint);
            }
        }

        if (_autoAuthority.HasAuthority())
        {
            if (MeshBlocks.Count == 1 && !SyncJoint.connected && ActiveLocal)
            {
                var leftHandLockedJoint = InputManager.Hands.Left.PlayerHand.GetIHandTick<LockObjectHand>().joint;
                var rightHandLockedJoint = InputManager.Hands.Left.PlayerHand.GetIHandTick<LockObjectHand>().joint;
                if (leftHandLockedJoint && leftHandLockedJoint.connectedBody == _body || rightHandLockedJoint && rightHandLockedJoint.connectedBody == _body)
                {
                    TouchingHand = true;
                }

                if (!_joint && !SyncJoint.connected && ActiveLocal)
                {
                    CalculateForces();
                }
                else
                {
                    _body.useGravity = true;
                }
            }
            else
            {
                _body.useGravity = true;
            }
        }

        TouchingHand = false;
    }

    /// <summary>
    /// calculate forces to move this object towards another BlockObject. This function runs for BlockObjects
    /// which have only 1 block and are not connected with a joint to another blockObject
    /// </summary>
    private void CalculateForces()
    {
        BlockObject blockObject = null;
        bool isInCubeArea = false;
        bool isNearHole = false;
        foreach (var nextOwnedObject in _blockObjectAuthorities)
        {
            if (!nextOwnedObject.HasAuthority())
            {
                continue;
            }

            var nextBlockObject = nextOwnedObject.GetComponent<BlockObject>();
            if (nextBlockObject != null && nextBlockObject != this && nextBlockObject.ActiveLocal)
            {
                blockObject = nextBlockObject;
                Vector3Int coord = GetOnlyMeshBlockCoord();
                Vector3 blockPosition = transform.TransformPoint(coord);
                Vector3 positionLocalToCubeArea = blockObject.transform.InverseTransformPoint(blockPosition);
                Vector3Int blockCoord = Vector3Int.RoundToInt(positionLocalToCubeArea);
                var inArea = BlockUtility.InCubeArea(blockObject, blockCoord, MeshBlocks[coord].name);
                if (inArea.isNearHole)
                    isNearHole = true;
                isInCubeArea = inArea.isInCubeArea;
                isInCubeArea = isInCubeArea && ((transform.parent == null && TouchingHand && _potentialBlocksParent.childCount == 0) ||
                                                transform.parent == blockObject._potentialBlocksParent);

                if (isInCubeArea)
                {
                    Vector3 bearingOffset = Vector3.zero;
                    var attachingToBearing = blockObject.MeshBlocks.TryGetValue(blockCoord, out MeshBlock block) && block.name == "bearing";
                    if (attachingToBearing)
                    {
                        bearingOffset = blockObject.transform.rotation * block.rotation * (0.1f * CubeScale * Vector3.up);
                    }

                    var position = blockObject.transform.TransformPoint(blockCoord) + bearingOffset;
                    var rotation = blockObject.GetTargetRotation(transform.rotation, blockCoord, global::Blocks.GetBlock(MeshBlocks[coord].name).blockType);

                    if (BlockUtility.AboutEquals(blockPosition, position) && BlockUtility.AboutEquals(transform.rotation, rotation))
                    {
                        for (var i = blockObject._potentialBlocksParent.childCount - 1; i >= 0; i--)
                        {
                            blockObject._potentialBlocksParent.GetChild(i).parent = null;
                        }

                        blockObject.transform.parent = null;

                        if (attachingToBearing)
                        {
                            transform.position = position;
                            transform.rotation = rotation;
                            _body.MovePosition(position);
                            _body.MoveRotation(rotation);

                            //construct the SyncJoint which contains all of the data required to create the joint
                            NetworkIdentity attachedTo = blockObject.netIdentity;
                            Quaternion rotationLocalToAttachedTo = Quaternion.Inverse(blockObject.transform.rotation) * rotation;
                            byte byteLocalRotation = BlockUtility.QuaternionToByte(rotationLocalToAttachedTo);
                            byte byteBearingLocalRotation = BlockUtility.QuaternionToByte(blockObject.MeshBlocks[blockCoord].rotation);
                            SyncJoint jointInfo = new SyncJoint(new NetworkIdentityReference(attachedTo), byteLocalRotation, byteBearingLocalRotation, coord,
                                blockCoord);

                            //connect the joint
                            Mixpanel.Track("Attach Block To Bearing");
                            CmdConnectJoint(jointInfo);

                            //client side prediction
                            ConnectJoint(jointInfo);
                            blockObject._connectedToSelfChanges.Enqueue((blockCoord, new NetworkIdentityReference(netIdentity),
                                SyncDictionary<Vector3Int, NetworkIdentityReference>.Operation.OP_ADD));

                            //sound
                            bearingAttachSound.PlaySoundAt(position);
                        }
                        else
                        {
                            _blockObjectAuthorities.Remove(_autoAuthority);
                            ActiveLocal = false; //just waiting for the server to delete this object

                            var localRotation = Quaternion.Inverse(blockObject.transform.rotation) * rotation;
                            localRotation = BlockUtility.SnapToNearestRightAngle(localRotation) * MeshBlocks[coord].rotation;
                            var localRotationByte = BlockUtility.QuaternionToByte(localRotation);

                            //sound
                            blockAttachSound.PlaySoundAt(position, 0, 0.35f, 0.6f);

                            Mixpanel.Track("Add Block");
                            blockObject.CmdAddBlock(MeshBlocks[coord].name, blockCoord, localRotationByte, netIdentity);

                            //client side prediction
                            var syncBlock = new SyncBlock(MeshBlocks[coord].name, BlockUtility.QuaternionToByte(localRotation));
                            blockObject._blockChanges.Enqueue((blockCoord, syncBlock, SyncDictionary<Vector3Int, SyncBlock>.Operation.OP_ADD));
                        }

                        return;
                    }

                    AddForceTowardsTarget(blockPosition, position);
                    AddTorqueTowardsTarget(rotation);
                    break;
                }
            }
        }

        //if transparent cube is active and has a hitbox, disable it if the block is in the process of being snapped and not being touched
        if (_transparentCubeCollider)
        {
            _transparentCubeCollider.enabled = !(isInCubeArea && !TouchingHand);
        }

        if ((isInCubeArea && isNearHole) || (isNearHole && TouchingHand))
        {
            _physicsBoxesObject.gameObject.layer = _blockLayer;
        }
        else
        {
            _physicsBoxesObject.gameObject.layer = _blockAreaLayer;
        }

        _body.useGravity = !isInCubeArea;

        if (isInCubeArea != _lastInCubeArea)
        {
            if (isInCubeArea)
            {
                if (blockObject)
                {
                    transform.parent = blockObject._potentialBlocksParent;
                }
                else
                {
                    Debug.LogError("This should never print.");
                }
            }
            else
            {
                transform.parent = null;
            }
        }

        _lastInCubeArea = isInCubeArea;
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        HandleCollision(collision);
        BlockBreakCollision(collision);
    }

    private void HandleCollision(Collision collision)
    {
        if (MaskIsValid(collision.gameObject.layer))
        {
            TouchingHand = true;
        }
    }

    private void BlockBreakCollision(Collision collision)
    {
        if (collision.gameObject.layer != PlayerHand.IndexTipLayer || collision.contactCount <= 0)
        {
            return;
        }

        var physicsHandController = collision.gameObject.GetComponentInParent<PhysicsHandController>();
        if (!physicsHandController)
        {
            return;
        }

        var skeletonType = physicsHandController.skeletonType;
        var handBreakMode = InputManager.Hands.GetHand(skeletonType).PlayerHand.GetIHandTick<BlockBreakHand>().HandBreakMode;

        if (handBreakMode == BlockBreakHand.BreakMode.None)
        {
            return;
        }

        if (handBreakMode == BlockBreakHand.BreakMode.Whole && Blocks.Count == 1 && !_syncJoint.connected && ConnectedToSelf.Count == 0)
        {
            handBreakMode = BlockBreakHand.BreakMode.Single;
        }

        switch (handBreakMode)
        {
            case BlockBreakHand.BreakMode.Single:

                ContactPoint contact = collision.GetContact(0);
                Vector3 contactPointRelativeToBlockObject = transform.InverseTransformPoint(contact.point);
                Vector3Int blockCoord = Vector3Int.RoundToInt(contactPointRelativeToBlockObject);
                if (MeshBlocks.ContainsKey(blockCoord)) //first check if the contact point is inside a block. this is to handle the case of blockGameObjects
                {
                    DamageBlock(blockCoord);
                }
                else //if contact point is not inside a block, move half a block in the finger contact normal direction to reach the block you are touching
                {
                    contactPointRelativeToBlockObject = transform.InverseTransformPoint(contact.point + contact.normal * 0.5f * CubeScale);
                    blockCoord = Vector3Int.RoundToInt(contactPointRelativeToBlockObject);
                    if (MeshBlocks.ContainsKey(blockCoord))
                    {
                        DamageBlock(blockCoord);
                    }
                }

                break;
            case BlockBreakHand.BreakMode.Whole:
            case BlockBreakHand.BreakMode.Duplicate:
            case BlockBreakHand.BreakMode.Save:
                //in the case of a whole effect, find the root object (through bearings connections) and increment that
                BlockUtility.GetRootBlockObject(this).IncrementWholeEffect(handBreakMode, collision.GetContact(0).point);
                break;
            default:
                Debug.LogError("Unknown mode: " + handBreakMode);
                break;
        }
    }

    private static bool MaskIsValid(int layer)
    {
        return PlayerHand.HandsMask == (PlayerHand.HandsMask | (1 << layer));
    }

    private void AddForceTowardsTarget(Vector3 blockPosition, Vector3 targetPosition)
    {
        Vector3 towards = targetPosition - blockPosition;

        //speed is maintained, but direction is changed to be towards target position (not realistic)
        _body.velocity = towards.normalized * _body.velocity.magnitude;

        //targetForce is the force required to get a velocity that would make the object get to the target position this update tick
        Vector3 targetVelocity = towards / Time.deltaTime;
        Vector3 velocityDifference = targetVelocity - _body.velocity;
        Vector3 targetForce = velocityDifference * _body.mass / Time.deltaTime / 2f;
        float distance = Vector3.Distance(blockPosition, targetPosition);

        //if already at target or have enough velocity to overshoot target this update tick, allow unlimited force to decelerate (not realistic)
        if (Mathf.Approximately(distance, 0) || _body.velocity.magnitude * Time.deltaTime > distance || Mathf.Approximately(MoveForce, 0))
            _body.AddForce(targetForce, ForceMode.Force);
        else //clamp target force to moveForce
            _body.AddForce(Vector3.ClampMagnitude(targetForce, MoveForce), ForceMode.Force);
    }

    private void AddTorqueTowardsTarget(Quaternion targetRotation)
    {
        var delta = targetRotation * Quaternion.Inverse(_body.rotation);

        delta.ToAngleAxis(out float angle, out Vector3 axis);

        // We get an infinite axis in the event that our rotation is already aligned.
        // allow instant deceleration to stop rotation (not realistic)
        if (float.IsInfinity(axis.x))
        {
            _body.angularVelocity = Vector3.zero;
        }
        else
        {
            if (angle > 180f)
                angle -= 360f;

            Vector3 targetAngularVelocity = Mathf.Deg2Rad * angle * axis.normalized / Time.deltaTime;

            //angular speed is maintained, but direction is changed to be towards target rotation (not realistic)
            _body.angularVelocity = targetAngularVelocity.normalized * _body.angularVelocity.magnitude;

            Vector3 angularVelocityDifference = targetAngularVelocity - _body.angularVelocity;

            Quaternion q = transform.rotation * _body.inertiaTensorRotation;
            Vector3 targetTorque = q * Vector3.Scale(_body.inertiaTensor, (Quaternion.Inverse(q) * angularVelocityDifference)) / Time.deltaTime; // / 2f;

            //if already at target, allow unlimited torque to decelerate (not realistic)
            if (Mathf.Approximately(angle, 0) || Mathf.Approximately(RotationTorque, 0))
                _body.AddTorque(targetTorque, ForceMode.Force);
            else //clamp target torque to rotationForce
                _body.AddTorque(Vector3.ClampMagnitude(targetTorque, RotationTorque), ForceMode.Force);
        }
    }
}