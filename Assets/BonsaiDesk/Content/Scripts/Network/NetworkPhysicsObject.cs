using Mirror;
using UnityEngine;

public class NetworkPhysicsObject : MonoBehaviour
{
    private NetworkIdentity networkIdentity;
    private Rigidbody body;

    // Start is called before the first frame update
    private void Start()
    {
        networkIdentity = GetComponent<NetworkIdentity>();
        body = GetComponent<Rigidbody>();

        updateBodyKinematic();
    }

    // Update is called once per frame
    private void Update()
    {
        updateBodyKinematic();
    }

    private void updateBodyKinematic()
    {
        bool kinematic = true;
        if (networkIdentity.isServer && networkIdentity.connectionToClient == null)
            kinematic = false;
        if (networkIdentity.hasAuthority)
            kinematic = false;
        if (body.isKinematic != kinematic)
            body.isKinematic = kinematic;
    }
}