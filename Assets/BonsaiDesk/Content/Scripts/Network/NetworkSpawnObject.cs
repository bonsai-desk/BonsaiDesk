using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class NetworkSpawnObject : NetworkBehaviour
{
    public GameObject spawnObject;
    public Transform spawnPoint;

    [Command(ignoreAuthority = true)]
    private void CmdSpawnObject(NetworkIdentity ownerIdentity)
    {
        GameObject spawnedObject = Instantiate(spawnObject, spawnPoint.position, spawnPoint.rotation);
        // NetworkServer.Spawn(spawnedObject, ownerIdentity.connectionToClient);
        NetworkServer.Spawn(spawnedObject);
    }

    public void SpawnObject()
    {
        CmdSpawnObject(NetworkClient.connection.identity);
    }
}