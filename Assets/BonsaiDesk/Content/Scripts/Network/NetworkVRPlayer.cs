using Mirror;
using UnityEngine;

public class NetworkVRPlayer : NetworkBehaviour
{
    public GameObject networkHandLeftPrefab;
    public GameObject networkHandRightPrefab;

    [SyncVar] private NetworkIdentity _leftHandId;
    [SyncVar] private NetworkIdentity _rightHandId;

    [SyncVar(hook = nameof(SpotChange))] public int spotId;

    public override void OnStartClient()
    {
        if (!isLocalPlayer)
            return;

        var spotInfo = GetSpot();
        Debug.LogError(spotId);
        Debug.LogError(spotInfo.tableEdge.name);
        GameObject.Find("GameManager").GetComponent<MoveToDesk>().SetTableEdge(spotInfo.tableEdge);
        InputManager.Hands.Left.SetHandTexture(spotInfo.handTexture);
        InputManager.Hands.Right.SetHandTexture(spotInfo.handTexture);
        
        CmdSpawnHands();
    }

    [Server]
    public void SetSpot(int spot)
    {
        //set spot to spot + 1 so the hook updates even if you have spot 0 which is the default value so it wouldn't call the hook
        spotId = spot + 1;
    }

    public SpotManager.SpotInfo GetSpot()
    {
        //spot - 1 for same reason in SetSpot
        return SpotManager.Instance.spotInfo[spotId - 1];
    }

    private void SpotChange(int oldValue, int newValue)
    {
        // Debug.LogError(Time.time + " " + newValue);
        // Debug.LogError(SpotManager.Instance.spotInfo.Length);
        // var spot = SpotManager.Instance.spotInfo[newValue - 1];
        // //GetComponentInChildren<MeshRenderer>().material.mainTexture = spot.headTexture;
        // Debug.LogError("done");
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