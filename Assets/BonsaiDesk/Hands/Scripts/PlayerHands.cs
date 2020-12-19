using UnityEngine;

public class PlayerHands : MonoBehaviour
{
    public static PlayerHands hands;

    // public GameObject OVRHandLeft;
    // public GameObject OVRHandRight;

    // public Rigidbody leftHandBody;
    // public Rigidbody rightHandBody;

    public PlayerHand left;
    public PlayerHand right;

    // public Transform leftThumbTip;
    // public Transform rightThumbTip;

    [HideInInspector] //0-4 thumb to pinky on left hand, 5-9 thumb to pinky right hand. Will have value of Vector3.zero is not tracking
    public Vector3[] fingerTipPositions;

    [HideInInspector] //0-4 thumb to pinky on left hand, 5-9 thumb to pinky right hand. Will have value of Vector3.zero is not tracking
    public Vector3[] physicsFingerTipPositions;

    [HideInInspector] //0-4 thumb to pinky on left hand, 5-9 thumb to pinky right hand. Will have value of Vector3.zero is not tracking
    public Vector3[] physicsFingerPadPositions;

    public Transform head;

    [HideInInspector]
    public PlayerHand activePointerPoseHand;

    private void Awake()
    {
        if (hands == null)
            hands = this;
    }

    private void Start()
    {
        // left = new PlayerHand(OVRHandLeft.GetComponent<OVRHand>(), OVRHandLeft.GetComponent<OVRSkeleton>(), leftHandBody, OVRSkeleton.SkeletonType.HandLeft, leftThumbTip);
        // right = new PlayerHand(OVRHandRight.GetComponent<OVRHand>(), OVRHandRight.GetComponent<OVRSkeleton>(), rightHandBody, OVRSkeleton.SkeletonType.HandRight, rightThumbTip);

        fingerTipPositions = new Vector3[10];
        physicsFingerTipPositions = new Vector3[10];
        physicsFingerPadPositions = new Vector3[10];
        CalculateFingerTipPositions();
    }

    private void Update()
    {
        CalculateFingerTipPositions();
        // left.Update();
        // right.Update();

        if (left.hitDistance > 1000f && right.hitDistance > 1000f)
        {
            activePointerPoseHand = null;
        }
        else
        {
            if (left.hitDistance < right.hitDistance)
                activePointerPoseHand = left;
            else
                activePointerPoseHand = right;
        }
    }

    //   private void FixedUpdate()
    //   {
    //       // if (left.tracking() && right.tracking())
    //       // {
    //       //     left.beamJointBody.MovePosition(left.fingerTips[0].position);
    //       //     right.beamJointBody.MovePosition(right.fingerTips[0].position);
    //       // }
    //
    //       // PlayerHand hand = left;
    //       // if (left.objectAttached)
    //       //     hand = left;
    //       // else if (right.objectAttached)
    //       //     hand = right;
    //       // else
    //       // {
    //       //     left.beamLine.enabled = false;
    //       //     right.beamLine.enabled = false;
    //       //     return;
    //       // }
    //       // Vector3 attachPoint = hand.beamHold.TransformPoint(hand.beamJoint.connectedAnchor);
    //       // if (!left.tracking() || !right.tracking())
    //       // {
    //       //     hand.beamLine.SetPosition(3, attachPoint);
    //       //     return;
    //       // }
    //       // left.beamLine.enabled = false;
    //       // right.beamLine.enabled = false;
    //
    //       // float fingerDistance = Vector3.Distance(left.beamJointBody.transform.position, right.beamJointBody.transform.position);
    //
    //       // float ld = left.FixedUpdateExternal(fingerDistance);
    //       // float rd = right.FixedUpdateExternal(fingerDistance);
    //       // float difference = Mathf.Max(ld, rd);
    //
    //       // float ropeLeft = hand.ropeLength - hand.beamJoint.linearLimit.limit;
    //       // if (hand.beamJoint.linearLimit.limit <= 0.001f && fingerDistance > ropeLeft + 0.001f)
    //       // {
    //       //     Vector3 direction = Quaternion.LookRotation(hand.otherHand().beamJointBody.transform.position - hand.beamJointBody.transform.position) * Vector3.forward;
    //       //     Vector3 start = hand.beamJointBody.transform.position + (direction * ropeLeft);
    //       //     hand.beamLine.SetPosition(0, start);
    //       //     hand.beamLine.SetPosition(1, (start + hand.beamJointBody.transform.position) / 2f);
    //       // }
    //       // else
    //       // {
    //       //     hand.beamLine.SetPosition(0, hand.otherHand().beamJointBody.transform.position);
    //       //     Vector3 ropeBottom = (hand.otherHand().beamJointBody.transform.position + hand.beamJointBody.transform.position) / 2f;
    //       //     float extraRope = hand.beamJoint.linearLimit.limit - Vector3.Distance(hand.beamJointBody.transform.position, attachPoint);
    //       //     extraRope = Mathf.Clamp(extraRope - difference, 0, Mathf.Infinity);
    //       //     float a = fingerDistance / 2f;
    //       //     float c = (fingerDistance + extraRope) / 2f;
    //       //     Vector3 down = Quaternion.LookRotation(hand.beamJointBody.transform.position - hand.otherHand().beamJointBody.transform.position) * Vector3.down;
    //       //     float downDistance = Mathf.Sqrt(Mathf.Abs((c * c) - (a * a)));
    //       //     if (!float.IsNaN(downDistance))
    //       //         ropeBottom += down * downDistance;
    //       //     hand.beamLine.SetPosition(1, ropeBottom);
    //       // }
    //
    //       // hand.beamLine.enabled = true;
    //       // hand.beamLine.SetPosition(2, hand.beamJointBody.transform.position);
    //       // hand.beamLine.SetPosition(3, attachPoint);
    //   }
    //
    //   // public static void DestroyJoint(Object obj)
    //   // {
    //   //     Destroy(obj);
    //   // }

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