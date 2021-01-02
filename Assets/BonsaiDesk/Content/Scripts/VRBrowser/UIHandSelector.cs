using UnityEngine;
using UnityEngine.EventSystems;

public class UIHandSelector : MonoBehaviour
{
    // private OVRInputModule inputModule;
    // public LineRenderer lineRenderer;
    //
    // public Transform nullPoint;
    //
    // private void Start()
    // {
    //     inputModule = FindObjectOfType<OVRInputModule>();
    // }
    //
    // private void Update()
    // {
    //     if (PlayerHands.hands.activePointerPoseHand != null)
    //     {
    //         lineRenderer.enabled = true;
    //         inputModule.rayTransform = PlayerHands.hands.activePointerPoseHand.oPointerPose;
    //         if (PlayerHands.hands.activePointerPoseHand.skeletonType == OVRSkeleton.SkeletonType.HandLeft)
    //             inputModule.joyPadClickButton = OVRInput.Button.Three;
    //         else if (PlayerHands.hands.activePointerPoseHand.skeletonType == OVRSkeleton.SkeletonType.HandRight)
    //             inputModule.joyPadClickButton = OVRInput.Button.One;
    //         else
    //             inputModule.joyPadClickButton = OVRInput.Button.None;
    //     }
    //     else
    //     {
    //         lineRenderer.enabled = false;
    //         inputModule.rayTransform = nullPoint;
    //         inputModule.joyPadClickButton = OVRInput.Button.None;
    //     }
    // }
}