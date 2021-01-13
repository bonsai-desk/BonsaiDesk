using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class TabletCollider : MonoBehaviour
{
    public UnityEvent action;

    private readonly Dictionary<int, EnterInfo> _lastEnter = new Dictionary<int, EnterInfo>();
    private int _handMask;
    private const float ButtonMinTouchTime = 0f;
    private const float ButtonMaxTouchTime = 1f;
    private const float MaxTabletMoveSquared = 0.01f * 0.01f;
    private const float MaxFingerMoveSquared = 0.035f * 0.035f;
    private const float ButtonRadiusSquared = 0.025f * 0.025f;

    private readonly Dictionary<int, bool> _fingerTipTouching = new Dictionary<int, bool>();

    private int _numFingersTouching;
    public int NumFingersTouching => _numFingersTouching;

    private void Start()
    {
        _handMask = LayerMask.GetMask("LeftHand", "RightHand");
    }

    private void Update()
    {
        _numFingersTouching = NumberFingersTouching();

        var keys = new List<int>(_fingerTipTouching.Keys);
        foreach (var key in keys)
        {
            _fingerTipTouching[key] = false;
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (CollisionIsValid(other))
        {
            int objectId = other.gameObject.GetInstanceID();
            _fingerTipTouching[objectId] = true;
            if (TouchPositionIsValid(other.GetContact(0).point))
            {
                _lastEnter[objectId] = new EnterInfo(Time.time, transform.position, other.transform.position);
            }
        }
    }

    private void OnCollisionStay(Collision other)
    {
        if (CollisionIsValid(other))
        {
            int objectId = other.gameObject.GetInstanceID();
            _fingerTipTouching[objectId] = true;
        }
    }

    private void OnCollisionExit(Collision other)
    {
        if (CollisionIsValid(other))
        {
            int objectId = other.gameObject.GetInstanceID();
            if (_lastEnter.TryGetValue(objectId, out var value))
            {
                float pressTime = Time.time - value.time;
                if (pressTime > ButtonMinTouchTime && pressTime < ButtonMaxTouchTime &&
                    Vector3.SqrMagnitude(transform.position - value.tabletPosition) < MaxTabletMoveSquared &&
                    Vector3.SqrMagnitude(other.transform.position - value.fingerPosition) < MaxFingerMoveSquared)
                {
                    action.Invoke();
                }

                _lastEnter.Remove(other.gameObject.GetInstanceID());
            }
        }
    }

    private int NumberFingersTouching()
    {
        int num = 0;
        foreach (var pair in _fingerTipTouching)
        {
            if (pair.Value)
                num++;
        }

        return num;
    }
    
    private bool MaskIsValid(int layer)
    {
        return _handMask == (_handMask | (1 << layer));
    }

    private bool CollisionIsValid(Collision other)
    {
        return MaskIsValid(other.gameObject.layer) &&
               (other.gameObject.CompareTag("FingerTip") || other.gameObject.CompareTag("IndexTip"));
    }

    private bool TouchPositionIsValid(Vector3 point)
    {
        var localPoint = transform.InverseTransformPoint(point);

        if (localPoint.y < 0)
            return false;

        float distanceSquared = Vector2.SqrMagnitude(localPoint.xz());
        return distanceSquared < ButtonRadiusSquared;
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