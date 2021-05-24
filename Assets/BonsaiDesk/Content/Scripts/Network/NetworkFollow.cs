using Mirror;
using UnityEngine;

public class NetworkFollow : NetworkBehaviour
{
    public string targetName;

    private Transform target;

    private bool inited = false;

    public enum RenderBehaviour
    {
        Normal,
        DisableRenderers,
        DoNotRenderLayer
    };

    public Rigidbody body;

    public RenderBehaviour renderBehaviour = RenderBehaviour.Normal;

    private void Init()
    {
        inited = true;

        int layer = LayerMask.NameToLayer("doNotRender");

        foreach (var rend in GetComponentsInChildren<MeshRenderer>(true))
        {
            if (renderBehaviour == RenderBehaviour.DoNotRenderLayer)
                rend.gameObject.layer = layer;
            if (renderBehaviour == RenderBehaviour.DisableRenderers)
                rend.enabled = false;
        }

        foreach (var rend in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderBehaviour == RenderBehaviour.DoNotRenderLayer)
                rend.gameObject.layer = layer;
            if (renderBehaviour == RenderBehaviour.DisableRenderers)
                rend.enabled = false;
        }

        GameObject targetObject = GameObject.Find(targetName);
        if (targetObject != null)
            target = targetObject.transform;
    }

    private void Update()
    {
        if (!isLocalPlayer && !hasAuthority)
            return;

        if (!inited)
            Init();

        if (target != null)
        {
            if (body)
            {
                body.MovePosition(target.position);
                body.MoveRotation(target.rotation);
            }
            else
            {
                transform.position = target.position;
                transform.rotation = target.rotation;
            }

            transform.localScale = target.localScale;
        }
    }
}