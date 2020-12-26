using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AutoAuthority : NetworkBehaviour
{
    [SyncVar] private double _lastInteractTime;
    [SyncVar] private uint _ownerIdentityId = uint.MaxValue;

    public bool debug;

    public Material hasAuthorityMaterial;
    public Material noAuthorityMaterial;
    public MeshRenderer meshRenderer;

    private Rigidbody _body;
    private int _lastInteractFrame;
    private int _lastSetNewOwnerFrame;

    private void Start()
    {
        if (isServer)
        {
            _lastInteractTime = NetworkTime.time;
        }
        
        _body = GetComponent<Rigidbody>();
        _body.isKinematic = true;

        UpdateMaterial();
    }

    private void Update()
    {
        if (debug)
        {
            debug = false;
            print(NetworkTime.time + " " + _lastInteractTime + " " + _ownerIdentityId);
        }

        UpdateMaterial();

        //if you don't have control over the object
        if (!HasAuthority())
        {
            _body.isKinematic = true;
            return;
        }

        //code past this point if you have control over the object
        _body.isKinematic = false;
    }

    //Hello function. I am a client. Do I have authority over this object?
    private bool ClientHasAuthority()
    {
        if (!isClient)
            Debug.LogError("ClientHasAuthority is only valid when called from a client.");
        return NetworkClient.connection != null && NetworkClient.connection.identity != null &&
               _ownerIdentityId == NetworkClient.connection.identity.netId;
    }

    //Hello function. I am a server. Do I have authority over this object?
    private bool ServerHasAuthority()
    {
        if (!isServer)
            Debug.LogError("ServerHasAuthority is only valid when called from a server.");
        return _ownerIdentityId == uint.MaxValue;
    }

    //Hello function. I don't know if I'm a client or a server, but whatever I am, do I have authority over this object?
    private bool HasAuthority()
    {
        return isServer && ServerHasAuthority() || isClient && ClientHasAuthority();
    }

    private void UpdateMaterial()
    {
        meshRenderer.sharedMaterial =
            isServer && !isClient && ServerHasAuthority() || isClient && ClientHasAuthority()
                ? hasAuthorityMaterial
                : noAuthorityMaterial;
    }

    public void Interact(uint identityId)
    {
        if (!isClient)
            return;
        
        if (_lastInteractFrame != Time.frameCount)
        {
            _lastInteractFrame = Time.frameCount;
            if (!ClientHasAuthority())
            {
                SetNewOwner(identityId, NetworkTime.time);
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

    private void SetNewOwner(uint newOwnerIdentityId, double fromLastInteractTime)
    {
        if (_lastSetNewOwnerFrame != Time.frameCount)
        {
            _lastSetNewOwnerFrame = Time.frameCount;
            CmdSetNewOwner(newOwnerIdentityId, fromLastInteractTime);
        }
    }

    [Command(ignoreAuthority = true)]
    private void CmdSetNewOwner(uint newOwnerIdentityId, double fromLastInteractTime)
    {
        //if passed owner is null, remove authority then return
        if (newOwnerIdentityId == uint.MaxValue)
        {
            netIdentity.RemoveClientAuthority();
            _ownerIdentityId = uint.MaxValue;
            // _lastInteractTime = NetworkTime.time;
            _lastInteractTime = fromLastInteractTime;
            return;
        }

        //if owner already has authority return
        if (_ownerIdentityId == newOwnerIdentityId)
        {
            return;
        }

        //remove objects owner if it had one
        if (netIdentity.connectionToClient != null)
        {
            netIdentity.RemoveClientAuthority();
        }

        //give the new owner authority
        netIdentity.AssignClientAuthority(NetworkIdentity.spawned[newOwnerIdentityId].connectionToClient);
        // _lastInteractTime = NetworkTime.time;
        _lastInteractTime = fromLastInteractTime;
        _ownerIdentityId = newOwnerIdentityId;
    }

    private void HandleRecursiveAuthority(Collision collision)
    {
        if (!HasAuthority())
            return;

        var autoAuthority = collision.gameObject.GetComponent<AutoAuthority>();
        if (autoAuthority == null)
            return;

        if (_lastInteractTime < autoAuthority._lastInteractTime)
            return;
        
        // print("---");
        // print(_lastInteractTime + " " + autoAuthority._lastInteractTime + " " + _ownerIdentityId + " " + autoAuthority._ownerIdentityId);
        // print(isClient && !autoAuthority.ClientHasAuthority());

        if (isClient && ClientHasAuthority() && !autoAuthority.ClientHasAuthority())
        {
            autoAuthority.SetNewOwner(NetworkClient.connection.identity.netId, _lastInteractTime);
        }
        else if (isServer && ServerHasAuthority() && !autoAuthority.ServerHasAuthority())
        {
            autoAuthority.SetNewOwner(uint.MaxValue, _lastInteractTime);
        }

        // if (!autoAuthority.HasAuthority())
        // {
        //     print("yeeeeee");
        //     if (isClient && ClientHasAuthority())
        //         autoAuthority.SetNewOwner(NetworkClient.connection.identity, _lastInteractTime);
        //     else if (isServer && ServerHasAuthority())
        //         autoAuthority.SetNewOwner(null, _lastInteractTime);
        // }
        // else
        // {
        //     print("nooooo");
        // }
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