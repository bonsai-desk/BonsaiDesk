using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class NetworkBlockSpawn : NetworkBehaviour
{
    public GameObject spawnObjectPrefab;

    private const float SpawnCooldown = 0.5f;

    private float _readyToSpawnTime = 0f;

    private void Update()
    {
        if (!(isClient && NetworkClient.connection != null && NetworkClient.connection.identity))
        {
            return;
        }

        const float halfBlock = BlockObject.CubeScale / 2f;
        if (!Physics.CheckBox(transform.position, new Vector3(halfBlock, halfBlock, halfBlock), transform.rotation))
        {
            _readyToSpawnTime += Time.deltaTime;

            if (_readyToSpawnTime > SpawnCooldown)
            {
                _readyToSpawnTime = 0f;
                if (NetworkClient.connection != null && NetworkClient.connection.identity)
                {
                    CmdSpawnObject(transform.position, transform.rotation, NetworkClient.connection.identity.netId);
                }
            }
        }
        else
        {
            _readyToSpawnTime = 0f;
        }
    }

    [Command(ignoreAuthority = true)]
    private void CmdSpawnObject(Vector3 position, Quaternion rotation, uint ownerId)
    {
        const float halfBlock = BlockObject.CubeScale / 2f;
        if (!Physics.CheckBox(position, new Vector3(halfBlock, halfBlock, halfBlock), rotation))
        {
            var spawnedObject = Instantiate(spawnObjectPrefab, position, rotation);
            spawnedObject.GetComponent<BlockObject>().Blocks.Add(Vector3Int.zero, new SyncBlock("wood1", 0));
            NetworkServer.Spawn(spawnedObject);
            spawnedObject.GetComponent<AutoAuthority>().ServerForceNewOwner(ownerId, NetworkTime.time, false);
        }
    }
}