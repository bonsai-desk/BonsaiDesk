using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MenuButton : MonoBehaviour, IHandTick
{
    public PlayerHand playerHand { get; set; }

    private const float ActivationRadius = 0.02f;
    private const float ActivationTime = 0.75f;

    public Transform buttonTransform;
    public Image progressImage;
    public UnityEvent action;

    private float _activeTimer = 0;
    private bool _activated = false;

    public void Tick()
    {
        var fingerTouchingButton = false;
        for (int i = 0; i < playerHand.OtherHand.HandComponents.PhysicsFingerTips.Length; i++)
        {
            if (Vector3.SqrMagnitude(playerHand.OtherHand.HandComponents.PhysicsFingerTips[i].position - buttonTransform.position) <
                ActivationRadius * ActivationRadius)
            {
                fingerTouchingButton = true;
                break;
            }
        }

        if (fingerTouchingButton)
        {
            _activeTimer += Time.deltaTime;
        }
        else
        {
            _activeTimer = 0;
            _activated = false;
        }

        progressImage.fillAmount = Mathf.Clamp01(_activeTimer / ActivationTime);

        if (!_activated && _activeTimer > ActivationTime)
        {
            _activated = true;
            action?.Invoke();
        }
    }
}