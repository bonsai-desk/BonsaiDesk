using UnityEngine;
using Mirror;

public class PinchPullHand : MonoBehaviour, IHandTick
{
    public PlayerHand playerHand { get; set; }

    public ConfigurableJoint pinchPullJoint;
    public Rigidbody pinchPullJointBody;
    public LineRenderer lineRenderer;

    private float _ropeLength;
    private uint _attachedToId;
    private Vector3 _localHitPoint;

    public const float MinRopeLength = 0.05f;

    //how far apart your hands can be when starting the pinch pull action
    private const float PinchPullGestureStartDistance = 0.15f;

    public void Tick()
    {
        pinchPullJointBody.MovePosition(playerHand.PhysicsPinchPosition());

        bool drawLocal = false;

        //detach pinch pull object if both hands are not pinching
        if (pinchPullJoint.connectedBody != null && (!playerHand.GetGesture(PlayerHand.Gesture.IndexPinching) ||
                                                     !playerHand.OtherHand()
                                                         .GetGesture(PlayerHand.Gesture.IndexPinching)))
        {
            DetachObject();
        }

        //calculate joint length based on finger distance
        if (pinchPullJoint.connectedBody != null)
        {
            var fingerDistance = Vector3.Distance(playerHand.PhysicsPinchPosition(),
                playerHand.OtherHand().PhysicsPinchPosition());

            var limit = pinchPullJoint.linearLimit;
            limit.limit = Mathf.Clamp(_ropeLength - fingerDistance, MinRopeLength, _ropeLength);
            pinchPullJoint.linearLimit = limit;
        }

        //start pinch pull action. note that the action is started by the hand grabbing the rope
        if (PlayerHands.hands.Tracking() && playerHand.GetGestureStart(PlayerHand.Gesture.IndexPinching) &&
            playerHand.OtherHand().GetGesture(PlayerHand.Gesture.IndexPinching) &&
            Vector3.Distance(playerHand.PhysicsPinchPosition(), playerHand.OtherHand().PhysicsPinchPosition()) <
            PinchPullGestureStartDistance &&
            playerHand.OtherHand().GetIHandTick<PinchPullHand>().pinchPullJoint.connectedBody == null)
        {
            var otherHitAutoAuthority = playerHand.OtherHand().GetIHandTick<PinchPullHand>().GetPinchPullCandidate();
            if (otherHitAutoAuthority.hitAutoAuthority != null)
            {
                playerHand.OtherHand().GetIHandTick<PinchPullHand>()
                    .AttachObject(otherHitAutoAuthority.hitAutoAuthority, otherHitAutoAuthority.hitPoint);
            }
        }

        //visualize pinch pull candidates
        if (pinchPullJoint.connectedBody == null &&
            playerHand.OtherHand().GetIHandTick<PinchPullHand>().pinchPullJoint.connectedBody == null)
        {
            //get pinch pull candidate each frame for visual indication
            var hit = GetPinchPullCandidate();
            if (hit.hitAutoAuthority != null)
            {
                hit.hitAutoAuthority.Interact(NetworkClient.connection.identity.netId);
                hit.hitAutoAuthority.VisualizePinchPull();
                drawLocal = true;
            }
        }

        if (playerHand.networkHand != null)
        {
            if (pinchPullJoint.connectedBody != null &&
                NetworkIdentity.spawned.TryGetValue(_attachedToId, out NetworkIdentity value))
            {
                var from = playerHand.OtherHand().PhysicsPinchPosition();
                var to = playerHand.PhysicsPinchPosition();
                var end = value.transform.TransformPoint(_localHitPoint);
                playerHand.networkHand.RenderPinchPullLine(from, to, end);
            }
            else
            {
                playerHand.networkHand.StopRenderPinchPullLine();
            }
        }

        DrawPinchPullLocal(drawLocal);
    }

    private void AttachObject(AutoAuthority attachToObject, Vector3 hitPoint)
    {
        _localHitPoint = attachToObject.transform.InverseTransformPoint(hitPoint);
        pinchPullJoint.connectedAnchor = _localHitPoint;

        float jointLimit = Vector3.Distance(pinchPullJoint.transform.position, hitPoint);
        SoftJointLimit softJointLimit = new SoftJointLimit
        {
            limit = jointLimit
        };
        pinchPullJoint.linearLimit = softJointLimit;

        pinchPullJoint.connectedBody = attachToObject.GetComponent<Rigidbody>();

        _ropeLength = jointLimit + Vector3.Distance(playerHand.PhysicsPinchPosition(),
            playerHand.OtherHand().PhysicsPinchPosition());

        _attachedToId = attachToObject.netId;
        attachToObject.CmdSetNewOwner(NetworkClient.connection.identity.netId, NetworkTime.time, true);
        playerHand.networkHand.CmdSetPinchPullInfo(_attachedToId, _localHitPoint, _ropeLength);
    }

    private void DetachObject()
    {
        pinchPullJoint.connectedBody.GetComponent<AutoAuthority>().CmdRemoveInUse();
        pinchPullJoint.connectedBody = null;
        playerHand.networkHand.CmdStopPinchPull();
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

    private (AutoAuthority hitAutoAuthority, Vector3 hitPoint) GetPinchPullCandidate()
    {
        if (PlayerHands.hands.Tracking() && playerHand.GetGesture(PlayerHand.Gesture.IndexPinching))
        {
            //perform raycast in a cone from the hand
            if (RaycastCone(out AutoAuthority hitAutoAuthority, out Vector3 hitPoint))
            {
                //if it is valid to perform a pinch pull with the hit object
                if (hitAutoAuthority.allowPinchPull && !hitAutoAuthority.InUse)
                {
                    return (hitAutoAuthority, hitPoint);
                }
            }
        }

        return (null, Vector3.zero);
    }

    private bool RaycastCone(out AutoAuthority hitAutoAuthority, out Vector3 hitPoint)
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
                    hitPoint = hit.point;
                    return true;
                }
            }
        }

        hitAutoAuthority = null;
        hitPoint = Vector3.zero;
        return false;
    }
}