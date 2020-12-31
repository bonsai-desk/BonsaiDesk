using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(PlayerHand))]
public class TogglePauseHand : MonoBehaviour, IHandTick
{
    public AngleToObject angleToObject;
    public AngleToObject headAngleToObject;
    public TogglePause togglePause;

    private bool _lastPointingAtScreen;

    public void Tick(PlayerHand playerHand)
    {
        if (NetworkClient.connection == null || NetworkClient.connection.identity == null)
            return;
        
        bool pointingAtScreen = false;
        if (!_lastPointingAtScreen && playerHand.GetGesture(PlayerHand.Gesture.WeakPalm) &&
            headAngleToObject.AngleBelowThreshold() || _lastPointingAtScreen)
            pointingAtScreen = angleToObject.AngleBelowThreshold();
        
        togglePause.Point(playerHand.skeletonType, pointingAtScreen && playerHand.oVRSkeleton.IsDataHighConfidence,
            playerHand.holdPosition.position);

        if (playerHand.GetGesture(PlayerHand.Gesture.FlatFist))
        {
            if (pointingAtScreen && !playerHand.GetLastGesture(PlayerHand.Gesture.FlatFist))
            {
                togglePause.StartToggleGesture(playerHand.skeletonType, playerHand.holdPosition.position);
            }

            togglePause.UpdateToggleGesturePosition(playerHand.skeletonType, playerHand.holdPosition.position);
        }
        else
        {
            togglePause.StopToggleGesture(playerHand.skeletonType, playerHand.holdPosition.position);
        }

        if (togglePause.currentGestureSkeleton == playerHand.skeletonType)
        {
            pointingAtScreen = false;
        }
        
        _lastPointingAtScreen = pointingAtScreen;
    }
}