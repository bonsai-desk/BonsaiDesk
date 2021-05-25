using System;
using System.Collections;
using Mirror;
using Smooth;
using UnityEngine;

public class NetworkVRPlayer : NetworkBehaviour
{
    public static NetworkVRPlayer localPlayer;
    
    public GameObject headObject;

    [SyncVar] public NetworkIdentity _leftHandId;
    [SyncVar] public NetworkIdentity _rightHandId;

    [SyncVar(hook = nameof(SpotChange))] public int spotId;

    private MoveToDesk _moveToDesk;

    public override void OnStartClient()
    {
        headObject.SetActive(false);
        StartCoroutine(WaitThenActivate());

        if (!isLocalPlayer)
        {
            return;
        }

        localPlayer = this;

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

    public override void OnStopClient()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        localPlayer = null;
    }

    private void OnDestroy()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        localPlayer = null;
    }

    public void LayoutChange(SpotManager.Layout newLayout)
    {
        if (!isLocalPlayer)
        {
            return;
        }

        if (!_moveToDesk)
        {
            _moveToDesk = GameObject.Find("GameManager").GetComponent<MoveToDesk>();
        }

        var tableEdge = SpotManager.Instance.GetSpotTransform(spotId - 1, newLayout);
        _moveToDesk.SetTableEdge(tableEdge);

        Debug.LogError($"go: {gameObject}");
        Debug.LogError($"this: {this}");

        if (gameObject)
        {
            var ssm = gameObject.GetComponent<SmoothSyncMirror>();
            if (ssm)
            {
                ssm.teleportOwnedObjectFromOwner();
                InputManager.Hands.Left.NetworkHand.GetComponent<SmoothSyncMirror>().teleportOwnedObjectFromOwner();
                InputManager.Hands.Right.NetworkHand.GetComponent<SmoothSyncMirror>().teleportOwnedObjectFromOwner();
            }
            else
            {
                Debug.LogError("oof");
            }
        }
    }

    private IEnumerator WaitThenActivate()
    {
        yield return new WaitForSeconds(1f);
        headObject.SetActive(true);
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
        {
            return _rightHandId.GetComponent<NetworkHand>();
        }

        if (skeletonType == OVRSkeleton.SkeletonType.HandRight)
        {
            return _leftHandId.GetComponent<NetworkHand>();
        }

        return null;
    }
}