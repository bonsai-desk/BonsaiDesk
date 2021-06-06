using System.Collections;
using Mirror;
using UnityEngine;

public class NetworkHand : NetworkBehaviour
{
    public OVRSkeleton.SkeletonType _skeletonType;
    private OVRHandTransformMapper mapper;

    [SyncVar(hook = nameof(FingerRotationsHook))]
    private ulong fingerRotations = 0;

    [SyncVar] private ulong thumbRotations = 0;

    private float lastSetTime = 0;

    private float[] renderedFingerRotations = new float[18];
    private Quaternion renderedThumbRotation = Quaternion.identity;

    private float[] oldRenderedFingerRotations = new float[18];
    private Quaternion oldRenderedThumbRotation = Quaternion.identity;

    public float updateInterval = 1f / 10f;

    private float lastRotationsUpdateTime = 0;

    public LineRenderer lineRenderer;

    [SyncVar] public NetworkIdentityReference ownerIdentity = new NetworkIdentityReference();

    //pinch pull info
    [SyncVar] private uint _pinchPullAttachedToId = uint.MaxValue;
    [SyncVar] private Vector3 _pinchPullLocalHitPoint = Vector3.zero;
    [SyncVar] private float _pinchPullRopeLength = 0f;

    [SyncVar(hook = nameof(OnActiveChange))]
    private bool _active;

    private bool _disableWaitPeriodDone = false;

    private GameObject physicsHand;
    private PhysicsHandController _physicsHandController;
    private SkinnedMeshRenderer _physicsHandRenderer;

    private Material _handMaterial;
    private Texture _handTexture;

    private void Start()
    {
        mapper = GetComponent<OVRHandTransformMapper>();
        for (int i = 0; i < renderedFingerRotations.Length; i++)
            renderedFingerRotations[i] = 0;
    }

    public override void OnStartServer()
    {
        _active = false;

        if (!isClient)
        {
            SetupPhysicsHand();
        }
    }

    public override void OnStartClient()
    {
        if (!hasAuthority)
        {
            SetupPhysicsHand();
            return;
        }

        if (_skeletonType == OVRSkeleton.SkeletonType.HandLeft)
            InputManager.Hands.Left.NetworkHand = this;
        if (_skeletonType == OVRSkeleton.SkeletonType.HandRight)
            InputManager.Hands.Right.NetworkHand = this;

        StartCoroutine(WaitThenActivateClientAuthority());
    }

    private void SetupPhysicsHand()
    {
        GameObject physicsHandPrefab;
        if (_skeletonType == OVRSkeleton.SkeletonType.HandLeft)
            physicsHandPrefab = Resources.Load<GameObject>("Left_Hand");
        else
            physicsHandPrefab = Resources.Load<GameObject>("Right_Hand");
        var hand = Instantiate(physicsHandPrefab);
        _physicsHandController = hand.transform.GetChild(0).GetComponent<PhysicsHandController>();
        _physicsHandController.SetKinematic();
        var physicsMapper = hand.transform.GetChild(1).GetComponent<OVRHandTransformMapper>();
        physicsMapper.targetObject = transform;
        physicsMapper.capsulesParent = transform;
        physicsMapper.TryAutoMapBonesTargetsByName();
        physicsMapper.moveObjectToTarget = true;
        physicsMapper.moveBonesToTargets = true;
        physicsMapper.fixRotation = false;
        physicsHand = hand;

        _physicsHandRenderer = physicsHand.GetComponentInChildren<SkinnedMeshRenderer>();
        _physicsHandRenderer.gameObject.layer = LayerMask.NameToLayer("networkPlayer");
        _handMaterial = _physicsHandRenderer.material;
        if (_handTexture)
        {
            _handMaterial.mainTexture = _handTexture;
        }

        HandComponents.SetLayerRecursive(hand.transform.GetChild(0), LayerMask.NameToLayer("onlyHands"));
        
        _physicsHandController.overrideCapsulesActive = true;
        _physicsHandRenderer.enabled = false;
        _physicsHandController.overrideCapsulesActiveTarget = false;
        StartCoroutine(WaitThenActivate());
    }
    
    private IEnumerator WaitThenActivate()
    {
        yield return new WaitForSeconds(1f);
        _disableWaitPeriodDone = true;
        OnActiveChange(false, _active);
    }
    
    private IEnumerator WaitThenActivateClientAuthority()
    {
        yield return new WaitForSeconds(1f);
        CmdSetActive(InputManager.Hands.GetHand(_skeletonType).TrackingRecently);
    }

    [Command]
    public void CmdSetActive(bool active)
    {
        _active = active;
    }

