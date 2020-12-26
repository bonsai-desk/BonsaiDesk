using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoBrowserController : MonoBehaviour
{
    private AutoBrowser _autoBrowser;
    
    private void Start()
    {
        _autoBrowser = GetComponent<AutoBrowser>();
        _autoBrowser.BrowserReady += () => { Debug.Log("_autoBrowser.BrowserReady");};
    }
}
