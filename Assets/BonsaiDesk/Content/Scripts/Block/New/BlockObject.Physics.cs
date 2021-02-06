﻿using System.Collections;
using System.Collections.Generic;
using Mirror;
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
    private bool _touchingHand = false;
    private bool _lastInCubeArea = false;

    private void PhysicsFixedUpdate()
    {
        if (_autoAuthority.HasAuthority())
        {
            if (blockObjectData.Blocks.Count == 1)
            {
                var leftHandLockedJoint = InputManager.Hands.Left.PlayerHand.GetIHandTick<LockObjectHand>().joint;
                var rightHandLockedJoint = InputManager.Hands.Left.PlayerHand.GetIHandTick<LockObjectHand>().joint;
                if (leftHandLockedJoint && leftHandLockedJoint.connectedBody == _body ||
                    rightHandLockedJoint && rightHandLockedJoint.connectedBody == _body)
                {
                    _touchingHand = true;
                }

                CalculateForces();
            }
            else
            {
                _body.useGravity = true;
            }
        }

        _touchingHand = false;
    }

    /// <summary>
    /// calculate forces to move this object towards another BlockObject. This function runs for BlockObjects
    /// which have only 1 block
    /// </summary>
    private void CalculateForces()
    {
        BlockObject blockObject = null;
        bool isInCubeArea = false;
        bool isNearHole = false;
        foreach (var nextOwnedObject in _blockObjectAuthorities)
        {
            //TODO go on with calculation and interact if in area
            if (!nextOwnedObject.HasAuthority())
            {
                continue;
            }

            var nextBlockObject = nextOwnedObject.GetComponent<BlockObject>();
            if (nextBlockObject != null && nextBlockObject != this)
            {
                blockObject = nextBlockObject;
                Vector3Int coord = GetOnlyBlockCoord();
                Vector3 blockPosition = transform.TransformPoint(coord);
                Vector3 positionLocalToCubeArea = blockObject.transform.InverseTransformPoint(blockPosition);
                Vector3Int blockCoord = Vector3Int.RoundToInt(positionLocalToCubeArea);
                var inArea = BlockUtility.InCubeArea(blockObject.blockObjectData, blockCoord,
                    blockObjectData.Blocks[coord].id);
                if (inArea.isNearHole)
                    isNearHole = true;
                isInCubeArea = inArea.isInCubeArea;
                isInCubeArea = isInCubeArea &&
                               ((transform.parent == null && _touchingHand && potentialBlocksParent.childCount == 0) ||
                                transform.parent == blockObject.potentialBlocksParent);

                if (isInCubeArea)
                {
                    // Vector3 bearingOffset = Vector3.zero;
                    // if (blockObject.blockObjectData.Blocks.TryGetValue(blockCoord, out BlockArea.MeshBlock block))
                    // {
                    //     if (Blocks.blocks[block.id].blockType == Block.BlockType.bearing)
                    //     {
                    //         bearingOffset = blockArea.transform.rotation *
                    //                         BlockArea.IntToQuat(block.forward, block.up) * Vector3.up * 0.1f *
                    //                         BlockArea.cubeScale;
                    //     }
                    // }

                    var position = blockObject.transform.TransformPoint(blockCoord);
                    var rotation = blockObject.GetTargetRotation(transform.rotation, blockCoord,
                        Blocks.blocks[blockObjectData.Blocks[coord].id].blockType);

                    if (BlockUtility.AboutEquals(blockPosition, position) &&
                        BlockUtility.AboutEquals(transform.rotation, rotation))
                    {
                        // transform.parent = initialParent;
                        // body.useGravity = true;
                        // myBlockArea.CmdSetActive(true);

                        // transform.GetChild(1).gameObject.layer = LayerMask.NameToLayer("blockArea");
                        // if (layer == -1)
                        //     layer = GetLayerRecursively(transform.GetChild(3).gameObject);
                        // if (layer == 0)
                        //     SetLayerRecursively(transform.GetChild(3).gameObject, LayerMask.NameToLayer("blockArea"));
                        // else
                        //     SetLayerRecursively(transform.GetChild(3).gameObject, layer);

                        // blockArea.LockBlock(gameObject);
                        for (var i = blockObject.potentialBlocksParent.childCount - 1; i >= 0; i--)
                        {
                            blockObject.potentialBlocksParent.GetChild(i).parent = null;
                        }

                        _blockObjectAuthorities.Remove(_autoAuthority);
                        gameObject.SetActive(false);
                        Destroy(gameObject);

                        var localRotation = Quaternion.Inverse(blockObject.transform.rotation) * rotation;
                        blockObject.AddBlock(blockObjectData.Blocks[coord].id, blockCoord,
                            BlockUtility.SnapToNearestRightAngle(localRotation), true);

                        return;
                    }

                    //float distance = Vector3.Distance(transform.position, position);
                    // float scale = distance / (BlockArea.cubeScale / 2f);
                    // scale = 1 - scale;
                    // scale = BlockArea.cubeScale / 2f + BlockArea.cubeScale / 2f * scale;
                    // transform.localScale = new Vector3(scale, scale, scale);
                    AddForceTowardsTarget(blockPosition, position);
                    AddTorqueTowardsTarget(rotation);
                    break;
                }
            }
        }

        // if ((isInCubeArea && isNearHole) || (isNearHole && touchingHand))
        // {
        //     transform.GetChild(1).gameObject.layer = LayerMask.NameToLayer("block");
        //     bool sphere = true;
        //     foreach (var block in myBlockArea.blocks)
        //         sphere = Blocks.blocks[block.Value.id].hasSphere;
        //     if (sphere)
        //         SetLayerRecursively(transform.GetChild(3).gameObject, LayerMask.NameToLayer("block"));
        // }
        // else if (isInCubeArea && !touchingHand &&
        //          Blocks.blocks[myBlockArea.blocks[myBlockArea.OnlyBlock()].id].blockType == Block.BlockType.bearing)
        // {
        //     transform.GetChild(1).gameObject.layer = LayerMask.NameToLayer("onlyHands");
        //     bool sphere = true;
        //     foreach (var block in myBlockArea.blocks)
        //         sphere = Blocks.blocks[block.Value.id].hasSphere;
        //     if (sphere)
        //         SetLayerRecursively(transform.GetChild(3).gameObject, LayerMask.NameToLayer("onlyHands"));
        // }
        // else
        // {
        //     transform.GetChild(1).gameObject.layer = LayerMask.NameToLayer("blockArea");
        //     if (layer == -1)
        //         layer = GetLayerRecursively(transform.GetChild(3).gameObject);
        //     if (layer == 0)
        //         SetLayerRecursively(transform.GetChild(3).gameObject, LayerMask.NameToLayer("blockArea"));
        //     else
        //         SetLayerRecursively(transform.GetChild(3).gameObject, layer);
        // }
        //

        _body.useGravity = !isInCubeArea;

        if (isInCubeArea != _lastInCubeArea)
        {
            if (isInCubeArea)
            {
                if (blockObject != null)
                {
                    transform.parent = blockObject.potentialBlocksParent;
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
    }

    private void HandleCollision(Collision collision)
    {
        if (MaskIsValid(collision.gameObject.layer))
        {
            _touchingHand = true;
        }
    }

    private bool MaskIsValid(int layer)
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
        if (Mathf.Approximately(distance, 0) || _body.velocity.magnitude * Time.deltaTime > distance ||
            Mathf.Approximately(MoveForce, 0))
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
            Vector3 targetTorque =
                q * Vector3.Scale(_body.inertiaTensor, (Quaternion.Inverse(q) * angularVelocityDifference)) /
                Time.deltaTime; // / 2f;

            //if already at target, allow unlimited torque to decelerate (not realistic)
            if (Mathf.Approximately(angle, 0) || Mathf.Approximately(RotationTorque, 0))
                _body.AddTorque(targetTorque, ForceMode.Force);
            else //clamp target torque to rotationForce
                _body.AddTorque(Vector3.ClampMagnitude(targetTorque, RotationTorque), ForceMode.Force);
        }
    }
}