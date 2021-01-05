using UnityEngine;

public class PlayerHands : MonoBehaviour
{
    public static PlayerHands hands;

    public PlayerHand left;
    public PlayerHand right;

    [HideInInspector] //0-4 thumb to pinky on left hand, 5-9 thumb to pinky right hand. Will have value of Vector3.zero if not tracking
    public Vector3[] fingerTipPositions;

    [HideInInspector] //0-4 thumb to pinky on left hand, 5-9 thumb to pinky right hand. Will have value of Vector3.zero if not tracking
    public Vector3[] physicsFingerTipPositions;

    [HideInInspector] //0-4 thumb to pinky on left hand, 5-9 thumb to pinky right hand. Will have value of Vector3.zero if not tracking
    public Vector3[] physicsFingerPadPositions;

    private IHandsTick[] _handsTicks;
    private bool leftHandGesturesReady;
    private bool rightHandGesturesReady;

    private void Awake()
    {
        if (hands == null)
            hands = this;
    }

    private void Start()
    {
        _handsTicks = GetComponentsInChildren<IHandsTick>();

        fingerTipPositions = new Vector3[10];
        physicsFingerTipPositions = new Vector3[10];
        physicsFingerPadPositions = new Vector3[10];
        CalculateFingerTipPositions();
    }

    private void Update()
    {
        CalculateFingerTipPositions();
        
        left.UpdateGestures();
        right.UpdateGestures();
        
        left.RunHandTicks();
        right.RunHandTicks();
    }

    public PlayerHand GetHand(OVRSkeleton.SkeletonType skeletonType)
    {
        if (skeletonType == OVRSkeleton.SkeletonType.HandLeft)
            return left;
        if (skeletonType == OVRSkeleton.SkeletonType.HandRight)
            return right;
        return null;
    }
    
    public PlayerHand GetOtherHand(OVRSkeleton.SkeletonType skeletonType)
    {
        if (skeletonType == OVRSkeleton.SkeletonType.HandLeft)
            return right;
        if (skeletonType == OVRSkeleton.SkeletonType.HandRight)
            return left;
        return null;
    }

    public void SetHandGesturesReady(OVRSkeleton.SkeletonType skeletonType)
    {
        if (skeletonType == OVRSkeleton.SkeletonType.HandLeft)
            leftHandGesturesReady = true;
        if (skeletonType == OVRSkeleton.SkeletonType.HandRight)
            rightHandGesturesReady = true;

        if (leftHandGesturesReady && rightHandGesturesReady)
        {
            for (var i = 0; i < _handsTicks.Length; i++)
            {
                _handsTicks[i].Tick(left, right);
            }

            left.UpdateLastGestures();
            right.UpdateLastGestures();
            leftHandGesturesReady = false;
            rightHandGesturesReady = true;
        }
    }

    public bool Tracking()
    {
        return left.Tracking() && right.Tracking();
    }

    public void CalculateFingerTipPositions()
    {
        int n = 0;
        if (left.Tracking())
        {
            for (int i = 0; i < 5; i++, n++)
            {
                fingerTipPositions[n] = left.fingerTips[i].position;
                physicsFingerTipPositions[n] = left.physicsFingerTips[i].position;
                physicsFingerPadPositions[n] = left.physicsFingerPads[i].position;
            }
        }
        else
        {
            for (int i = 0; i < 5; i++, n++)
            {
                fingerTipPositions[n] = Vector3.zero;
                physicsFingerTipPositions[n] = Vector3.zero;
                physicsFingerPadPositions[n] = Vector3.zero;
            }
        }

        if (right.Tracking())
        {
            for (int i = 0; i < 5; i++, n++)
            {
                fingerTipPositions[n] = right.fingerTips[i].position;
                physicsFingerTipPositions[n] = right.physicsFingerTips[i].position;
                physicsFingerPadPositions[n] = right.physicsFingerTips[i].position;
            }
        }
        else
        {
            for (int i = 0; i < 5; i++, n++)
            {
                fingerTipPositions[n] = Vector3.zero;
                physicsFingerTipPositions[n] = Vector3.zero;
                physicsFingerPadPositions[n] = Vector3.zero;
            }
        }
    }
}