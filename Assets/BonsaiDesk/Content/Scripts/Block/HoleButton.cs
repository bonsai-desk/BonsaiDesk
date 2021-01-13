using UnityEngine;
using UnityEngine.Events;

public class HoleButton : MonoBehaviour
{
    public UnityEvent action;
    public UnityEvent leftAction;
    public UnityEvent rightAction;

    private Transform button;
    private Transform buttonMesh;

    private MeshRenderer buttonMeshRenderer;

    private Material defaultMaterial;
    public Material activeMaterial;

    public enum OnClickAction
    {
        None,
        Destroy,
        Disable
    };

    public OnClickAction onClickAction = OnClickAction.None;
    public bool activateOnRelease = true;

    private DeskController deskController;

    private Hole hole;

    private bool activated = false;
    private bool pressed = false;
    private bool enteredFromFlat = false;
    private bool lastIsPressRange = false;
    private bool lastHeightValid = false;

    private float holeRadiusLerp = 0;

    public OVR.SoundFXRef audioSource;

    private float pressCooldown = 0.15f;
    private float lastPressTime = 0;

    public static float shrinkTime = 0.35f;
    private bool disabling = false;

    private void Start()
    {
        Init();

        button = transform.GetChild(0);
        button.transform.localPosition = new Vector3(0, -0.03f, 0);
        buttonMesh = button.GetChild(0);

        buttonMeshRenderer = buttonMesh.GetComponent<MeshRenderer>();
        defaultMaterial = buttonMeshRenderer.sharedMaterial;
    }

    private void Update()
    {
        if (!activated)
            holeRadiusLerp += Time.deltaTime / shrinkTime;
        else
        {
            holeRadiusLerp -= Time.deltaTime / shrinkTime;
            if (holeRadiusLerp <= 0)
            {
                pressed = false;
                activated = false;
                enteredFromFlat = true;

                if (disabling)
                {
                    RemoveHole();
                    gameObject.SetActive(false);
                    return;
                }

                if (onClickAction == OnClickAction.Destroy)
                {
                    RemoveHole();
                    Destroy(gameObject);
                    return;
                }
                else if (onClickAction == OnClickAction.Disable)
                {
                    RemoveHole();
                    gameObject.SetActive(false);
                    return;
                }
            }
        }
        holeRadiusLerp = Mathf.Clamp01(holeRadiusLerp);
        hole.radius = Mathf.Lerp(0, 0.05f, holeRadiusLerp);
        deskController.UpdateHoleRadiiInShader();

        float t = 0;
        int index = 0;

        float closeHeightLerp = 0;
        for (int i = 0; i < InputManager.Hands.physicsFingerTipPositions.Length; i++)
        {
            float activationRadius = 0.1f;

            float heightDistance = (InputManager.Hands.physicsFingerTipPositions[i].y - 0.025f) - transform.position.y;
            float heightLerp = (heightDistance - 0.005f) / activationRadius;

            float sideDistance = Vector2.Distance(new Vector2(InputManager.Hands.physicsFingerTipPositions[i].x, InputManager.Hands.physicsFingerTipPositions[i].z), new Vector2(transform.position.x, transform.position.z));
            float sideLerp = (sideDistance - 0.04f) / activationRadius;

            float newT = Mathf.Max(heightLerp, sideLerp);
            newT = Mathf.Clamp01(newT);
            newT = 1 - newT;
            newT = Mathf.Lerp(0, newT, holeRadiusLerp);

            if (newT > t)
            {
                t = newT;
                closeHeightLerp = heightLerp;
                index = i;
            }
        }

        Vector3 position = button.transform.localPosition;
        float lerp = MathUtils.interpolate(t);
        if (activated)
            lerp = Mathf.Clamp01((holeRadiusLerp - 0.75f) / 0.25f);
        position.y = Mathf.Lerp(-0.03f, 0, lerp) + 0.0025f;
        button.transform.localPosition = position;

        bool inPressRange = t == 1;

        if (inPressRange && inPressRange != lastIsPressRange)
        {
            if (lastHeightValid)
                enteredFromFlat = true;
        }

        bool heightValid = 1 - Mathf.Clamp01(closeHeightLerp) == 1;

        if (!inPressRange)
        {
            if (heightValid)
                pressed = false;
        }

        if (Time.time - lastPressTime > pressCooldown)
        {
            if (inPressRange)
            {
                if (!enteredFromFlat)
                {
                    if (!activated)
                    {
                        buttonMeshRenderer.sharedMaterial = activeMaterial;
                        if (!activateOnRelease)
                        {
                            activated = true;
                            ActivatedAction(index);
                        }
                    }
                    pressed = true;
                }
            }
            else
            {
                if (pressed)
                {
                    if (!activated)
                    {
                        if (onClickAction != OnClickAction.None)
                        {
                            activated = true;
                            buttonMeshRenderer.sharedMaterial = activeMaterial;
                            ActivatedAction(index);
                        }
                        else
                        {
                            pressed = false;
                            enteredFromFlat = true;
                            ActivatedAction(index);
                        }
                    }
                }
                if (!activated)
                    buttonMeshRenderer.sharedMaterial = defaultMaterial;
                enteredFromFlat = false;
            }
        }
        lastIsPressRange = inPressRange;
        lastHeightValid = heightValid;
    }

    private void OnEnable()
    {
        GetHole();
        holeRadiusLerp = 0;
        pressed = false;
        activated = false;
        enteredFromFlat = true;
        disabling = false;
    }

    private void OnDisable()
    {
        RemoveHole();
        holeRadiusLerp = 1;
    }

    private void OnDestroy()
    {
        RemoveHole();
    }

    public void Init()
    {
        GetHole();
    }

    private void GetHole()
    {
        if (deskController == null)
            deskController = GameObject.Find("GameManager").GetComponent<DeskController>();
        if (hole == null)
        {
            Vector3 localPos = deskController.tableParent.InverseTransformPoint(transform.position);
            hole = deskController.CreateHole(new Vector2(localPos.x, localPos.z), 0);
            hole.holeObject.parent = transform;
        }
    }

    private void RemoveHole()
    {
        if (hole != null)
        {
            deskController.DestroyHole(hole);
            hole = null;
        }
    }

    public void DisableButton()
    {
        activated = true;
        disabling = true;
    }

    private void ActivatedAction(int index)
    {
        if (Time.time - lastPressTime < pressCooldown)
            return;
        lastPressTime = Time.time;

        if (deskController != null)
        {
            audioSource.PlaySoundAt(transform.position);
            action.Invoke();

            if (index < 5)
                leftAction.Invoke();
            else
                rightAction.Invoke();
        }
    }
}