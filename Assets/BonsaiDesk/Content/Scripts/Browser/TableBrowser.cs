using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuplex.WebView;

public class TableBrowser : NewBrowser
{
    public string initialUrl;
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        _webViewPrefab.DragMode = DragMode.DragToScroll;
        BrowserReady += () =>
        {
            LoadUrl(initialUrl);
            var view = _webViewPrefab.transform.Find("WebViewPrefabResizer/WebViewPrefabView");
            CustomInputModule.Singleton.screens.Add(view);
        };

    }
    
    public override Vector2Int ChangeAspect(Vector2 newAspect)
    {
        var aspectRatio = newAspect.x / newAspect.y;
        var localScale = new Vector3(_bounds.y * aspectRatio, _bounds.y, 1);
        var maxLength = localScale.x;
        if (localScale.x > _bounds.x)
        {
            localScale = new Vector3(_bounds.x, _bounds.x * (1f / aspectRatio), 1);
            maxLength = localScale.y;
        }

        var resolution = AutoResolution(_bounds, distanceEstimate, pixelPerDegree, newAspect);

        var res = resolution.x > resolution.y ? resolution.x : resolution.y;
        var scale = _bounds.x > _bounds.y ? _bounds.x : _bounds.y;
        var resScaled = res / scale;
       
        Debug.Log(res);
        Debug.Log(scale);
        Debug.Log(resScaled);
        Debug.Log(_bounds);
        
        _webViewPrefab.WebView.SetResolution(resScaled);
        _webViewPrefab.Resize(_bounds.x, _bounds.y);
        
        Debug.Log($"[BONSAI] ChangeAspect resolution {resolution}");

        boundsTransform.localScale = localScale;
        
        
#if UNITY_ANDROID && !UNITY_EDITOR
        RebuildOverlay(resolution);
#endif

        return resolution;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
