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

    private float _inUseTimeout;
    private float _inUseSetTime;
    [SyncVar] private uint _inUseBy;
    [SyncVar] public bool isKinematic;
    private float _clientSetOwnerFakeTime;
    private const float FakeOwnerDuration = 1f;

    public bool InUse => GetInUse();
    public uint InUseBy => GetInUseBy();
    public bool InUseBySomeoneElse => GetInUseBySomeoneElse();

    public bool allowPinchPull = true;

    public MeshRenderer meshRenderer;

    private BlockObject _blockObject; //reference to own blockObject if it exists 

    private Rigidbody _body;
    private int _lastInteractFrame;
    private int _lastSetNewOwnerFrame;

    private int _visualizePinchPullFrame = -1;
    public Material cachedMaterial;
    private int _colorPropertyId;

    private int _setKinematicLocalFrame = -1;

    private float _keepAwakeTime = -10f;

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
            if (Time.time - _inUseSetTime > _inUseTimeout && _inUseBy != 0)
            {
                _inUseBy = 0;
            }

            if (!_blockObject && (transform.Invalid() || Vector3.SqrMagnitude(transform.position) > 20f * 20f ||
                                  transform.position.y < -1f || transform.position.y > 5f))
            {
                ServerStripOwnerAndDestroy();
                return;
            }
        }

        //if you don't have control over the object
        if (!HasAuthority())
        {
            var shouldFakeOwner = Time.time - _clientSetOwnerFakeTime < FakeOwnerDuration;
            if (!shouldFakeOwner)
            {
                _body.isKinematic = true;
            }

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
    public bool ClientHasAuthority()
    {
        if (!isClient)
            Debug.LogError("ClientHasAuthority is only valid when called from a client.");
        return NetworkClient.connection != null && NetworkClient.connection.identity != null && _ownerIdentityId == NetworkClient.connection.identity.netId;
    }

    //Hello function. I am a server. Do I have authority over this object?
    [Server]
    public bool ServerHasAuthority()
    {
        if (!isServer)
            Debug.LogError("ServerHasAuthority is only valid when called from a server.");
        return _ownerIdentityId == uint.MaxValue;
    }

    //Hello function. I don't know if I'm a client or a server, but whatever I am, do I have authority over this object?
    public bool HasAuthority()
    {
        return isServer && NetworkServer.active && ServerHasAuthority() || isClient && NetworkClient.active && ClientHasAuthority();
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

        var color = Color.white;
        var visualizePinchPullFrame = _visualizePinchPullFrame;
        if (_blockObject)
        {
            visualizePinchPullFrame = BlockUtility.GetRootBlockObject(_blockObject).AutoAuthority._visualizePinchPullFrame;
        }

        if (NetworkManagerGame.Singleton.visualizeAuthority)
        {
            var hasAuthority = isServer && !isClient && ServerHasAuthority() || isClient && ClientHasAuthority();
            if (hasAuthority)
            {
                if (InUse)
                {
                    color = Color.cyan;
                }
                else
                {
                    color = Color.green;
                }
            }
            else
            {
                if (InUse)
                {
                    color = Color.magenta;
                }
                else
                {
                    color = Color.red;
                }
            }
        }
        else if (Time.frameCount <= visualizePinchPullFrame + 1)
        {
            color = Color.red;
        }

        if (cachedMaterial == null)
        {
            cachedMaterial = meshRenderer.material;
        }

        cachedMaterial.SetColor(_colorPropertyId, color);
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

    private bool GetInUse()
    {
        if (_blockObject)
        {
            return BlockUtility.GetRootBlockObject(_blockObject).AutoAuthority._inUseBy != 0;
        }

        return _inUseBy != 0;
    }

    private uint GetInUseBy()
    {
        if (_blockObject)
        {
            return BlockUtility.GetRootBlockObject(_blockObject).AutoAuthority._inUseBy;
        }

        return _inUseBy;
    }

    private bool GetInUseBySomeoneElse()
    {
        if (NetworkClient.connection != null && NetworkClient.connection.identity)
        {
            return InUse && NetworkClient.connection.identity.netId != InUseBy;
        }
        else if (isServer)
        {
            return InUse && uint.MaxValue != InUseBy;
        }
        else
        {
            Debug.LogError("Not connected and not server");
            return false;
        }
    }

    [Server]
    public void SetInUseBy(uint inUseBy, float inUseTimeout = 1f)
    {
        if (_blockObject)
        {
            var rootObject = BlockUtility.GetRootBlockObject(_blockObject);
            if (rootObject != _blockObject && _inUseBy != 0) //if not at root, but inUse is set, reset it. only the root should be used 
            {
                _inUseBy = 0;
            }

            rootObject.AutoAuthority._inUseBy = inUseBy;
            rootObject.AutoAuthority._inUseSetTime = Time.time;
            rootObject.AutoAuthority._inUseTimeout = inUseTimeout;
        }
        else
        {
            _inUseBy = inUseBy;
            _inUseSetTime = Time.time;
            _inUseTimeout = inUseTimeout;
        }
    }

    [Command(ignoreAuthority = true)]
    public void CmdRemoveInUse(uint identityId)
    {
        if (_ownerIdentityId == identityId || _inUseBy == identityId)
        {
            SetInUseBy(0, 0);
        }
    }

    public void RefreshInUse(float inUseTimeout = 1f)
    {
        if (NetworkClient.connection != null && NetworkClient.connection.identity)
        {
            CmdRefreshInUse(NetworkClient.connection.identity.netId, inUseTimeout);
        }
        else if (isServer)
        {
            ServerRefreshInUse(uint.MaxValue, inUseTimeout);
        }
        else
        {
            Debug.LogError("Not connected and not server");
        }
    }

    [Command(ignoreAuthority = true)]
    private void CmdRefreshInUse(uint identityId, float inUseTimeout)
    {
        ServerRefreshInUse(identityId, inUseTimeout);
    }

    [Server]
    private void ServerRefreshInUse(uint identityId, float inUseTimeout)
    {
        if (_inUseBy == identityId)
        {
            SetInUseBy(identityId, inUseTimeout);
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
            CmdSetNewOwner(newOwnerIdentityId, fromLastInteractTime, false, 0);
            if (!isClient)
            {
                Debug.LogError("SetNewOwner: commands are for clients");
            }
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

    [Client]
    public void ClientSetNewOwnerFake(uint newOwnerIdentityId, double fromLastInteractTime, bool inUse = false, float inUseTimeout = 1f)
    {
        if (InUseBySomeoneElse)
        {
            return;
        }

        _clientSetOwnerFakeTime = Time.time;
        _body.isKinematic = false;
        CmdSetNewOwner(newOwnerIdentityId, fromLastInteractTime, inUse, inUseTimeout);
    }

    [Command(ignoreAuthority = true)]
    public void CmdSetNewOwner(uint newOwnerIdentityId, double fromLastInteractTime, bool inUse, float inUseTimeout)
    {
        if (!inUse && Time.time - GetServerLastOwnerChange() < OwnerChangeCooldown)
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
            SetInUseBy(newOwnerIdentityId, inUseTimeout);
        }

        ServerTryChangeOwnerAllConnected(newOwnerIdentityId, fromLastInteractTime);
    }

    //very similar to CmdSetNewOwner, but ignores inUse flag. This functions still checks that the owner is different from the current owner
    [Server]
    public void ServerForceNewOwner(uint newOwnerIdentityId, double fromLastInteractTime, bool inUse = false, float inUseTimeout = 1f)
    {
        if (inUse)
        {
            SetInUseBy(newOwnerIdentityId, inUseTimeout);
        }
        else
        {
            SetInUseBy(0, 0);
        }

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
    public void ServerStripOwnerAndDestroy(bool ignoreConnections = false)
    {
        if (ignoreConnections && BlockUtility.GetRootBlockObject(_blockObject) != _blockObject)
        {
            // the only reason you would ignore connections is if there are connections, but this will print an error if there are connections.
            // it is setup more as a failsafe, so if you ignore connections and there are connections, it will mostly work, but you will get some errors.
            // ideally, you would not ever need the ignoreConnections flag
            Debug.LogError("Ignoring connections, but this object does not equal root.");
        }

        if (!ignoreConnections && _blockObject)
        {
            var rootObject = BlockUtility.GetRootBlockObject(_blockObject);
            BlockObject.ServerDestroyFromBlockObjectRoot(rootObject);
            return;
        }

        ServerForceNewOwner(uint.MaxValue, NetworkTime.time, true, 1f);
        gameObject.SetActive(false);
        GetComponent<SmoothSyncMirror>().clearBuffer();
        NetworkServer.Destroy(gameObject);
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