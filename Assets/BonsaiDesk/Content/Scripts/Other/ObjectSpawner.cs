using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    public GameObject cubePrefab;

    public void spawnObject()
    {
        // GameObject cube = Instantiate(cubePrefab);
        // cube.transform.position = createPosition.position;
        // cube.transform.rotation = createPosition.rotation;
        for (int i = 0; i < 100; i++)
            createRandomCube();
    }

    private void createRandomCube()
    {
        GameObject cube = Instantiate(cubePrefab);
        cube.transform.parent = GameObject.Find("TrashObjects").transform;
        cube.transform.rotation = Quaternion.AngleAxis(Random.value * 360, Vector3.up);
        cube.transform.position = transform.position;
        cube.transform.position += new Vector3(Random.value * 2 - 1, Random.value * 0.1f, Random.value * 1 - 0.5f);
    }
}