    private void OnActiveChange(bool oldValue, bool newValue)
    {
        if (hasAuthority)
        {
            return;
        }

        if (_physicsHandController && _physicsHandRenderer && _disableWaitPeriodDone)
        {
            var active = newValue;
            if (active)
            {
                _physicsHandController.overrideCapsulesActive = false;
                _physicsHandRenderer.enabled = true;
            }
            else
            {
                _physicsHandController.overrideCapsulesActive = true;
                _physicsHandRenderer.enabled = false;
            }

            _physicsHandController.overrideCapsulesActiveTarget = false;
        }
    }

    public void ChangeHandTexture(Texture texture)
    {
        _handTexture = texture;
        if (_handMaterial)
        {
            _handMaterial.mainTexture = texture;
        }
    }

    public override void OnStopServer()
    {
        if (physicsHand)
        {
            Destroy(physicsHand);
        }

        base.OnStopServer();
    }

    public override void OnStopClient()
    {
        if (physicsHand)
        {
            Destroy(physicsHand);
        }

        base.OnStopClient();
    }

    private void Update()
    {
        if (!hasAuthority)
        {
            if (Time.time - lastRotationsUpdateTime < updateInterval)
                SetFingerRotations( /*hand*/);

            //maxvalue if attached to nothing, so don't draw
            //no need to retry if ownerIdentity.Value is null since this is in update, so it will keep trying in the next frames
            if (_pinchPullAttachedToId != uint.MaxValue && NetworkIdentity.spawned.TryGetValue(_pinchPullAttachedToId, out NetworkIdentity value) && ownerIdentity.Value)
            {
                var otherHand = ownerIdentity.Value.GetComponent<NetworkVRPlayer>().GetOtherHand(_skeletonType);
                if (otherHand)
                {
                    var from = otherHand.GetComponent<OVRHandTransformMapper>().CustomBones[20]
                        .position;
                    var to = GetComponent<OVRHandTransformMapper>().CustomBones[20].position;
                    var end = value.transform.TransformPoint(_pinchPullLocalHitPoint);
                    RenderPinchPullLine(from, to, end);
                }
            }
            else
            {
                StopRenderPinchPullLine();
            }

            return;
        }

        HandComponents hand = _skeletonType == OVRSkeleton.SkeletonType.HandLeft ? InputManager.Hands.Left : InputManager.Hands.Right;

        if (!hand.Tracking)
            return;

        if (Time.time - lastSetTime > updateInterval)
        {
            lastSetTime = Time.time;
            var (rotations, tRotations) = GetFingerRotations(hand.PhysicsMapper);
            CmdSetFingerRotations(rotations, tRotations);

            //uncomment this for the network hand to also update for the player who owns it
            // SetFingerRotations();
        }
    }

    [Command]
    public void CmdSetPinchPullInfo(uint pinchPullAttachedToId, Vector3 pinchPullLocalHitPoint, float pinchPullRopeLength)
    {
        _pinchPullAttachedToId = pinchPullAttachedToId;
        _pinchPullLocalHitPoint = pinchPullLocalHitPoint;
        _pinchPullRopeLength = pinchPullRopeLength;
    }

    [Command]
    public void CmdStopPinchPull()
    {
        _pinchPullAttachedToId = uint.MaxValue;
    }

    public void StopRenderPinchPullLine()
    {
        for (int i = 0; i < 4; i++)
            lineRenderer.SetPosition(i, Vector3.zero);
    }

    //from is hand holding end of string, to is other hand, end is somewhere on the hit object
    public void RenderPinchPullLine(Vector3 from, Vector3 to, Vector3 end)
    {
        //p1 = from
        //p2 = to

        var positions = new Vector3[4];

        float fingerDistance = Vector3.Distance(from, to);
        float fingerToConnectedPosition = Vector3.Distance(to, end);
        if (fingerDistance > _pinchPullRopeLength - 0.05f) //PinchPullHand.MinRopeLength TODO add static reference back
        {
            Vector3 direction = Quaternion.LookRotation(from - to) * Vector3.forward;
            Vector3 start = to + (direction * _pinchPullRopeLength);

            positions[0] = start;
            positions[1] = (start + to) / 2f;
        }
        else
        {
            positions[0] = from;

            Vector3 ropeBottom = (from + to) / 2f;
            float extraRope = _pinchPullRopeLength - (fingerDistance + fingerToConnectedPosition);
            float a = fingerDistance / 2f;
            float c = (fingerDistance + extraRope) / 2f;
            Vector3 down = Quaternion.LookRotation(to - from) * Vector3.down;
            float downDistance = Mathf.Sqrt(Mathf.Abs((c * c) - (a * a)));
            if (!float.IsNaN(downDistance))
                ropeBottom += down * downDistance;

            positions[1] = ropeBottom;
        }

        positions[2] = to;
        positions[3] = end;
        lineRenderer.SetPositions(positions);
    }

