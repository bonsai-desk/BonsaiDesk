using System.Collections;
using UnityEngine;

public class SpawnObject : MonoBehaviour
{
    public GameObject prefab;
    public Transform parent;

    private bool objectPresent = false;
    private bool aboutToSpawnObject = false;

    private void Update()
    {
        if (!objectPresent && !aboutToSpawnObject)
        {
            aboutToSpawnObject = true;
            StartCoroutine(spawnObject());
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

    private void instantiateObject()
    {
        GameObject newObject = Instantiate(prefab);
        newObject.transform.position = transform.position;
        newObject.transform.parent = parent;
        aboutToSpawnObject = false;
    }

    private IEnumerator spawnObject()
    {
        yield return new WaitForSeconds(0.35f);
        if (aboutToSpawnObject)
            instantiateObject();
    }

    private void OnEnable()
    {
        instantiateObject();
    }
}