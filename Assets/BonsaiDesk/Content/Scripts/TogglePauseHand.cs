using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(PlayerHand))]
public class TogglePauseHand : MonoBehaviour, IHandTick
{
    public PlayerHand playerHand { get; set; }

    public AngleToObject angleToObject;
    public AngleToObject headAngleToObject;
    public TogglePause togglePause;

    private bool _lastPointingAtScreen;

    public void Tick()
    {
        if (NetworkClient.connection == null || NetworkClient.connection.identity == null)
            return;

        bool pointingAtScreen = false;
        if (!_lastPointingAtScreen && playerHand.GetGesture(PlayerHand.Gesture.WeakPalm) &&
            headAngleToObject.AngleBelowThreshold() || _lastPointingAtScreen)
            pointingAtScreen = angleToObject.AngleBelowThreshold();

        if (!playerHand.HandComponents.TrackingRecently)
            pointingAtScreen = false;

        togglePause.Point(playerHand.skeletonType, pointingAtScreen, playerHand.palm.position);

        if (playerHand.GetGesture(PlayerHand.Gesture.FlatFist))
        {
            if (pointingAtScreen && !playerHand.GetLastGesture(PlayerHand.Gesture.FlatFist))
            {
                togglePause.StartToggleGesture(playerHand.skeletonType, playerHand.palm.position);
            }

            togglePause.UpdateToggleGesturePosition(playerHand.skeletonType, playerHand.palm.position);
        }
        else
        {
            togglePause.StopToggleGesture(playerHand.skeletonType, playerHand.palm.position);
        }

        if (togglePause.currentGestureSkeleton == playerHand.skeletonType)
        {
            pointingAtScreen = false;
        }

        _lastPointingAtScreen = pointingAtScreen;
    }
}