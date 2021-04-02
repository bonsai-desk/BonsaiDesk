﻿using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

public class LockObjectHand : MonoBehaviour, IHandTick
{
    public PlayerHand playerHand { get; set; }

    [HideInInspector] public ConfigurableJoint joint;

    public void Tick()
    {
        //TODO add drag if picking up larger object/blockArea with more than 4 blocks

        if (joint && !joint.connectedBody)
        {
            Destroy(joint);
            return;
        }

        //detach if object is inUse by someone else
        if (joint && joint.connectedBody)
        {
            var autoAuthority = joint.connectedBody.GetComponent<AutoAuthority>();
            if (!autoAuthority.ClientHasAuthority() && autoAuthority.InUse)
            {
                Destroy(joint);
                return;
            }
        }

        if (joint && (!playerHand.HandComponents.TrackingRecently ||
                      !playerHand.GetGesture(PlayerHand.Gesture.IndexTargetPinching) && !playerHand.GetGesture(PlayerHand.Gesture.Fist)))
        {
            DetachObject();
            return;
        }

        if (joint)
            return;

        //code below here if not holding object

        if (playerHand.GetGestureStart(PlayerHand.Gesture.IndexTargetPinching) || playerHand.GetGestureStart(PlayerHand.Gesture.Fist))
        {
            var hitAutoAuthority = GetLockObjectCandidate();
            if (hitAutoAuthority && !hitAutoAuthority.isKinematic && !hitAutoAuthority.InUse)
            {
                ConnectObject(hitAutoAuthority);
            }
        }
    }

    private AutoAuthority GetLockObjectCandidate()
    {
        //TODO use overlap sphere non alloc
        Collider[] pinchHits = new Collider[0];
        if (playerHand.GetGestureStart(PlayerHand.Gesture.IndexTargetPinching))
            pinchHits = Physics.OverlapSphere(playerHand.PinchPosition(), 0, PlayerHand.AllButHandsMask, QueryTriggerInteraction.Ignore);
        Collider[] fistHits = new Collider[0];
        if (playerHand.GetGestureStart(PlayerHand.Gesture.Fist) || playerHand.GetGestureStart(PlayerHand.Gesture.IndexTargetPinching))
            fistHits = Physics.OverlapSphere(playerHand.palm.position, 0.02f, PlayerHand.AllButHandsMask, QueryTriggerInteraction.Ignore);
        Collider[] hits = new Collider[pinchHits.Length + fistHits.Length];
        pinchHits.CopyTo(hits, 0);
        fistHits.CopyTo(hits, pinchHits.Length);
        for (int i = 0; i < hits.Length; i++)
        {
            //recursivly check if hit object or its parent has an AutoAuthority script
            var check = hits[i].transform;
            var hitAutoAuthority = check.GetComponent<AutoAuthority>();
            while (!hitAutoAuthority)
            {
                if (check.parent)
                    check = check.parent;
                else
                    break;
                hitAutoAuthority = check.GetComponent<AutoAuthority>();
            }

            if (hitAutoAuthority)
            {
                return hitAutoAuthority;
            }
        }

        return null;
    }

    private void DetachObject()
    {
        if (!joint)
            return;

        //OVRHand sometimes says hand index finger is not pinching even when it is and the confidence of hand/finger
        //is high. This causes the DetachObject function to be called.

        joint.connectedBody.GetComponent<AutoAuthority>().CmdRemoveInUse(NetworkClient.connection.identity.netId);
        Destroy(joint);
    }

    private void ConnectObject(AutoAuthority autoAuthority)
    {
        autoAuthority.CmdSetNewOwner(NetworkClient.connection.identity.netId, NetworkTime.time, true);

        joint = InputManager.Hands.GetHand(playerHand.skeletonType).PhysicsHand.gameObject.AddComponent<ConfigurableJoint>();
        joint.anchor = playerHand.transform.InverseTransformPoint(autoAuthority.transform.position);
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = Vector3.zero;

        JointDrive positionDrive = new JointDrive
        {
            positionSpring = 2500f,
            positionDamper = 1f,
            maximumForce = Mathf.Infinity
        };

        joint.xDrive = positionDrive;
        joint.yDrive = positionDrive;
        joint.zDrive = positionDrive;

        JointDrive rotationDrive = new JointDrive
        {
            positionSpring = 10f,
            positionDamper = 1f,
            maximumForce = Mathf.Infinity
        };

        joint.angularXDrive = rotationDrive;
        joint.angularYZDrive = rotationDrive;

        joint.connectedBody = autoAuthority.GetComponent<Rigidbody>();
    }
}