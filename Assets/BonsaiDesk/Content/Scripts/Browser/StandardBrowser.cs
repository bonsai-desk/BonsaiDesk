using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuplex.WebView;

public class StandardBrowser : MonoBehaviour
{
    private WebViewPrefab _webViewPrefab;
    void Start()
    {
        _webViewPrefab = WebViewPrefab.Instantiate(0.5f, 0.5f);
        _webViewPrefab.Initialized += SetupWebView;
        
        _webViewPrefab.transform.SetParent(transform, false);
        _webViewPrefab.transform.localEulerAngles = new Vector3(0, 180, 0);
    }
    
    private void SetupWebView (object sender, EventArgs eventArgs)
    {
        Debug.Log("[BONSAI] SetupWebView");
        _webViewPrefab.Resize(1f, 1f);
        _webViewPrefab.WebView.LoadUrl("www.google.com");
    }

}
