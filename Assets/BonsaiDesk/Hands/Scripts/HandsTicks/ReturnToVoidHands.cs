using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ReturnToVoidHands : MonoBehaviour, IHandsTick
{
    public float gestureStartDistanceThreshold = 0.375f;
    public float gestureMoveDistance = 0.3f;
    public UnityEvent action;
    
    private float _startDistance;
    private bool _gestureInProgress;

    public void Tick(PlayerHand leftPlayerHand, PlayerHand rightPlayerHand)
    {
        float? distance = null;

        //first frame of both hands fist
        if (leftPlayerHand.GetGestureStart(PlayerHand.Gesture.WeakFist) &&
            rightPlayerHand.GetGesture(PlayerHand.Gesture.WeakFist) ||
            leftPlayerHand.GetGesture(PlayerHand.Gesture.WeakFist) &&
            rightPlayerHand.GetGestureStart(PlayerHand.Gesture.WeakFist) &&
            !_gestureInProgress)
        {
            _gestureInProgress = true;
            distance = Vector3.Distance(leftPlayerHand.holdPosition.position, rightPlayerHand.holdPosition.position);
            _startDistance = distance.Value;
        }

        if (!leftPlayerHand.GetGesture(PlayerHand.Gesture.WeakFist) || !rightPlayerHand.GetGesture(PlayerHand.Gesture.WeakFist))
        {
            _gestureInProgress = false;
        }

        if (_gestureInProgress)
        {
            if (!distance.HasValue)
            {
                distance = Vector3.Distance(leftPlayerHand.holdPosition.position,
                    rightPlayerHand.holdPosition.position);
            }

            if (distance - _startDistance > gestureMoveDistance)
            {
                _gestureInProgress = false;
                action.Invoke();
            }
        }
    }
}