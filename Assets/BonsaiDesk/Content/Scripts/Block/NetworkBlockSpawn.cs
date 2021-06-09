using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Smooth;
using UnityEngine;

public class NetworkBlockSpawn : NetworkBehaviour
{
    public static NetworkBlockSpawn InstanceLeft;
    public static NetworkBlockSpawn InstanceRight;

    public enum SpawnerLocation
    {
        Left,
        Right
    }

    public SpawnerLocation spawnerLocation;

    public GameObject spawnObjectPrefab;

    private const float SpawnCooldown = 0.35f;

    private float _readyToSpawnTime = 0f;

    private NetworkIdentity _lastSpawned;

    public void SetLastSpawned(NetworkIdentity nid)
    {
        _lastSpawned = nid;
    }

    private Rigidbody _lastSpawnedBody;

    private string _spawnBlockName = "";

    const float HalfBlock = BlockObject.CubeScale / 2f;
    private readonly Vector3 _halfExtends = new Vector3(HalfBlock, HalfBlock, HalfBlock);

    private Collider[] _results = new Collider[10];

    private void Awake()
    {
        if (spawnerLocation == SpawnerLocation.Left)
        {
            InstanceLeft = this;
        }
        else
        {
            InstanceRight = this;
        }
    }

    private void Update()
    {
        if (!(isClient && NetworkClient.connection != null && NetworkClient.connection.identity))
        {
            return;
        }

        transform.GetChild(0).gameObject.SetActive(!string.IsNullOrEmpty(_spawnBlockName));

        var (foundNotLastSpawned, foundLastSpawned) = CheckBox(transform.position, transform.rotation);
        if (!foundLastSpawned)
        {
            _lastSpawned = null;
            _lastSpawnedBody = null;
        }

        var recalculateSpawnerPos = foundNotLastSpawned && !foundLastSpawned;

        var newSpawnPos = CheckRow(transform.parent.position);
        var newDistance = Vector3.Distance(newSpawnPos, transform.parent.position);

        var currentDistance = Vector3.Distance(transform.position, transform.parent.position);

        if (recalculateSpawnerPos || (newDistance < currentDistance &&
                                      (currentDistance - newDistance > 0.075f || (currentDistance - newDistance > 0.01f && newDistance < 0.01f))))
        {
            //move spawner
            var oldPosition = transform.position;
            transform.position = newSpawnPos;
            if (CheckBox(oldPosition, transform.rotation).foundLastSpawned)
            {
                _lastSpawned.transform.position = transform.position;
                _lastSpawned.transform.rotation = transform.rotation;
                _lastSpawnedBody.MovePosition(transform.position);
                _lastSpawnedBody.MoveRotation(transform.rotation);
                _lastSpawnedBody.velocity = Vector3.zero;
                _lastSpawnedBody.angularVelocity = Vector3.zero;
                _lastSpawned.GetComponent<SmoothSyncMirror>().teleportOwnedObjectFromOwner();
            }
        }

        if (!string.IsNullOrEmpty(_spawnBlockName) && !Physics.CheckBox(transform.position, _halfExtends, transform.rotation,
            BlockUtility.DefaultLayerMaskPlusNetworkHands, QueryTriggerInteraction.Ignore))
        {
            _readyToSpawnTime += Time.deltaTime;

            if (_readyToSpawnTime >= SpawnCooldown)
            {
                _readyToSpawnTime = 0f;
                if (NetworkClient.connection != null && NetworkClient.connection.identity)
                {
                    CmdSpawnObject(_spawnBlockName, transform.position, transform.rotation, NetworkClient.connection.identity.netId);
                }
            }
        }
        else
        {
            _readyToSpawnTime = 0f;
        }
    }

    private (bool foundNotLastSpawned, bool foundLastSpawned) CheckBox(Vector3 center, Quaternion rotation)
    {
        var foundNotLastSpawned = false;
        var foundLastSpawned = false;
        var num = Physics.OverlapBoxNonAlloc(center, _halfExtends, _results, rotation, BlockUtility.DefaultLayerMaskMinusPlayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < num; i++)
        {
            if (_lastSpawnedBody && _results[i].attachedRigidbody == _lastSpawnedBody)
            {
                foundLastSpawned = true;
            }
            else
            {
                foundNotLastSpawned = true;
            }
        }

        return (foundNotLastSpawned, foundLastSpawned);
    }

