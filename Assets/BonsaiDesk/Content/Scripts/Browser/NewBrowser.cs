using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuplex.WebView;

public class NewBrowser : MonoBehaviour
{
    public Vector2 startingAspect = new Vector2(16, 9);
    public Material holePuncherMaterial;
    public float distanceEstimate = 1;
    public int pixelPerDegree = 16;
    protected Vector2 _bounds;
    private GameObject _boundsObject;
    private OVROverlay _overlay;
    public Transform boundsTransform;
    public Transform overlayTransform;
    public Transform holePuncherTransform;
    public Transform screenTransform;
    protected WebViewPrefabCustom _webViewPrefab;
    private Transform _resizer;
    private Transform _webViewView;

    protected virtual void Start()
    {
        Debug.Log("browser start");
        
        CacheTransforms();

        //SetupOverlayObject();

        SetupWebViewPrefab();
        
        SetupHolePuncher();
    }

    public event Action BrowserReady;

    private void SetupHolePuncher()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        holePuncherTransform.GetComponent<Renderer>().sharedMaterial = holePuncherMaterial;
#else
        holePuncherTransform.GetComponent<MeshRenderer>().enabled = false;
#endif
    }

    private void SetupOverlayObject()
    {;
        throw new NotImplementedException();
    }

    private void CacheTransforms()
    {
        _bounds = boundsTransform.transform.localScale.xy();
    }

    protected void RebuildOverlay(Vector2Int resolution)
    {
        Destroy(_overlay);
        _overlay = overlayTransform.gameObject.AddComponent<OVROverlay>();
        _overlay.externalSurfaceWidth = resolution.x;
        _overlay.externalSurfaceHeight = resolution.y;
        
        _overlay.currentOverlayType = OVROverlay.OverlayType.Underlay;
        _overlay.isExternalSurface = true;
        StartCoroutine(UpdateAndroidSurface());
    }

    private IEnumerator UpdateAndroidSurface()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        while (_overlay.externalSurfaceObject == IntPtr.Zero || _webViewPrefab.WebView == null)
        {
            Debug.Log("[BONSAI] while WebView not setup\nexternalSurfaceObject: <" + _overlay.externalSurfaceObject + ">\nWebView: <" + _webViewPrefab.WebView + ">");
            yield return null;
        }

        Debug.Log("[BONSAI] SetSurface" + _overlay.externalSurfaceObject + ", WebView " + _webViewPrefab.WebView);
        (_webViewPrefab.WebView as AndroidGeckoWebView).SetSurface(_overlay.externalSurfaceObject);
        Debug.Log("[BONSAI] Done SetSurface");
#endif
        yield break;
    }

    public virtual Vector2Int ChangeAspect(Vector2 newAspect)
    {
        throw new NotImplementedException();
    }


    private void SetupWebViewPrefab()
    {
        PreConfigureWebView();
        
        _webViewPrefab = WebViewPrefabCustom.Instantiate(_bounds.x, _bounds.y);
        
        _webViewPrefab.transform.localPosition = Vector3.zero;
        
        _webViewPrefab.transform.SetParent(screenTransform, false);
        
        _resizer = _webViewPrefab.transform.Find("WebViewPrefabResizer");
        _webViewView = _resizer.transform.Find("WebViewPrefabView");
        
        holePuncherTransform.SetParent(_webViewView, false);
        overlayTransform.SetParent(_webViewView, false);

#if UNITY_ANDROID && !UNITY_EDITOR
        _webViewView.GetComponent<MeshRenderer>().enabled = false;
#endif

        _webViewPrefab.Initialized += (sender, eventArgs) =>
        {
            const int ppuu = 2000;
            _webViewPrefab.WebView.SetResolution(ppuu);
            var res = new Vector2Int((int) (ppuu * _bounds.x), (int) (ppuu * _bounds.y));
            RebuildOverlay(res);
            //ChangeAspect(startingAspect);
            BrowserReady?.Invoke();
        };
    }

    private static void PreConfigureWebView()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidGeckoWebView.EnableRemoteDebugging();
#elif UNITY_EDITOR
        StandaloneWebView.EnableRemoteDebugging(8080);
#endif
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidGeckoWebView.SetUserPreferences(@"
            user_pref('media.autoplay.default', 0);
            user_pref('media.geckoview.autoplay.request', false);
        ");
#elif UNITY_EDITOR
        StandaloneWebView.SetCommandLineArguments("--autoplay-policy=no-user-gesture-required");
#endif
    }


    public void LoadUrl(string url)
    {
        _webViewPrefab.WebView.LoadUrl(url);
    }

    public void LoadHtml(string html)
    {
        Debug.Log("load html");
        _webViewPrefab.WebView.LoadHtml(html);
    }

    public void PostMessage(string data)
    {
        //Debug.Log($"[BONSAI {NetworkClient.connection.identity.netId}] PostMessage {data}");
        _webViewPrefab.WebView.PostMessage(data);
    }

    public void PostMessages(IEnumerable<string> msgs)
    {
        foreach (var data in msgs) PostMessage(data);
    }

    public void OnMessageEmitted(EventHandler<EventArgs<string>> messageEmitted)
    {
        _webViewPrefab.WebView.MessageEmitted += messageEmitted;
    }


    public static int ResolvablePixels(float height, float distanceEstimate, int pixelPerDegree)
    {
        // calculates the optimal resolution along some dimension
        // height : side of billboard in unity units
        // distanceEstimate : estimated closest distance from (user) --- (billboard)
        // pixelPerDegree : resolving resolution of headset
        return (int) Math.Round(
            pixelPerDegree * (360f / (2f * Math.PI)) * 2f * Math.Atan(height / (2f * distanceEstimate))
        );
    }

    protected static Vector2Int AutoResolution(Vector2 span, float distance, int ppd, Vector2 aspect)
    {
        
        Vector2Int resolution;
        
        if (span.y > span.x)
        {
            var res = ResolvablePixels(span.y, distance, ppd);
            resolution =  ResolutionFromY(res, aspect);
        }
        else
        {
            var res = ResolvablePixels(span.x, distance, ppd);
            resolution =  ResolutionFromX(res, aspect);
        }

        return resolution;
    }
    
    private static Vector2Int ResolutionFromX(int xResolution, Vector2 aspect)
    {
        return new Vector2Int( xResolution, (int) (aspect.y/aspect.x * xResolution));
    }

    private static Vector2Int ResolutionFromY(int yResolution, Vector2 aspect)
    {
        return new Vector2Int((int) (aspect.x / aspect.y * yResolution), yResolution);
    }
}
