using UnityEngine;

public class LoadingManager : MonoBehaviour
{
    public TableBrowser browser;
    private bool _initialFade;
    private float _lastLog;
    private bool _setFreq;
    private float _startedAt;

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
        }

        if (!_setFreq && OVRManager.OVRManagerinitialized)
        {
            _setFreq = true;

            var newFreq = 72f;
            foreach (var freq in OVRManager.display.displayFrequenciesAvailable)
            {
                if (freq <= 90)
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