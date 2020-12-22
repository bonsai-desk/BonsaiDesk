using Mirror;
using UnityEngine;

public class SyncListVector3 : SyncList<Vector3> { }

public class NetworkVRPlayer : NetworkBehaviour
{
    public static NetworkVRPlayer self;

    public GameObject networkHandLeftPrefab;
    public GameObject networkHandRightPrefab;

    public GameObject blockAreaPrefab;

    public LineRenderer beamLine;
    // readonly SyncListVector3 beamLinePoints = new SyncListVector3();

    [SyncVar]
    private Vector3 beamAttachPoint;

    [SyncVar]
    private NetworkIdentity attachedObjectNetId;

    [SyncVar]
    private NetworkIdentity leftHandId;

    [SyncVar]
    private NetworkIdentity rightHandId;

    [SyncVar]
    private float ropeLength;

    [SyncVar]
    private int ropeHandIndex = 0;

    [SyncVar]
    private bool ropeActive = false;

    private Transform leftFingerTip;
    private Transform rightFingerTip;

    private void Start()
    {
        // beamLinePoints.Callback += updateBeamLineRenderer;

        if (!isLocalPlayer)
            return;

        if (self == null)
            self = this;

        CmdSpawnHands();
    }

    private void Update()
    {
        if (leftHandId != null && rightHandId != null && (leftFingerTip == null || rightFingerTip == null))
        {
            leftFingerTip = leftHandId.GetComponent<OVRHandTransformMapper>().CustomBones[20];
            rightFingerTip = rightHandId.GetComponent<OVRHandTransformMapper>().CustomBones[20];
        }

        if (!isLocalPlayer)
        {
            if (ropeActive && leftFingerTip != null && rightFingerTip != null && attachedObjectNetId != null)
            {
                // if (attachedObjectNetId != null)
                //     beamLine.SetPosition(3, attachedObjectNetId.gameObject.transform.TransformPoint(beamAttachPoint));
                // beamLine.SetPosition(0, leftFingerTip.position);
                // beamLine.SetPosition(1, rightFingerTip.position);
                // beamLine.SetPosition(2, rightFingerTip.position);
                if (ropeHandIndex == 0)
                    SetBeamPoints(leftFingerTip.position, rightFingerTip.position, beamAttachPoint, attachedObjectNetId.transform, ropeLength);
                else
                    SetBeamPoints(rightFingerTip.position, leftFingerTip.position, beamAttachPoint, attachedObjectNetId.transform, ropeLength);
            }
            else
            {
                for (int i = 0; i < 4; i++)
                    beamLine.SetPosition(i, Vector3.zero);
            }
        }

        if (!isLocalPlayer)
            return;
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer)
            return;

        PlayerHand left = PlayerHands.hands.left;
        PlayerHand right = PlayerHands.hands.right;

        if (left.Tracking() && right.Tracking())
        {
            left.beamJointBody.MovePosition(left.fingerTips[0].position);
            right.beamJointBody.MovePosition(right.fingerTips[0].position);
        }

        PlayerHand hand;
        if (left.objectAttached)
            hand = left;
        else if (right.objectAttached)
            hand = right;
        else
        {
            // beamLine.enabled = false;
            // beamLine.enabled = false;
            // CmdClearBeamPoints();
            if (ropeActive)
                CmdSetRopeActive(false);
            for (int i = 0; i < 4; i++)
                beamLine.SetPosition(i, Vector3.zero);
            return;
        }
        //Vector3 attachPoint = hand.beamHold.TransformPoint(hand.beamJoint.connectedAnchor);
        // if (!left.tracking() || !right.tracking())
        // {
        //     beamLine.SetPosition(3, attachPoint);
        //     return;
        // }
        // beamLine.enabled = false;
        // beamLine.enabled = false;

        //float fingerDistance = Vector3.Distance(left.beamJointBody.transform.position, right.beamJointBody.transform.position);

        //float ld = left.FixedUpdateExternal(fingerDistance);
        //float rd = right.FixedUpdateExternal(fingerDistance);
        // float difference = Mathf.Max(ld, rd);

        int ropeHandIndex = 0;
        if (hand._skeletonType == OVRSkeleton.SkeletonType.HandLeft)
            ropeHandIndex = 1;

