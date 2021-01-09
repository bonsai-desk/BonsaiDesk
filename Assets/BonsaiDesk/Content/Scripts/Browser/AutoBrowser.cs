using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Vuplex.WebView;

public delegate void BrowserReadyEvent();

public class AutoBrowser : MonoBehaviour
{
    public Vector2 startingAspect = new Vector2(16, 9);
    public Transform holePuncher;
    public Material holePuncherMaterial;
    public Rigidbody screenRigidBody;

    public float distanceEstimate = 1;
    public int pixelPerDegree = 16;

    private Transform _overlayObject;

    private OVROverlay _overlay;
    private WebViewPrefab _webViewPrefab;

    private Vector3 _defaultLocalPosition;
    private Vector3 _belowTableLocalPosition;

    private Vector2 _bounds;

    public event BrowserReadyEvent BrowserReady;

    private void Start()
    {
        //create empty overlay object
        _overlayObject = new GameObject().transform;
        _overlayObject.name = "OverlayObject";
        _overlayObject.SetParent(transform.GetChild(0), false);

        //set bounds for content contains fit
        _bounds = holePuncher.transform.localScale.xy();

        //setup above and below table location
        _defaultLocalPosition = transform.localPosition;
        _belowTableLocalPosition = _defaultLocalPosition;
        _belowTableLocalPosition.y = -_bounds.y / 2f;

        // If android, set holePuncher material to underlay punch through
#if UNITY_ANDROID && !UNITY_EDITOR
        holePuncher.GetComponent<Renderer>().sharedMaterial = holePuncherMaterial;
#endif

        // Enable autoplay and remote debugging
        // TODO enable mobile mode
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidGeckoWebView.EnableRemoteDebugging();
        AndroidGeckoWebView.SetUserPreferences(@"
            user_pref('media.autoplay.default', 0);
            user_pref('media.geckoview.autoplay.request', false);
        ");
#endif

        //initial size does not matter because it will be immediately resized by ChangeAspect
        _webViewPrefab = WebViewPrefab.Instantiate(1, 1);
        Destroy(_webViewPrefab.Collider);
        _webViewPrefab.Visible = false;
        _webViewPrefab.Initialized +=
            (sender, eventArgs) =>
            {
                
#if UNITY_ANDROID && !UNITY_EDITOR
    AndroidGeckoWebView.EnsureBuiltInExtension(
        "resource://android/assets/ublock/",
        "uBlock0@raymondhill.net"
    );
#endif
                
                ChangeAspect(startingAspect);
                BrowserReady?.Invoke();
            };
    }

    #region interface

    public void SetHeight(float t)
    {
        Vector3 oldPosition = transform.localPosition;

        transform.localPosition = Vector3.Lerp(_belowTableLocalPosition, _defaultLocalPosition, Mathf.Clamp01(t));

        var height = _overlayObject.localScale.y;
        var halfHeight = height / 2f;
        t = (transform.localPosition.y + halfHeight) / height;
        t = Mathf.Clamp01(t);

        var holePunchScale = _overlayObject.localScale;
        holePunchScale.y = _overlayObject.localScale.y * t;
        holePuncher.localScale = holePunchScale;

        var holePunchPosition = holePuncher.localPosition;
        holePunchPosition.y = halfHeight * (1f - t);
        holePuncher.localPosition = holePunchPosition;

        //TODO is this laggy? also this runs even if you don't have authority over the screen
        if (Mathf.Approximately(t, 0))
        {
            screenRigidBody.velocity = Vector3.zero;
            screenRigidBody.angularVelocity = Vector3.zero;
            transform.GetChild(0).localPosition = Vector3.zero;
            transform.GetChild(0).localRotation = Quaternion.identity;
        }
    }

    public Vector2Int ChangeAspect(Vector2 newAspect)
    {
        var aspectRatio = newAspect.x / newAspect.y;
        var localScale = new Vector3(_bounds.y * aspectRatio, _bounds.y, 1);
        if (localScale.x > _bounds.x)
            localScale = new Vector3(_bounds.x, _bounds.x * (1f / aspectRatio), 1);

        var resolution = AutoResolution(localScale.y, distanceEstimate, pixelPerDegree, newAspect);

        _webViewPrefab.WebView.SetResolution(1);
        _webViewPrefab.WebView.Resize(resolution.x, resolution.y);

        Destroy(_overlay);
        _overlay = _overlayObject.gameObject.AddComponent<OVROverlay>();
        _overlay.externalSurfaceWidth = resolution.x;
        _overlay.externalSurfaceHeight = resolution.y;
        _overlayObject.localScale = localScale;
        _overlay.currentOverlayType = OVROverlay.OverlayType.Underlay;

#if UNITY_EDITOR
        _overlay.isExternalSurface = false;
#else
        _overlay.isExternalSurface = true;
#endif

        StartCoroutine(UpdateAndroidSurface());
        
        return resolution;
    }

    public void LoadUrl(string url)
    {
        _webViewPrefab.WebView.LoadUrl(url);
    }

    public void LoadHtml(string html)
    {
        _webViewPrefab.WebView.LoadHtml(html);
    }

    public void PostMessage(string data)
    {
        //Debug.Log($"[BONSAI {NetworkClient.connection.identity.netId}] PostMessage {data}");
        _webViewPrefab.WebView.PostMessage(data);
    }

    public void PostMessages(IEnumerable<string> msgs)
    {
        foreach (var data in msgs)
        {
            PostMessage(data);
        }
        
    }

    public void OnMessageEmitted(EventHandler<EventArgs<string>> messageEmitted)
    {
        _webViewPrefab.WebView.MessageEmitted += messageEmitted;
    }

    #endregion interface

    #region private methods

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

    #endregion private methods

    #region static methods

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

    #endregion
}