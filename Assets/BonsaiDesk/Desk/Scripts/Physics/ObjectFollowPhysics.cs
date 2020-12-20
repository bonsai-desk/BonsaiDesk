using UnityEngine;

public class ObjectFollowPhysics : MonoBehaviour
{
    public Transform target;
    private Rigidbody body;

    public float lbsForce = 70f;
    public float lbsTorque = 10f;
    public float snapBackDistance = 0.2f;

    private float moveForce;
    private float rotationTorque;

    private const float lbsToKg = 0.45359237f;

    [HideInInspector]
    public float distance;

    [HideInInspector]
    public OVRSkeleton oVRSkeleton;

    [HideInInspector]
    public GameObject hideObject;

    [HideInInspector]
    public Material material;

    private Color handColor;
    private bool active = false;

    private void Start()
    {
        body = GetComponent<Rigidbody>();
        body.maxAngularVelocity = float.MaxValue;
        
        moveForce = lbsForce * lbsToKg * 9.81f;
        rotationTorque = lbsTorque * lbsToKg * 9.81f;
        
        if (hideObject == null && transform.childCount > 0)
        {
            hideObject = transform.GetChild(0).gameObject;
            if (material != null)
                handColor = material.GetColor("_Color");
        }
    }

    private void FixedUpdate()
    {
        bool valid = true;
        if (oVRSkeleton != null && hideObject != null)
            valid = oVRSkeleton.IsDataValid && oVRSkeleton.IsDataHighConfidence;
        if (valid)
        {
            body.isKinematic = false;
            if (hideObject == null || oVRSkeleton == null || active)
            {
                distance = Vector3.Distance(transform.position, target.position);
                if (distance < snapBackDistance)
                {
                    addForceTowardsTarget();
                    addTorqueTowardsTarget();
                }
                else
                {
                    resetToTarget();
                }
            }
            else
            {
                active = true;
                body.isKinematic = false;
                if (material != null)
                {
                    material.SetColor("_Color", handColor);
                    // material.SetFloat("_Glossiness", 0.5f);
                }
                resetToTarget();
            }
        }
        else
        {
            active = false;
            body.isKinematic = true;
            if (material != null)
            {
                material.SetColor("_Color", new Color(handColor.r, handColor.g, handColor.b, 0.25f));
                // material.SetFloat("_Glossiness", 0);
            }
        }
    }

    private void resetToTarget()
    {
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        transform.position = target.position;
        transform.rotation = target.rotation;
    }

    private void addForceTowardsTarget()
    {
        Vector3 towards = target.position - transform.position;

        //speed is maintained, but direction is changed to be towards target position (not realistic)
        body.velocity = towards.normalized * body.velocity.magnitude;

        //targetForce is the force required to get a velocity that would make the object get to the target position this update tick
        Vector3 targetVelocity = towards / Time.deltaTime;
        Vector3 velocityDifference = targetVelocity - body.velocity;
        Vector3 targetForce = velocityDifference * body.mass / Time.deltaTime / 2f;
        //float distance = Vector3.Distance(transform.position, target.position);

        //if already at target or have enough velocity to overshoot target this update tick, allow unlimited force to decelerate (not realistic)
        if (Mathf.Approximately(distance, 0) || body.velocity.magnitude * Time.deltaTime > distance || Mathf.Approximately(moveForce, 0))
            body.AddForce(targetForce, ForceMode.Force);
        else //clamp target force to moveForce
            body.AddForce(Vector3.ClampMagnitude(targetForce, moveForce), ForceMode.Force);
    }

    private void addTorqueTowardsTarget()
    {
        var delta = target.rotation * Quaternion.Inverse(body.rotation);

        float angle; Vector3 axis;
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
}