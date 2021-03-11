using Mirror;
using UnityEngine;

public class NetworkServerTest : NetworkBehaviour
{
    public GameObject blockPrefab;

    public GameObject tabletPrefab;

    // Start is called before the first frame update
    private void Start()
    {
        if (!isServer)
            return;
    }

    public override void OnStartServer()
    {
        if (!isServer)
            return;

        //for (int i = 0; i <= 2; i++)
        //{
        //    GameObject tablet = Instantiate(tabletPrefab);
        //    tablet.transform.position = new Vector3(0.4f, 0.05f + (i * 0.01f), 0.75f / 2f);
        //    tablet.GetComponent<TabletPhysics>().videoIndex = i;
        //    NetworkServer.Spawn(tablet);
        //}
    }

    // Update is called once per frame
    private void Update()
    {
        if (!isServer)
            return;

        // if (Input.GetKeyDown(KeyCode.S))
        // {
        //     GameObject block = Instantiate(blockPrefab);
        //     block.transform.position += new Vector3(0.1f, 0, 0);
        //     NetworkServer.Spawn(block);
        // }
    }
}