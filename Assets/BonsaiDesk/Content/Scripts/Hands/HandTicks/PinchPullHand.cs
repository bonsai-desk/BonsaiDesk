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
        pinchPullJointBody.MovePosition(playerHand.PinchPosition());

        //detach if object is inUse by someone else
        if (pinchPullJoint.connectedBody)
        {
            var autoAuthority = pinchPullJoint.connectedBody.GetComponent<AutoAuthority>();
            if (!autoAuthority.ClientHasAuthority() && autoAuthority.InUse)
            {
                DetachObject();
                return;
            }
        }

        bool drawLocal = false;

        //detach pinch pull object if both hands are not pinching
        if (pinchPullJoint.connectedBody != null && (!playerHand.GetGesture(PlayerHand.Gesture.IndexPinching) ||
                                                     !playerHand.OtherHand
                                                         .GetGesture(PlayerHand.Gesture.IndexPinching)))
        {
            DetachObject();
        }

        //calculate joint length based on finger distance
        if (pinchPullJoint.connectedBody != null)
        {
            var fingerDistance = Vector3.Distance(playerHand.PinchPosition(),
                playerHand.OtherHand.PinchPosition());

            var limit = pinchPullJoint.linearLimit;
            limit.limit = Mathf.Clamp(_ropeLength - fingerDistance, MinRopeLength, _ropeLength);
            pinchPullJoint.linearLimit = limit;
        }

        //start pinch pull action. note that the action is started by the hand which grabs the rope
        if (InputManager.Hands.TrackingRecently() && playerHand.GetGestureStart(PlayerHand.Gesture.IndexPinching) &&
            playerHand.OtherHand.GetGesture(PlayerHand.Gesture.IndexPinching) &&
            Vector3.Distance(playerHand.PinchPosition(), playerHand.OtherHand.PinchPosition()) <
            PinchPullGestureStartDistance &&
            playerHand.OtherHand.GetIHandTick<PinchPullHand>().pinchPullJoint.connectedBody == null)
        {
            var otherHitAutoAuthority = playerHand.OtherHand.GetIHandTick<PinchPullHand>().GetPinchPullCandidate();
            if (otherHitAutoAuthority.hitAutoAuthority != null)
            {
                playerHand.OtherHand.GetIHandTick<PinchPullHand>()
                    .AttachObject(otherHitAutoAuthority.hitAutoAuthority, otherHitAutoAuthority.hitPoint);
            }
        }

        //visualize pinch pull candidates
        if (pinchPullJoint.connectedBody == null &&
            playerHand.OtherHand.GetIHandTick<PinchPullHand>().pinchPullJoint.connectedBody == null)
        {
            //get pinch pull candidate each frame for visual indication
            var hit = GetPinchPullCandidate();
            if (hit.hitAutoAuthority != null)
            {
                hit.hitAutoAuthority.Interact(NetworkClient.connection.identity.netId);
                hit.hitAutoAuthority.VisualizePinchPull();
                if (InputManager.Hands.TrackingRecently())
                    drawLocal = true;
            }
        }

        if (InputManager.Hands.GetHand(playerHand.skeletonType).NetworkHand != null)
        {
            if (pinchPullJoint.connectedBody != null &&
                NetworkIdentity.spawned.TryGetValue(_attachedToId, out NetworkIdentity value))
            {
                var from = playerHand.OtherHand.PinchPosition();
                var to = playerHand.PinchPosition();
                var end = value.transform.TransformPoint(_localHitPoint);
                InputManager.Hands.GetHand(playerHand.skeletonType).NetworkHand.RenderPinchPullLine(from, to, end);
            }
            else
            {
                InputManager.Hands.GetHand(playerHand.skeletonType).NetworkHand.StopRenderPinchPullLine();
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

        _ropeLength = jointLimit + Vector3.Distance(playerHand.PinchPosition(),
            playerHand.OtherHand.PinchPosition());

        _attachedToId = attachToObject.netId;
        attachToObject.CmdSetNewOwner(NetworkClient.connection.identity.netId, NetworkTime.time, true);
        InputManager.Hands.GetHand(playerHand.skeletonType).NetworkHand
            .CmdSetPinchPullInfo(_attachedToId, _localHitPoint, _ropeLength);

        if (attachToObject.isKinematic)
        {
            attachToObject.CmdSetKinematic(false);
        }
    }

    private void DetachObject()
    {
        pinchPullJoint.connectedBody.GetComponent<AutoAuthority>()
            .CmdRemoveInUse(NetworkClient.connection.identity.netId);
        pinchPullJoint.connectedBody = null;
        InputManager.Hands.GetHand(playerHand.skeletonType).NetworkHand.CmdStopPinchPull();
    }

    private void DrawPinchPullLocal(bool shouldDraw)
    {
        if (!shouldDraw)
        {
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, Vector3.zero);
            return;
        }

        var from = playerHand.PinchPosition();
        var to = playerHand.OtherHand.PinchPosition();

        lineRenderer.SetPosition(0, from);
        lineRenderer.SetPosition(1, Vector3.MoveTowards(from, to, PinchPullGestureStartDistance));
    }

    private (AutoAuthority hitAutoAuthority, Vector3 hitPoint) GetPinchPullCandidate()
    {
        if (playerHand.HandComponents.TrackingRecently &&
            playerHand.GetGesture(PlayerHand.Gesture.IndexPinching))
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

            Vector3 start = playerHand.pinchPullPointer.position; //start of raycast
            Vector3 end = playerHand.pinchPullPointer.TransformPoint(posOnCircle); //end of raycast

            //linecast includes if start is inside of an object. can hit anything except hands
            if (Physics.Linecast(start, end, out RaycastHit hit, PlayerHand.AllButHandsMask,
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