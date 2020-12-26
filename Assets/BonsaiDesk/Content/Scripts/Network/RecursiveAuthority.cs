using Mirror;
using UnityEngine;

public class RecursiveAuthority : NetworkBehaviour
{
    [SyncVar]
    public double authorityChangeTime = 0;

    // Start is called before the first frame update
    //   private void Start()
    //   {
    //   }

    // Update is called once per frame
    //   private void Update()
    //   {
    //   }

    private void OnCollisionEnter(Collision collision)
    {
        if (!hasAuthority)
            return;

        HandleAuthorityChange(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!hasAuthority)
            return;
        
        HandleAuthorityChange(collision);
    }

    private void HandleAuthorityChange(Collision collision)
    {
        NetworkIdentity objectNetId = collision.gameObject.GetComponent<NetworkIdentity>();
        if (objectNetId != null && collision.gameObject.GetComponent<Rigidbody>() != null)
        {
            if (!objectNetId.hasAuthority)
            {
                RecursiveAuthority ra = collision.gameObject.GetComponent<RecursiveAuthority>();
                if (ra != null)
                {
                    if (authorityChangeTime > ra.authorityChangeTime)
                    {
                        // NetworkVRPlayer.self.CmdReceiveOwnershipOfObject(objectNetId);
                    }
                }
                else
                {
                    // NetworkVRPlayer.self.CmdReceiveOwnershipOfObject(objectNetId);
                }
            }
        }
    }

    public override void OnStartAuthority()
    {
        CmdSetAuthorityChangeTime();
    }

    [Command(ignoreAuthority = true)]
    private void CmdSetAuthorityChangeTime()
    {
        authorityChangeTime = NetworkTime.time;
    }
}