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
    public readonly PhysicsHandController PhysicsHandController;

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
        PhysicsHandController = PhysicsHand.GetComponent<PhysicsHandController>();
        TargetHand = handObject.GetChild(1);
        PlayerHand.transform.SetParent(PhysicsHand, false);

        PhysicsMapper = PhysicsHand.GetComponentInChildren<OVRHandTransformMapper>();
        PlayerHand.physicsMapper = PhysicsMapper;

        TargetMapper = TargetHand.GetComponent<OVRHandTransformMapper>();
        TargetMapper.targetObject = handAnchor;

        OVRSkeleton = handAnchor.GetComponentInChildren<OVRSkeleton>();
        SetLayerRecursive(PhysicsHand,
            OVRSkeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.HandLeft
                ? LayerMask.NameToLayer("LeftHand")
                : LayerMask.NameToLayer("RightHand"));
        OVRHand = handAnchor.GetComponentInChildren<OVRHand>();

        PhysicsFingerTips = GetFingerTips(PhysicsMapper);
        TargetFingerTips = GetFingerTips(TargetMapper);

        SetTagRecursive(PhysicsMapper.BoneTargets[(int) OVRSkeleton.BoneId.Hand_Thumb3], "FingerTip");
        SetTagRecursive(PhysicsMapper.BoneTargets[(int) OVRSkeleton.BoneId.Hand_Index3], "IndexTip");
        SetTagRecursive(PhysicsMapper.BoneTargets[(int) OVRSkeleton.BoneId.Hand_Middle3], "FingerTip");
        SetTagRecursive(PhysicsMapper.BoneTargets[(int) OVRSkeleton.BoneId.Hand_Ring3], "FingerTip");
        SetTagRecursive(PhysicsMapper.BoneTargets[(int) OVRSkeleton.BoneId.Hand_Pinky3], "FingerTip");
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

    private static void SetLayerRecursive(Transform go, int layer)
    {
        go.gameObject.layer = layer;
        foreach (Transform child in go)
        {
            SetLayerRecursive(child, layer);
        }
    }

    private static void SetTagRecursive(Transform go, string tag)
    {
        go.gameObject.tag = tag;
        foreach (Transform child in go)
        {
            SetTagRecursive(child, tag);
        }
    }
}