    private void FingerRotationsHook(ulong oldRotations, ulong newRotations)
    {
        SetOldRotations();
    }

    [Command]
    private void CmdSetFingerRotations(ulong rotations, ulong tRotations)
    {
        fingerRotations = rotations;
        thumbRotations = tRotations;

        SetOldRotations();
    }

    private void SetOldRotations()
    {
        for (int i = 0; i < renderedFingerRotations.Length; i++)
            oldRenderedFingerRotations[i] = renderedFingerRotations[i];
        oldRenderedThumbRotation = renderedThumbRotation;
        lastRotationsUpdateTime = Time.time;
    }

    private (ulong rotations, ulong tRotations) GetFingerRotations(OVRHandTransformMapper mapper)
    {
        ulong rotations = 0;
        ulong tRotations = 0;

        //index middle ring pinky
        for (int i = 0; i < 4; i++)
        {
            int fingerIndex = 6 + (i * 3);
            if (i == 3)
                fingerIndex++;

            Vector3 localRight1 = mapper.CustomBones[fingerIndex].localRotation * Vector3.right;
            localRight1.z = 0;
            float rotation1 = Vector3.Angle(Vector3.right, localRight1);
            rotation1 = Mathf.Clamp(rotation1, 0f, 90f);
            byte rotation1Byte = (byte) Mathf.FloorToInt((rotation1 / 90f) * 255f);
            rotation1Byte = (byte) Mathf.Clamp(rotation1Byte, 0, 255);
            rotations <<= 8;
            rotations |= rotation1Byte;

            Vector3 localRight2 = mapper.CustomBones[fingerIndex + 1].localRotation * Vector3.right;
            localRight2.z = 0;
            float rotation2 = Vector3.Angle(Vector3.right, localRight2);
            rotation2 = Mathf.Clamp(rotation2, 0f, 90f);
            byte rotation2Byte = (byte) Mathf.FloorToInt((rotation2 / 90f) * 255f);
            rotation2Byte = (byte) Mathf.Clamp(rotation2Byte, 0, 255);
            rotations <<= 8;
            rotations |= rotation2Byte;
        }

        //thumb
        int thumbIndex = 3;

        Quaternion localRotation = mapper.CustomBones[thumbIndex].localRotation;
        for (int i = 0; i < 4; i++)
        {
            byte qPart = (byte) ((localRotation[i] + 1f) / 2f * 255f);
            tRotations <<= 8;
            tRotations |= qPart;
        }

        Vector3 localRightThumb1 = mapper.CustomBones[thumbIndex + 1].localRotation * Vector3.right;
        localRightThumb1.z = 0;
        float rotationThumb1 = Vector3.Angle(Vector3.right, localRightThumb1);
        rotationThumb1 = Mathf.Clamp(rotationThumb1, 0f, 90f);
        byte rotationThumb1Byte = (byte) Mathf.FloorToInt((rotationThumb1 / 90f) * 255f);
        rotationThumb1Byte = (byte) Mathf.Clamp(rotationThumb1Byte, 0, 255);
        tRotations <<= 8;
        tRotations |= rotationThumb1Byte;

        Vector3 localRightThumb2 = mapper.CustomBones[thumbIndex + 2].localRotation * Vector3.right;
        localRightThumb2.z = 0;
        float rotationThumb2 = Vector3.Angle(Vector3.right, localRightThumb2);
        rotationThumb2 = Mathf.Clamp(rotationThumb2, 0f, 90f);
        byte rotationThumb2Byte = (byte) Mathf.FloorToInt((rotationThumb2 / 90f) * 255f);
        rotationThumb2Byte = (byte) Mathf.Clamp(rotationThumb2Byte, 0, 255);
        tRotations <<= 8;
        tRotations |= rotationThumb2Byte;

        return (rotations, tRotations);
    }

