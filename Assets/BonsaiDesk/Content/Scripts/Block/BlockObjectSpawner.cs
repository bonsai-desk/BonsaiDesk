using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Mirror;
using UnityEngine;

public class BlockObjectSpawner : NetworkBehaviour
{
    public static BlockObjectSpawner Instance;
    
    private const int ChunkSize = 1024; //smaller than UDP ethernet packet

    //just used on server
    private readonly Dictionary<string, byte[]> _partialMessages = new Dictionary<string, byte[]>();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnStartServer()
    {
        _partialMessages.Clear();
    }

    public override void OnStartClient()
    {
        var files = BlockObjectFileReader.GetBlockObjectFiles();

        if (files != null && files.Length > 0)
        {
            var blocksString = BlockObjectFileReader.LoadFile(files[0].fileName);
            if (!string.IsNullOrEmpty(blocksString))
            {
                BlockObjectSpawner.Instance.SpawnFromString(blocksString);
            }
        }
    }

    public void SpawnFromString(string blocksString)
    {
        if (isServer)
        {
            if (!NetworkServer.active)
            {
                Debug.LogError("Cannot call SpawnFromString. Server is not active.");
                return;
            }
            
            ServerSpawnFromString(blocksString);
        }
        else if (isClient)
        {
            if (!NetworkClient.active)
            {
                Debug.LogError("Cannot call SpawnFromString. Client is not active.");
                return;
            }

            ClientSpawnFromString(blocksString);
        }
    }

    private static byte[] GetBytes(string str)
    {
        byte[] bytes = new byte[str.Length * sizeof(char)];
        System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
        return bytes;
    }

    // Do NOT use on arbitrary bytes; only use on GetBytes's output on the SAME system
    private static string GetString(byte[] bytes)
    {
        char[] chars = new char[bytes.Length / sizeof(char)];
        System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
        return new string(chars);
    }

    [Client]
    private void ClientSpawnFromString(string blocksString)
    {
        var bytes = GetBytes(blocksString);
        var guid = Guid.NewGuid().ToString();
        for (int i = 0; i < bytes.Length; i += ChunkSize)
        {
            var chunkSize = Math.Min(ChunkSize, bytes.Length - i);
            var chunk = new byte[chunkSize];
            Array.Copy(bytes, i, chunk, 0, chunkSize);
            var isEnd = i + ChunkSize >= bytes.Length;
            CmdSpawnFromString(chunk, i, bytes.Length, isEnd, guid);
        }
    }

    [Command(ignoreAuthority = true)]
    private void CmdSpawnFromString(byte[] chunk, int offset, int totalSize, bool isEnd, string guid)
    {
        if (!_partialMessages.ContainsKey(guid))
        {
            _partialMessages.Add(guid, new byte[totalSize]);
            StartCoroutine(MessageTimeout(guid));
        }

        var bytes = _partialMessages[guid];
        for (int i = 0; i < chunk.Length; i++)
        {
            if (offset + i >= bytes.Length)
            {
                Debug.LogError($"offset + i {offset + i} is greater than or equal to bytes length {bytes.Length}");
                return;
            }

            bytes[offset + i] = chunk[i];
        }

        if (isEnd)
        {
            var finalString = GetString(bytes);
            _partialMessages.Remove(guid);
            ServerSpawnFromString(finalString);
        }
    }

    private IEnumerator MessageTimeout(string guid)
    {
        yield return new WaitForSeconds(10f);
        if (_partialMessages.ContainsKey(guid))
        {
            Debug.LogError("Message timeout: " + guid);
            _partialMessages.Remove(guid);
        }
    }

    [Server]
    private void ServerSpawnFromString(string blocksString)
    {
        var data = BlockUtility.DeserializeBlocks(blocksString);

        if (data == null)
        {
            Debug.LogError("data is null");
            return;
        }

        var idToBlockObject = new Dictionary<int, BlockObject>();

        var n = 0;
        foreach (var pair in data.entriesByAttachedTo)
        {
            var list = pair.Value;
            for (int i = 0; i < list.Count; i++)
            {
                var entry = list[i];

                var spawnPosition = new Vector3(0, -5f, 0);
                var spawnRotation = Quaternion.identity;
                if (entry.attachedTo < 0)
                {
                    spawnRotation = entry.rootRotation;
                }
                else
                {
                    var parent = idToBlockObject[entry.attachedTo].transform;
                    spawnPosition = parent.TransformPoint(entry.localPosition);
                    spawnRotation = parent.rotation * entry.localRotation;
                }

                var blockObjectGameObject = Instantiate(StaticPrefabs.instance.blockObjectPrefab, spawnPosition, spawnRotation);
                var blockObject = blockObjectGameObject.GetComponent<BlockObject>();
                while (entry.blocks.Count > 0)
                {
                    blockObject.Blocks.Add(entry.blocks.Dequeue());
                }

                NetworkServer.Spawn(blockObjectGameObject);

                if (entry.attachedTo >= 0)
                {
                    var netIdRef = new NetworkIdentityReference(idToBlockObject[entry.attachedTo].GetComponent<NetworkIdentity>());
                    var syncJoint = new SyncJoint(netIdRef, entry.jointLocalRotation, entry.jointBearingLocalRotation, entry.jointAttachedToMeAtCoord,
                        entry.jointOtherBearingCoord);
                    blockObject.ServerConnectJoint(syncJoint);
                }

                blockObjectGameObject.GetComponent<AutoAuthority>().ServerForceNewOwner(uint.MaxValue, NetworkTime.time, false);

                idToBlockObject.Add(entry.id, blockObject);

                if (n == data.entriesByAttachedTo.Count - 1 && i == list.Count - 1)
                {
                    blockObject.ServerTeleportToDeskSurface();
                }
            }

            n++;
        }
    }
}