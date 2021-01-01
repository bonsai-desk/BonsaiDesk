using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PinchPullHand : MonoBehaviour, IHandTick
{
    public void Tick(PlayerHand playerHand)
    {
        //return if not pinching
        if (!playerHand.GetGesture(PlayerHand.Gesture.IndexPinching))
        {
            playerHand.beamHoldControl.SetActive(false);
            return;
        }
        
        //perform raycast in a cone from the hand
        if (RaycastCone(playerHand, out AutoAuthority hitAutoAuthority))
        {
            hitAutoAuthority.VisualizePinchPull();
        }
    }
    
    private bool RaycastCone(PlayerHand playerHand, out AutoAuthority hitAutoAuthority)
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
                    if (hit.distance < 0.2f || !hitAutoAuthority.allowPinchPull)
                        break;
                    playerHand.beamHoldControl.SetActive(true);
                    return true;
                }
            }
        }

        playerHand.beamHoldControl.SetActive(false);
        hitAutoAuthority = null;
        return false;
    }
}