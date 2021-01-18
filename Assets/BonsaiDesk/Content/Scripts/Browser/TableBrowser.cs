using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuplex.WebView;

public class TableBrowser : Browser
{
    // Start is called before the first frame update
    void Start()
    {
        base.Start();
        BrowserReady += () =>
        {
            LoadUrl("https://youtube.com");
            var view = _webViewPrefab.transform.Find("WebViewPrefabResizer/WebViewPrefabView");
            CustomInputModule.Singleton.screens.Add(view);
        };

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
