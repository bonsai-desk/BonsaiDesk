using Mirror;
using UnityEngine;

public class TabletPhysics : NetworkBehaviour
{
    private Rigidbody body;

    public float lbsForce = 1f;
    public float lbsTorque = 1f;

    private float moveForce;
    private float rotationTorque;

    private const float lbsToKg = 0.45359237f;

    private Vector3 position;
    private Quaternion rotation;

    private bool touchingHand = false;
    //private bool lastTouchingHand = false;

    public Transform tabletSpot;

    private bool canActivate = true;

    public string videoId = "";

    [SyncVar]
    public int videoIndex = 0;

    public string[] videoIds;
    public Material[] materials;
    public MeshRenderer meshRenderer;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("LeftHand") || collision.gameObject.layer == LayerMask.NameToLayer("RightHand"))
        {
            touchingHand = true;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("LeftHand") || collision.gameObject.layer == LayerMask.NameToLayer("RightHand"))
        {
            touchingHand = true;
        }
    }

    private void Start()
    {
        body = GetComponent<Rigidbody>();
        body.maxAngularVelocity = float.MaxValue;

        moveForce = lbsForce * lbsToKg * 9.81f;
        rotationTorque = lbsTorque * lbsToKg * 9.81f;

        if (tabletSpot == null)
            tabletSpot = GameObject.Find("TabletSpot").transform;

        videoId = videoIds[videoIndex];
        meshRenderer.sharedMaterial = materials[videoIndex];
    }

    private void FixedUpdate()
    {
        if (!hasAuthority)
            return;
        // if ((PlayerHands.hands.right.heldBody != null && PlayerHands.hands.right.heldBody.gameObject == gameObject) ||
        //     (PlayerHands.hands.left.heldBody != null && PlayerHands.hands.left.heldBody.gameObject == gameObject))
        //     touchingHand = true;

        Vector3 positionRelative = tabletSpot.InverseTransformPoint(transform.position);
        var inArea = true;
        for (int i = 0; i < 3; i++)
            if (Mathf.Abs(positionRelative[i]) > 0.5f)
                inArea = false;

        if (inArea && !touchingHand)
        {
            position = tabletSpot.position;
            rotation = tabletSpot.rotation;

            if (AboutEquals(position, transform.position) && AboutEquals(rotation, transform.rotation))
            {
                //tablet in place
                if (canActivate)
                {
                    canActivate = false;
                    if (!string.IsNullOrEmpty(videoId))
                    {
                        NetworkVRPlayer.self.PlayVideo(videoId);
                        print("play");
                    }
                }
            }

            AddForceTowardsTarget();
            AddTorqueTowardsTarget();
        }

        if (inArea && !touchingHand)
        {
            body.useGravity = false;
        }
        else
        {
            body.useGravity = true;
            if (!canActivate && !inArea)
            {
                NetworkVRPlayer.self.StopVideo();
                canActivate = true;
            }
        }

        touchingHand = false;
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    //   private int SetLayerRecursively(GameObject obj)
    //   {
    //       if (obj.layer != 0)
    //           return obj.layer;
    //
    //       foreach (Transform child in obj.transform)
    //       {
    //           int layer = GetLayerRecursively(child.gameObject);
    //           if (layer != 0)
    //               return layer;
    //       }
    //
    //       return 0;
    //   }

    private int GetLayerRecursively(GameObject obj)
    {
        if (obj.layer != 0)
            return obj.layer;

        foreach (Transform child in obj.transform)
        {
            int layer = GetLayerRecursively(child.gameObject);
            if (layer != 0)
                return layer;
        }

        return 0;
    }

    private bool AboutEquals(Vector3 v1, Vector3 v2)
    {
        //+ or - 1mm
        float tolerance = 0.001f;
        float distance = (v2.x - v1.x) * (v2.x - v1.x) + (v2.y - v1.y) * (v2.y - v1.y) + (v2.z - v1.z) * (v2.z - v1.z);
        return distance < tolerance * tolerance;
    }

    private bool AboutEquals(Quaternion q1, Quaternion q2)
    {
        //+ or - 1 degree
        return Mathf.Abs(Quaternion.Dot(q1, q2)) > 0.98888889f;
    }

    //private void resetToTarget()
    //{
    //    body.velocity = Vector3.zero;
    //    body.angularVelocity = Vector3.zero;
    //    body.MovePosition(position);
    //    body.MoveRotation(rotation);
    //}

    private void AddForceTowardsTarget()
    {
        Vector3 towards = position - transform.position;

        //speed is maintained, but direction is changed to be towards target position (not realistic)
        body.velocity = towards.normalized * body.velocity.magnitude;

        //targetForce is the force required to get a velocity that would make the object get to the target position this update tick
        Vector3 targetVelocity = towards / Time.deltaTime;
        Vector3 velocityDifference = targetVelocity - body.velocity;
        Vector3 targetForce = velocityDifference * body.mass / Time.deltaTime / 2f;
        float distance = Vector3.Distance(transform.position, position);

        //if already at target or have enough velocity to overshoot target this update tick, allow unlimited force to decelerate (not realistic)
        if (Mathf.Approximately(distance, 0) || body.velocity.magnitude * Time.deltaTime > distance || Mathf.Approximately(moveForce, 0))
            body.AddForce(targetForce, ForceMode.Force);
        else //clamp target force to moveForce
            body.AddForce(Vector3.ClampMagnitude(targetForce, moveForce), ForceMode.Force);
    }

    private void AddTorqueTowardsTarget()
    {
        float angle;
        Vector3 axis;

        var delta = rotation * Quaternion.Inverse(body.rotation);

        delta.ToAngleAxis(out angle, out axis);

        // We get an infinite axis in the event that our rotation is already aligned.
        // allow instant deceleration to stop rotation (not realistic)
        if (float.IsInfinity(axis.x))
        {
            body.angularVelocity = Vector3.zero;
        }
        else
        {
            if (angle > 180f)
                angle -= 360f;

            Vector3 targetAngularVelocity = axis.normalized * Mathf.Deg2Rad * angle / Time.deltaTime;

            //angular speed is maintained, but direction is changed to be towards target rotation (not realistic)
            body.angularVelocity = targetAngularVelocity.normalized * body.angularVelocity.magnitude;

            Vector3 angularVelocityDifference = targetAngularVelocity - body.angularVelocity;

            Quaternion q = transform.rotation * body.inertiaTensorRotation;
            Vector3 targetTorque = q * Vector3.Scale(body.inertiaTensor, (Quaternion.Inverse(q) * angularVelocityDifference)) / Time.deltaTime; // / 2f;

            //if already at target or have enough angular velocity to overshoot target this update tick, allow unlimited torque to decelerate (not realistic)
            if (Mathf.Approximately(angle, 0) || body.angularVelocity.magnitude * Mathf.Rad2Deg * Time.deltaTime > angle || Mathf.Approximately(rotationTorque, 0))
                body.AddTorque(targetTorque, ForceMode.Force);
            else //clamp target torque to rotationForce
                body.AddTorque(Vector3.ClampMagnitude(targetTorque, rotationTorque), ForceMode.Force);
        }
    }

    // public static Vector3 TransformPointUnscaled(Transform transformIn, Vector3 position)
    // {
    //     var localToWorldMatrix = Matrix4x4.TRS(transformIn.position, transformIn.rotation, Vector3.one);
    //     return localToWorldMatrix.MultiplyPoint3x4(position);
    // }

    // public static Vector3 InverseTransformPointUnscaled(Transform transformIn, Vector3 position)
    // {
    //     var worldToLocalMatrix = Matrix4x4.TRS(transformIn.position, transformIn.rotation, Vector3.one).inverse;
    //     return worldToLocalMatrix.MultiplyPoint3x4(position);
    // }
}