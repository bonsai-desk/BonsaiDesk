using System;
using System.Collections;
using Mirror;
using Smooth;
using UnityEngine;

public class NetworkVRPlayer : NetworkBehaviour
{
    public static NetworkVRPlayer localPlayer;

    public GameObject headObject;

    [SyncVar] public NetworkIdentityReference leftHandId = new NetworkIdentityReference();
    [SyncVar] public NetworkIdentityReference rightHandId = new NetworkIdentityReference();

    [SyncVar(hook = nameof(SpotChange))] public int spotId;

    private MoveToDesk _moveToDesk;

    private Coroutine _tryingToSetTexturesCoroutine;

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

        //move head (hands are teleported as part of SetTableEdge)
        GetComponent<NetworkFollow>().MoveToTarget();
        GetComponent<SmoothSyncMirror>().teleportOwnedObjectFromOwner();
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
        if (_tryingToSetTexturesCoroutine != null)
        {
            StopCoroutine(_tryingToSetTexturesCoroutine);
        }

        _tryingToSetTexturesCoroutine = StartCoroutine(KeepTryingToSetTextures(textures));
    }

    private IEnumerator KeepTryingToSetTextures(SpotManager.ColorInfo textures)
    {
        int attempts = 250;
        while (!leftHandId.Value || !rightHandId.Value)
        {
            attempts--;
            if (attempts < 0)
            {
                Debug.LogError("KeepTryingToSetTextures: out of attempts");
                yield break;
            }

            yield return null;
        }

        leftHandId.Value.GetComponent<NetworkHand>().ChangeHandTexture(textures.handTexture);
        rightHandId.Value.GetComponent<NetworkHand>().ChangeHandTexture(textures.handTexture);

        _tryingToSetTexturesCoroutine = null;
    }

    [Server]
    public void SetHandIdentities(NetworkIdentityReference lid, NetworkIdentityReference rid)
    {
        leftHandId = lid;
        rightHandId = rid;
    }

    public NetworkHand GetOtherHand(OVRSkeleton.SkeletonType skeletonType)
    {
        if (skeletonType == OVRSkeleton.SkeletonType.HandLeft && rightHandId.Value)
        {
            return rightHandId.Value.GetComponent<NetworkHand>();
        }

        if (skeletonType == OVRSkeleton.SkeletonType.HandRight && leftHandId.Value)
        {
            return leftHandId.Value.GetComponent<NetworkHand>();
        }

        return null;
    }
}