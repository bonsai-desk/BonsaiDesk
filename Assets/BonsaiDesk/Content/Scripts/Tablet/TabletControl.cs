using Mirror;
using UnityEngine;

public class TabletControl : NetworkBehaviour
{
    public PhysicMaterial lowFrictionPhysicMaterial;
    public BoxCollider worldBox;
    public TabletCollider tabletCollider;
    public Rigidbody tabletBody;
    
    private PhysicMaterial _defaultPhysicMaterial;

    private void Start()
    {
        _defaultPhysicMaterial = worldBox.sharedMaterial;
    }
    
    private void Update()
    {
        worldBox.sharedMaterial =
            tabletCollider.NumFingersTouching >= 4 ? lowFrictionPhysicMaterial : _defaultPhysicMaterial;
        tabletBody.mass = tabletCollider.NumFingersTouching >= 4 ? 0.050f : 0.300f;
    }

    public void TabletPlay()
    {
        CmdDestroySelf();
    }

    [Command]
    private void CmdDestroySelf()
    {
        NetworkServer.Destroy(gameObject);
    }
}