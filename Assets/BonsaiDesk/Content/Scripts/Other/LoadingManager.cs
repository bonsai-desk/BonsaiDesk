﻿using UnityEngine;

public class LoadingManager : MonoBehaviour
{
    public TableBrowser browser;
    private bool _initialFade;
    private bool _setFreq;
    private float _startedAt;
    private float _lastLog;

    private void Start()
    {
        _startedAt = Time.realtimeSinceStartup;
        BonsaiLog($"Started at {_startedAt}");
        browser.ListenersReady += HandleListenersReady;
    }

    private void Update()
    {
        if (Time.time > _lastLog)
        {
            _lastLog += 1.5f;
            var msg = CaptiveReality.Jni.Util.StaticCall("sayHello", "Invalid response", "com.example.bonsai.HelloWorld");
            Debug.Log(msg);
        }
            
        if (!_setFreq && OVRManager.OVRManagerinitialized)
        {
            _setFreq = true;

            float newFreq = 72f;
            foreach (var freq in OVRManager.display.displayFrequenciesAvailable)
            {
                if (freq <= 90f)
                {
                    newFreq = freq;
                }
            }

            BonsaiLog($"Setting frequency to {newFreq}");
            OVRPlugin.systemDisplayFrequency = newFreq;
        }

        if (!_initialFade && Time.realtimeSinceStartup - _startedAt > 5f)
        {
            BonsaiLogWarning($"Failed to trigger fade in after 5 seconds, triggering now ({Time.realtimeSinceStartup})");
            _initialFade = true;
            FadeIn();
        }
    }

    private void HandleListenersReady()
    {
        BonsaiLog($"Fade in after {Time.realtimeSinceStartup - _startedAt} seconds");
        _initialFade = true;
        FadeIn();
    }

    private void BonsaiLog(string msg)
    {
        Debug.Log("<color=orange>BonsaiLoad: </color>: " + msg);
    }

    private void BonsaiLogWarning(string msg)
    {
        Debug.LogWarning("<color=orange>BonsaiLoad: </color>: " + msg);
    }

    private void BonsaiLogError(string msg)
    {
        Debug.LogError("<color=orange>BonsaiLoad: </color>: " + msg);
    }

    private void FadeIn()
    {
        LaunchManager.Instance?.logoMove?.FadeOut();
    }
}