using System;
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
    public readonly HandAnimatorController TargetHandAnimatorController;

    //0 - thumb, 1 - index, 2 - middle, 3 - ring, 4 - pinky
    public readonly Transform[] PhysicsFingerTips;
    public readonly Transform[] TargetFingerTips;

    public NetworkHand NetworkHand;

    public bool MapperTargetsInitialized = false;
    public bool Tracking { get; private set; } = false;
    public bool TrackingRecently { get; private set; } = false;
    private bool _lastTrackingRecently = false;

    private readonly int _physicsLayer;
    private readonly int _indexPhysicsLayer;
    private readonly int _onlyScreenLayer;
    private readonly int _touchScreenSurfaceLayer;
    private readonly int _indexForScreenPhysicsLayer;

    private float _lastTrackingTime;
    private const float RecentTrackingThreshold = 0.35f;
    private SkinnedMeshRenderer _physicsRenderer;
    private Material _handMaterial;
    private float _handAlpha = 1f;
    private bool _zTestOverlay = false;

    public HandComponents(PlayerHand playerHand, Transform handAnchor, Transform handObject, RuntimeAnimatorController animationController)
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
        _handMaterial.SetInt("_ZWrite", 1);
        MakeMaterialOpaque();
        PhysicsHandController = PhysicsHand.GetComponent<PhysicsHandController>();
        PhysicsHandController.isOwnHand = true;
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
        _onlyScreenLayer = OVRSkeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.HandLeft
            ? LayerMask.NameToLayer("OnlyTouchScreenSurfaceLeft")
            : LayerMask.NameToLayer("OnlyTouchScreenSurfaceRight");
        _indexForScreenPhysicsLayer = LayerMask.NameToLayer("IndexTipForTouchScreenSurface");
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

        var handTargetAnimator = TargetHand.gameObject.AddComponent<Animator>();
        handTargetAnimator.runtimeAnimatorController = animationController;
        handTargetAnimator.enabled = false;

        TargetHandAnimatorController = TargetHand.gameObject.AddComponent<HandAnimatorController>();
        TargetHandAnimatorController.controller =
            PlayerHand.skeletonType == OVRSkeleton.SkeletonType.HandLeft ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
        TargetHandAnimatorController.animator = handTargetAnimator;
    }

    public void SetPhysicsLayerRegular()
    {
        SetLayerRecursive(PhysicsHand, _physicsLayer);
        SetLayerRecursive(PhysicsMapper.BoneTargets[(int) OVRSkeleton.BoneId.Hand_Index3], _indexPhysicsLayer);
    }

    public void SetPhysicsLayerForTouchScreen()
    {
        SetLayerRecursive(PhysicsHand, _onlyScreenLayer);
        SetLayerRecursive(PhysicsMapper.BoneTargets[(int) OVRSkeleton.BoneId.Hand_Index3], _indexForScreenPhysicsLayer);
        PlayerHand.stylus.gameObject.layer = _indexForScreenPhysicsLayer;
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
        // if (!Tracking && tracking)
        // {
        //     PhysicsHandController.SetCapsulesActiveTarget(false);
        //     PhysicsHandController.ResetFingerJoints();
        //     PhysicsHandController.SetCapsulesActiveTarget(true);
        // }

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

        if (NetworkHand)
        {
            if (TrackingRecently != _lastTrackingRecently)
            {
                NetworkHand.CmdSetActive(TrackingRecently);
            }
        }

        _lastTrackingRecently = TrackingRecently;

        UpdateRendererTransparency();
    }

    public void SetHandTexture(Texture texture)
    {
        _handMaterial.mainTexture = texture;
    }

    private void UpdateRendererTransparency()
    {
        bool isTransparent = _handMaterial.GetInt("_DstBlend") == (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;

        float handAlphaTarget = Tracking ? 1f : 0f;
        _handAlpha = Mathf.MoveTowards(_handAlpha, handAlphaTarget, Time.deltaTime / RecentTrackingThreshold);
        var playing = Application.isFocused && Application.isPlaying || Application.isEditor;
        var controllersAndInVoid = !InputManager.Hands.UsingHandTracking && !MoveToDesk.Singleton.oriented;
        if (!playing || controllersAndInVoid)
        {
            _handAlpha = 0;
        }

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

    public void TurnOffHandForPause()
    {
        _physicsRenderer.enabled = false;
    }

    public void ZTestRegular()
    {
        _zTestOverlay = false;

        bool isTransparent = _handMaterial.GetInt("_DstBlend") == (int) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;

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
        _handMaterial.SetInt("_SrcBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
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
    /// The wording for SetHandColliderActiveForScreen is confusing, so this function makes it easier to use.
    /// If usingScreen is true, your hand clips through the screen. If it is false, your whole hand hits it like normal
    /// </summary>
    /// <param name="usingScreen">true if a screen is up and your hand should clip through it</param>
    public void SetPhysicsForUsingScreen(bool usingScreen)
    {
        SetHandColliderActiveForScreen(!usingScreen);
    }

    /// <summary>
    /// Use SetPhysicsForUsingScreen instead.
    /// changes the layer collision matrix for TouchScreenSurface to not collide or not collide with this hand
    /// excluding the index tip collider
    /// </summary>
    /// <param name="active">if active is true, your hand will hit the screen like normal</param>
    private void SetHandColliderActiveForScreen(bool active)
    {
        Physics.IgnoreLayerCollision(_touchScreenSurfaceLayer, _physicsLayer, !active);
        Physics.IgnoreLayerCollision(_touchScreenSurfaceLayer, _onlyScreenLayer, !active);
    }

    public static void SetLayerRecursive(Transform go, int layer)
    {
        if (!go.CompareTag("KeepLayer"))
        {
            go.gameObject.layer = layer;
        }

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