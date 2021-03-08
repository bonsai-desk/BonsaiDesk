using UnityEditor;
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
    public bool TrackingRecently { get; private set; } = false;

    private readonly int _physicsLayer;
    private readonly int _indexPhysicsLayer;
    private readonly int _onlyScreenLayer;
    private readonly int _touchScreenSurfaceLayer;

    private float _lastTrackingTime;
    private const float RecentTrackingThreshold = 0.35f;
    private SkinnedMeshRenderer _physicsRenderer;
    private Material _handMaterial;
    private float _handAlpha = 1f;
    private bool _zTestOverlay = false;

    public HandComponents(PlayerHand playerHand, Transform handAnchor, Transform handObject)
    {
        PlayerHand = playerHand;
        PlayerHand.HandComponents = this;
        HandAnchor = handAnchor;

        PhysicsHand = handObject.GetChild(0);
        _physicsRenderer = PhysicsHand.GetComponentInChildren<SkinnedMeshRenderer>();
        if (NetworkManagerGame.Singleton.serverOnlyIfEditor && Application.isEditor)
        {
            _physicsRenderer.enabled = false;
        }

        _handMaterial = _physicsRenderer.material;
        _handMaterial.mainTexture = Resources.Load<Texture>("Green-Square");
        _handMaterial.SetInt("_ZWrite", 1);
        MakeMaterialOpaque();
        PhysicsHandController = PhysicsHand.GetComponent<PhysicsHandController>();
        TargetHand = handObject.GetChild(1);
        PlayerHand.transform.SetParent(PhysicsHand, false);
        TargetHand.GetComponentInChildren<SkinnedMeshRenderer>().enabled = InputManager.Hands.renderTargetHands;

        handObject.name += "_Local";
        PhysicsHand.name += "_Local";
        TargetHand.name += "_Local";

        PhysicsMapper = PhysicsHand.GetComponentInChildren<OVRHandTransformMapper>();

        TargetMapper = TargetHand.GetComponent<OVRHandTransformMapper>();
        TargetMapper.targetObject = handAnchor;

        OVRSkeleton = handAnchor.GetComponentInChildren<OVRSkeleton>();
        PlayerHand.skeletonType = OVRSkeleton.GetSkeletonType();
        _physicsLayer = OVRSkeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.HandLeft
            ? LayerMask.NameToLayer("LeftHand")
            : LayerMask.NameToLayer("RightHand");
        _indexPhysicsLayer = LayerMask.NameToLayer("IndexTip");
        _onlyScreenLayer = LayerMask.NameToLayer("OnlyTouchScreenSurface");
        OVRHand = handAnchor.GetComponentInChildren<OVRHand>();

        PhysicsFingerTips = GetFingerTips(PhysicsMapper);
        TargetFingerTips = GetFingerTips(TargetMapper);

        SetTagRecursive(PhysicsMapper.BoneTargets[(int) OVRSkeleton.BoneId.Hand_Thumb3], "FingerTip");
        SetTagRecursive(PhysicsMapper.BoneTargets[(int) OVRSkeleton.BoneId.Hand_Index3], "IndexTip");
        SetTagRecursive(PhysicsMapper.BoneTargets[(int) OVRSkeleton.BoneId.Hand_Middle3], "FingerTip");
        SetTagRecursive(PhysicsMapper.BoneTargets[(int) OVRSkeleton.BoneId.Hand_Ring3], "FingerTip");
        SetTagRecursive(PhysicsMapper.BoneTargets[(int) OVRSkeleton.BoneId.Hand_Pinky3], "FingerTip");

        var bodies = PhysicsHand.GetComponentsInChildren<Rigidbody>();
        foreach (var body in bodies)
        {
            body.gameObject.AddComponent<HandAuthority>();
        }

        _touchScreenSurfaceLayer = LayerMask.NameToLayer("TouchScreenSurface");
    }

    public void SetPhysicsLayerRegular()
    {
        SetLayerRecursive(PhysicsHand, _physicsLayer);
        SetLayerRecursive(PhysicsMapper.BoneTargets[(int) OVRSkeleton.BoneId.Hand_Index3], _indexPhysicsLayer);
    }

    public void SetPhysicsLayerForTouchScreen()
    {
        SetLayerRecursive(PhysicsHand, _onlyScreenLayer);
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
        //if tracking just started this frame
        if (!Tracking && tracking)
        {
            PhysicsHandController.SetCapsulesActiveTarget(false);
            PhysicsHandController.ResetFingerJoints();
            PhysicsHandController.SetCapsulesActiveTarget(true);
        }

        Tracking = tracking;

        if (tracking)
        {
            _lastTrackingTime = Time.time;
            TrackingRecently = true;
        }
        else if (Time.time - _lastTrackingTime > RecentTrackingThreshold)
        {
            TrackingRecently = false;
        }

        UpdateRendererTransparency();
    }

    private void UpdateRendererTransparency()
    {
        bool isTransparent =
            _handMaterial.GetInt("_DstBlend") == (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;

        float handAlphaTarget = Tracking ? 1f : 0f;
        _handAlpha = Mathf.MoveTowards(_handAlpha, handAlphaTarget, Time.deltaTime / RecentTrackingThreshold);

        if (Mathf.Approximately(_handAlpha, 1f))
        {
            if (isTransparent)
            {
                MakeMaterialOpaque();
            }

            if (!_physicsRenderer.enabled)
            {
                _physicsRenderer.enabled = true;
            }

            PhysicsHandController.SetCapsulesActiveTarget(true);
        }
        else if (Mathf.Approximately(_handAlpha, 0f))
        {
            if (_physicsRenderer.enabled)
            {
                _physicsRenderer.enabled = false;
            }

            PhysicsHandController.SetCapsulesActiveTarget(false);
        }
        else
        {
            if (!isTransparent)
            {
                MakeMaterialTransparent();
            }

            if (!_physicsRenderer.enabled)
            {
                _physicsRenderer.enabled = true;
            }

            _handMaterial.color = new Color(1, 1, 1, _handAlpha);

            PhysicsHandController.SetCapsulesActiveTarget(true);
        }
    }

    public void ZTestRegular()
    {
        _zTestOverlay = false;

        bool isTransparent =
            _handMaterial.GetInt("_DstBlend") == (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;

        if (isTransparent)
        {
            _handMaterial.renderQueue = (int) UnityEngine.Rendering.RenderQueue.Transparent;
        }
        else
        {
            _handMaterial.renderQueue = (int) UnityEngine.Rendering.RenderQueue.Geometry;
        }
    }

    public void ZTestOverlay()
    {
        _zTestOverlay = true;
        _handMaterial.renderQueue = (int) UnityEngine.Rendering.RenderQueue.Overlay + 2;
    }

    private void MakeMaterialTransparent()
    {
        _handMaterial.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.One);
        _handMaterial.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _handMaterial.DisableKeyword("_ALPHATEST_ON");
        _handMaterial.DisableKeyword("_ALPHABLEND_ON");
        _handMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        if (!_zTestOverlay)
            _handMaterial.renderQueue = (int) UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private void MakeMaterialOpaque()
    {
        _handMaterial.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.One);
        _handMaterial.SetInt("_DstBlend", (int) UnityEngine.Rendering.BlendMode.Zero);
        _handMaterial.DisableKeyword("_ALPHATEST_ON");
        _handMaterial.DisableKeyword("_ALPHABLEND_ON");
        _handMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        if (!_zTestOverlay)
            _handMaterial.renderQueue = (int) UnityEngine.Rendering.RenderQueue.Geometry;

        _handMaterial.color = new Color(1, 1, 1, 1);
    }

    /// <summary>
    /// changes the layer collision matrix for TouchScreenSurface to not collide or not collide with this hand
    /// excluding the index tip collider
    /// </summary>
    /// <param name="active"></param>
    public void SetHandColliderActiveForScreen(bool active)
    {
        Physics.IgnoreLayerCollision(_touchScreenSurfaceLayer, _physicsLayer, !active);
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