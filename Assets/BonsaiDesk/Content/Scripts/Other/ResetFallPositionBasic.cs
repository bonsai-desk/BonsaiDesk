using Mirror;
using Smooth;
using UnityEngine;

public class ResetFallPositionBasic : MonoBehaviour
{
    private NetworkIdentity networkIdentity;

    private Rigidbody body;

    // Start is called before the first frame update
    private void Start()
    {
        networkIdentity = GetComponent<NetworkIdentity>();
        body = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        if (!(networkIdentity.isServer && networkIdentity.connectionToClient == null || networkIdentity.hasAuthority))
            return;

        if (body.worldCenterOfMass.y < -1f)
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.MovePosition(new Vector3(0, 0.35f, 0.75f / 2f));
            SmoothSyncMirror smooth = GetComponent<SmoothSyncMirror>();
            if (smooth != null)
                smooth.teleport();
        }
    }
}