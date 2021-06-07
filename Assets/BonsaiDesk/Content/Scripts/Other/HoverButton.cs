using System;
using System.Collections;
using System.Collections.Generic;
using OVR;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HoverButton : MonoBehaviour
{
    public float activationRadius = 0.0275f;
    public bool halfSphere;
    private const float ActivationTime = 0.75f;

    public Image progressImage;
    public UnityEvent action;

    private float _activeTimer = 0;
    private bool _activated = false;

    private int emitterId = -1;

    public SoundFXRef tickSound;

    private void OnDisable()
    {
        if (emitterId != -1)
        {
            AudioManager.StopSound(emitterId, false);
            emitterId = -1;
        }
        
        _activeTimer = 0;
        progressImage.fillAmount = Mathf.Clamp01(_activeTimer / ActivationTime);
    }

    private void Update()
    {
        var fingerTouchingButton = false;
        if (!Mathf.Approximately(transform.lossyScale.sqrMagnitude, 0))
        {
            for (int i = 0; i < InputManager.Hands.physicsFingerTipPositions.Length; i++)
            {
                if (Vector3.SqrMagnitude(InputManager.Hands.physicsFingerTipPositions[i] - transform.position) < activationRadius * activationRadius &&
                    (!halfSphere || halfSphere && transform.InverseTransformPoint(InputManager.Hands.physicsFingerTipPositions[i]).z < 0.0015f))
                {
                    fingerTouchingButton = true;
                    break;
                }
            }
        }

        if (fingerTouchingButton)
        {
            if (emitterId == -1)
            {
                emitterId = tickSound.PlaySoundAt(transform.position);
                if (emitterId != -1)
                {
                    AudioManager.AttachSoundToParent(emitterId, transform);
                }
            }

            _activeTimer += Time.deltaTime;
        }
        else
        {
            if (emitterId != -1)
            {
                AudioManager.DetachSoundFromParent(emitterId);
                AudioManager.StopSound(emitterId, false);
                emitterId = -1;
            }

            _activeTimer = 0;
            _activated = false;
        }

        progressImage.fillAmount = Mathf.Clamp01(_activeTimer / ActivationTime);

        if (transform.gameObject.activeInHierarchy && !_activated && _activeTimer > ActivationTime)
        {
            _activated = true;
            action?.Invoke();
        }
    }

    private void OnDestroy()
    {
        if (emitterId != -1)
        {
            AudioManager.DetachSoundFromParent(emitterId);
            AudioManager.StopSound(emitterId, false);
            emitterId = -1;
        }
    }
}