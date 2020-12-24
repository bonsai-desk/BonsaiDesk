using System;
using System.Collections;
using UnityEngine;
using Vuplex.WebView;

public class AutoBrowser : MonoBehaviour
{
    public string initialURL;
    public float width = 1;
    public Vector2 aspect = new Vector2(16, 9);

    public int xResolution = 850;
    public bool autoSetResolution;
    public float distanceEstimate = 1;
    public int pixelPerDegree = 16;

    public Texture dummyTexture;

    private OVROverlay _overlay;

    private Vector2Int _resolution;
    private WebViewPrefab _webViewPrefab;

    private void Start()
    {
        if (autoSetResolution)
        {
            var height = width * aspect.y / aspect.x;
            var xRes = GetResolution(width, distanceEstimate, pixelPerDegree);
            var yRes = GetResolution(height, distanceEstimate, pixelPerDegree);
            _resolution = new Vector2Int(xRes, yRes);
        }
        else
        {
            _resolution = new Vector2Int(xResolution, (int) Math.Round(xResolution * aspect.y / aspect.x));
        }

        _overlay = gameObject.AddComponent<OVROverlay>();
        _overlay.externalSurfaceWidth = _resolution.x;
        _overlay.externalSurfaceHeight = _resolution.y;
        _overlay.transform.localScale = new Vector3(width, width * aspect.y / aspect.x, 1);
        
        if (dummyTexture)
            _overlay.textures = new Texture[] {dummyTexture, dummyTexture};
        
#if UNITY_EDITOR
        _overlay.isExternalSurface = false;
        _overlay.previewInEditor = true;
#else
        _overlay.isExternalSurface = true;
#endif

        _webViewPrefab = WebViewPrefab.Instantiate(width, width * aspect.y / aspect.x);
        _webViewPrefab.Visible = false;
        _webViewPrefab.Initialized +=
            (sender, eventArgs) => StartCoroutine(SetupWebView(sender, eventArgs));
    }

    private IEnumerator SetupWebView(object sender, EventArgs eventArgs)
    {
        _webViewPrefab.WebView.LoadUrl(initialURL);

#if UNITY_ANDROID && !UNITY_EDITOR
        while (_overlay.externalSurfaceObject == IntPtr.Zero || _webViewPrefab.WebView == null)
        {
            Debug.Log("[BONSAI] while WebView not setup\nexternalSurfaceObject: <" + _overlay.externalSurfaceObject + ">\nWebView: <" + _webViewPrefab.WebView + ">");
            yield return null;
        }
#endif

        _webViewPrefab.WebView.SetResolution(1);
        _webViewPrefab.WebView.Resize(_resolution.x, _resolution.y);

        var surface = _overlay.externalSurfaceObject;

#if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log("[BONSAI] SetSurface" + surface + ", WebView " + _webViewPrefab.WebView);
        (_webViewPrefab.WebView as AndroidGeckoWebView).SetSurface(surface);
        Debug.Log("[BONSAI] Done SetSurface");
#endif
        yield break;
    }

    public static int GetResolution(float width, float distanceEstimate, int pixelPerDegree)
    {
        // calculates the optimal resolution along some dimension
        // x : side of billboard in unity units
        // distance : estimated closest distance from (user) --- (billboard)
        // pixelPerDegree : resolving resolution of headset
        return (int) Math.Round(
            pixelPerDegree * (360f / (2 * Math.PI)) * 2 * Math.Atan(width / (2 * distanceEstimate))
        );
    }
}