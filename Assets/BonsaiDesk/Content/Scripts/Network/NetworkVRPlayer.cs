using System;
using System.Collections;
using Mirror;
using Smooth;
using UnityEngine;

public class NetworkVRPlayer : NetworkBehaviour
{
    public GameObject headObject;

    [SyncVar] public NetworkIdentity _leftHandId;
    [SyncVar] public NetworkIdentity _rightHandId;

    [SyncVar(hook = nameof(SpotChange))] public int spotId;

    private MoveToDesk _moveToDesk;

    private void Awake()
    {
        Debug.LogError(Time.time + " awakele layotu");

    }
    public override void OnStartClient()
    {
        headObject.SetActive(false);
        StartCoroutine(WaitThenActivate());

        if (!isLocalPlayer)
            return;

        Debug.LogError(Time.time + " start al ");
        SpotManager.Instance.LayoutChange -= HandleLayoutChange;
        SpotManager.Instance.LayoutChange += HandleLayoutChange;
        
        if (!_moveToDesk)
        {
            _moveToDesk = GameObject.Find("GameManager").GetComponent<MoveToDesk>();
        }

        var tableEdge = SpotManager.Instance.GetSpotTransform(spotId - 1);
        _moveToDesk.SetTableEdge(tableEdge);

        var textures = SpotManager.Instance.GetColorInfo(spotId - 1);
        InputManager.Hands.Left.SetHandTexture(textures.handTexture);
        InputManager.Hands.Right.SetHandTexture(textures.handTexture);
    }
    
    private IEnumerator WaitThenActivate()
    {
        yield return new WaitForSeconds(1f);
        headObject.SetActive(true);
    }

    private void HandleLayoutChange(object sender, SpotManager.Layout newLayout)
    {
        if (!isLocalPlayer)
            return;

        if (!_moveToDesk)
        {
            _moveToDesk = GameObject.Find("GameManager").GetComponent<MoveToDesk>();
        }

        var tableEdge = SpotManager.Instance.GetSpotTransform(spotId - 1, newLayout);
        _moveToDesk.SetTableEdge(tableEdge);

        Debug.LogError($"{this} {gameObject}");
        
        if (gameObject)
        {
        var ssm = GetComponent<SmoothSyncMirror>();
        Debug.LogError(Time.time + " handle layotu");
        if (ssm)
        {
            ssm.teleportOwnedObjectFromOwner();
        }
        else
        {
            Debug.LogError("oof");
        }
        }

        InputManager.Hands.Left.NetworkHand.GetComponent<SmoothSyncMirror>().teleportOwnedObjectFromOwner();
        InputManager.Hands.Right.NetworkHand.GetComponent<SmoothSyncMirror>().teleportOwnedObjectFromOwner();
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