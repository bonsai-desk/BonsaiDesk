using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class LockObjectHand : MonoBehaviour, IHandTick
{
    public PlayerHand playerHand { get; set; }

    private ConfigurableJoint _joint;

    public void Tick()
    {
        //TODO add drag if picking up larger object/blockArea with more than 4 blocks

        if (_joint && !_joint.connectedBody)
        {
            Destroy(_joint);
            return;
        }

        if (_joint && (!playerHand.Tracking() ||
                       !playerHand.GetGesture(PlayerHand.Gesture.IndexPinching) &&
                       !playerHand.GetGesture(PlayerHand.Gesture.Fist)))
        {
            DetachObject();
            return;
        }

        if (_joint)
            return;

        //code below here if not holding object
        
        if (playerHand.GetGestureStart(PlayerHand.Gesture.IndexPinching) ||
            playerHand.GetGestureStart(PlayerHand.Gesture.Fist))
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
        if (playerHand.GetGestureStart(PlayerHand.Gesture.IndexPinching))
            pinchHits = Physics.OverlapSphere(playerHand.PhysicsPinchPosition(), 0, PlayerHand.AllButHands,
                QueryTriggerInteraction.Ignore);
        Collider[] fistHits = new Collider[0];
        if (playerHand.GetGestureStart(PlayerHand.Gesture.Fist))
            fistHits = Physics.OverlapSphere(playerHand.holdPosition.position, 0.02f, PlayerHand.AllButHands,
                QueryTriggerInteraction.Ignore);
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
        if (!_joint)
            return;
        
        //OVRHand sometimes says hand index finger is not pinching even when it is and the confidence of hand/finger
        //is high. This causes the DetachObject function to be called.

        _joint.connectedBody.GetComponent<AutoAuthority>().CmdRemoveInUse();
        Destroy(_joint);
    }

    private void ConnectObject(AutoAuthority autoAuthority)
    {
        autoAuthority.CmdSetNewOwner(NetworkClient.connection.identity.netId, NetworkTime.time, true);
        
        _joint = playerHand.gameObject.AddComponent<ConfigurableJoint>();
        _joint.anchor = playerHand.transform.InverseTransformPoint(autoAuthority.transform.position);
        _joint.autoConfigureConnectedAnchor = false;
        _joint.connectedAnchor = Vector3.zero;
        
        JointDrive positionDrive = new JointDrive
        {
            positionSpring = 2500f,
            positionDamper = 1f,
            maximumForce = Mathf.Infinity
        };
    
        _joint.xDrive = positionDrive;
        _joint.yDrive = positionDrive;
        _joint.zDrive = positionDrive;
    
        JointDrive rotationDrive = new JointDrive
        {
            positionSpring = 10f,
            positionDamper = 1f,
            maximumForce = Mathf.Infinity
        };
    
        _joint.angularXDrive = rotationDrive;
        _joint.angularYZDrive = rotationDrive;

        _joint.connectedBody = autoAuthority.GetComponent<Rigidbody>();
    }
}