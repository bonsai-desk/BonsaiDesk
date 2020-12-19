using UnityEngine;

public class TabletControl : MonoBehaviour
{
    public PhysicMaterial lowFrictionMaterial;
    private PhysicMaterial defaultMaterial;
    public BoxCollider worldBox;

    private bool touchingHand = false;
    private bool lastTouchingHand = false;

    public GameObject mediaParent;
    public GameObject modelParent;

    public GameObject model;
    private float lastModelToggleTime = 0;

    public BoxCollider buttonBox;

    public enum TabletType
    {
        Media,
        Model
    };

    public TabletType tabletType = TabletType.Media;

    private void Start()
    {
        defaultMaterial = worldBox.material;

        if (tabletType == TabletType.Media)
        {
            GetComponent<TabletPhysics>().enabled = true;
            mediaParent.SetActive(true);
            modelParent.SetActive(false);
        }
        else
        {
            GetComponent<TabletPhysics>().enabled = false;
            mediaParent.SetActive(false);
            modelParent.SetActive(true);
        }
        model.SetActive(false);
    }

    private void Update()
    {
        if (touchingHand != lastTouchingHand)
        {
            if (touchingHand)
                worldBox.material = lowFrictionMaterial;
            else
                worldBox.material = defaultMaterial;
        }

        lastTouchingHand = touchingHand;
        touchingHand = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("LeftHand") || collision.gameObject.layer == LayerMask.NameToLayer("RightHand"))
        {
            touchingHand = true;

            if (collision.contacts[0].thisCollider == buttonBox && Time.time - lastModelToggleTime > 1f)
            {
                lastModelToggleTime = Time.time;
                model.SetActive(!model.activeSelf);
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("LeftHand") || collision.gameObject.layer == LayerMask.NameToLayer("RightHand"))
        {
            touchingHand = true;
        }
    }
}