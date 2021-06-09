using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;

public class CubeHologramControl : NetworkBehaviour
{
    public float floatLevel = -0.01f;
    public SmoothSyncVars smoothSyncVars;
    public Transform quad;
    public Transform triangle;
    public Transform icons;
    private Vector3 _targetScale;

    public bool keepHologramUp;

    private Material _material;
    private Material _hologramMaterial;

    private float _lerp;
    private const float AnimationTime = 0.25f;
    private const float ActivationRadius = 0.125f;

    private Rigidbody _body;

    private static CubeHologramControl _localActiveCube;
    private static float _localActiveCubeDistance = float.PositiveInfinity;

    private void Awake()
    {
        _targetScale = quad.localScale;
        quad.localScale = Vector3.zero;
        triangle.localScale = Vector3.zero;
        icons.localScale = Vector3.zero;
        _body = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        var inRange = InRange();
        var shouldShowThumbnail = inRange && !_body.isKinematic;
        var clientAuthority = smoothSyncVars.AutoAuthority.isClient && smoothSyncVars.AutoAuthority.ClientHasAuthority();
        var serverAuthority = smoothSyncVars.AutoAuthority.isServer && smoothSyncVars.AutoAuthority.ServerHasAuthority();

        if (inRange && smoothSyncVars.AutoAuthority.isClient && !clientAuthority)
        {
            smoothSyncVars.RequestAuthority();
        }

        if (clientAuthority)
        {
            smoothSyncVars.Set("showThumbnail", shouldShowThumbnail);
        }

        if (serverAuthority)
        {
            smoothSyncVars.Set("showThumbnail", false);
        }

        _lerp = CubicBezier.EaseOut.MoveTowards01(_lerp, AnimationTime, smoothSyncVars.Get("showThumbnail"));
        var lerp = _lerp;
        if (keepHologramUp)
        {
            lerp = 1f;
        }

        var toHead = InputManager.Hands.head.position - transform.position;
        toHead.y = 0;
        var atHead = Quaternion.LookRotation(-toHead);
        quad.rotation = atHead;
        icons.rotation = atHead;

        var startPosition = transform.position;
        var targetPosition = transform.position + new Vector3(0, 2.5f * 0.05f, 0);
        quad.position = Vector3.Lerp(startPosition, targetPosition, lerp);
        quad.localScale = Vector3.Lerp(Vector3.zero, _targetScale, lerp);

        icons.position = Vector3.Lerp(startPosition, targetPosition + atHead * Vector3.forward * floatLevel, lerp);
        icons.localScale = Vector3.Lerp(Vector3.zero, new Vector3(1.25f, 1.25f, 1f), lerp);

        CalculateTriangle(atHead);
    }

    private bool InRange()
    {
        var inRange = false;
        float closest = float.PositiveInfinity;
        var hd1 = Vector2.SqrMagnitude(InputManager.Hands.Left.PlayerHand.palm.position.xz() - transform.position.xz());
        if (hd1 < ActivationRadius * ActivationRadius && InputManager.Hands.Left.PlayerHand.palm.position.y < transform.position.y + 0.2f)
        {
            inRange = true;
        }
        if (hd1 < closest)
        {
            closest = hd1;
        }
        var hd2 = Vector2.SqrMagnitude(InputManager.Hands.Right.PlayerHand.palm.position.xz() - transform.position.xz());
        if (hd2 < ActivationRadius * ActivationRadius && InputManager.Hands.Right.PlayerHand.palm.position.y < transform.position.y + 0.2f)
        {
            inRange = true;
        }
        if (hd2 < closest)
        {
            closest = hd2;
        }

        for (int i = 0; i < InputManager.Hands.physicsFingerTipPositions.Length; i++)
        {
            var horizontalDistance = Vector2.SqrMagnitude(InputManager.Hands.physicsFingerTipPositions[i].xz() - transform.position.xz());
            if (horizontalDistance < ActivationRadius * ActivationRadius && InputManager.Hands.physicsFingerTipPositions[i].y > transform.position.y &&
                InputManager.Hands.physicsFingerTipPositions[i].y < transform.position.y + 0.2f)
            {
                inRange = true;
            }
            
            if (horizontalDistance < closest)
            {
                closest = horizontalDistance;
            }
        }

        if (_localActiveCube == this)
        {
            _localActiveCubeDistance = closest;
        }

        if (!inRange && _localActiveCube == this)
        {
            _localActiveCubeDistance = float.PositiveInfinity;
            _localActiveCube = null;
        }

        if (inRange && (closest < _localActiveCubeDistance || !_localActiveCube))
        {
            _localActiveCubeDistance = closest;
            _localActiveCube = this;
        }

        if (_localActiveCube != this)
        {
            inRange = false;
        }

        return inRange;
    }

    private void CalculateTriangle(Quaternion atHead)
    {
        triangle.position = transform.position + new Vector3(0, 0.5f * 0.05f, 0);
        triangle.rotation = atHead;
        triangle.localScale = new Vector3(quad.localScale.x,
            (quad.transform.position.y - transform.position.y) * (1f / 0.05f) - (quad.localScale.y / 2f) - 0.5f, 1);
    }
}