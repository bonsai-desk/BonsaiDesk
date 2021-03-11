using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CloseGestureHand : MonoBehaviour, IHandTick
{
    public PlayerHand playerHand { get; set; }
    
    public AngleToObject angleToHead;
    public Transform head;
    public UnityEvent action;
    
    private const float AngleToHeadThreshold = 40f;

    public void Tick()
    {
        if (playerHand.GetGestureStart(PlayerHand.Gesture.Fist) &&
            angleToHead.AngleBelowThreshold() &&
            Vector3.Angle(-angleToHead.transform.forward, head.forward) < AngleToHeadThreshold)
        {
            action?.Invoke();
        }
    }
}