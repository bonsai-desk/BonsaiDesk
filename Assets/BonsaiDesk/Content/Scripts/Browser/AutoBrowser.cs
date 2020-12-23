using System;
using System.Collections;
using UnityEngine;
using Vuplex.WebView;

public class AutoBrowser : MonoBehaviour
{
    private OVROverlay _overlay;
    private WebViewPrefab _webViewPrefab;

    private void Start()
    {
        _overlay = GetComponent<OVROverlay>();

        _webViewPrefab = WebViewPrefab.Instantiate(0.5f, 0.5f);
        _webViewPrefab.Initialized +=
            (sender, eventArgs) => StartCoroutine(SetupWebView(sender, eventArgs));

        _webViewPrefab.transform.SetParent(transform, false);
        _webViewPrefab.transform.localEulerAngles = new Vector3(0, 180, 0);
    }

    private IEnumerator SetupWebView(object sender, EventArgs eventArgs)
    {
        Debug.Log("[BONSAI] SetupWebView");
        _webViewPrefab.Resize(1f, 1f);

        while (_overlay.externalSurfaceObject == IntPtr.Zero || _webViewPrefab.WebView == null)
        {
            Debug.Log("[BONSAI] externalSurfaceObject " + _overlay.externalSurfaceObject);
            Debug.Log("[BONSAI] WebView " + _webViewPrefab.WebView);
            yield return null;
        }
        
        var surface = _overlay.externalSurfaceObject;

        Debug.Log("[BONSAI] externalSurfaceObject " + _overlay.externalSurfaceObject +
                  ", WebView " + _webViewPrefab.WebView);

        Debug.Log("[BONSAI] _overlay dimensions "
                  + _overlay.externalSurfaceWidth + "," + _overlay.externalSurfaceHeight);

#if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log("[BONSAI] SetSurface" + surface);
        (_webViewPrefab.WebView as AndroidGeckoWebView).SetSurface(surface);
        Debug.Log("[BONSAI] Done SetSurface");
#endif
        _webViewPrefab.WebView.LoadUrl("www.google.com");
    }
}