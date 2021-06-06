using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class BlockDeserializeTest : NetworkBehaviour
{
    [TextArea] public string blocksString;

    public override void OnStartServer()
    {
        var data = BlockUtility.DeserializeBlocks(blocksString);

        if (data == null)
        {
            Debug.LogError("data is null");
            return;
        }

        var idToBlockObject = new Dictionary<int, BlockObject>();

        foreach (var pair in data.entriesByAttachedTo)
        {
            var list = pair.Value;
            for (int i = 0; i < list.Count; i++)
            {
                var entry = list[i];

                var spawnPosition = new Vector3(0, 1.5f, 0);
                var spawnRotation = Quaternion.identity;
                if (entry.attachedTo >= 0)
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
            }
        }
    }
}