using Mirror;
using UnityEngine;

public class TabletControl : MonoBehaviour
{
    public PhysicMaterial lowFrictionPhysicMaterial;
    public BoxCollider worldBox;

    private PhysicMaterial _defaultPhysicMaterial;

    private bool _touchingHand = false;
    private bool _lastTouchingHand = false;

    private int _leftHandLayerMask;
    private int _rightHandLayerMask;

    private void Start()
    {
        _defaultPhysicMaterial = worldBox.sharedMaterial;

        _leftHandLayerMask = LayerMask.NameToLayer("LeftHand");
        _rightHandLayerMask = LayerMask.NameToLayer("RightHand");
    }

    private void Update()
    {
        if (_touchingHand != _lastTouchingHand)
        {
            worldBox.sharedMaterial = _touchingHand ? lowFrictionPhysicMaterial : _defaultPhysicMaterial;
        }

        _lastTouchingHand = _touchingHand;
        _touchingHand = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == _leftHandLayerMask || collision.gameObject.layer == _rightHandLayerMask)
        {
            _touchingHand = true;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.layer == _leftHandLayerMask || collision.gameObject.layer == _rightHandLayerMask)
        {
            _touchingHand = true;
        }
    }

    public void TabletPlay()
    {
        NetworkServer.Destroy(gameObject);
    }
}