using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingManager : MonoBehaviour
{
    public TableBrowser browser;
    private float _startedAt;
    private bool _initialFade;
    
    void Start()
    {
        _startedAt = Time.realtimeSinceStartup;
        BonsaiLog($"Started at {_startedAt}");
        browser.ListenersReady += HandleListenersReady;
    }

    private void HandleListenersReady()
    {
        _initialFade = true;
        FadeIn();
    }

    void Update()
    {
        if (!_initialFade && Time.realtimeSinceStartup - _startedAt > 5f)
        {
            BonsaiLogWarning($"Failed to trigger fade in after 5 seconds, triggering now ({Time.realtimeSinceStartup})");
            _initialFade = true;
            FadeIn();
        }
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
        BonsaiLog("Fading in");
        LaunchManager.Instance?.logoMove?.FadeOut();
    }
}
