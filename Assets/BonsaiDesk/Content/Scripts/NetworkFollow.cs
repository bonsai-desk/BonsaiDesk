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
        DissableRenderers,
        DoNotRenderLayer
    };

    public Rigidbody body;

    public RenderBehaviour renderBehaviour = RenderBehaviour.Normal;

    // void Start()
    // {
    //     if (!(isLocalPlayer || (hasAuthority && !isServer)))
    //         return;
    // }

    private void init()
    {
        inited = true;

        int layer = LayerMask.NameToLayer("doNotRender");
        var r = GetComponent<MeshRenderer>();
        var sr = GetComponent<SkinnedMeshRenderer>();
        if (r != null)
        {
            if (renderBehaviour == RenderBehaviour.DoNotRenderLayer)
                r.gameObject.layer = layer;
            if (renderBehaviour == RenderBehaviour.DissableRenderers)
                r.enabled = false;
        }
        if (sr != null)
        {
            if (renderBehaviour == RenderBehaviour.DoNotRenderLayer)
                sr.gameObject.layer = layer;
            if (renderBehaviour == RenderBehaviour.DissableRenderers)
                sr.enabled = false;
        }
        foreach (var rend in GetComponentsInChildren<MeshRenderer>())
        {
            if (renderBehaviour == RenderBehaviour.DoNotRenderLayer)
                rend.gameObject.layer = layer;
            if (renderBehaviour == RenderBehaviour.DissableRenderers)
                rend.enabled = false;
        }
        foreach (var rend in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (renderBehaviour == RenderBehaviour.DoNotRenderLayer)
                rend.gameObject.layer = layer;
            if (renderBehaviour == RenderBehaviour.DissableRenderers)
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
            init();

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