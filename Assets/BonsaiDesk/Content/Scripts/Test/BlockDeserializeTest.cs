using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockDeserializeTest : MonoBehaviour
{
    [TextArea] public string blocksString;

    void Start()
    {
        var data = BlockUtility.DeserializeBlocks(blocksString);

        if (data == null)
        {
            Debug.LogError("aata is null");
            return;
        }

        foreach (var pair in data.entriesByAttachedTo)
        {
            var list = pair.Value;
            for (int i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                if (entry.attachedTo >= 0)
                {
                    Debug.LogWarning(entry.blocks.Count + " blocks. attached to structure with " + data.idToEntry[entry.attachedTo].blocks.Count + " blocks");
                }
                else
                {
                    Debug.LogWarning(entry.blocks.Count + " blocks");
                }
            }
        }
    }
}