    private Vector3 CheckRow(Vector3 startPos)
    {
        var searchDirection = spawnerLocation == SpawnerLocation.Left ? -transform.parent.right : transform.parent.right;

        var testPos = startPos;
        var padding = 0f;
        while (true)
        {
            var (foundNotLastSpawned, foundLastSpawned) = CheckBox(testPos, transform.parent.rotation);
            if (foundNotLastSpawned)
            {
                testPos += searchDirection * (BlockObject.CubeScale * 0.25f);
                padding = BlockObject.CubeScale;
            }
            else
            {
                break;
            }
        }

        if (Mathf.Approximately(0, padding))
        {
            return testPos;
        }

        return CheckRow(testPos + searchDirection * padding);
    }

    [Command(ignoreAuthority = true)]
    private void CmdSpawnObject(string blockName, Vector3 position, Quaternion rotation, uint ownerId)
    {
        if (string.IsNullOrEmpty(blockName))
        {
            return;
        }

        const float halfBlock = BlockObject.CubeScale / 2f;
        if (!Physics.CheckBox(position, new Vector3(halfBlock, halfBlock, halfBlock), rotation, BlockUtility.DefaultLayerMaskPlusNetworkHands,
            QueryTriggerInteraction.Ignore))
        {
            var spawnedObject = Instantiate(spawnObjectPrefab, position, rotation);
            var blockObject = spawnedObject.GetComponent<BlockObject>();
            if (spawnerLocation == SpawnerLocation.Left)
            {
                blockObject.ServerSetSpawnedForClientLeft(ownerId);
            }
            else
            {
                blockObject.ServerSetSpawnedForClientRight(ownerId);
            }

            blockObject.Blocks.Add(Vector3Int.zero, new SyncBlock(blockName, 0));

            NetworkConnection conn = null;
            foreach (var pair in NetworkServer.connections)
            {
                if (pair.Value.identity && pair.Value.identity.netId == ownerId)
                {
                    conn = pair.Value;
                }
            }

            NetworkServer.Spawn(spawnedObject, conn);
            spawnedObject.GetComponent<AutoAuthority>().ServerForceNewOwner(ownerId, NetworkTime.time, false);
            foreach (var pair in NetworkServer.connections)
            {
                if (pair.Value.identity && pair.Value.identity.netId == ownerId)
                {
                    TargetSpawnedBlock(pair.Value, spawnedObject.GetComponent<NetworkIdentity>());
                }
            }
        }
    }

    [TargetRpc]
    private void TargetSpawnedBlock(NetworkConnection target, NetworkIdentity blockIdentity)
    {
        _lastSpawned = blockIdentity;
        _lastSpawnedBody = blockIdentity.GetComponent<Rigidbody>();
    }

    public void SetSpawnBlockName(string blockName)
    {
        if (blockName == _spawnBlockName) //do nothing if the block is unchanged
        {
            return;
        }

        _spawnBlockName = blockName;
        DeleteLastSpawnedIfInRange();
        if (!string.IsNullOrEmpty(blockName))
        {
            _readyToSpawnTime = SpawnCooldown;
        }
    }

    private void DeleteLastSpawnedIfInRange()
    {
        if (!_lastSpawned)
        {
            return;
        }

        var num = Physics.OverlapBoxNonAlloc(transform.position, _halfExtends, _results, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < num; i++)
        {
            if (_results[i].attachedRigidbody)
            {
                var identity = _results[i].attachedRigidbody.GetComponent<NetworkIdentity>();
                if (identity && identity == _lastSpawned)
                {
                    var lastBlockObject = _lastSpawned.GetComponent<BlockObject>();
                    if (lastBlockObject && lastBlockObject.Blocks.Count == 1)
                    {
                        lastBlockObject.ActiveLocal = false;
                        lastBlockObject.AutoAuthority.CmdDestroy();
                    }

                    _lastSpawned = null;
                    _lastSpawnedBody = null;
                    return;
                }
            }
        }
    }
}