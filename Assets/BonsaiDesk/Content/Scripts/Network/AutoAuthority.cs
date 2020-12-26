using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AutoAuthority : NetworkBehaviour
{
    [SyncVar] public double _lastInteractTime = 0f;
    public double lastInteractTime => _lastInteractTime;

    public Material hasAuthorityMaterial;
    public Material noAuthorityMaterial;
    public MeshRenderer renderer;

    private Rigidbody body;
    private int lastInteractFrame;
    private int lastSetNewOwnerFrame;

    private void Start()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        
        UpdateMaterial();
    }

    private void Update()
    {
        UpdateMaterial();

        //if you don't have control over the object
        if (!hasAuthority)
        {
            body.isKinematic = true;
            return;
        }

        //code past this point if you have control over the object
        body.isKinematic = false;
    }

    private void UpdateMaterial()
    {
        renderer.sharedMaterial = hasAuthority ? hasAuthorityMaterial : noAuthorityMaterial;
    }

    public void Interact(NetworkIdentity ownerIdentity)
    {
        if (lastInteractFrame != Time.frameCount)
        {
            lastInteractFrame = Time.frameCount;
            if (!hasAuthority)
            {
                SetNewOwner(ownerIdentity, NetworkTime.time);
            }
            else
            {
                CmdUpdateInteractTime();
            }
        }
    }

    [Command(ignoreAuthority = true)]
    private void CmdUpdateInteractTime()
    {
        _lastInteractTime = NetworkTime.time;
    }

    private void SetNewOwner(NetworkIdentity ownerIdentity, double fromLastInteractTime)
    {
        if (lastSetNewOwnerFrame != Time.frameCount)
        {
            lastSetNewOwnerFrame = Time.frameCount;
            CmdSetNewOwner(ownerIdentity, fromLastInteractTime);
        }
    }

    [Command(ignoreAuthority = true)]
    private void CmdSetNewOwner(NetworkIdentity ownerIdentity, double fromLastInteractTime)
    {
        //if passed owner is null, remove authority then return
        if (ownerIdentity == null)
        {
            netIdentity.RemoveClientAuthority();
            // _lastInteractTime = NetworkTime.time;
            _lastInteractTime = fromLastInteractTime;
            return;
        }

        //if owner already has authority return
        if (netIdentity.connectionToClient == ownerIdentity.connectionToClient)
        {
            return;
        }

        //remove objects owner if it had one
        if (netIdentity.connectionToClient != null)
        {
            netIdentity.RemoveClientAuthority();
        }

        //give the new owner authority
        netIdentity.AssignClientAuthority(ownerIdentity.connectionToClient);
        // _lastInteractTime = NetworkTime.time;
        _lastInteractTime = fromLastInteractTime;
    }

    private void HandleRecursiveAuthority(Collision collision)
    {
        if (!hasAuthority)
            return;

        var autoAuthority = collision.gameObject.GetComponent<AutoAuthority>();
        if (autoAuthority == null)
            return;

        if (lastInteractTime < autoAuthority.lastInteractTime)
            return;

        if (!autoAuthority.hasAuthority)
        {
            autoAuthority.SetNewOwner(NetworkClient.connection.identity, lastInteractTime);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleRecursiveAuthority(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        HandleRecursiveAuthority(collision);
    }
}