using Mirror;
using UnityEngine;

public class HandAuthority : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        var autoAuthority = collision.gameObject.GetComponent<AutoAuthority>();
        if (autoAuthority != null && !autoAuthority.hasAuthority)
        {
            autoAuthority.CmdSetNewOwner(NetworkClient.connection.identity);
        }
    }
}