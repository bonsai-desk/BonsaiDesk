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
        var autoAuthority = collision.gameObject.GetComponent<AutoAuthority>();
        if (autoAuthority != null)
        {
            autoAuthority.Interact(NetworkClient.connection.identity);
        }
    }
}