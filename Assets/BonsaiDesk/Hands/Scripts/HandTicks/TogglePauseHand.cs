using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerHand))]
public class TogglePauseHand : MonoBehaviour, IHandTick
{
    public AngleToObject angleToObject;
    public AngleToObject headAngleToObject;
    public TogglePause togglePause;
    
    private bool lastPointingAtScreen;
    public void Tick(PlayerHand playerHand)
    {
        bool pointingAtScreen = false;
        if (!lastPointingAtScreen && playerHand.GetGesture(PlayerHand.Gesture.WeakPalm) && headAngleToObject.angleBelowThreshold() || lastPointingAtScreen)
            pointingAtScreen = angleToObject.angleBelowThreshold();
        togglePause.Point(playerHand.skeletonType, pointingAtScreen && playerHand.oVRSkeleton.IsDataHighConfidence,
            playerHand.holdPosition.position);
        if (playerHand.GetGesture(PlayerHand.Gesture.WeakFist))
        {
            if (pointingAtScreen && !playerHand.GetLastGesture(PlayerHand.Gesture.WeakFist))
                togglePause.StartToggleGesture(playerHand.skeletonType, playerHand.holdPosition.position);
            togglePause.UpdateToggleGesturePosition(playerHand.skeletonType, playerHand.holdPosition.position);
        }

        lastPointingAtScreen = pointingAtScreen;
    }
}