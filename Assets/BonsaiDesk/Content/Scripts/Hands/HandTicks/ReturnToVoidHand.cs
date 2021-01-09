﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReturnToVoidHand : MonoBehaviour, IHandTick
{
    public PlayerHand playerHand { get; set; }

    public MoveToDesk moveToDesk;
    public AngleToObject angleToHead;
    public Transform head;
    public float angleToHeadThreshold = 20f;

    public void Tick()
    {
        if (moveToDesk.oriented &&
            playerHand.GetGestureStart(PlayerHand.Gesture.Fist) &&
            angleToHead.AngleBelowThreshold() &&
            Vector3.Angle(-angleToHead.transform.forward, head.forward) < angleToHeadThreshold)
        {
            moveToDesk.ResetPosition();
        }
    }
}