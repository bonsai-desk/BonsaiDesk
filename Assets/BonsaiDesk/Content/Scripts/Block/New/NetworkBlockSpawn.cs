using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
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

    private const float SpawnCooldown = 0.5f;

    private float _readyToSpawnTime = 0f;

    private NetworkIdentity lastSpawned = null;

    private string _spawnBlockName = "";

    private int _defaultLayerMask;

    const float HalfBlock = BlockObject.CubeScale / 2f;
    private readonly Vector3 _halfExtends = new Vector3(HalfBlock, HalfBlock, HalfBlock);

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

    private void Start()
    {
        int myLayer = LayerMask.NameToLayer("Default");
        int layerMask = 0;
        for (int i = 0; i < 32; i++)
        {
            if (!Physics.GetIgnoreLayerCollision(myLayer, i))
            {
                layerMask |= 1 << i;
            }
        }

        _defaultLayerMask = layerMask;
    }

    private void Update()
    {
        if (!(isClient && NetworkClient.connection != null && NetworkClient.connection.identity))
        {
            return;
        }

        if (!string.IsNullOrEmpty(_spawnBlockName) && !Physics.CheckBox(transform.position, _halfExtends, transform.rotation, _defaultLayerMask))
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

    [Command(ignoreAuthority = true)]
    private void CmdSpawnObject(string blockName, Vector3 position, Quaternion rotation, uint ownerId)
    {
        if (string.IsNullOrEmpty(blockName))
        {
            return;
        }
        
        const float halfBlock = BlockObject.CubeScale / 2f;
        if (!Physics.CheckBox(position, new Vector3(halfBlock, halfBlock, halfBlock), rotation, _defaultLayerMask))
        {
            var spawnedObject = Instantiate(spawnObjectPrefab, position, rotation);
            spawnedObject.GetComponent<BlockObject>().Blocks.Add(Vector3Int.zero, new SyncBlock(blockName, 0));
            NetworkServer.Spawn(spawnedObject);
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
        lastSpawned = blockIdentity;
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
        if (!lastSpawned)
        {
            return;
        }

        var hits = Physics.OverlapBox(transform.position, _halfExtends, transform.rotation);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].attachedRigidbody)
            {
                var identity = hits[i].attachedRigidbody.GetComponent<NetworkIdentity>();
                if (identity && identity == lastSpawned)
                {
                    if (lastSpawned.GetComponent<BlockObject>().Blocks.Count == 1)
                    {
                        lastSpawned.GetComponent<AutoAuthority>().CmdDestroy();
                    }

                    lastSpawned = null;
                    return;
                }
            }
        }
    }
}