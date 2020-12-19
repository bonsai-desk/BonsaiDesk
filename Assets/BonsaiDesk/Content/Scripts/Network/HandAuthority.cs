using Mirror;
using UnityEngine;

public class HandAuthority : MonoBehaviour
{
    // Start is called before the first frame update
    //   private void Start()
    //   {
    //   }
    //
    //   // Update is called once per frame
    //   private void Update()
    //   {
    //   }

    private void OnCollisionEnter(Collision collision)
    {
        NetworkIdentity objectNetId = collision.gameObject.GetComponent<NetworkIdentity>();
        if (objectNetId != null && collision.gameObject.GetComponent<Rigidbody>() != null)
        {
            if (!NetworkVRPlayer.self.netIdentity.connectionToServer.clientOwnedObjects.Contains(objectNetId))
                NetworkVRPlayer.self.CmdReceiveOwnershipOfObject(objectNetId);
        }
    }
}