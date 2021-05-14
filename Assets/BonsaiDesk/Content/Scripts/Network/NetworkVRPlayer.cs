using Mirror;
using UnityEngine;

public class NetworkVRPlayer : NetworkBehaviour
{
    public GameObject networkHandLeftPrefab;
    public GameObject networkHandRightPrefab;

    [SyncVar] public NetworkIdentity _leftHandId;
    [SyncVar] public NetworkIdentity _rightHandId;

    [SyncVar(hook = nameof(SpotChange))] public int spotId;

    public override void OnStartClient()
    {
        if (!isLocalPlayer)
            return;
            
        SpotManager.Instance.LayoutChange -= HandleLayoutChange;
        SpotManager.Instance.LayoutChange += HandleLayoutChange;

        var tableEdge = SpotManager.Instance.GetSpotTransform(spotId - 1);
        GameObject.Find("GameManager").GetComponent<MoveToDesk>().SetTableEdge(tableEdge);
        
        var textures = SpotManager.Instance.GetColorInfo(spotId - 1);
        InputManager.Hands.Left.SetHandTexture(textures.handTexture);
        InputManager.Hands.Right.SetHandTexture(textures.handTexture);
    }
    
    private void HandleLayoutChange(object sender, SpotManager.Layout newLayout)
    {
        if (!isLocalPlayer)
            return;
        
        var tableEdge = SpotManager.Instance.GetSpotTransform(spotId - 1, newLayout);
        GameObject.Find("GameManager").GetComponent<MoveToDesk>().SetTableEdge(tableEdge);
    }

    [Server]
    public void SetSpot(int spot)
    {
        //set spot to spot + 1 so the hook updates even if you have spot 0 which is the default value so it wouldn't call the hook
        spotId = spot + 1;
        SetTextures(spotId);
    }

    private void SpotChange(int oldValue, int newValue)
    {
        SetTextures(newValue);
    }

    private void SetTextures(int spot)
    {
        //spot - 1 for same reason in SetSpot
        var textures = SpotManager.Instance.GetColorInfo(spot - 1);
        GetComponentInChildren<MeshRenderer>().material.mainTexture = textures.headTexture;
        _leftHandId.GetComponent<NetworkHand>().ChangeHandTexture(textures.handTexture);
        _rightHandId.GetComponent<NetworkHand>().ChangeHandTexture(textures.handTexture);
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