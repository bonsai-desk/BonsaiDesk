using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mirror;
using Smooth;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
public partial class BlockObject : NetworkBehaviour
{
    public const float CubeScale = 0.05f;
    private const float MaxVelocity = 10f;
    private const float MaxAngularVelocity = 50f;

    //contains all of the AutoAuthority of all BlockObjects in the scene
    private static readonly HashSet<AutoAuthority> _blockObjectAuthorities = new HashSet<AutoAuthority>();

    //---all of the data required to reconstruct this block object---

    //contains information about blocks and their rotations
    private readonly SyncDictionary<Vector3Int, SyncBlock> _blocks = new SyncDictionary<Vector3Int, SyncBlock>();

    //other blockObjects connected to this one. the coord is which bearing they are attached to. use their syncJoint for joint info
    private readonly SyncDictionary<Vector3Int, NetworkIdentityReference> _connectedToSelf = new SyncDictionary<Vector3Int, NetworkIdentityReference>();

    //all info required to connect this block to its parent
    [SyncVar(hook = nameof(OnSyncJointChange))]
    private SyncJoint _syncJoint;

    //used to reset the blockObject if it gets into an invalid configuration (bent joints)
    [SyncVar] private Vector3 _validLocalPosition;
    [SyncVar] private Quaternion _validLocalRotation;

    //---end all of the data required to reconstruct this block object---

    //if this is is not null, it will find the object it was spawned by and tell it to apply its client side prediction stored in _toBeRemovedClientSidePrediction
    //the purpose is to prevent a split block structure from spawning in the current block structure before the blocks are removed
    [SyncVar] private NetworkIdentityReference _spawnedForBrokenStructure = new NetworkIdentityReference();

    public SyncDictionary<Vector3Int, SyncBlock> Blocks => _blocks;
    public SyncDictionary<Vector3Int, NetworkIdentityReference> ConnectedToSelf => _connectedToSelf;
    public SyncJoint SyncJoint => _syncJoint;

    private Queue<Vector3Int> _toBeRemovedClientSidePrediction = new Queue<Vector3Int>();

    private bool _activeLocal = true;

    public bool ActiveLocal
    {
        get { return _activeLocal; }
        set { SetActiveLocal(value); }
    }

    //used to make sure ServerUpdateValidOrientationFromRoot is only called once if many blocks are added
    private int _updateValidOrientationFromRootFrame;

    //caches changes made to the sync dictionary so all block changes can be made at once with a single mesh update
    //also allows client side prediction by queueing up changes that you have only just sent the command for
    private readonly Queue<(Vector3Int coord, SyncBlock syncBlock, SyncDictionary<Vector3Int, SyncBlock>.Operation op)> _blockChanges =
        new Queue<(Vector3Int coord, SyncBlock syncBlock, SyncDictionary<Vector3Int, SyncBlock>.Operation op)>();

    //caches changes made to the sync dictionary so joint changes can be made at once
    //also allows client side prediction by queueing up changes that you have only just sent the command for
    private readonly Queue<(Vector3Int coord, NetworkIdentityReference identity, SyncDictionary<Vector3Int, NetworkIdentityReference>.Operation op)>
        _connectedToSelfChanges =
            new Queue<(Vector3Int coord, NetworkIdentityReference identity, SyncDictionary<Vector3Int, NetworkIdentityReference>.Operation op)>();

    //public inspector variables
    public Material blockObjectMaterial;
    public PhysicMaterial blockPhysicMaterial;
    public PhysicMaterial spherePhysicMaterial;
    public GameObject saveDialogPrefab;
    public GameObject breakWholeDialogPrefab;

    //contains the information about the local state of the mesh. The structure of the mesh can be slightly
    //different depending on the order of block add/remove even though the final result looks the same
    public readonly Dictionary<Vector3Int, MeshBlock> MeshBlocks = new Dictionary<Vector3Int, MeshBlock>();

    //physics joint based on data from syncJoint. is null is not connected to anything
    private HingeJoint _joint = null;
    public HingeJoint Joint => _joint;

    //contains the keys for entries in _meshBlocks which have blockGameObjects. This saves having to loop through
    //the entire _meshBlocks to check for blockGameObject
    private readonly HashSet<Vector3Int> _blockGameObjects = new HashSet<Vector3Int>();

    //contains the keys for entries in _meshBlocks which are damages. This saves having to loop through
    //the entire _meshBlocks to check for damaged blocks
    private readonly HashSet<Vector3Int> _damagedBlocks = new HashSet<Vector3Int>();

    //used to keep track of any full block effect such as duplicate or delete all
    private WholeEffect _activeWholeEffect;

    //stores any active dialog. otherwise is null
    private GameObject _activeDialog;
    private Vector3 _dialogLocalPositionRoot;

    //mesh stuff
    private Transform _meshObject;
    private MeshFilter _meshFilter;
    private Mesh _mesh;
    private List<Vector3> _vertices = new List<Vector3>();
    private List<Vector3> _uv = new List<Vector3>();
    private List<Vector2> _uv2 = new List<Vector2>();
    private List<int> _triangles = new List<int>();

    //holds blockGameObjects
    private Transform _blockGameObjectsParent;

    //is active if there is only one block and it has a blockGameObject
    private GameObject _transparentCube;
    private BoxCollider _transparentCubeCollider = null;
    public GameObject transparentCubePrefab;

    //physics
    private Rigidbody _body;
    public Rigidbody Body => _body;
    private Transform _physicsBoxesObject;
    private Queue<BoxCollider> _boxCollidersInUse = new Queue<BoxCollider>();
    private bool _resetCoM; //flag to reset CoM on the next physics update
    private AutoAuthority _autoAuthority;
    public AutoAuthority AutoAuthority => _autoAuthority;
    private Transform _sphereObject;

    //this object is the parent of any BlockObject with 1 block trying to attach to another BlockObject
    private Transform _potentialBlocksParent;

    //used to make sure Init is only called once
    private bool _isInit = false;

    //material property ids
    private static readonly int TextureArray = Shader.PropertyToID("_TextureArray");
    private static readonly int EffectProgress = Shader.PropertyToID("_EffectProgress");
    private static readonly int EffectColor = Shader.PropertyToID("_EffectColor");
    private static readonly int Health = Shader.PropertyToID("_Health");

    //used so we can just check for problems every few seconds, not every frame
    private float _nextCheckBlockObjectForProblemsTime;

