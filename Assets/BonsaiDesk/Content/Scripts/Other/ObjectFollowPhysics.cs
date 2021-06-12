using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ObjectFollowPhysics : MonoBehaviour
{
    public Transform target;
    public float lbsForce = 70f;
    public float lbsTorque = 10f;
    public float snapBackDistance = 0.2f;

    internal Rigidbody Body;
    protected NetworkIdentity NetworkIdentity;

    private const float LbsToKg = 0.45359237f;
    private float _moveForce;
    private float _rotationTorque;
    private float _sqrSnapBackDistance;

    protected virtual void Awake()
    {
        Body = GetComponent<Rigidbody>();
        Body.maxAngularVelocity = 100f;

        NetworkIdentity = GetComponent<NetworkIdentity>();

        _moveForce = lbsForce * LbsToKg * 9.81f;
        _rotationTorque = lbsTorque * LbsToKg * 9.81f;
        _sqrSnapBackDistance = snapBackDistance * snapBackDistance;
    }

    protected virtual void FixedUpdate()
    {
        if (!target)
        {
            return;
        }

        if (NetworkIdentity && !NetworkIdentity.hasAuthority)
        {
            Body.isKinematic = true;
            return;
        }

        var sqrDistance = Vector3.SqrMagnitude(transform.position - target.position);

        AddForceTowardsTarget(sqrDistance);
        AddTorqueTowardsTarget();

        if (sqrDistance > _sqrSnapBackDistance)
        {
            TryResetToTarget();
        }
    }

    protected virtual void TryResetToTarget()
    {
    }

    private void AddForceTowardsTarget(float sqrDistance)
    {
        Vector3 towards = target.position - transform.position;

        //speed is maintained, but direction is changed to be towards target position (not realistic)
        Body.velocity = towards.normalized * Body.velocity.magnitude;

        //targetForce is the force required to get a velocity that would make the object get to the target position this update tick
        Vector3 targetVelocity = towards / Time.deltaTime;
        Vector3 velocityDifference = targetVelocity - Body.velocity;
        Vector3 targetForce = velocityDifference * Body.mass / Time.deltaTime / 2f;
        //float distance = Vector3.Distance(transform.position, target.position);

        //if already at target or have enough velocity to overshoot target this update tick, allow unlimited force to decelerate (not realistic)
        if (Mathf.Approximately(sqrDistance, 0) || Body.velocity.sqrMagnitude * Time.deltaTime > sqrDistance || Mathf.Approximately(_moveForce, 0))
            Body.AddForce(targetForce, ForceMode.Force);
        else //clamp target force to moveForce
            Body.AddForce(Vector3.ClampMagnitude(targetForce, _moveForce), ForceMode.Force);
    }

    private void AddTorqueTowardsTarget()
    {
        var delta = target.rotation * Quaternion.Inverse(Body.rotation);

        float angle;
        Vector3 axis;
        delta.ToAngleAxis(out angle, out axis);

        // We get an infinite axis in the event that our rotation is already aligned.
        // allow instant deceleration to stop rotation (not realistic)
        if (float.IsInfinity(axis.x))
        {
            Body.angularVelocity = Vector3.zero;
        }
        else
        {
            if (angle > 180f)
                angle -= 360f;

            Vector3 targetAngularVelocity = Mathf.Deg2Rad * angle * axis.normalized / Time.deltaTime;

            //angular speed is maintained, but direction is changed to be towards target rotation (not realistic)
            Body.angularVelocity = targetAngularVelocity.normalized * Body.angularVelocity.magnitude;

            Vector3 angularVelocityDifference = targetAngularVelocity - Body.angularVelocity;

            Quaternion q = transform.rotation * Body.inertiaTensorRotation;
            Vector3 targetTorque = q * Vector3.Scale(Body.inertiaTensor, (Quaternion.Inverse(q) * angularVelocityDifference)) / Time.deltaTime; // / 2f;

            //if already at target or have enough angular velocity to overshoot target this update tick, allow unlimited torque to decelerate (not realistic)
            if (Mathf.Approximately(angle, 0) || Body.angularVelocity.magnitude * Mathf.Rad2Deg * Time.deltaTime > angle ||
                Mathf.Approximately(_rotationTorque, 0))
                Body.AddTorque(targetTorque, ForceMode.Force);
            else //clamp target torque to rotationForce
                Body.AddTorque(Vector3.ClampMagnitude(targetTorque, _rotationTorque), ForceMode.Force);
        }
    }
}