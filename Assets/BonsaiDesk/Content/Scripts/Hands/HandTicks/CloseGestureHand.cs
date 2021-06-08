using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CloseGestureHand : MonoBehaviour, IHandTick
{
    public PlayerHand playerHand { get; set; }

    public AngleToObject angleToHead;
    public UnityEvent action;

    private bool _gestureInProgress;
    private float _progress;
    private bool _activated;
    private const float ActivationTime = 0.35f;

    public Image progressImage;
    public GameObject fistProgress;
    public GameObject fistDescription;

    public void Tick()
    {
        if (playerHand.GetGestureStart(PlayerHand.Gesture.Fist) && angleToHead.AngleBelowThreshold() && !playerHand.stylus.gameObject.activeInHierarchy)
        {
            _gestureInProgress = true;
        }
        if (playerHand.GetGesture(PlayerHand.Gesture.Fist) && angleToHead.AngleBelowThreshold() && !playerHand.stylus.gameObject.activeInHierarchy)
        {
            fistDescription.SetActive(false);
            if (_gestureInProgress)
            {
                fistProgress.SetActive(true);
            
                if (progressImage)
                {
                    _progress += Time.deltaTime * (1f / ActivationTime);
                }
                else
                {
                    _progress = 1f;
                }
            }
        }
        else
        {
            fistProgress.SetActive(false);
            fistDescription.SetActive(true);
            _progress = 0;
            _activated = false;
            _gestureInProgress = false;
        }

        if (progressImage)
        {
            progressImage.fillAmount = Mathf.Clamp01(_progress);
        }

        if (!_activated && _progress >= 1f)
        {
            _activated = true;
            action?.Invoke();
        }
    }
}