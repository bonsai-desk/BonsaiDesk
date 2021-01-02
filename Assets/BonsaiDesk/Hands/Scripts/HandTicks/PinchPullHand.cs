using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class PinchPullHand : MonoBehaviour, IHandTick
{
    public PlayerHand playerHand { get; set; }

    public LineRenderer lineRenderer;

    //how far apart your hands can be when starting the pinch pull action
    private const float PinchPullGestureStartDistance = 0.1f;

    public void Tick()
    {
        bool drawLocal = false;

        if (playerHand.GetGestureStart(PlayerHand.Gesture.IndexPinching))
        {
            
        }
        
        AutoAuthority hitAutoAuthority = GetPinchPullCandidate();
        if (hitAutoAuthority != null)
        {
            hitAutoAuthority.VisualizePinchPull();
            drawLocal = true;
        }

        DrawPinchPullLocal(drawLocal);
    }

    private void DrawPinchPullLocal(bool shouldDraw)
    {
        if (!shouldDraw)
        {
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, Vector3.zero);
            return;
        }

        var from = playerHand.PhysicsPinchPosition();
        var to = playerHand.OtherHand().PhysicsPinchPosition();

        lineRenderer.SetPosition(0, from);
        lineRenderer.SetPosition(1, Vector3.MoveTowards(from, to, PinchPullGestureStartDistance));
    }

    private AutoAuthority GetPinchPullCandidate()
    {
        if (PlayerHands.hands.Tracking() && playerHand.GetGesture(PlayerHand.Gesture.IndexPinching))
        {
            //perform raycast in a cone from the hand
            if (RaycastCone(out AutoAuthority hitAutoAuthority))
            {
                //if it is valid to perform a pinch pull with the hit object
                if (hitAutoAuthority.allowPinchPull && !hitAutoAuthority.InUse)
                {
                    return hitAutoAuthority;
                }
            }
        }

        return null;
    }

    private bool RaycastCone(out AutoAuthority hitAutoAuthority)
    {
        const float length = 1f; //length of raycast

        const float r = 0.15f; //radius of the raycast cone
        const float loops = 5; //how many spiral loops

        //create spiral cone of raycasts
        for (float t = 0; t < 2f * Mathf.PI; t += Mathf.PI * 2f / 15.25744f / loops)
        {
            Vector3 posOnCircle = new Vector3(Mathf.Cos(t * loops) * t / (Mathf.PI * 2f) * r * 2f,
                Mathf.Sin(t * loops) * t / (Mathf.PI * 2f) * r * 2f, length);

            Vector3 start = playerHand.pointerPose.position; //start of raycast
            Vector3 end = playerHand.pointerPose.TransformPoint(posOnCircle); //end of raycast

            //linecast includes if start is inside of an object. can hit anything except hands
            if (Physics.Linecast(start, end, out RaycastHit hit, PlayerHand.AllButHands,
                QueryTriggerInteraction.Ignore))
            {
                //recursivly check if hit object or its parent has an AutoAuthority script
                var check = hit.transform;
                hitAutoAuthority = check.GetComponent<AutoAuthority>();
                while (hitAutoAuthority == null)
                {
                    if (check.parent != null)
                        check = check.parent;
                    else
                        break;
                    hitAutoAuthority = check.GetComponent<AutoAuthority>();
                }

                if (hitAutoAuthority != null)
                {
                    if (hit.distance < 0.2f)
                        break;
                    return true;
                }
            }
        }

        playerHand.beamHoldControl.SetActive(false);
        hitAutoAuthority = null;
        return false;
    }
}