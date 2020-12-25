using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReturnToVoidHand : MonoBehaviour, IHandTick
{
    public MoveToDesk moveToDesk;
    public AngleToObject angleToHead;
    public Transform head;
    public float angleToHeadThreshold = 20f;
    // public float headAngleToHorizonThreshold = 15f;

    public void Tick(PlayerHand playerHand)
    {
        if (moveToDesk.oriented &&
            playerHand.GetGestureStart(PlayerHand.Gesture.Fist) &&
            angleToHead.AngleBelowThreshold() &&
            Vector3.Angle(-angleToHead.transform.forward, head.forward) < angleToHeadThreshold)
        {
            // var forward = head.forward;
            // forward.y = 0;
            // var angle = Vector3.Angle(head.forward, forward);
            // if (angle < headAngleToHorizonThreshold)
            // {
            moveToDesk.ResetPosition();
            // }
        }
    }
}