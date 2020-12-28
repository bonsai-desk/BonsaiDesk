using UnityEngine;

public class AutoBrowserController : MonoBehaviour
{
    public string initialURL;
    public TogglePause togglePause;
    private AutoBrowser _autoBrowser;

    private void Start()
    {
        _autoBrowser = GetComponent<AutoBrowser>();
        _autoBrowser.BrowserReady += () =>
        {
            Debug.Log("_autoBrowser.BrowserReady");
            _autoBrowser.LoadUrl(initialURL);
        };
        togglePause.PauseChanged += HandlePauseChange;
    }

    private void HandlePauseChange(bool paused)
    {
        var message = "{\"type\": \"video\", \"command\": \"" + (paused ? "pause" : "play") + "\"}";
        Debug.Log("[BONSAI] HandlePauseChange " + message);
        _autoBrowser.PostMessage(message);
    }

    public void Set169()
    {
        StartCoroutine(_autoBrowser.SetNewAspect(new Vector2(16, 9)));
    }
    public void Set11()
    {
        StartCoroutine(_autoBrowser.SetNewAspect(new Vector2(1, 1)));
    }
}