using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Vuplex.WebView;

public class Browser : MonoBehaviour
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
    protected WebViewPrefab _webViewPrefab;

    protected void Start()
    {
        Debug.Log("browser start");
        CacheTransforms();

        //SetupOverlayObject();

        SetupWebViewPrefab();
        
        SetupHolePuncher();
    }

    private void Update()
    {
    }

    private void StretchHolePuncher()
    {
        
    }


    public event Action BrowserReady;

    private void SetupHolePuncher()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        holePuncherTransform.GetComponent<Renderer>().sharedMaterial = holePuncherMaterial;
        _webViewPrefab.Visible = false;
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

    private void RebuildOverlay(Vector2Int resolution)
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

    public Vector2Int ChangeAspect(Vector2 newAspect)
    {
        var aspectRatio = newAspect.x / newAspect.y;
        var localScale = new Vector3(_bounds.y * aspectRatio, _bounds.y, 1);
        if (localScale.x > _bounds.x)
            localScale = new Vector3(_bounds.x, _bounds.x * (1f / aspectRatio), 1);
        

        var resolution = AutoResolution(localScale.y, distanceEstimate, pixelPerDegree, newAspect);

        if (!Mathf.Approximately(1, _webViewPrefab.WebView.Resolution))
        {
            _webViewPrefab.WebView.SetResolution(1);
        }
        
        _webViewPrefab.WebView.Resize(resolution.x, resolution.y);

        boundsTransform.localScale = localScale;
        
#if UNITY_ANDROID && !UNITY_EDITOR
        RebuildOverlay(resolution);
#endif

        return resolution;
    }

    private void SetupWebViewPrefab()
    {
        PreConfigureWebView();

        _webViewPrefab = WebViewPrefab.Instantiate(1, 1);
        Destroy(_webViewPrefab.Collider);
        _webViewPrefab.transform.SetParent(boundsTransform, false);
        _webViewPrefab.transform.localPosition = new Vector3(0, 0.5f, 0);
        _webViewPrefab.transform.localEulerAngles = new Vector3(0, 180, 0);

        _webViewPrefab.Initialized += (sender, eventArgs) =>
        {
            ChangeAspect(startingAspect);
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

    private static Vector2Int AutoResolution(float height, float distance, int ppd, Vector2 aspect)
    {
        var yRes = ResolvablePixels(height, distance, ppd);
        return ResolutionFromY(yRes, aspect);
    }

    private static Vector2Int ResolutionFromY(int yResolution, Vector2 aspect)
    {
        return new Vector2Int((int) (aspect.x / aspect.y * yResolution), yResolution);
    }
}
