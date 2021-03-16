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
        if (!Physics.CheckBox(transform.position, new Vector3(halfBlock, halfBlock, halfBlock), Quaternion.identity))
        {
            _readyToSpawnTime += Time.deltaTime;

            if (_readyToSpawnTime > SpawnCooldown)
            {
                _readyToSpawnTime = 0f;
                CmdSpawnObject(transform.position);
            }
        }
        else
        {
            _readyToSpawnTime = 0f;
        }
    }

    [Command(ignoreAuthority = true)]
    private void CmdSpawnObject(Vector3 position)
    {
        var spawnedObject = Instantiate(spawnObjectPrefab, position, Quaternion.identity);
        spawnedObject.GetComponent<BlockObject>().Blocks.Add(Vector3Int.zero, new SyncBlock(0, 0));
        NetworkServer.Spawn(spawnedObject);
    }
}