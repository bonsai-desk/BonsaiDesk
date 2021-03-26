using Mirror;
using UnityEngine;

public class NetworkVRPlayer : NetworkBehaviour
{
    public GameObject networkHandLeftPrefab;
    public GameObject networkHandRightPrefab;

    [SyncVar] public NetworkIdentity _leftHandId;
    [SyncVar] public NetworkIdentity _rightHandId;

    [SyncVar(hook = nameof(SpotChange))] private int spotId;

    public override void OnStartClient()
    {
        if (!isLocalPlayer)
            return;

        var spotInfo = GetSpot();
        GameObject.Find("GameManager").GetComponent<MoveToDesk>().SetTableEdge(spotInfo.tableEdge);
        InputManager.Hands.Left.SetHandTexture(spotInfo.handTexture);
        InputManager.Hands.Right.SetHandTexture(spotInfo.handTexture);
    }

    [Server]
    public void SetSpot(int spot)
    {
        //set spot to spot + 1 so the hook updates even if you have spot 0 which is the default value so it wouldn't call the hook
        spotId = spot + 1;
        SetTextures(spotId);
    }

    public SpotManager.SpotInfo GetSpot()
    {
        //spot - 1 for same reason in SetSpot
        return SpotManager.Instance.spotInfo[spotId - 1];
    }

    private void SpotChange(int oldValue, int newValue)
    {
        SetTextures(newValue);
    }

    private void SetTextures(int spot)
    {
        //spot - 1 for same reason in SetSpot
        var spotInfo = SpotManager.Instance.spotInfo[spot - 1];
        GetComponentInChildren<MeshRenderer>().material.mainTexture = spotInfo.headTexture;
        _leftHandId.GetComponent<NetworkHand>().ChangeHandTexture(spotInfo.handTexture);
        _rightHandId.GetComponent<NetworkHand>().ChangeHandTexture(spotInfo.handTexture);
    }

    [Server]
    public void SetHandIdentities(NetworkIdentity lid, NetworkIdentity rid)
    {
        _leftHandId = lid;
        _rightHandId = rid;
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