        if (hand.beamJoint != null && hand.beamJoint.connectedBody != null)
        {
            SetBeamPoints(hand.OtherHand().beamJointBody.transform.position, hand.beamJointBody.transform.position, hand.beamJoint.connectedAnchor, hand.beamJoint.connectedBody.transform, hand.ropeLength);

            if (!ropeActive)
                CmdSetBeamPoints(hand.beamJoint.connectedAnchor, hand.beamJoint.connectedBody.GetComponent<NetworkIdentity>(), hand.ropeLength, ropeHandIndex);
        }
    }

    private void SetBeamPoints(Vector3 p1, Vector3 p2, Vector3 connectedAnchor, Transform connectedTo, float ropeLength)
    {
        Vector3 connectedPosition = connectedTo.TransformPoint(connectedAnchor);

        float fingerDistance = Vector3.Distance(p1, p2);
        float fingerToConnectedPosition = Vector3.Distance(p2, connectedPosition);
        if (fingerDistance > ropeLength - 0.001f)
        {
            Vector3 direction = Quaternion.LookRotation(p1 - p2) * Vector3.forward;
            Vector3 start = p2 + (direction * ropeLength);

            // beamLine.SetPosition(0, start);
            // beamLine.SetPosition(1, (start + hand.beamJointBody.transform.position) / 2f);

            // points[0] = start;
            // points[1] = (start + hand.beamJointBody.transform.position) / 2f;

            beamLine.SetPosition(0, start);
            beamLine.SetPosition(1, (start + p2) / 2f);
        }
        else
        {
            beamLine.SetPosition(0, p1);

            Vector3 ropeBottom = (p1 + p2) / 2f;
            float extraRope = ropeLength - (fingerDistance + fingerToConnectedPosition);
            // extraRope = Mathf.Clamp(extraRope - difference, 0, Mathf.Infinity);
            float a = fingerDistance / 2f;
            float c = (fingerDistance + extraRope) / 2f;
            Vector3 down = Quaternion.LookRotation(p2 - p1) * Vector3.down;
            float downDistance = Mathf.Sqrt(Mathf.Abs((c * c) - (a * a)));
            if (!float.IsNaN(downDistance))
                ropeBottom += down * downDistance;

            beamLine.SetPosition(1, ropeBottom);
        }

        // beamLine.enabled = true;
        // beamLine.SetPosition(2, hand.beamJointBody.transform.position);
        // beamLine.SetPosition(3, attachPoint);

        // points[2] = hand.beamJointBody.transform.position;
        // points[3] = attachPoint;

        beamLine.SetPosition(2, p2);
        beamLine.SetPosition(3, connectedPosition);
    }

    //play
    public void PlayVideo(string videoId)
    {
        BrowserManager.instance.CueVideo(videoId);
        CmdPlayVideo(videoId);
    }

    [Command]
    private void CmdPlayVideo(string videoId)
    {
        NetworkManagerGame.singleton.videoState = NetworkManagerGame.VideoState.Cued;
        RpcPlayVideo(videoId);
    }

    [ClientRpc(excludeOwner = true)]
    private void RpcPlayVideo(string videoId)
    {
        BrowserManager.instance.CueVideo(videoId);
    }

    //pause
    public void PauseVideo()
    {
        BrowserManager.instance.PauseVideo();
        CmdPauseVideo();
    }

    [Command]
    private void CmdPauseVideo()
    {
        RpcPauseVideo();
    }

    [ClientRpc(excludeOwner = true)]
    private void RpcPauseVideo()
    {
        BrowserManager.instance.PauseVideo();
    }

    //resume
    public void ResumeVideo()
    {
        BrowserManager.instance.ResumeVideo();
        CmdResumeVideo();
    }

    [Command]
    private void CmdResumeVideo()
    {
        RpcResumeVideo();
    }

    [ClientRpc(excludeOwner = true)]
    private void RpcResumeVideo()
    {
        BrowserManager.instance.ResumeVideo();
    }

    //stop
    public void StopVideo()
    {
        BrowserManager.instance.StopVideo();
        CmdStopVideo();
    }

    [Command]
    private void CmdStopVideo()
    {
        NetworkManagerGame.singleton.videoState = NetworkManagerGame.VideoState.None;
        RpcStopVideo();
    }

    [ClientRpc(excludeOwner = true)]
    private void RpcStopVideo()
    {
        BrowserManager.instance.StopVideo();
    }

    [Command]
    public void CmdUpdateYoutubePlayerState(int state)
    {
        NetworkManagerGame.singleton.playerInfos[connectionToClient].youtubePlayerState = state;
    }

    [Command]
    public void CmdUpdateYoutubePlayerCurrentTime(float currentTime)
    {
        NetworkManagerGame.singleton.playerInfos[connectionToClient].youtubePlayerCurrentTime = currentTime;
    }

    [Command]
    private void CmdSetRopeActive(bool ropeActive)
    {
        this.ropeActive = ropeActive;
    }

    [Command]
    private void CmdSetBeamPoints(Vector3 attachPoint, NetworkIdentity attachObject, float ropeLength, int ropeHandIndex)
    {
        beamAttachPoint = attachPoint;
        attachedObjectNetId = attachObject;
        this.ropeLength = ropeLength;
        this.ropeHandIndex = ropeHandIndex;
        ropeActive = true;
    }

    [Command]
    private void CmdSpawnHands()
    {
        GameObject leftHand = Instantiate(networkHandLeftPrefab);
        NetworkServer.Spawn(leftHand, connectionToClient);
        leftHandId = leftHand.GetComponent<NetworkIdentity>();

        GameObject rightHand = Instantiate(networkHandRightPrefab);
        NetworkServer.Spawn(rightHand, connectionToClient);
        rightHandId = rightHand.GetComponent<NetworkIdentity>();
    }

    [Command]
    public void CmdSpawnBlock(Vector3 position, Quaternion rotation, int blockid)
    {
        GameObject block = Instantiate(blockAreaPrefab, position, rotation);
        block.GetComponent<BlockArea>().networkBlocks.Add(Vector3Int.zero, new NetworkBlockInfo() { id = (byte)blockid, rotation = BlockArea.IdentityByte() });
        NetworkServer.Spawn(block, connectionToClient);
    }

    [Command(ignoreAuthority = true)]
    public void CmdReceiveOwnershipOfObject(NetworkIdentity objectNetId)
    {
        if (objectNetId == null)
            return;

        if (objectNetId.connectionToClient == connectionToClient)
        {
            return;
        }
        else
        {
            if (objectNetId.connectionToClient != null)
            {
                objectNetId.RemoveClientAuthority();
            }
            objectNetId.AssignClientAuthority(connectionToClient);
        }
    }
}