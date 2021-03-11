using UnityEngine;

public class StaticPrefabs : MonoBehaviour
{
    public static StaticPrefabs instance;

    public GameObject blockAreaPrefab;

    // Start is called before the first frame update
    private void Start()
    {
        if (instance == null)
            instance = this;
    }
}