using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoBrowserController : MonoBehaviour
{
    public string initialURL;
    private AutoBrowser _autoBrowser;
    
    private void Start()
    {
        _autoBrowser = GetComponent<AutoBrowser>();
        _autoBrowser.BrowserReady += () =>
        {
            Debug.Log("_autoBrowser.BrowserReady");
            _autoBrowser.LoadUrl(initialURL);
        };
    }
}