    private void SetFingerRotations( /*PlayerHand hand*/)
    {
        ulong rotations = fingerRotations;
        ulong tRotations = thumbRotations;

        //index middle ring pinky
        for (int i = 3; i >= 0; i--)
        {
            int fingerIndex = 6 + (i * 3);
            if (i == 3)
                fingerIndex++;

            byte rotation2Byte = (byte) (rotations & 0b_1111_1111);
            rotations >>= 8;
            float rotation2 = (rotation2Byte / 255f) * 90f;

            byte rotation1Byte = (byte) (rotations & 0b_1111_1111);
            rotations >>= 8;
            float rotation1 = (rotation1Byte / 255f) * 90f;

            renderedFingerRotations[fingerIndex] += (rotation1 - oldRenderedFingerRotations[fingerIndex]) * Time.deltaTime / updateInterval;
            renderedFingerRotations[fingerIndex + 1] += (rotation2 - oldRenderedFingerRotations[fingerIndex + 1]) * Time.deltaTime / updateInterval;

            // mapper.CustomBones[fingerIndex].localRotation = hand.oVRSkeleton.BindPoses[fingerIndex].Transform.localRotation * Quaternion.Euler(0, 0, -renderedFingerRotations[fingerIndex]);
            // mapper.CustomBones[fingerIndex + 1].localRotation = hand.oVRSkeleton.BindPoses[fingerIndex + 1].Transform.localRotation * Quaternion.Euler(0, 0, -renderedFingerRotations[fingerIndex + 1]);
            // mapper.CustomBones[fingerIndex + 2].localRotation = hand.oVRSkeleton.BindPoses[fingerIndex + 2].Transform.localRotation * Quaternion.Euler(0, 0, -renderedFingerRotations[fingerIndex + 1]);

            mapper.CustomBones[fingerIndex].localEulerAngles = new Vector3(mapper.CustomBones[fingerIndex].localRotation.x,
                mapper.CustomBones[fingerIndex].localRotation.y, -renderedFingerRotations[fingerIndex]);
            mapper.CustomBones[fingerIndex + 1].localEulerAngles = new Vector3(mapper.CustomBones[fingerIndex + 1].localRotation.x,
                mapper.CustomBones[fingerIndex + 1].localRotation.y, -renderedFingerRotations[fingerIndex + 1]);
            mapper.CustomBones[fingerIndex + 2].localEulerAngles = new Vector3(mapper.CustomBones[fingerIndex + 2].localRotation.x,
                mapper.CustomBones[fingerIndex + 2].localRotation.y, -renderedFingerRotations[fingerIndex + 1]);
        }

        //thumb
        int thumbIndex = 3;

        byte rotationThumb2Byte = (byte) (tRotations & 0b_1111_1111);
        tRotations >>= 8;
        float rotationThumb2 = (rotationThumb2Byte / 255f) * 90f;

        //byte rotationThumb1Byte = (byte)(tRotations & 0b_1111_1111);
        tRotations >>= 8;
        //float rotationThumb1 = (rotationThumb1Byte / 255f) * 90f;

        renderedFingerRotations[thumbIndex + 1] += (rotationThumb2 - oldRenderedFingerRotations[thumbIndex + 1]) * Time.deltaTime / updateInterval;
        renderedFingerRotations[thumbIndex + 2] += (rotationThumb2 - oldRenderedFingerRotations[thumbIndex + 2]) * Time.deltaTime / updateInterval;

        Quaternion localRotation = Quaternion.identity;
        for (int i = 3; i >= 0; i--)
        {
            byte qPart = (byte) (tRotations & 0b_1111_1111);
            localRotation[i] = (qPart / 255f) * 2f - 1f;
            tRotations >>= 8;
        }

        renderedThumbRotation = Quaternion.RotateTowards(renderedThumbRotation, localRotation,
            Vector3.Angle(oldRenderedThumbRotation * Vector3.right, localRotation * Vector3.right) * Time.deltaTime / updateInterval);

        mapper.CustomBones[thumbIndex].localRotation = renderedThumbRotation;

        // mapper.CustomBones[thumbIndex + 1].localRotation = hand.oVRSkeleton.BindPoses[thumbIndex + 1].Transform.localRotation * Quaternion.Euler(0, 0, -renderedFingerRotations[thumbIndex + 1]);
        // mapper.CustomBones[thumbIndex + 2].localRotation = hand.oVRSkeleton.BindPoses[thumbIndex + 2].Transform.localRotation * Quaternion.Euler(0, 0, -renderedFingerRotations[thumbIndex + 2]);

        mapper.CustomBones[thumbIndex + 1].localEulerAngles = new Vector3(mapper.CustomBones[thumbIndex + 1].localEulerAngles.x,
            mapper.CustomBones[thumbIndex + 1].localEulerAngles.y, -renderedFingerRotations[thumbIndex + 1]);
        mapper.CustomBones[thumbIndex + 2].localEulerAngles = new Vector3(mapper.CustomBones[thumbIndex + 2].localEulerAngles.x,
            mapper.CustomBones[thumbIndex + 2].localEulerAngles.y, -renderedFingerRotations[thumbIndex + 2]);
    }
}