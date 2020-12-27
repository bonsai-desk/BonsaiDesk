using System;
using System.Collections;
using UnityEngine;
using Vuplex.WebView;

public delegate void BrowserReadyEvent();

public class AutoBrowser : MonoBehaviour
{
    public event BrowserReadyEvent BrowserReady;
    
    public float width = 1;
    public Vector2 aspect = new Vector2(16, 9);

    public int xResolution = 850;
    public bool autoSetResolution;
    public float distanceEstimate = 1;
    public int pixelPerDegree = 16;

    private OVROverlay _overlay;
    private GameObject _holePuncher;

    private Vector2Int _resolution;
    
    private WebViewPrefab _webViewPrefab;

    public Material holePuncherMaterial;
    public Material dummyMaterial;

    private void Start()
    {
        _resolution = autoSetResolution
            ? GetAutoResolution(width, distanceEstimate, pixelPerDegree, aspect)
            : GetResolutionFromX(xResolution, aspect);

        var localScale = new Vector3(width, width * aspect.y / aspect.x, 1);

        _overlay = gameObject.AddComponent<OVROverlay>();
        _overlay.externalSurfaceWidth = _resolution.x;
        _overlay.externalSurfaceHeight = _resolution.y;
        _overlay.transform.localScale = localScale;
        _overlay.currentOverlayType = OVROverlay.OverlayType.Underlay;

// TODO         
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidGeckoWebView.EnableRemoteDebugging();
        AndroidGeckoWebView.SetUserPreferences(@"
            user_pref('media.autoplay.default', 0);
            user_pref('media.geckoview.autoplay.request', false);
        ");
#endif
        
        
        _holePuncher = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _holePuncher.transform.SetParent(transform, false);
        _holePuncher.transform.localScale = new Vector3(0.995f,0.995f,1f);
        
#if UNITY_ANDROID && !UNITY_EDITOR
        _holePuncher.GetComponent<Renderer>().material = holePuncherMaterial;
#else
        _holePuncher.GetComponent<Renderer>().material = dummyMaterial;
#endif

#if UNITY_EDITOR
        _overlay.isExternalSurface = false;
        _overlay.previewInEditor = false;
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
        
        BrowserReady?.Invoke();
        
        yield break;
    }

    private void HandleAspectChange()
    {
        _resolution = autoSetResolution
            ? GetAutoResolution(width, distanceEstimate, pixelPerDegree, aspect)
            : GetResolutionFromX(xResolution, aspect);
        
        _overlay.externalSurfaceWidth = _resolution.x;
        _overlay.externalSurfaceHeight = _resolution.y;
        _overlay.transform.localScale = new Vector3(width, width * aspect.y / aspect.x, 1);

        if (_webViewPrefab.WebView is null) return;
        _webViewPrefab.WebView.SetResolution(1);
        _webViewPrefab.WebView.Resize(_resolution.x, _resolution.y);
    }

    private static Vector2Int GetAutoResolution(float width, float distanceEstimate, int pixelPerDegree, Vector2 aspect)
    {
        var height = width * aspect.y / aspect.x;
        var xRes = ResolvablePixels(width, distanceEstimate, pixelPerDegree);
        var yRes = ResolvablePixels(height, distanceEstimate, pixelPerDegree);
        return new Vector2Int(xRes, yRes);
    }

    private static Vector2Int GetResolutionFromX(int xResolution, Vector2 aspect)
    {
        return new Vector2Int(xResolution, (int) Math.Round(xResolution * aspect.y / aspect.x));
    }

    public static int ResolvablePixels(float width, float distanceEstimate, int pixelPerDegree)
    {
        // calculates the optimal resolution along some dimension
        // width : side of billboard in unity units
        // distanceEstimate : estimated closest distance from (user) --- (billboard)
        // pixelPerDegree : resolving resolution of headset
        return (int) Math.Round(
            pixelPerDegree * (360f / (2 * Math.PI)) * 2 * Math.Atan(width / (2 * distanceEstimate))
        );
    }

    public void LoadUrl(string url)
    {
        _webViewPrefab.WebView.LoadUrl(url);
    }

    public void SendKeyInput(string key)
    {
        _webViewPrefab.WebView.HandleKeyboardInput(key);
    }

    public void PostMessage(string data)
    {
        _webViewPrefab.WebView.PostMessage(data);
    }
}
