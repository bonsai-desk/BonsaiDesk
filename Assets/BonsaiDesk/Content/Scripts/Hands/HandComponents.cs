using UnityEngine;

public class HandComponents
{
    public readonly PlayerHand PlayerHand;
    public readonly Transform HandAnchor;
    public readonly Transform PhysicsHand;
    public readonly Transform TargetHand;
    public readonly OVRHandTransformMapper PhysicsMapper;
    public readonly OVRHandTransformMapper TargetMapper;
    public readonly OVRSkeleton OVRSkeleton;
    public readonly OVRHand OVRHand;

    //0 - thumb, 1 - index, 2 - middle, 3 - ring, 4 - pinky
    public readonly Transform[] PhysicsFingerTips;
    public readonly Transform[] TargetFingerTips;

    public NetworkHand NetworkHand;

    public bool MapperTargetsInitialized = false;
    public bool Tracking { get; private set; } = false;

    public HandComponents(PlayerHand playerHand, Transform handAnchor, Transform handObject)
    {
        PlayerHand = playerHand;
        HandAnchor = handAnchor;

        PhysicsHand = handObject.GetChild(0);
        TargetHand = handObject.GetChild(1);
        PlayerHand.transform.SetParent(PhysicsHand, false);

        PhysicsMapper = PhysicsHand.GetComponentInChildren<OVRHandTransformMapper>();

        TargetMapper = TargetHand.GetComponent<OVRHandTransformMapper>();
        TargetMapper.targetObject = handAnchor;

        OVRSkeleton = handAnchor.GetComponentInChildren<OVRSkeleton>();
        OVRHand = handAnchor.GetComponentInChildren<OVRHand>();

        PhysicsFingerTips = GetFingerTips(PhysicsMapper);
        TargetFingerTips = GetFingerTips(TargetMapper);
    }

    private static Transform[] GetFingerTips(OVRHandTransformMapper mapper)
    {
        var fingerTips = new Transform[5];
        fingerTips[0] = mapper.CustomBones[(int) OVRSkeleton.BoneId.Hand_ThumbTip];
        fingerTips[1] = mapper.CustomBones[(int) OVRSkeleton.BoneId.Hand_IndexTip];
        fingerTips[2] = mapper.CustomBones[(int) OVRSkeleton.BoneId.Hand_MiddleTip];
        fingerTips[3] = mapper.CustomBones[(int) OVRSkeleton.BoneId.Hand_RingTip];
        fingerTips[4] = mapper.CustomBones[(int) OVRSkeleton.BoneId.Hand_PinkyTip];
        return fingerTips;
    }

    public void SetTracking(bool tracking)
    {
        Tracking = tracking;
    }
}