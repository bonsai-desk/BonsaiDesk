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
        DoNotRenderLayer,
        DoNotRenderHeadLayer
    };

    public Rigidbody body;

    public RenderBehaviour renderBehaviour = RenderBehaviour.Normal;

    private void Init()
    {
        inited = true;

        int layer = LayerMask.NameToLayer("doNotRender");
        int layerHead = LayerMask.NameToLayer("doNotRenderHead");

        foreach (var rend in GetComponentsInChildren<MeshRenderer>(true))
        {
            if (renderBehaviour == RenderBehaviour.DoNotRenderLayer)
                rend.gameObject.layer = layer;
            if (renderBehaviour == RenderBehaviour.DoNotRenderHeadLayer)
                rend.gameObject.layer = layerHead;
            if (renderBehaviour == RenderBehaviour.DisableRenderers)
                rend.enabled = false;
        }

        foreach (var rend in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderBehaviour == RenderBehaviour.DoNotRenderLayer)
                rend.gameObject.layer = layer;
            if (renderBehaviour == RenderBehaviour.DoNotRenderHeadLayer)
                rend.gameObject.layer = layerHead;
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

        MoveToTarget();
    }

    public void MoveToTarget()
    {
        if (target != null)
        {
            if (body)
            {
                transform.position = target.position;
                transform.rotation = target.rotation;
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