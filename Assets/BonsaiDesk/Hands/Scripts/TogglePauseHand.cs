using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerHand))]
public class TogglePauseHand : MonoBehaviour, IHandTick
{
    void Update()
    {
        // bool pointingAtScreen = false;
        // if (!lastPointingAtScreen && fistMinStrength < 0.35f && headAngleToObject.angleBelowThreshold() || lastPointingAtScreen)
        //     pointingAtScreen = angleToObject.angleBelowThreshold();
        // togglePause.Point(_skeletonType, pointingAtScreen && oVRSkeleton.IsDataHighConfidence,
        //     holdPosition.position);
        // if (weakFist)
        // {
        //     if (pointingAtScreen && !lastWeakFist)
        //         togglePause.StartToggleGesture(_skeletonType, holdPosition.position);
        //     togglePause.UpdateToggleGesturePosition(_skeletonType, holdPosition.position);
        // }
    }

    public void Tick(PlayerHand playerHand)
    {
        print("Tick: " + playerHand.skeletonType);
    }
}
