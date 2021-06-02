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

    [SyncVar] private bool _inUse = false;
    [SyncVar] public bool isKinematic = false;
    public bool destroyIfBelow = true; //also if far from origin or high above

    public bool InUse => _inUse;

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
        _serverLastOwnerChange = 0;

        if (_ownerIdentityId == 0)
        {
            _ownerIdentityId = uint.MaxValue;
        }

        _lastInteractTime = NetworkTime.time;
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

        if (isServer && destroyIfBelow)
        {
            var distanceSquared = Vector3.SqrMagnitude(transform.position);
            if (distanceSquared > 20f * 20f || transform.position.y < -2f || transform.position.y > 5f || PhysicsHandController.InvalidTransform(transform))
            {
                //for now if it has a bearing, don't teleport back TODO: add teleport for bearing objects
                var isBlockObjectAndHasConnections = _blockObject && _blockObject.syncJoint.connected || _blockObject.ConnectedToSelf.Count > 0;
                if (!isBlockObjectAndHasConnections)
                {
                    if (_blockObject && (_blockObject.Blocks.Count > 4 || _blockObject.syncJoint.connected || _blockObject.ConnectedToSelf.Count > 0))
                    {
                        ServerForceNewOwner(uint.MaxValue, NetworkTime.time, false);
                        GetComponent<SmoothSyncMirror>().clearBuffer();
                        _body.velocity = Vector3.zero;
                        _body.angularVelocity = Vector3.zero;

                        GetComponent<SmoothSyncMirror>().teleportAnyObjectFromServer(new Vector3(0, 2, 0), Quaternion.identity, transform.localScale);
                    }
                    else
                    {
                        ServerStripOwnerAndDestroy();
                    }

                    return;
                }
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
        if (Time.time - _serverLastOwnerChange < OwnerChangeCooldown)
        {
            return;
        }

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
        _serverLastOwnerChange = Time.time;
    }

    [Server]
    public void ServerForceNewOwner(uint newOwnerIdentityId, double fromLastInteractTime, bool inUse)
    {
        if (Time.time - _serverLastOwnerChange < OwnerChangeCooldown)
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
        _serverLastOwnerChange = Time.time;
        _inUse = inUse;
    }

    [Server]
    public void ServerStripOwnerAndDestroy()
    {
        ServerForceNewOwner(uint.MaxValue, NetworkTime.time, true);
        gameObject.SetActive(false);
        GetComponent<SmoothSyncMirror>().clearBuffer();
        NetworkServer.Destroy(gameObject);
    }

    [Command]
    public void CmdDestroy()
    {
        gameObject.SetActive(false);
        ServerStripOwnerAndDestroy();
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

    private void OnDestroy()
    {
        Destroy(cachedMaterial);
    }
}