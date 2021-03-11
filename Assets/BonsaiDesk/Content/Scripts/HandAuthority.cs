using Mirror;
using UnityEngine;

public class HandAuthority : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        HandleHandAuthority(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        HandleHandAuthority(collision);
    }

    private void HandleHandAuthority(Collision collision)
    {
        if (NetworkClient.connection == null || !NetworkClient.connection.identity)
            return;

        var autoAuthority = collision.gameObject.GetComponent<AutoAuthority>();
        if (autoAuthority != null)
        {
            autoAuthority.Interact(NetworkClient.connection.identity.netId);
        }
    }
}