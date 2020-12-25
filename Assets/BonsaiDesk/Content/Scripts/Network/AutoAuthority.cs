using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AutoAuthority : NetworkBehaviour
{
    public Material hasAuthorityMaterial;
    public Material noAuthorityMaterial;
    public MeshRenderer renderer;

    private Rigidbody body;

    private void Start()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
    }
    
    private void Update()
    {
        if (hasAuthority)
            renderer.sharedMaterial = hasAuthorityMaterial;
        else
            renderer.sharedMaterial = noAuthorityMaterial;

        //if you don't have control over the object
        if (!(hasAuthority || (isServer && netIdentity.connectionToClient == null)))
        {
            body.isKinematic = true;
            return;
        }
        
        //code past this point if you have control over the object
        body.isKinematic = false;
    }

    [Command(ignoreAuthority = true)]
    public void CmdSetNewOwner(NetworkIdentity ownerIdentity)
    {
        //if owner already has authority, return
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
    }
}