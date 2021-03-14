using UnityEngine;

public class StaticPrefabs : MonoBehaviour
{
    public static StaticPrefabs instance;

    public GameObject blockAreaPrefab;
    public GameObject blockObjectPrefab;

    // Start is called before the first frame update
    private void Start()
    {
        if (instance == null)
            instance = this;
    }
}