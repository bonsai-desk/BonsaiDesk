using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ColliderButton : MonoBehaviour
{
    public UnityEvent action;

    private readonly Dictionary<int, EnterInfo> _lastEnter = new Dictionary<int, EnterInfo>();
    private int _handMask;
    private const float ButtonMinTouchTime = 0;
    private const float ButtonMaxTouchTime = 10.25f;
    private const float MaxTabletMoveSquared = 0.01f * 0.01f;
    private const float MaxFingerMoveSquared = 0.01f * 0.01f;
    private const float ButtonRadiusSquared = 0.025f * 0.025f;

    private void Start()
    {
        _handMask = LayerMask.GetMask("LeftHand", "RightHand");
    }

    private bool MaskIsValid(int layer)
    {
        return _handMask == (_handMask | (1 << layer));
    }

    private bool TouchPositionIsValid(Vector3 point)
    {
        var localPoint = transform.InverseTransformPoint(point);

        if (localPoint.y < 0)
            return false;

        float distanceSquared = Vector2.SqrMagnitude(localPoint.xz());
        return distanceSquared < ButtonRadiusSquared;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (MaskIsValid(other.gameObject.layer) && TouchPositionIsValid(other.GetContact(0).point))
        {
            _lastEnter[other.gameObject.GetInstanceID()] =
                new EnterInfo(Time.time, transform.position, other.transform.position);
        }
    }

    private void OnCollisionExit(Collision other)
    {
        if (MaskIsValid(other.gameObject.layer) && _lastEnter.TryGetValue(other.gameObject.GetInstanceID(), out var value))
        {
            float pressTime = Time.time - value.time;
            DebugText.textString = (pressTime + "\n" + Vector3.Distance(transform.position, value.tabletPosition) +
                                    "\n" + Vector3.Distance(other.transform.position, value.fingerPosition));
            if (pressTime > ButtonMinTouchTime && pressTime < ButtonMaxTouchTime &&
                Vector3.SqrMagnitude(transform.position - value.tabletPosition) < MaxTabletMoveSquared &&
                Vector3.SqrMagnitude(other.transform.position - value.fingerPosition) < MaxFingerMoveSquared)
            {
                action.Invoke();
                DebugText.textString += "\nvalid";
            }

            _lastEnter.Remove(other.gameObject.GetInstanceID());
        }
    }

    private struct EnterInfo
    {
        public readonly float time;
        public readonly Vector3 tabletPosition;
        public readonly Vector3 fingerPosition;

        public EnterInfo(float time, Vector3 tabletPosition, Vector3 fingerPosition)
        {
            this.time = time;
            this.tabletPosition = tabletPosition;
            this.fingerPosition = fingerPosition;
        }
    }
}