using Mirror;
using UnityEngine;

public class NetworkVRPlayer : NetworkBehaviour
{
    public GameObject networkHandLeftPrefab;
    public GameObject networkHandRightPrefab;

    [SyncVar] private NetworkIdentity _leftHandId;
    [SyncVar] private NetworkIdentity _rightHandId;

    [SyncVar] public int spotId;

    public override void OnStartClient()
    {
        if (!isLocalPlayer)
            return;
        
        CmdSpawnHands();

        var spotInfo = SpotManager.Instance.spotInfo[spotId];
        GameObject.Find("GameManager").GetComponent<MoveToDesk>().SetTableEdge(spotInfo.tableEdge);
    }

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