using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ReturnToVoidHands : MonoBehaviour, IHandsTick
{
    public PlayerHand leftPlayerHand { get; set; }
    public PlayerHand rightPlayerHand { get; set; }

    public AngleToObject angleToHeadLeft;
    public AngleToObject angleToHeadRight;
    public Transform head;
    public UnityEvent action;

    private const float AngleToHeadThreshold = 40f;

    public void Tick()
    {
        if (HandValid(leftPlayerHand, angleToHeadLeft) && HandValid(rightPlayerHand, angleToHeadRight) &&
            (leftPlayerHand.GetGestureStart(PlayerHand.Gesture.Fist) ||
             rightPlayerHand.GetGestureStart(PlayerHand.Gesture.Fist)))
        {
            action?.Invoke();
        }
    }

    private bool HandValid(PlayerHand playerHand, AngleToObject angleToHead)
    {
        return playerHand.GetGesture(PlayerHand.Gesture.Fist) &&
               angleToHead.AngleBelowThreshold() &&
               Vector3.Angle(-angleToHead.transform.forward, head.forward) < AngleToHeadThreshold;
    }
}