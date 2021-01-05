using Mirror;
using UnityEngine;

public class NetworkVRPlayer : NetworkBehaviour
{
    public GameObject networkHandLeftPrefab;
    public GameObject networkHandRightPrefab;

    [SyncVar] private NetworkIdentity _leftHandId;
    [SyncVar] private NetworkIdentity _rightHandId;

    private void Start()
    {
        if (!isLocalPlayer)
            return;

        CmdSpawnHands();
    }
    
    // private void SetBeamPoints(Vector3 p1, Vector3 p2, Vector3 connectedAnchor, Transform connectedTo, float ropeLength)
    // {
    //     Vector3 connectedPosition = connectedTo.TransformPoint(connectedAnchor);
    //
    //     float fingerDistance = Vector3.Distance(p1, p2);
    //     float fingerToConnectedPosition = Vector3.Distance(p2, connectedPosition);
    //     if (fingerDistance > ropeLength - 0.001f)
    //     {
    //         Vector3 direction = Quaternion.LookRotation(p1 - p2) * Vector3.forward;
    //         Vector3 start = p2 + (direction * ropeLength);
    //
    //         beamLine.SetPosition(0, start);
    //         beamLine.SetPosition(1, (start + p2) / 2f);
    //     }
    //     else
    //     {
    //         beamLine.SetPosition(0, p1);
    //
    //         Vector3 ropeBottom = (p1 + p2) / 2f;
    //         float extraRope = ropeLength - (fingerDistance + fingerToConnectedPosition);
    //         float a = fingerDistance / 2f;
    //         float c = (fingerDistance + extraRope) / 2f;
    //         Vector3 down = Quaternion.LookRotation(p2 - p1) * Vector3.down;
    //         float downDistance = Mathf.Sqrt(Mathf.Abs((c * c) - (a * a)));
    //         if (!float.IsNaN(downDistance))
    //             ropeBottom += down * downDistance;
    //
    //         beamLine.SetPosition(1, ropeBottom);
    //     }
    //
    //     beamLine.SetPosition(2, p2);
    //     beamLine.SetPosition(3, connectedPosition);
    // }

    [Command]
    private void CmdSpawnHands()
    {
        GameObject leftHand = Instantiate(networkHandLeftPrefab);
        leftHand.GetComponent<NetworkHand>().ownerIdentity = connectionToClient.identity;
        NetworkServer.Spawn(leftHand, connectionToClient);
        _leftHandId = leftHand.GetComponent<NetworkIdentity>();

        GameObject rightHand = Instantiate(networkHandRightPrefab);
        rightHand.GetComponent<NetworkHand>().ownerIdentity = connectionToClient.identity;
        NetworkServer.Spawn(rightHand, connectionToClient);
        _rightHandId = rightHand.GetComponent<NetworkIdentity>();
    }

    public NetworkHand GetOtherHand(OVRSkeleton.SkeletonType skeletonType)
    {
        if (skeletonType == OVRSkeleton.SkeletonType.HandLeft)
            return _rightHandId.GetComponent<NetworkHand>();
        if (skeletonType == OVRSkeleton.SkeletonType.HandRight)
            return _leftHandId.GetComponent<NetworkHand>();
        return null;
    }
}