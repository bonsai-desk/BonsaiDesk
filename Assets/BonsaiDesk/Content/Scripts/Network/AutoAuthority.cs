using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Smooth;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AutoAuthority : NetworkBehaviour
{
    [SyncVar] private double _lastInteractTime;
    [SyncVar] private uint _ownerIdentityId = uint.MaxValue;
    [SyncVar] private bool _inUse = false;
    [SyncVar] public bool isKinematic = false;

    public bool InUse => _inUse;

    public bool allowPinchPull = true;

    public MeshRenderer meshRenderer;

    private Rigidbody _body;
    private int _lastInteractFrame;
    private int _lastSetNewOwnerFrame;

    private bool _visualizePinchPull = false;
    private Material _cachedMaterial;
    private int _colorPropertyId;
    private int _colorId = 0;

    private Color[] _colors = new[]
    {
        Color.white,
        Color.yellow,
        Color.green
    };

    public override void OnStartServer()
    {
        base.OnStartServer();
        _lastInteractTime = NetworkTime.time;
        if (connectionToClient != null)
            _ownerIdentityId = connectionToClient.identity.netId;
    }

    private void Start()
    {
        _body = GetComponent<Rigidbody>();
        _body.isKinematic = true;

        _colorPropertyId = Shader.PropertyToID("_Color");

        UpdateColor();
    }

    private void Update()
    {
        UpdateColor();
        _visualizePinchPull = false;

        if (isServer && transform.position.y < -100f)
        {
            ServerForceNewOwner(uint.MaxValue, NetworkTime.time, true);
            NetworkServer.Destroy(gameObject);
            return;
        }

        //if you don't have control over the object
        if (!HasAuthority())
        {
            _body.isKinematic = true;
            return;
        }

        //code past this point if you have control over the object
        _body.isKinematic = isKinematic;
    }

    //Hello function. I am a client. Do I have authority over this object?
    public bool ClientHasAuthority()
    {
        if (!isClient)
            Debug.LogError("ClientHasAuthority is only valid when called from a client.");
        return NetworkClient.connection != null && NetworkClient.connection.identity != null &&
               _ownerIdentityId == NetworkClient.connection.identity.netId;
    }

    //Hello function. I am a server. Do I have authority over this object?
    public bool ServerHasAuthority()
    {
        if (!isServer)
            Debug.LogError("ServerHasAuthority is only valid when called from a server.");
        return _ownerIdentityId == uint.MaxValue;
    }

    //Hello function. I don't know if I'm a client or a server, but whatever I am, do I have authority over this object?
    public bool HasAuthority()
    {
        return isServer && ServerHasAuthority() || isClient && ClientHasAuthority();
    }

    private void UpdateColor()
    {
        if (meshRenderer == null)
            return;

        bool shouldVisualizeAuthority = NetworkManagerGame.Singleton.visualizeAuthority &&
                                        (isServer && !isClient && ServerHasAuthority() ||
                                         isClient && ClientHasAuthority());

        int newColorId = 0;
        if (_visualizePinchPull)
            newColorId = 1;
        else if (shouldVisualizeAuthority)
            newColorId = 2;

        if (_cachedMaterial == null)
            _cachedMaterial = meshRenderer.material;

        if (newColorId != _colorId)
        {
            _colorId = newColorId;
            _cachedMaterial.SetColor(_colorPropertyId, _colors[_colorId]);
        }
    }

    [Command(ignoreAuthority = true)]
    public void CmdSetKinematic(bool newIsKinematic)
    {
        isKinematic = newIsKinematic;
    }

    public void VisualizePinchPull()
    {
        _visualizePinchPull = true;
        UpdateColor();
    }

    [Server]
    public void SetInUse(bool inUse)
    {
        _inUse = inUse;
    }

    [Command(ignoreAuthority = true)]
    public void CmdRemoveInUse(uint identityId)
    {
        if (_ownerIdentityId == identityId)
            _inUse = false;
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
        //cannot switch owner if it is in use (held/pinch pulled/ect)
        if (_inUse)
            return;

        if (_lastSetNewOwnerFrame != Time.frameCount)
        {
            _lastSetNewOwnerFrame = Time.frameCount;
            CmdSetNewOwner(newOwnerIdentityId, fromLastInteractTime, false);
        }
    }

    [Command(ignoreAuthority = true)]
    public void CmdSetNewOwner(uint newOwnerIdentityId, double fromLastInteractTime, bool inUse)
    {
        //cannot switch owner if it is in use (held/pinch pulled/ect)
        if (_inUse)
            return;

        if (inUse)
            _inUse = true;

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

        //if the new owner is not the server, give them authority
        if (newOwnerIdentityId != uint.MaxValue)
        {
            netIdentity.AssignClientAuthority(NetworkIdentity.spawned[newOwnerIdentityId].connectionToClient);
        }

        _lastInteractTime = fromLastInteractTime;
        _ownerIdentityId = newOwnerIdentityId;
    }

    [Server]
    public void ServerForceNewOwner(uint newOwnerIdentityId, double fromLastInteractTime, bool inUse)
    {
        //remove objects owner if it had one
        if (netIdentity.connectionToClient != null)
        {
            netIdentity.RemoveClientAuthority();
        }

        //if the new owner is not the server, give them authority
        if (newOwnerIdentityId != uint.MaxValue)
        {
            netIdentity.AssignClientAuthority(NetworkIdentity.spawned[newOwnerIdentityId].connectionToClient);
        }

        _lastInteractTime = fromLastInteractTime;
        _ownerIdentityId = newOwnerIdentityId;
        _inUse = inUse;
    }

    private void HandleRecursiveAuthority(Collision collision)
    {
        if (!HasAuthority())
            return;

        var autoAuthority = collision.gameObject.GetComponent<AutoAuthority>();
        if (autoAuthority == null)
            return;

        //TODO handle this unlikely case
        // if (_lastInteractTime == autoAuthority._lastInteractTime)

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