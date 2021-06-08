using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadFollowPhysics : ObjectFollowPhysics
{
    public string targetName;

    private SphereCollider _sphereCollider;
    private bool _init;

    protected override void Awake()
    {
        base.Awake();

        _sphereCollider = GetComponent<SphereCollider>();

        var targetObject = GameObject.Find(targetName);
        if (targetObject)
        {
            target = targetObject.transform;
        }
    }

    private void Update()
    {
        if (!NetworkIdentity.isLocalPlayer)
        {
            return;
        }

        if (!_init)
        {
            _init = true;
            var layerHead = LayerMask.NameToLayer("doNotRenderHead");

            foreach (var rend in GetComponentsInChildren<MeshRenderer>(true))
            {
                rend.gameObject.layer = layerHead;
            }

            foreach (var rend in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                rend.gameObject.layer = layerHead;
            }
        }
    }

    private float Vector3Max(Vector3 v)
    {
        var max = v.x;
        if (v.y > max)
        {
            max = v.y;
        }

        if (v.z > max)
        {
            max = v.z;
        }

        return max;
    }

    protected override void TryResetToTarget()
    {
        if (!Physics.CheckSphere(target.transform.position, Vector3Max(transform.localScale) * _sphereCollider.radius, BlockUtility.DefaultLayerMask))
        {
            ResetToTarget();
        }
    }

    public void ResetToTarget()
    {
        Body.velocity = Vector3.zero;
        Body.angularVelocity = Vector3.zero;
        transform.position = target.position;
        transform.rotation = target.rotation;
        Body.MovePosition(target.position);
        Body.MoveRotation(target.rotation);
    }
}