    //for testing purposes
    public bool debug = false;

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _autoAuthority = GetComponent<AutoAuthority>();
    }

    public override void OnStartServer()
    {
        if (Blocks.Count <= 0)
        {
            Debug.LogError("Cannot have block object with no blocks");
            NetworkServer.Destroy(gameObject);
            return;
        }

        Init();
        BlockUtility.GetRootBlockObject(this).ServerUpdateValidOrientationFromRoot();
    }

    public override void OnStartClient()
    {
        Init();
    }

    private void Init()
    {
        if (_isInit)
        {
            return;
        }

        _isInit = true;

        gameObject.name = "Block Object - " + Random.Range(0, int.MaxValue);

        if (_spawnedForBrokenStructure != null && _spawnedForBrokenStructure.Value)
        {
            _spawnedForBrokenStructure.Value.GetComponent<BlockObject>().ApplyToBeRemovedClientSidePrediction();
        }

        _nextCheckBlockObjectForProblemsTime = Time.time + 2f + Random.value * 2f;

        //make copy of material so material asset is not changed
        blockObjectMaterial = new Material(blockObjectMaterial);
        blockObjectMaterial.SetTexture(TextureArray, BlockUtility.BlockTextureArray);

        PhysicsStart();

        if (!_blockObjectAuthorities.Contains(_autoAuthority))
        {
            _blockObjectAuthorities.Add(_autoAuthority);
        }

        _body.maxAngularVelocity = MaxAngularVelocity;

        transform.localScale = new Vector3(CubeScale, CubeScale, CubeScale);

        SetupChildren();

        _mesh = new Mesh();
        _mesh.MarkDynamic();
        _meshFilter.mesh = _mesh;

        CreateInitialMesh();
        ConnectInitialJoints();

        Blocks.Callback -= OnBlocksDictionaryChange;
        Blocks.Callback += OnBlocksDictionaryChange;

        ConnectedToSelf.Callback -= OnConnectedToSelfDictionaryChange;
        ConnectedToSelf.Callback += OnConnectedToSelfDictionaryChange;
    }

    private void Update()
    {
        ProcessBlockChanges();
        ProcessConnectedToSelfChanges();
        UpdateDamagedBlocks();
        UpdateWholeEffects();
        UpdateDialogPosition();
        CheckPotentialBlocksParent();
        CheckForProblems();
    }

    private void FixedUpdate()
    {
        if (!_isInit)
        {
            return;
        }

        CheckTeleport();

        if (!_autoAuthority.HasAuthority())
        {
            return;
        }

        _body.velocity = Vector3.ClampMagnitude(_body.velocity, MaxVelocity);

        if (_resetCoM)
        {
            _resetCoM = false;
            _body.ResetInertiaTensor();
            _body.ResetCenterOfMass();
            _body.WakeUp();
        }

        if (isClient)
        {
            PhysicsFixedUpdate();
        }
    }

    public override void OnStopServer()
    {
        //clean up any existing connections
        //this shouldn't need to happen unless something goes wrong
        if (SyncJoint.connected)
        {
            Debug.LogError("SyncJoint should have been cleaned up before destroy");
            ServerDisconnectJoint();
        }

        var toBeDisconnected = new Queue<BlockObject>();
        foreach (var pair in ConnectedToSelf)
        {
            if (pair.Value != null && pair.Value.Value)
            {
                var connectedBlockObject = pair.Value.Value.GetComponent<BlockObject>();
                if (connectedBlockObject.SyncJoint.connected)
                {
                    toBeDisconnected.Enqueue(connectedBlockObject);
                }
            }
        }

        while (toBeDisconnected.Count > 0)
        {
            Debug.LogError("Connected blockObject should have been cleaned up before destroy");
            toBeDisconnected.Dequeue().ServerDisconnectJoint();
        }
    }

    private void OnDestroy()
    {
        CloseDialog();

        if (_blockObjectAuthorities.Contains(_autoAuthority))
        {
            _blockObjectAuthorities.Remove(_autoAuthority);
        }

        Destroy(blockObjectMaterial); //auto authority will also destroy this material

        foreach (var coord in _blockGameObjects)
        {
            Destroy(MeshBlocks[coord].material);
        }
    }

    private void SetupChildren()
    {
        var meshObject = new GameObject("Mesh");
        _meshObject = meshObject.transform;
        meshObject.transform.SetParent(transform, false);
        var meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = blockObjectMaterial;
        _autoAuthority.SetCachedMaterial(blockObjectMaterial);
        _autoAuthority.meshRenderer = meshRenderer;
        _meshFilter = meshObject.AddComponent<MeshFilter>();

        var physicsBoxes = new GameObject("PhysicsBoxes");
        _physicsBoxesObject = physicsBoxes.transform;
        physicsBoxes.transform.SetParent(transform, false);

        _potentialBlocksParent = new GameObject("PotentialBlocks").transform;
        const float inverseScale = 1f / CubeScale;
        _potentialBlocksParent.localScale = new Vector3(inverseScale, inverseScale, inverseScale);
        _potentialBlocksParent.SetParent(transform, false);

        var sphereObject = new GameObject("Sphere");
        _sphereObject = sphereObject.transform;
        sphereObject.transform.SetParent(transform, false);
        sphereObject.layer = LayerMask.NameToLayer("sphere");

        _blockGameObjectsParent = new GameObject("BlockGameObjects").transform;
        _blockGameObjectsParent.SetParent(transform, false);

        _transparentCube = Instantiate(transparentCubePrefab, transform);
        _transparentCube.name = "TransparentCube";
        _transparentCube.SetActive(false);
    }

    private void OnBlocksDictionaryChange(SyncDictionary<Vector3Int, SyncBlock>.Operation op, Vector3Int key, SyncBlock value)
    {
        _blockChanges.Enqueue((key, value, op));

#if UNITY_EDITOR
        if (debug)
        {
            EditorApplication.isPaused = true;
        }
#endif
    }

    //loops through any blocks in _blockChanges and adds/removes blocks from the mesh
    private void ProcessBlockChanges()
    {
        if (_blockChanges.Count == 0)
        {
            return;
        }

        while (_blockChanges.Count > 0)
        {
            var (coord, syncBlock, op) = _blockChanges.Dequeue();

            switch (op)
            {
                case SyncDictionary<Vector3Int, SyncBlock>.Operation.OP_ADD:
                    if (MeshBlocks.TryGetValue(coord, out MeshBlock meshBlock))
                    {
                        var checkRot = BlockUtility.QuaternionToByte(meshBlock.rotation);
                        if (checkRot == 0)
                        {
                            checkRot = 36;
                        }
                        var otherRot = syncBlock.rotation;
                        if (otherRot == 0)
                        {
                            otherRot = 36;
                        }
                        if (meshBlock.name != syncBlock.name || checkRot != otherRot)
                        {
                            Debug.LogError(
                                "MeshBlock already exists, but does not equal syncBlock name or rotation. Did client side prediction cause it to get un-synced?");
                        }
                    }
                    else
                    {
                        AddBlockToMesh(syncBlock.name, coord, BlockUtility.ByteToQuaternion(syncBlock.rotation));
                    }

                    break;
                case SyncDictionary<Vector3Int, SyncBlock>.Operation.OP_REMOVE:
                    if (MeshBlocks.ContainsKey(coord))
                    {
                        RemoveBlockFromMesh(coord);
                    }

                    break;
                default:
                    Debug.LogError("Unknown dictionary operation.");
                    break;
            }
        }

        UpdateMesh();
    }

    //loops through any identities in _connectedToSelfChanges and connects/disconnects joints
    private void ProcessConnectedToSelfChanges()
    {
        if (_connectedToSelfChanges.Count == 0)
        {
            return;
        }

        while (_connectedToSelfChanges.Count > 0)
        {
            var (coord, identity, op) = _connectedToSelfChanges.Dequeue();

            switch (op)
            {
                case SyncDictionary<Vector3Int, NetworkIdentityReference>.Operation.OP_ADD:
                    if (identity == null || !identity.Value)
                    {
                        break;
                    }

                    var otherBlockObjectAdd = identity.Value.GetComponent<BlockObject>();

                    if (!otherBlockObjectAdd)
                    {
                        break;
                    }

                    otherBlockObjectAdd.ConnectJoint(otherBlockObjectAdd._syncJoint);

                    break;
                case SyncDictionary<Vector3Int, NetworkIdentityReference>.Operation.OP_REMOVE:
                    if (identity == null || !identity.Value)
                    {
                        break;
                    }

                    var otherBlockObjectRemove = identity.Value.GetComponent<BlockObject>();

                    if (!otherBlockObjectRemove)
                    {
                        break;
                    }

                    otherBlockObjectRemove.DisconnectJoint();

                    break;
                default:
                    Debug.LogError("Unknown dictionary operation.");
                    break;
            }
        }
    }

    private void OnConnectedToSelfDictionaryChange(SyncDictionary<Vector3Int, NetworkIdentityReference>.Operation op, Vector3Int key,
        NetworkIdentityReference value)
    {
        _connectedToSelfChanges.Enqueue((key, value, op));
    }

    [Command(ignoreAuthority = true)]
    private void CmdConnectJoint(SyncJoint jointInfo)
    {
        ServerConnectJoint(jointInfo);
    }

    [Server]
    public void ServerConnectJoint(SyncJoint jointInfo)
    {
        if (_syncJoint.connected)
        {
            Debug.LogError("SyncJoint was already connected. (not returning, but weird stuff might happen)");
        }

        _syncJoint = jointInfo;

        if (jointInfo.attachedTo == null || !jointInfo.attachedTo.Value)
        {
            Debug.LogError("jointInfo attachedTo is null");
            return;
        }

        var attachedToBlockObject = jointInfo.attachedTo.Value.GetComponent<BlockObject>();

        if (!attachedToBlockObject)
        {
            Debug.LogError("attachedTo blockObject does not exist in CmdConnectJoint");
            return;
        }

        if (attachedToBlockObject.ConnectedToSelf.ContainsKey(jointInfo.otherBearingCoord))
        {
            Debug.LogError("ConnectedToSelf already contains key.");
            return;
        }

        attachedToBlockObject.ConnectedToSelf.Add(jointInfo.otherBearingCoord, new NetworkIdentityReference(netIdentity));
        BlockUtility.GetRootBlockObject(this).ServerUpdateValidOrientationFromRoot();
    }

    private void OnSyncJointChange(SyncJoint oldValue, SyncJoint newValue)
    {
        if (oldValue.connected && newValue.connected && oldValue != newValue)
        {
            DisconnectJoint();
        }

        if (newValue.connected)
        {
            ConnectJoint(newValue);
        }
        else
        {
            DisconnectJoint();
        }
    }

    private void ConnectJoint(SyncJoint jointInfo)
    {
        if (!jointInfo.connected)
        {
            return;
        }

        if (_joint) //joint already exists. hopefully because of client side prediction
        {
            if (jointInfo != SyncJoint)
            {
                Debug.LogError("Joint already exists, but does not equal jointInfo. Did client side prediction cause it to get un-synced?");
                DisconnectJoint();
            }
            else
            {
                return;
            }
        }

        //joints are doubly linked, so maybe one exists before the other? If that is the case, just return and it will be handled by the other blockObject
        if (!jointInfo.attachedTo.Value)
        {
            return;
        }

        var attachedToBody = jointInfo.attachedTo.Value.GetComponent<Rigidbody>();

        if (!attachedToBody)
        {
            return;
        }

        //perfectly allign block before attaching joint. This is because a joint is not fully defined
        //by the axis, anchor, and connected anchor. The initial relative position of the two objects matters

        //idk if this step is required, but it makes sure the transform and body are the same
        attachedToBody.MovePosition(attachedToBody.transform.position);
        attachedToBody.MoveRotation(attachedToBody.transform.rotation);
        _body.MovePosition(transform.position);
        _body.MoveRotation(transform.rotation);

        //save previous orientation. position/rotation will be changed to connect the joint, then it can go back to what it was
        //hopefully the previous orientation is valid so the joint does not try to move it too far
        var previousPosition = transform.position;
        var previousRotation = transform.rotation;

        var axisLocalToAttachedTo = BlockUtility.ByteToQuaternion(jointInfo.bearingLocalRotation) * Vector3.up;
        var bearingOffset = axisLocalToAttachedTo * 0.1f;
        transform.position = jointInfo.attachedTo.Value.transform.TransformPoint(jointInfo.otherBearingCoord + bearingOffset);
        transform.rotation = jointInfo.attachedTo.Value.transform.rotation * BlockUtility.ByteToQuaternion(jointInfo.localRotation);

        _body.MovePosition(transform.position);
        _body.MoveRotation(transform.rotation);

        //create hinge joint component
        _joint = gameObject.AddComponent<HingeJoint>();

        _joint.autoConfigureConnectedAnchor = false;
        _joint.enableCollision = true;

        var axisLocalToSelf = transform.InverseTransformDirection(jointInfo.attachedTo.Value.transform.rotation * axisLocalToAttachedTo);
        axisLocalToSelf = new Vector3(Mathf.Round(axisLocalToSelf.x), Mathf.Round(axisLocalToSelf.y), Mathf.Round(axisLocalToSelf.z));
        axisLocalToSelf = axisLocalToSelf.normalized;

        _joint.axis = axisLocalToSelf;
        _joint.anchor = jointInfo.attachedToMeAtCoord;
        _joint.connectedAnchor = jointInfo.otherBearingCoord + 0.1f * axisLocalToAttachedTo;

        _joint.connectedBody = attachedToBody;

        transform.position = previousPosition;
        transform.rotation = previousRotation;
        _body.MovePosition(transform.position);
        _body.MoveRotation(transform.rotation);
    }

    private void DisconnectJoint()
    {
        if (_joint)
        {
            Destroy(_joint);
            _joint = null;
        }
    }

    [Command(ignoreAuthority = true)]
    private void CmdAddBlock(string blockName, Vector3Int coord, byte rotationByte, NetworkIdentity identityToDestroy)
    {
        identityToDestroy.GetComponent<AutoAuthority>().ServerStripOwnerAndDestroy();

        if (Blocks.ContainsKey(coord))
        {
            Debug.LogError("Command: Attempted to add block which already exists");
            return;
        }
        
        Blocks.Add(coord, new SyncBlock(blockName, rotationByte));
        if (_updateValidOrientationFromRootFrame != Time.frameCount)
        {
            BlockUtility.GetRootBlockObject(this).ServerUpdateValidOrientationFromRoot();
        }
    }

    private void AddBlockToMesh(string blockName, Vector3Int coord, Quaternion rotation)
    {
        if (MeshBlocks.ContainsKey(coord))
        {
            Debug.LogError("Attempted to add block to mesh which already exists in _meshBlocks");
            return;
        }

        var blockMesh = BlockUtility.GetBlockMesh(blockName, coord, rotation);
        _vertices.AddRange(blockMesh.vertices);
        _uv.AddRange(blockMesh.uv);
        _uv2.AddRange(blockMesh.uv2);
        for (int i = 0; i < blockMesh.triangles.Length; i++)
        {
            blockMesh.triangles[i] += _vertices.Count - (6 * 4);
        }

        _triangles.AddRange(blockMesh.triangles);

        GameObject blockGameObject = null;
        Material blockGameObjectMaterial = null;
        Block block = global::Blocks.GetBlock(blockName);
        if (block.blockGameObjectPrefab)
        {
            blockGameObject = Instantiate(block.blockGameObjectPrefab, _blockGameObjectsParent);
            blockGameObject.transform.localPosition = coord;
            blockGameObject.transform.localRotation = rotation;
            _blockGameObjects.Add(coord);

            blockGameObjectMaterial = blockGameObject.GetComponentInChildren<MeshRenderer>().material;
        }

        MeshBlocks.Add(coord, new MeshBlock(blockName, rotation, MeshBlocks.Count, blockGameObject, blockGameObjectMaterial));
    }

    [Command(ignoreAuthority = true)]
    private void CmdRemoveBlock(Vector3Int coord, uint identityId)
    {
        if (!Blocks.ContainsKey(coord))
        {
            Debug.LogError("Command: Attempted to remove block which does not exist in Blocks.");
            return;
        }

        //flood fill blocks surrounding the block which will be removed so we can know if removing this block
        //should result in the block object splitting into 2 or more block objects
        var (filledBlocksGroups, indexOfLargest) = BlockUtility.GetFilledBlocksGroups(coord, Blocks);

        //if there is only one group of filled blocks, just remove the block
        if (filledBlocksGroups.Count <= 1)
        {
            //check if removed block was where own joint is attached
            if (SyncJoint.connected && SyncJoint.attachedToMeAtCoord == coord)
            {
                ServerDisconnectJoint();
            }

            //check if removed block was where a bearing is which is attached to another blockObject
            if (ConnectedToSelf.TryGetValue(coord, out var netIdRef) && netIdRef != null && netIdRef.Value)
            {
                netIdRef.Value.GetComponent<BlockObject>().ServerDisconnectJoint();
            }

            if (Blocks.Count <= 1)
            {
                _autoAuthority.ServerStripOwnerAndDestroy();
                return;
            }

            if (filledBlocksGroups.Count != 1)
            {
                Debug.LogError("filledBlocksGroups should have exactly 1 element here");
                _autoAuthority.ServerStripOwnerAndDestroy();
                return;
            }

            if (ServerHandleSingleBearingAfterRemove(filledBlocksGroups[0], true, ConnectedToSelf, SyncJoint, _autoAuthority, null))
            {
                return;
            }

            Blocks.Remove(coord);
        }
        else //if there are 2 or more groups of filled blocks, we must split this object into 2 or more objects
        {
            //remove largest block group from the list
            var largestGroup = filledBlocksGroups[indexOfLargest];
            filledBlocksGroups.RemoveAt(indexOfLargest);

            //every block that is not a part of the largest group will be removed and spawned as a new block
            //this way the largest group of blocks can be reused instead of regenerated
            Blocks.Remove(coord);
            foreach (var filledBlocks in filledBlocksGroups)
            {
                foreach (var pair in filledBlocks)
                {
                    Blocks.Remove(pair.Key);
                }
            }

            //immediately remove the blocks so the newly spawned blocks to not clip
            //this will only happen for the server/host, so hopefully it does not glitch on the client
            ProcessBlockChanges();

            //cache the joints so we can still access it if it is disconnected
            var cachedSyncJoint = SyncJoint;
            var cachedSyncJoints = new Dictionary<Vector3Int, (NetworkIdentityReference netIdRef, SyncJoint syncJoint)>();

            //handle removing this blockObjects main joint
            //the blockObject that this code is currently running on becomes the largestGroup. if this blockObject had a joint, and the joint is connected
            //at a coord that is no longer a part of this blockObject, it should be removed
            if (cachedSyncJoint.connected && !largestGroup.ContainsKey(cachedSyncJoint.attachedToMeAtCoord))
            {
                ServerDisconnectJoint(); //also calls ProcessConnectedToSelfChanges
            }

            //handle removing this blockObjects ConnectedToSelf blockObjects joints
            //disconnect any blockObjects that were connected to this blockObject but are not connected to the largestGroup (which this blockObject is becoming)
            var connectedToDisconnect = new Queue<BlockObject>();
            foreach (var pair in ConnectedToSelf)
            {
                if (pair.Value != null && pair.Value.Value && !largestGroup.ContainsKey(pair.Key))
                {
                    var connectedBlockObject = pair.Value.Value.GetComponent<BlockObject>();
                    if (connectedBlockObject)
                    {
                        connectedToDisconnect.Enqueue(connectedBlockObject);
                    }
                }
            }

            while (connectedToDisconnect.Count > 0)
            {
                var connectedBlockObject = connectedToDisconnect.Dequeue();
                if (connectedBlockObject.SyncJoint.connected)
                {
                    cachedSyncJoints.Add(connectedBlockObject.SyncJoint.otherBearingCoord,
                        (new NetworkIdentityReference(connectedBlockObject.netIdentity), connectedBlockObject.SyncJoint));
                }

                connectedBlockObject.ServerDisconnectJoint();
            }

            //if the largest group is a single bearing, this function will handle it
            ServerHandleSingleBearingAfterRemove(largestGroup, true, ConnectedToSelf, SyncJoint, _autoAuthority, null);

            //generate the new block objects from the remaining blocks groups
            foreach (var filledBlocks in filledBlocksGroups)
            {
                if (ServerHandleSingleBearingAfterRemove(filledBlocks, false, null, new SyncJoint(), null, cachedSyncJoints))
                {
                    continue;
                }

                var newBlockObject = Instantiate(StaticPrefabs.instance.blockObjectPrefab, transform.position, transform.rotation);

                //add all blocks before spawning. they will be turned into MeshBlocks in Init
                var newBlockObjectScript = newBlockObject.GetComponent<BlockObject>();
                foreach (var pair in filledBlocks)
                {
                    newBlockObjectScript.Blocks.Add(pair.Key, pair.Value);
                }

                newBlockObjectScript._spawnedForBrokenStructure = new NetworkIdentityReference(netIdentity);

                //spawn it before joints are added so it has a valid NetworkIdentity/netId
                NetworkServer.Spawn(newBlockObject);

                //handle transferring this blockObjects main syncJoint
                if (cachedSyncJoint.connected && filledBlocks.ContainsKey(cachedSyncJoint.attachedToMeAtCoord))
                {
                    if (cachedSyncJoint.attachedTo != null && cachedSyncJoint.attachedTo.Value)
                    {
                        //add joints/joint references in both directions (doubly linked references)
                        newBlockObjectScript._syncJoint = cachedSyncJoint;
                        cachedSyncJoint.attachedTo.Value.GetComponent<BlockObject>().ConnectedToSelf.Add(cachedSyncJoint.otherBearingCoord,
                            new NetworkIdentityReference(newBlockObjectScript.netIdentity)); //must be done after spawn so netIdentity has a non-zero netId
                    }
                }

                //handle transferring this blockObject's ConnectedToSelf joints
                foreach (var pair in cachedSyncJoints)
                {
                    if (pair.Value.syncJoint.connected && filledBlocks.ContainsKey(pair.Key))
                    {
                        //in this case it is checking that it currently points to "this" blockObject. even though it will now point to newBlockObjectScript,
                        //this check still makes sure that the original connection was actually valid
                        if (pair.Value.syncJoint.attachedTo != null && pair.Value.syncJoint.attachedTo.Value)
                        {
                            //add joints/joint references in both directions (doubly linked references)
                            //create copy of SyncJoint with nedIdRef now pointing to the new blockObject, not this
                            pair.Value.netIdRef.Value.GetComponent<BlockObject>()._syncJoint = new SyncJoint(pair.Value.syncJoint,
                                new NetworkIdentityReference(newBlockObjectScript.netIdentity));
                            newBlockObjectScript.ConnectedToSelf.Add(pair.Value.syncJoint.otherBearingCoord, pair.Value.netIdRef);
                        }
                    }
                }

                //since the joints are set after spawning, OnStartServer has already been called, so call ConnectInitialJoints
                //I don't think this is required since after spawning SyncVar hooks will run, but it doesn't hurt since ConnectJoint will do nothing
                //if the joint is already connected
                newBlockObjectScript.ConnectInitialJoints();

                //give authority of the newly created blockObject to whoever called this CmdRemoveBlock function
                newBlockObject.GetComponent<AutoAuthority>().ServerForceNewOwner(identityId, NetworkTime.time, false);
            }
        }

        if (_updateValidOrientationFromRootFrame != Time.frameCount)
        {
            BlockUtility.GetRootBlockObject(this).ServerUpdateValidOrientationFromRoot();
        }
    }

    [Server]
    private static bool ServerHandleSingleBearingAfterRemove(Dictionary<Vector3Int, SyncBlock> blockGroup, bool isLargestGroup,
        SyncDictionary<Vector3Int, NetworkIdentityReference> connectedToSelf, SyncJoint syncJoint, AutoAuthority autoAuthority,
        Dictionary<Vector3Int, (NetworkIdentityReference netIdRef, SyncJoint syncJoint)> cachedSyncJoints)
    {
        if (blockGroup.Count == 1)
        {
            Vector3Int onlyBlockCoord = Vector3Int.zero;
            SyncBlock onlyBlock = new SyncBlock();
            foreach (var pair in blockGroup)
            {
                onlyBlockCoord = pair.Key;
                onlyBlock = blockGroup[pair.Key];
                break;
            }

            if (onlyBlock.name == "bearing")
            {
                if (isLargestGroup)
                {
                    if (connectedToSelf.TryGetValue(onlyBlockCoord, out var netIdRef))
                    {
                        //isLargestGroup: single bearing attached
                        if (netIdRef != null && netIdRef.Value)
                        {
                            netIdRef.Value.GetComponent<BlockObject>().ServerDisconnectJoint();
                        }

                        autoAuthority.ServerStripOwnerAndDestroy(true);
                    }
                    else
                    {
                        if (syncJoint.connected)
                        {
                            Debug.LogError("isLargestGroup: A single bearing should not have a SyncJoint");
                        }

                        //largest group: single bearing NOT attached
                        autoAuthority.ServerStripOwnerAndDestroy(true);
                    }
                }
                else
                {
                    if (cachedSyncJoints.TryGetValue(onlyBlockCoord, out var cache))
                    {
                        //single bearing attached
                        if (cache.netIdRef != null && cache.netIdRef.Value)
                        {
                            var attachedToBlockObject = cache.netIdRef.Value.GetComponent<BlockObject>();
                            if (attachedToBlockObject && attachedToBlockObject.SyncJoint.connected)
                            {
                                attachedToBlockObject.ServerDisconnectJoint();
                            }
                        }
                    }
                    else
                    {
                        if (syncJoint.connected)
                        {
                            Debug.LogError("A single bearing should not have a SyncJoint");
                        }

                        //if not largest group, then it hasn't been created in the first place, so you don't need to destroy it
                    }
                }

                return true;
            }
        }

        return false;
    }

    [Server]
    public void ServerDisconnectJoint()
    {
        if (!_syncJoint.connected)
        {
            Debug.LogError("Joint was not connected");
            return;
        }

        var oldSyncJoint = _syncJoint;
        _syncJoint = new SyncJoint();
        DisconnectJoint();

        if (oldSyncJoint.attachedTo == null || !oldSyncJoint.attachedTo.Value)
        {
            Debug.LogError("oldSyncJoint attachedTo is null");
            return;
        }

        var attachedToBlockObject = oldSyncJoint.attachedTo.Value.GetComponent<BlockObject>();

        if (!attachedToBlockObject)
        {
            Debug.LogError("attachedTo blockObject does not exist in ServerDisconnectJoint");
            return;
        }

        if (!attachedToBlockObject.ConnectedToSelf.ContainsKey(oldSyncJoint.otherBearingCoord))
        {
            Debug.LogError("ConnectedToSelf does not contain key.");
            return;
        }

        attachedToBlockObject.ConnectedToSelf.Remove(oldSyncJoint.otherBearingCoord);
        attachedToBlockObject.ProcessConnectedToSelfChanges();
        BlockUtility.GetRootBlockObject(this).ServerUpdateValidOrientationFromRoot();
    }

    private void RemoveBlockFromMesh(Vector3Int coord)
    {
        if (!MeshBlocks.ContainsKey(coord))
        {
            Debug.LogError("Attempted to remove block which does not exist in _meshBlocks.");
            return;
        }

        int vStart = MeshBlocks[coord].positionInList * 6 * 4;
        int tStart = MeshBlocks[coord].positionInList * 6 * 6;

        int vLastStart = MeshBlocks.Count * 6 * 4 - (6 * 4);
        int tLastStart = MeshBlocks.Count * 6 * 6 - (6 * 6);

        //move the last block mesh into where the block you want to remove is
        for (int i = 0; i < 6 * 6; i++)
        {
            _triangles[tStart + i] = _triangles[tLastStart + i] - ((MeshBlocks.Count - 1 - MeshBlocks[coord].positionInList) * 6 * 4);
        }

        _triangles.RemoveRange(tLastStart, 6 * 6);

        for (int i = 0; i < 6 * 4; i++)
        {
            _vertices[vStart + i] = _vertices[vLastStart + i];
            _uv[vStart + i] = _uv[vLastStart + i];
            _uv2[vStart + i] = _uv2[vLastStart + i];
        }

        //remove the last block because it has been moved somewhere else
        _vertices.RemoveRange(vLastStart, 6 * 4);
        _uv.RemoveRange(vLastStart, 6 * 4);
        _uv2.RemoveRange(vLastStart, 6 * 4);

        if (MeshBlocks[coord].blockGameObject)
        {
            Destroy(MeshBlocks[coord].material);
            Destroy(MeshBlocks[coord].blockGameObject);
            _blockGameObjects.Remove(coord);
        }

        //find the last block. its not a list, so you must loop through
        //this block is no longer the last block, but the positionInList has not been updated yet, so we can find it like this
        int max = -1;
        Vector3Int key = Vector3Int.zero;
        foreach (var entry in MeshBlocks)
        {
            if (entry.Value.positionInList > max)
            {
                max = entry.Value.positionInList;
                key = entry.Key;
            }
        }

        //update the positionInList for the block which moved
        MeshBlocks[key].positionInList = MeshBlocks[coord].positionInList;

        //finally remove the block from the mesh blocks
        MeshBlocks.Remove(coord);
    }

    private void IncrementWholeEffect(BlockBreakHand.BreakMode mode, Vector3 contactPoint)
    {
        if (_activeWholeEffect == null)
        {
            _activeWholeEffect = new WholeEffect(mode);
        }

        if (_activeWholeEffect.mode != mode)
        {
            return;
        }

        const float ActiveTime = 1f;
        _activeWholeEffect.progress += Time.deltaTime / ActiveTime;
        _activeWholeEffect.framesSinceLastDamage = 0;

        if (_activeWholeEffect.progress > 1 && !_activeWholeEffect.activated)
        {
            switch (_activeWholeEffect.mode)
            {
                case BlockBreakHand.BreakMode.Whole:
                    if (_activeDialog)
                    {
                        Destroy(_activeDialog);
                        _activeDialog = null;
                    }

                    _dialogLocalPositionRoot = transform.InverseTransformPoint(contactPoint);
                    _activeDialog = Instantiate(breakWholeDialogPrefab);
                    UpdateDialogPosition();
                    _activeDialog.transform.GetChild(0).GetComponent<HoverButton>().action.AddListener(CloseDialog);
                    _activeDialog.transform.GetChild(1).GetComponent<HoverButton>().action.AddListener(Delete);
                    break;
                case BlockBreakHand.BreakMode.Duplicate:
                    if (NetworkClient.connection != null && NetworkClient.connection.identity)
                    {
                        CmdDuplicate(NetworkClient.connection.identity.netId);
                    }

                    break;
                case BlockBreakHand.BreakMode.Save:
                    if (_activeDialog)
                    {
                        Destroy(_activeDialog);
                        _activeDialog = null;
                    }

                    _dialogLocalPositionRoot = transform.InverseTransformPoint(contactPoint);
                    _activeDialog = Instantiate(saveDialogPrefab);
                    UpdateDialogPosition();
                    _activeDialog.transform.GetChild(0).GetComponent<HoverButton>().action.AddListener(CloseDialog);
                    _activeDialog.transform.GetChild(1).GetComponent<HoverButton>().action.AddListener(Save);

                    break;
                default:
                    Debug.LogError("Unknown mode: " + _activeWholeEffect.mode);
                    break;
            }

            _activeWholeEffect.activated = true;
        }
    }

    private void DamageBlock(Vector3Int coord)
    {
        if (!Blocks.ContainsKey(coord) || !MeshBlocks.ContainsKey(coord))
        {
            return;
        }

        var meshBlock = MeshBlocks[coord];

        //if health is already below 0, then we have probably already sent a command to remove the block
        //we don't want to send a duplicate command, so just return
        if (meshBlock.health < 0)
        {
            return;
        }

        const float BreakTime = 0.225f;
        meshBlock.health -= Time.deltaTime / BreakTime;
        meshBlock.framesSinceLastDamage = 0;

        if (meshBlock.health < 0)
        {
            _damagedBlocks.Remove(coord);

            var nId = uint.MaxValue;
            if (NetworkClient.connection != null && NetworkClient.connection.identity)
            {
                nId = NetworkClient.connection.identity.netId;
            }

            //client side prediction - remove block locally
            if (MeshBlocks.ContainsKey(coord))
            {
                _blockChanges.Enqueue((coord, new SyncBlock(), SyncDictionary<Vector3Int, SyncBlock>.Operation.OP_REMOVE));
            }

            //no need to do client side prediction if you are a host/server
            //I don't bother to check for this in other client side prediction code bits because it wouldn't save any performance in most cases
            //here, however, GetFilledBlocksGroups is not simple, and it will be called again by the server
            if (isClient && !isServer)
            {
                //client side prediction - determine which blocks will be removed and when the first split structure blockObject is spawned, apply the prediction
                var (filledBlocksGroups, indexOfLargest) = BlockUtility.GetFilledBlocksGroups(coord, Blocks);
                if (indexOfLargest >= 0)
                {
                    for (int i = 0; i < filledBlocksGroups.Count; i++)
                    {
                        if (filledBlocksGroups[i].Count == 1)
                        {
                            Vector3Int onlyBlockCoord = Vector3Int.zero;
                            SyncBlock onlyBlock = new SyncBlock();
                            foreach (var pair in filledBlocksGroups[i])
                            {
                                onlyBlockCoord = pair.Key;
                                onlyBlock = filledBlocksGroups[i][pair.Key];
                                break;
                            }

                            if (onlyBlock.name == "bearing")
                            {
                                _blockChanges.Enqueue((onlyBlockCoord, new SyncBlock(), SyncDictionary<Vector3Int, SyncBlock>.Operation.OP_REMOVE));
                            }
                        }

                        if (i != indexOfLargest)
                        {
                            foreach (var pair in filledBlocksGroups[i])
                            {
                                _toBeRemovedClientSidePrediction.Enqueue(pair.Key);
                            }
                        }
                    }
                }
            }

            CmdRemoveBlock(coord, nId);

            return;
        }

        if (!_damagedBlocks.Contains(coord))
        {
            _damagedBlocks.Add(coord);
        }
    }

    private void ApplyToBeRemovedClientSidePrediction()
    {
        if (_toBeRemovedClientSidePrediction.Count == 0)
        {
            return;
        }

        while (_toBeRemovedClientSidePrediction.Count > 0)
        {
            var coord = _toBeRemovedClientSidePrediction.Dequeue();
            if (MeshBlocks.ContainsKey(coord))
            {
                _blockChanges.Enqueue((coord, new SyncBlock(), SyncDictionary<Vector3Int, SyncBlock>.Operation.OP_REMOVE));
            }
        }

        ProcessBlockChanges();
    }

    private void UpdateWholeEffects()
    {
        if (_syncJoint.connected) //only the root blockObject is responsible for whole block effects
        {
            return;
        }

        ApplyWholeEffectMaterialProperties(Color.red, 0);


        if (_activeWholeEffect == null)
        {
            return;
        }

        _activeWholeEffect.framesSinceLastDamage++;
        if (_activeWholeEffect.framesSinceLastDamage >= 3)
        {
            _activeWholeEffect.progress = 0;
            _activeWholeEffect = null;
            return;
        }

        var color = Color.red;
        switch (_activeWholeEffect.mode)
        {
            case BlockBreakHand.BreakMode.Whole:
                color = Color.red;
                break;
            case BlockBreakHand.BreakMode.Duplicate:
                color = Color.blue;
                break;
            case BlockBreakHand.BreakMode.Save:
                color = Color.yellow;
                break;
            default:
                Debug.LogError("Unknown case: " + _activeWholeEffect.mode);
                break;
        }

        ApplyWholeEffectMaterialProperties(color, _activeWholeEffect.progress);
    }

    private void ApplyWholeEffectMaterialProperties(Color color, float progress)
    {
        blockObjectMaterial.SetFloat(EffectProgress, Mathf.Clamp01(progress));
        blockObjectMaterial.SetColor(EffectColor, color);
        foreach (var coord in _blockGameObjects)
        {
            MeshBlocks[coord].material.SetFloat(EffectProgress, 1 - Mathf.Clamp01(progress));
            MeshBlocks[coord].material.SetColor(EffectColor, color);
        }

        foreach (var pair in ConnectedToSelf)
        {
            if (pair.Value.Value)
            {
                pair.Value.Value.GetComponent<BlockObject>().ApplyWholeEffectMaterialProperties(color, progress);
            }
        }
    }

    private void UpdateDamagedBlocks()
    {
        var damagedBlocksForShader = new Vector4[10];
        var noLongerDamagedBlocks = new Queue<Vector3Int>();

        int i = 0;
        foreach (var block in _damagedBlocks)
        {
            if (MeshBlocks.TryGetValue(block, out var meshBlock))
            {
                meshBlock.framesSinceLastDamage++;
                if (meshBlock.framesSinceLastDamage >= 3)
                {
                    meshBlock.health = 1;
                    noLongerDamagedBlocks.Enqueue(block);
                }

                if (meshBlock.material)
                {
                    meshBlock.material.SetFloat(Health, meshBlock.health);
                }
                else
                {
                    if (i < damagedBlocksForShader.Length)
                    {
                        damagedBlocksForShader[i] = new Vector4(block.x, block.y, block.z, meshBlock.health);
                        i++;
                    }
                }
            }
        }

        while (noLongerDamagedBlocks.Count > 0)
        {
            _damagedBlocks.Remove(noLongerDamagedBlocks.Dequeue());
        }

        blockObjectMaterial.SetVectorArray("damagedBlocks", damagedBlocksForShader);
        blockObjectMaterial.SetInt("numDamagedBlocks", i);
    }

    private void UpdateMesh()
    {
        //first set triangles to empty so we can update the vertices without triangles referencing (now) invalid vertices
        _mesh.triangles = new int[0];
        _mesh.vertices = _vertices.ToArray();
        _mesh.triangles = _triangles.ToArray();

        _mesh.SetUVs(0, _uv);
        _mesh.SetUVs(1, _uv2);

        _mesh.RecalculateNormals();
        _mesh.RecalculateTangents();
        _mesh.RecalculateBounds();

        if (MeshBlocks.Count == 0) //probably just waiting to get deleted by server
        {
            ActiveLocal = false;
            CloseDialog();
            return;
        }

        var (boxCollidersNotNeeded, mass, destroySphere) = BlockUtility.UpdateHitBox(MeshBlocks, _boxCollidersInUse, _physicsBoxesObject, _sphereObject,
            blockPhysicMaterial, spherePhysicMaterial, this);
        while (boxCollidersNotNeeded.Count > 0)
        {
            Destroy(boxCollidersNotNeeded.Dequeue());
        }

        if (destroySphere)
        {
            var s = _sphereObject.gameObject.GetComponent<SphereCollider>();
            if (s)
            {
                Destroy(s);
            }
        }

        if (MeshBlocks.Count == 1 && GetOnlyMeshBlock().blockGameObjectPrefab)
        {
            _transparentCube.transform.localPosition = GetOnlyMeshBlockCoord();
            _transparentCube.SetActive(true);
            if (!_transparentCubeCollider)
            {
                _transparentCubeCollider = _transparentCube.AddComponent<BoxCollider>();
                _transparentCubeCollider.material = blockPhysicMaterial;
            }
        }
        else
        {
            _transparentCube.SetActive(false);
            if (_transparentCubeCollider)
            {
                Destroy(_transparentCubeCollider);
                _transparentCubeCollider = null;
            }
        }

        _body.mass = mass;
        _resetCoM = true;

        //if any blocks are added or removed, close the save dialog if it is up
        CloseDialog();
    }

    private void CreateInitialMesh()
    {
        foreach (var pair in Blocks)
        {
            AddBlockToMesh(pair.Value.name, pair.Key, BlockUtility.ByteToQuaternion(pair.Value.rotation));
        }

        UpdateMesh();
    }

    private void ConnectInitialJoints()
    {
        ConnectJoint(_syncJoint);
        foreach (var pair in ConnectedToSelf)
        {
            var otherIdentity = pair.Value;

            if (!otherIdentity.Value)
            {
                continue;
            }

            var otherBlockObject = otherIdentity.Value.GetComponent<BlockObject>();

            if (!otherBlockObject)
            {
                continue;
            }

            otherBlockObject.ConnectJoint(otherBlockObject._syncJoint);
        }
    }

    private Vector3Int GetOnlyBlockCoord()
    {
        if (Blocks.Count != 1)
        {
            Debug.LogError("GetOnlyBlockCoord is only valid when there is only 1 block");
        }

        foreach (var block in Blocks)
        {
            return block.Key;
        }

        return Vector3Int.zero;
    }

    private Block GetOnlyBlock()
    {
        if (Blocks.Count != 1)
        {
            Debug.LogError("GetOnlyBlock is only valid when there is only 1 block");
        }

        return global::Blocks.GetBlock(Blocks[GetOnlyBlockCoord()].name);
    }

    private Vector3Int GetOnlyMeshBlockCoord()
    {
        if (MeshBlocks.Count != 1)
        {
            Debug.LogError("GetOnlyMeshBlockCoord is only valid when there is only 1 mesh block");
        }

        foreach (var block in MeshBlocks)
        {
            return block.Key;
        }

        return Vector3Int.zero;
    }

    private Block GetOnlyMeshBlock()
    {
        if (MeshBlocks.Count != 1)
        {
            Debug.LogError("GetOnlyMeshBlock is only valid when there is only 1 mesh block");
        }

        return global::Blocks.GetBlock(MeshBlocks[GetOnlyMeshBlockCoord()].name);
    }

    private Quaternion GetTargetRotation(Quaternion blockRotation, Vector3Int coord, Block.BlockType blockType)
    {
        if (blockType == Block.BlockType.SurfaceMounted)
        {
            List<Vector3> upCheckAxes = new List<Vector3>();
            for (var i = 0; i < 6; i++)
            {
                Vector3Int testBlock = coord + BlockUtility.Directions[i];
                if (MeshBlocks.TryGetValue(testBlock, out MeshBlock block))
                {
                    if (!block.blockGameObject)
                    {
                        upCheckAxes.Add(-BlockUtility.Directions[i]);
                    }
                }
            }

            Quaternion rotationLocalToArea = Quaternion.Inverse(transform.rotation) * blockRotation;
            Vector3 up = BlockUtility.ClosestToAxis(rotationLocalToArea, Vector3.up, upCheckAxes.ToArray());
            Vector3[] forwardCheckAxes = new Vector3[4];
            int n = 0;
            Vector3Int upInt = Vector3Int.RoundToInt(up);
            for (int i = 0; i < BlockUtility.Directions.Length; i++)
            {
                if (!(BlockUtility.Directions[i] == upInt || BlockUtility.Directions[i] == -upInt))
                {
                    forwardCheckAxes[n] = BlockUtility.Directions[i];
                    n++;
                }
            }

            Vector3 forward = BlockUtility.ClosestToAxis(rotationLocalToArea, Vector3.forward, forwardCheckAxes);
            return transform.rotation * Quaternion.LookRotation(forward, up);
        }
        else
        {
            return transform.rotation * BlockUtility.SnapToNearestRightAngle(Quaternion.Inverse(transform.rotation) * blockRotation);
        }
    }

    [Command]
    private void CmdDuplicate(uint ownerId)
    {
        var blockObjectGameObject = Instantiate(StaticPrefabs.instance.blockObjectPrefab, new Vector3(0, 1.5f, 0), Quaternion.identity);
        var blockObject = blockObjectGameObject.GetComponent<BlockObject>();
        foreach (var pair in Blocks)
        {
            blockObject.Blocks.Add(pair);
        }

        NetworkServer.Spawn(blockObjectGameObject);
        blockObjectGameObject.GetComponent<AutoAuthority>().ServerForceNewOwner(ownerId, NetworkTime.time, false);
    }

    private void UpdateDialogPosition()
    {
        if (!_activeDialog)
        {
            return;
        }

        _activeDialog.transform.position = transform.TransformPoint(_dialogLocalPositionRoot) + new Vector3(0, 0.1f, 0);

        var forwardFlat = _activeDialog.transform.position - InputManager.Hands.head.position;
        forwardFlat.y = 0;
        _activeDialog.transform.rotation = Quaternion.LookRotation(forwardFlat);
    }

    private void CloseDialog()
    {
        var root = BlockUtility.GetRootBlockObject(this);
        if (_activeDialog)
        {
            Destroy(_activeDialog);
            _activeDialog = null;
        }

        if (root._activeDialog)
        {
            Destroy(root._activeDialog);
            root._activeDialog = null;
        }
    }

    private void CheckPotentialBlocksParent()
    {
        for (int i = _potentialBlocksParent.childCount - 1; i >= 0; i--)
        {
            var childBlockObject = _potentialBlocksParent.GetChild(i).GetComponent<BlockObject>();
            if (!childBlockObject || Time.time - childBlockObject._lastTouchingHandTime > 1f)
            {
                _potentialBlocksParent.GetChild(i).parent = null;
            }
        }

        if (_potentialBlocksParent.childCount > 0)
        {
            _autoAuthority.SetKinematicLocalForOneFrame();
        }
    }

    private void CheckForProblems()
    {
        //these client side-prediction fixes can be tested by just commenting out the command but still running the client side prediction and see how it recovers
        //also try commenting out different combinations of commands. one at a time, all, etc.

        // TODO: this will check for things that should be connected but are not, and it will connect them.
        //       it does not check if something is connected but should not be. for that case, you would need a timer on when the joint was connected
        //       so a client side prediction joint would not be removed too quickly

        // TODO: check that Blocks matches _meshBlocks. same as joints, it would need a timer to client side prediction is not overridden

        // TODO: in addition to these checks, each time a client side prediction happens, it should start a coroutine to check if the server agrees
        //       after ~1 second

        // TODO: check client side prediction for disabled gameObject. blockObject can disable itself if remove block is called with a single block

        //are there any other client side predictions should should be checked that I forgot?

        if (Time.time > _nextCheckBlockObjectForProblemsTime)
        {
            _nextCheckBlockObjectForProblemsTime = Time.time + 2f + Random.value * 2f;
            var problem = BlockObjectNullTest.CheckBlockObjectForProblems(this);
            if (problem.syncConnectionProblem)
            {
                //TODO: can anything be done if this problem is detected? Does it matter if this happens on a client vs on the server?
                //      maybe if this happens on the server, it should just delete it. if it happens on a client but not the server, what do we do?
                Debug.LogError("Found sync connection problem with blockObject: " + gameObject.name);
            }

            if (problem.hingeJointProblem)
            {
                Debug.LogError("Found hinge joint problem with blockObject: " + gameObject.name + ". Attempting to correct.");
                var blockObjects = BlockUtility.GetBlockObjectsFromRoot(BlockUtility.GetRootBlockObject(this));
                for (int i = 0; i < blockObjects.Count; i++)
                {
                    blockObjects[i].ConnectInitialJoints(); //this will not edit any existing joints which are correct
                }
            }
        }
    }

    private void Save()
    {
        var root = BlockUtility.GetRootBlockObject(this);

        Vector3? saveDialogPos = null;
        if (_activeDialog)
        {
            saveDialogPos = _activeDialog.transform.position;
        }

        if (root._activeDialog)
        {
            saveDialogPos = _activeDialog.transform.position;
        }

        var dataString = BlockUtility.SerializeBlocksFromRoot(root, saveDialogPos);

        if (!string.IsNullOrEmpty(dataString))
        {
            Debug.LogWarning(dataString);

            var data = BlockUtility.DeserializeBlocks(dataString);
            if (data != null)
            {
                //do something with the dataString
                BlockObjectSpawner.Instance.SpawnFromString(dataString);
            }
            else
            {
                Debug.LogError("Could not parse the newly serialized blockObject");
            }
        }
        else
        {
            Debug.LogError("Could not serialize blockObject (returned dataString is empty)");
        }

        CloseDialog();
    }

    private void Delete()
    {
        CloseDialog();
        _autoAuthority.CmdDestroy();
    }

    [Server]
    private void ServerUpdateValidOrientationFromRoot()
    {
        _updateValidOrientationFromRootFrame = Time.frameCount;

        var toUpdate = new Queue<BlockObject>();
        toUpdate.Enqueue(this);

        while (toUpdate.Count > 0)
        {
            var next = toUpdate.Dequeue();
            if (next.SyncJoint.attachedTo != null && next.SyncJoint.attachedTo.Value)
            {
                next._validLocalPosition = next.SyncJoint.attachedTo.Value.transform.InverseTransformPoint(next.transform.position);
                next._validLocalRotation = Quaternion.Inverse(next.SyncJoint.attachedTo.Value.transform.rotation) * next.transform.rotation;
            }

            foreach (var pair in next.ConnectedToSelf)
            {
                if (pair.Value != null && pair.Value.Value)
                {
                    var childBlockObject = pair.Value.Value.GetComponent<BlockObject>();
                    toUpdate.Enqueue(childBlockObject);
                }
            }
        }
    }

    [Server]
    public static void ServerDestroyFromBlockObjectRoot(BlockObject rootObject)
    {
        ServerDisconnectAllJointsFromRoot(rootObject);

        var blockObjects = BlockUtility.GetBlockObjectsFromRoot(rootObject);

        for (int i = 0; i < blockObjects.Count; i++)
        {
            blockObjects[i].AutoAuthority.ServerForceNewOwner(uint.MaxValue, NetworkTime.time, true);
            blockObjects[i].gameObject.SetActive(false);
            blockObjects[i].GetComponent<SmoothSyncMirror>().clearBuffer();
            NetworkServer.Destroy(blockObjects[i].gameObject);
        }
    }

    [Server]
    private static void ServerDisconnectAllJointsFromRoot(BlockObject rootObject)
    {
        foreach (var pair in rootObject.ConnectedToSelf.ToList())
        {
            if (pair.Value != null && pair.Value.Value)
            {
                var blockObject = pair.Value.Value.GetComponent<BlockObject>();
                if (blockObject)
                {
                    ServerDisconnectAllJointsFromRoot(blockObject);
                }
                else
                {
                    Debug.LogError("Could not find blockObject for disconnect");
                }
            }
            else
            {
                Debug.LogError("Could not find identity for disconnect");
            }
        }

        if (rootObject.SyncJoint.connected)
        {
            rootObject.ServerDisconnectJoint();
        }
    }

    private void SetActiveLocal(bool isActive)
    {
        _activeLocal = isActive;
        _meshObject.gameObject.SetActive(_activeLocal);
        _physicsBoxesObject.gameObject.SetActive(_activeLocal);
        _sphereObject.gameObject.SetActive(_activeLocal);
        _blockGameObjectsParent.gameObject.SetActive(_activeLocal);
    }
}