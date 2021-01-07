using System.Collections;
using UnityEngine;

public class BlockSpawner : MonoBehaviour
{
    private bool objectPresent = false;
    private bool aboutToSpawnObject = false;

    public int _blockId = -1;

    public int BlockId
    {
        get
        {
            return _blockId;
        }
        set
        {
            _blockId = value;
            // changeBlockId(value);
        }
    }

    private Coroutine coroutine;

    // BlockArea mostRecentBlock;

    private void Update()
    {
        if (objectPresent)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                aboutToSpawnObject = false;
            }
        }
        if (!objectPresent && !aboutToSpawnObject && BlockId != -1)
        {
            aboutToSpawnObject = true;
            coroutine = StartCoroutine(SpawnObject());
        }
        objectPresent = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        objectPresent = true;
    }

    private void OnTriggerStay(Collider other)
    {
        objectPresent = true;
    }

    private void InstantiateObject()
    {
        // NetworkVRPlayer.self.CmdSpawnBlock(transform.position, transform.rotation, BlockId);

        // GameObject newObject = Instantiate(blockAreaPrefab.GetPrefab());
        // newObject.GetComponent<BlockArea>().startBlockId = blockId;
        // newObject.transform.position = transform.position;
        // newObject.transform.rotation = transform.rotation;

        aboutToSpawnObject = false;
        // mostRecentBlock = newObject.GetComponent<BlockArea>();
        // return newObject;
    }

    private IEnumerator SpawnObject()
    {
        yield return new WaitForSeconds(0.35f);
        if (aboutToSpawnObject)
            InstantiateObject();
    }

    // void OnEnable()
    // {
    //     if (blockAreaPrefab.GetPrefab() != null && parent != null && blockId != -1)
    //         instantiateObject();
    // }

    // void changeBlockId(int blockId)
    // {
    //     float s = BlockArea.cubeScale / 2f;
    //     Vector3 size = new Vector3(s, s, s);
    //     var hits = Physics.OverlapBox(transform.position, size, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
    //     if (hits.Length == 0)
    //         instantiateObject();
    //     foreach (var hit in hits)
    //     {
    //         if (hit.transform.parent != null && hit.transform.parent.gameObject == mostRecentBlock.gameObject)
    //         {
    //             if (mostRecentBlock != null && mostRecentBlock.blocks.Count == 1)
    //             {
    //                 Destroy(mostRecentBlock.gameObject);
    //                 if (blockId != -1)
    //                 {
    //                     GameObject ob = instantiateObject();
    //                     Vector3 pos = mostRecentBlock.gameObject.transform.position;
    //                     Quaternion rot = mostRecentBlock.gameObject.transform.rotation;
    //                     ob.transform.position = pos;
    //                     ob.transform.rotation = rot;
    //                 }
    //             }
    //             break;
    //         }
    //     }
    // }
}