using System;
using System.Collections;
using UnityEngine;
using Vuplex.WebView;

public delegate void BrowserReadyEvent();

public class AutoBrowser : MonoBehaviour
{
    public Vector2 startingAspect = new Vector2(16, 9);
    public Transform holePuncher;
    public Material holePuncherMaterial;

    public float distanceEstimate = 1;
    public int pixelPerDegree = 16;

    private Transform _overlayObject;

    private OVROverlay _overlay;
    private WebViewPrefab _webViewPrefab;

    private Vector3 _defaultLocalPosition;
    private Vector3 _belowTableLocalPosition;

    private Vector2 _bounds;

    //private MeshRenderer _holePuncherRenderer;

    private float TargetHeight;
    public float Height;

    public bool ShouldBeRaised;

    private void Start()
    {
        //create empty overlay object
        _overlayObject = new GameObject().transform;
        _overlayObject.name = "OverlayObject";
        _overlayObject.SetParent(transform.GetChild(0), false);

        //setup hole puncher object
        //_holePuncherRenderer = holePuncher.GetComponent<MeshRenderer>();
        _bounds = holePuncher.transform.localScale.xy();
        _defaultLocalPosition = transform.localPosition;
        _belowTableLocalPosition = _defaultLocalPosition;
        _belowTableLocalPosition.y = -_bounds.y / 2f;

        //TODO
        //_holePuncherRenderer.enabled = false;
        transform.localPosition = _belowTableLocalPosition;
        holePuncher.localPosition = _belowTableLocalPosition;

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
        _webViewPrefab =
            WebViewPrefab.Instantiate(holePuncher.localScale.x / holePuncher.localScale.y * holePuncher.localScale.y,
                holePuncher.localScale.y);
        _webViewPrefab.Visible = false;
        _webViewPrefab.Initialized +=
            (sender, eventArgs) =>
            {
                ChangeAspect(startingAspect);
                BrowserReady?.Invoke();
            };
    }

    #region interface

    public void SetHeight(float t)
    {
        transform.localPosition = Vector3.Lerp(_belowTableLocalPosition, _defaultLocalPosition, Mathf.Clamp01(t));
        
        var height = _overlayObject.localScale.y;
        var halfHeight = height / 2f;
        t = (transform.localPosition.y + halfHeight) / height;
        t = Mathf.Clamp01(t); //1 is visible, 0 is invisible

        var holePunchScale = holePuncher.localScale;
        holePunchScale.y = _overlayObject.localScale.y * t;
        holePuncher.localScale = holePunchScale;

        var holePunchPosition = holePuncher.localPosition;
        holePunchPosition.y = halfHeight * (1f - t);
        holePuncher.localPosition = holePunchPosition;
    }

    public event BrowserReadyEvent BrowserReady;

    public void ChangeAspect(Vector2 newAspect)
    {
        var aspectRatio = newAspect.x / newAspect.y;
        holePuncher.localScale = new Vector3(_bounds.x, _bounds.y, 1);
        holePuncher.localScale = new Vector3(holePuncher.localScale.y * aspectRatio, holePuncher.localScale.y, 1);
        if (holePuncher.localScale.x > _bounds.x)
            holePuncher.localScale = new Vector3(_bounds.x, _bounds.x * (1f / aspectRatio), 1);

        var resolution = Resolution();

        _webViewPrefab.WebView.SetResolution(1);
        _webViewPrefab.WebView.Resize(resolution.x, resolution.y);

        RebuildOverlay();
    }

//   public IEnumerator DropScreen(float duration)
//   {
//       yield return MoveScreen(duration, CubicBezier.EaseIn,
//           _defaultLocalPosition, _belowTableLocalPosition);
//       _holePuncherRenderer.enabled = false;
//   }
//
//   public IEnumerator RaiseScreen(float duration)
//   {
//       _holePuncherRenderer.enabled = true;
//       yield return MoveScreen(duration, CubicBezier.EaseOut,
//           _belowTableLocalPosition, _defaultLocalPosition);
//   }
//
//   public IEnumerator MoveScreen(float duration, CubicBezier easeFunction, Vector3 from, Vector3 to)
//   {
//       float counter = 0;
//       while (counter < 1f)
//       {
//           counter += 1f / duration * Time.deltaTime;
//           var t = easeFunction.Sample(counter);
//
//           //lerp browser height
//           transform.localPosition = Vector3.Lerp(from, to, t);
//
//           //lerp hole puncher
//           var height = _overlayObject.localScale.y;
//           var halfHeight = height / 2f;
//           t = (transform.localPosition.y + halfHeight) / height;
//           t = Mathf.Clamp01(t); //1 is visible, 0 is invisible
//
//           var holePunchScale = holePuncher.localScale;
//           holePunchScale.y = _overlayObject.localScale.y * t;
//           holePuncher.localScale = holePunchScale;
//
//           var holePunchPosition = holePuncher.localPosition;
//           holePunchPosition.y = halfHeight * (1f - t);
//           holePuncher.localPosition = holePunchPosition;
//
//           yield return null;
//       }
//
//       holePuncher.localScale = _overlayObject.localScale;
//       holePuncher.localPosition = new Vector3(holePuncher.localPosition.x, 0, holePuncher.localPosition.z);
//   }

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
        Debug.Log("[BONSAI] PostMessage " + data);
        _webViewPrefab.WebView.PostMessage(data);
    }
    
    public void OnMessageEmitted(EventHandler<EventArgs<string>> messageEmitted)
    {
        _webViewPrefab.WebView.MessageEmitted += messageEmitted;
    }

    #endregion interface

    #region private methods

    private void RebuildOverlay()
    {
        var resolution = Resolution();
        Destroy(_overlay);
        _overlay = _overlayObject.gameObject.AddComponent<OVROverlay>();
        _overlay.externalSurfaceWidth = resolution.x;
        _overlay.externalSurfaceHeight = resolution.y;
        var localScale = new Vector3(holePuncher.localScale.x / holePuncher.localScale.y * holePuncher.localScale.y,
            holePuncher.localScale.y, 1);
        _overlayObject.localScale = localScale;
        holePuncher.localScale = localScale;
        _overlay.currentOverlayType = OVROverlay.OverlayType.Underlay;

#if UNITY_EDITOR
        _overlay.isExternalSurface = false;
#else
        _overlay.isExternalSurface = true;
#endif

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

    private Vector2Int Resolution()
    {
        // return autoSetResolution
        //     ? AutoResolution(holePuncher.localScale.y, distanceEstimate, pixelPerDegree, holePuncher.localScale.xy())
        //     : ResolutionFromY(yResolution, holePuncher.localScale.xy());
        return AutoResolution(holePuncher.localScale.y, distanceEstimate, pixelPerDegree, holePuncher.localScale.xy());
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