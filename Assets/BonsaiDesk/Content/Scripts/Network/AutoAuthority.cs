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

    [SyncVar(hook = nameof(OnOwnerChange))]
    private uint _ownerIdentityId;

    private float _serverLastOwnerChange;
    private const float OwnerChangeCooldown = 0.25f;

    [SyncVar] private bool _inUse;
    [SyncVar] public bool isKinematic;

    public bool InUse => IsInUse();

    public bool allowPinchPull = true;

    public MeshRenderer meshRenderer;

    private BlockObject _blockObject; //reference to own blockObject if it exists 

    private Rigidbody _body;
    private int _lastInteractFrame;
    private int _lastSetNewOwnerFrame;

    private int _visualizePinchPullFrame = -1;
    public Material cachedMaterial;
    private int _colorPropertyId;
    private int _colorId = 0;

    private int _setKinematicLocalFrame = -1;

    private float _keepAwakeTime = -10f;

    private Color[] _colors = new[]
    {
        Color.white,
        Color.red,
        Color.green
    };

    public override void OnStartServer()
    {
        ServerSetLastOwnerChange(0);

        if (_ownerIdentityId == 0)
        {
            _ownerIdentityId = uint.MaxValue;
        }

        ServerSetLastInteractTime(NetworkTime.time);
        if (connectionToClient != null)
            _ownerIdentityId = connectionToClient.identity.netId;
    }

    private void Awake()
    {
        _blockObject = GetComponent<BlockObject>();
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

        if (isServer)
        {
            if (!_blockObject && (PhysicsHandController.InvalidTransform(transform) || Vector3.SqrMagnitude(transform.position) > 20f * 20f ||
                                  transform.position.y < -1f || transform.position.y > 5f))
            {
                ServerStripOwnerAndDestroy();
                return;
            }
        }

        //if you don't have control over the object
        if (!HasAuthority())
        {
            _body.isKinematic = true;
            return;
        }

        //code past this point if you have control over the object
        if (Time.frameCount != _setKinematicLocalFrame)
        {
            _body.isKinematic = isKinematic;
        }

        if (Time.time - _keepAwakeTime < 1f)
        {
            _body.WakeUp();
        }
    }

    public void SetKinematicLocalForOneFrame()
    {
        _body.isKinematic = true;
        _setKinematicLocalFrame = Time.frameCount;
    }

    private void OnOwnerChange(uint oldValue, uint newValue)
    {
        GetComponent<SmoothSyncMirror>().clearBuffer();
    }

    //Hello function. I am a client. Do I have authority over this object?
    [Client]
    private bool ClientHasAuthority()
    {
        if (!isClient)
            Debug.LogError("ClientHasAuthority is only valid when called from a client.");
        return NetworkClient.connection != null && NetworkClient.connection.identity != null && _ownerIdentityId == NetworkClient.connection.identity.netId;
    }

    //Hello function. I am a server. Do I have authority over this object?
    [Server]
    private bool ServerHasAuthority()
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

    public void KeepAwake()
    {
        _keepAwakeTime = Time.time;
    }

    public void SetCachedMaterial(Material mat)
    {
        cachedMaterial = mat;
    }

    private void UpdateColor()
    {
        if (meshRenderer == null)
            return;

        bool shouldVisualizeAuthority = NetworkManagerGame.Singleton.visualizeAuthority &&
                                        (isServer && !isClient && ServerHasAuthority() || isClient && ClientHasAuthority());

        int newColorId = 0;
        var visualizePinchPullFrame = _visualizePinchPullFrame;
        if (_blockObject)
        {
            visualizePinchPullFrame = BlockUtility.GetRootBlockObject(_blockObject).AutoAuthority._visualizePinchPullFrame;
        }

        if (Time.frameCount <= visualizePinchPullFrame + 1)
            newColorId = 1;
        else if (shouldVisualizeAuthority)
            newColorId = 2;

        if (cachedMaterial == null)
            cachedMaterial = meshRenderer.material;

        if (newColorId != _colorId)
        {
            _colorId = newColorId;
            cachedMaterial.SetColor(_colorPropertyId, _colors[_colorId]);
        }
    }

    [Command(ignoreAuthority = true)]
    public void CmdSetKinematic(bool newIsKinematic)
    {
        isKinematic = newIsKinematic;
    }

    public void VisualizePinchPull(bool checkRoot = true)
    {
        if (checkRoot && _blockObject)
        {
            var rootObject = BlockUtility.GetRootBlockObject(_blockObject);
            if (rootObject != _blockObject)
            {
                rootObject.AutoAuthority.VisualizePinchPull(false);
                return;
            }
        }

        _visualizePinchPullFrame = Time.frameCount;
    }

    private bool IsInUse()
    {
        if (_blockObject)
        {
            return BlockUtility.GetRootBlockObject(_blockObject).AutoAuthority._inUse;
        }

        return _inUse;
    }

    [Server]
    public void SetInUse(bool inUse)
    {
        if (_blockObject)
        {
            var rootObject = BlockUtility.GetRootBlockObject(_blockObject);
            if (rootObject != _blockObject && _inUse) //if not at root, but inUse is set, reset it. only the root should be used 
            {
                _inUse = false;
            }

            rootObject.AutoAuthority._inUse = inUse;

            return;
        }

        _inUse = inUse;
    }

    [Command(ignoreAuthority = true)]
    public void CmdRemoveInUse(uint identityId)
    {
        if (_ownerIdentityId == identityId)
        {
            SetInUse(false);
        }
    }

    public void Interact()
    {
        if (!isClient)
            return;

        if (NetworkClient.connection != null && NetworkClient.connection.identity)
        {
            Interact(NetworkClient.connection.identity.netId);
        }
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
        ServerSetLastInteractTime(NetworkTime.time);
    }

    private void SetNewOwner(uint newOwnerIdentityId, double fromLastInteractTime)
    {
        //cannot switch owner if it is in use (held/pinch pulled/ect)
        if (InUse)
            return;

        if (_lastSetNewOwnerFrame != Time.frameCount)
        {
            _lastSetNewOwnerFrame = Time.frameCount;
            CmdSetNewOwner(newOwnerIdentityId, fromLastInteractTime, false);
        }
    }

    [Server]
    private float GetServerLastOwnerChange()
    {
        if (_blockObject)
        {
            return BlockUtility.GetRootBlockObject(_blockObject).AutoAuthority._serverLastOwnerChange;
        }

        return _serverLastOwnerChange;
    }

    [Server]
    private void ServerSetLastOwnerChange(float value)
    {
        if (_blockObject)
        {
            BlockUtility.GetRootBlockObject(_blockObject).AutoAuthority._serverLastOwnerChange = value;
            return;
        }

        _serverLastOwnerChange = value;
    }

    private double GetLastInteractTime()
    {
        if (_blockObject)
        {
            return BlockUtility.GetRootBlockObject(_blockObject).AutoAuthority._lastInteractTime;
        }

        return _lastInteractTime;
    }

    [Server]
    private void ServerSetLastInteractTime(double value)
    {
        if (_blockObject)
        {
            BlockUtility.GetRootBlockObject(_blockObject).AutoAuthority._lastInteractTime = value;
            return;
        }

        _lastInteractTime = value;
    }

    [Command(ignoreAuthority = true)]
    public void CmdSetNewOwner(uint newOwnerIdentityId, double fromLastInteractTime, bool inUse)
    {
        if (Time.time - GetServerLastOwnerChange() < OwnerChangeCooldown)
        {
            return;
        }

        //cannot switch owner if it is in use (held/pinch pulled/ect)
        if (InUse)
        {
            return;
        }

        if (inUse)
        {
            SetInUse(true);
        }

        ServerTryChangeOwnerAllConnected(newOwnerIdentityId, fromLastInteractTime);
    }

    //very similar to CmdSetNewOwner, but ignores inUse flag. This functions still checks that the owner is different from the current owner
    [Server]
    public void ServerForceNewOwner(uint newOwnerIdentityId, double fromLastInteractTime, bool inUse)
    {
        if (Time.time - GetServerLastOwnerChange() < OwnerChangeCooldown)
        {
            return;
        }

        SetInUse(inUse);

        ServerTryChangeOwnerAllConnected(newOwnerIdentityId, fromLastInteractTime);
    }

    [Server]
    private void ServerTryChangeOwnerAllConnected(uint newOwnerIdentityId, double fromLastInteractTime)
    {
        if (_blockObject)
        {
            var rootObject = BlockUtility.GetRootBlockObject(_blockObject);
            ServerTryChangeOwnerConnectedToSelf(rootObject, rootObject.AutoAuthority, newOwnerIdentityId, fromLastInteractTime);
            return;
        }

        ServerTryChangeOwner(newOwnerIdentityId, fromLastInteractTime);
    }

    [Server]
    private void ServerTryChangeOwnerConnectedToSelf(BlockObject currentBlockObject, AutoAuthority currentAutoAuthority, uint newOwnerIdentityId,
        double fromLastInteractTime)
    {
        currentAutoAuthority.ServerTryChangeOwner(newOwnerIdentityId, fromLastInteractTime);
        foreach (var pair in currentBlockObject.ConnectedToSelf)
        {
            if (pair.Value != null && pair.Value.Value)
            {
                var currentChildAutoAuthority = pair.Value.Value.GetComponent<AutoAuthority>();
                if (currentChildAutoAuthority && currentChildAutoAuthority._blockObject)
                {
                    currentChildAutoAuthority.ServerTryChangeOwnerConnectedToSelf(currentChildAutoAuthority._blockObject, currentChildAutoAuthority,
                        newOwnerIdentityId, fromLastInteractTime);
                }
            }
        }
    }

    [Server]
    private void ServerTryChangeOwner(uint newOwnerIdentityId, double fromLastInteractTime)
    {
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

        ServerSetLastInteractTime(fromLastInteractTime);
        _ownerIdentityId = newOwnerIdentityId;
        ServerSetLastOwnerChange(Time.time);
    }

    [Command(ignoreAuthority = true)]
    public void CmdDestroy()
    {
        gameObject.SetActive(false);
        ServerStripOwnerAndDestroy();
    }

    [Server]
    public void ServerStripOwnerAndDestroy()
    {
        if (_blockObject)
        {
            var rootObject = BlockUtility.GetRootBlockObject(_blockObject);
            ServerDestroyFromBlockObjectRoot(rootObject);
            return;
        }

        ServerForceNewOwner(uint.MaxValue, NetworkTime.time, true);
        gameObject.SetActive(false);
        GetComponent<SmoothSyncMirror>().clearBuffer();
        NetworkServer.Destroy(gameObject);
    }

    [Server]
    private void ServerDestroyFromBlockObjectRoot(BlockObject rootObject)
    {
        foreach (var pair in rootObject.ConnectedToSelf)
        {
            if (pair.Value != null && pair.Value.Value)
            {
                var blockObject = pair.Value.Value.GetComponent<BlockObject>();
                if (blockObject)
                {
                    ServerDestroyFromBlockObjectRoot(blockObject);
                }
                else
                {
                    Debug.LogError("Could not find blockObject for destroying");
                }
            }
            else
            {
                Debug.LogError("Could not find identity for destroying");
            }
        }

        rootObject.AutoAuthority.ServerForceNewOwner(uint.MaxValue, NetworkTime.time, true);
        rootObject.gameObject.SetActive(false);
        rootObject.GetComponent<SmoothSyncMirror>().clearBuffer();
        NetworkServer.Destroy(rootObject.gameObject);
    }

    private void HandleRecursiveAuthority(Collision collision)
    {
        if (!HasAuthority())
            return;

        var autoAuthority = collision.gameObject.GetComponent<AutoAuthority>();
        if (autoAuthority == null)
            return;

        var lastInteractTime = GetLastInteractTime();

        if (lastInteractTime <= autoAuthority.GetLastInteractTime())
            return;

        if (isClient && ClientHasAuthority() && !autoAuthority.ClientHasAuthority())
        {
            if (NetworkClient.connection != null && NetworkClient.connection.identity)
            {
                autoAuthority.SetNewOwner(NetworkClient.connection.identity.netId, lastInteractTime);
            }
        }
        else if (isServer && ServerHasAuthority() && !autoAuthority.ServerHasAuthority())
        {
            autoAuthority.SetNewOwner(uint.MaxValue, lastInteractTime);
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

    private void OnDestroy()
    {
        Destroy(cachedMaterial);
    }
}