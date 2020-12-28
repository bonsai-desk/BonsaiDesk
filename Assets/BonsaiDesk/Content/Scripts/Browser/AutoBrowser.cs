using System;
using System.Collections;
using UnityEngine;
using Vuplex.WebView;

public delegate void BrowserReadyEvent();

public class AutoBrowser : MonoBehaviour
{
    public float height = 1;

    public Vector2 aspect = new Vector2(16, 9);

    public int yResolution = 850;
    public bool autoSetResolution;
    public float distanceEstimate = 1;
    public int pixelPerDegree = 16;

    public Material holePuncherMaterial;
    public Material dummyMaterial;
    private readonly Vector3 _holePuncherFixedScale = new Vector3(1f, 1f, 1f);
    private GameObject _holePuncher;

    private OVROverlay _overlay;

    private Vector2Int _resolution;

    private WebViewPrefab _webViewPrefab;

    private void Start()
    {
        _resolution = autoSetResolution
            ? GetAutoResolution(height, distanceEstimate, pixelPerDegree, aspect)
            : GetResolutionFromY(yResolution, aspect);


        SetOverlay();

        transform.position =
            new Vector3(transform.position.x, transform.localScale.y / 2, transform.position.z);

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
        _holePuncher.transform.localScale = _holePuncherFixedScale;

#if UNITY_ANDROID && !UNITY_EDITOR
        _holePuncher.GetComponent<Renderer>().material = holePuncherMaterial;
#else
        _holePuncher.GetComponent<Renderer>().material = dummyMaterial;
#endif


        _webViewPrefab = WebViewPrefab.Instantiate(aspect.x / aspect.y * height, height);
        _webViewPrefab.Visible = false;
        _webViewPrefab.Initialized +=
            (sender, eventArgs) => StartCoroutine(SetupWebView(sender, eventArgs));
    }

    public event BrowserReadyEvent BrowserReady;

    private IEnumerator SwapSurface()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        while (_overlay.externalSurfaceObject == IntPtr.Zero || _webViewPrefab.WebView == null)
        {
            Debug.Log("[BONSAI] while WebView not setup\nexternalSurfaceObject: <" + _overlay.externalSurfaceObject + ">\nWebView: <" + _webViewPrefab.WebView + ">");
            yield return null;
        }
#endif
        var surface = _overlay.externalSurfaceObject;

#if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log("[BONSAI] SetSurface" + surface + ", WebView " + _webViewPrefab.WebView);
        (_webViewPrefab.WebView as AndroidGeckoWebView).SetSurface(surface);
        Debug.Log("[BONSAI] Done SetSurface");
#endif
        yield break;
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

    private Vector3 GetScale()
    {
        return new Vector3(aspect.x / aspect.y * height, height, 1);
    }

    private void SetOverlay()
    {
        Destroy(_overlay);
        _overlay = gameObject.AddComponent<OVROverlay>();
        _overlay.externalSurfaceWidth = _resolution.x;
        _overlay.externalSurfaceHeight = _resolution.y;
        _overlay.transform.localScale = GetScale();
        _overlay.currentOverlayType = OVROverlay.OverlayType.Underlay;

#if UNITY_EDITOR
        _overlay.isExternalSurface = false;
#else
        _overlay.isExternalSurface = true;
#endif

        StartCoroutine(SwapSurface());
    }

    private static Vector2Int GetAutoResolution(float height, float distanceEstimate, int pixelPerDegree,
        Vector2 aspect)
    {
        var yRes = ResolvablePixels(height, distanceEstimate, pixelPerDegree);
        return GetResolutionFromY(yRes, aspect);
    }

    private static Vector2Int GetResolutionFromY(int yResolution, Vector2 aspect)
    {
        return new Vector2Int((int) (aspect.x / aspect.y * yResolution), yResolution);
    }

    public void ChangeAspect(Vector2 newAspect)
    {
        Debug.Log("[BONSAI] NewAspect " + newAspect);

        aspect = newAspect;

        _resolution = autoSetResolution
            ? GetAutoResolution(height, distanceEstimate, pixelPerDegree, aspect)
            : GetResolutionFromY(yResolution, aspect);

        Debug.Log("[BONSAI] NewResolution " + _resolution);

        _webViewPrefab.WebView.SetResolution(1);
        _webViewPrefab.WebView.Resize(_resolution.x, _resolution.y);

        SetOverlay();
    }

    public IEnumerator DropScreen(float duration)
    {
        float counter = 0;
        while (counter < duration)
        {
            counter += Time.deltaTime;
            var a = CubicBezier.EaseIn.Sample(counter / duration);

            transform.position = new Vector3(transform.position.x, (1 - a) * height / 2 + a * -height / 2,
                transform.position.z);

            _holePuncher.transform.localPosition = new Vector3(
                _holePuncher.transform.localPosition.x,
                a / 2,
                _holePuncher.transform.localPosition.z
            );

            _holePuncher.transform.localScale = new Vector3(
                _holePuncher.transform.localScale.x,
                (1 - a) * _holePuncherFixedScale.y,
                _holePuncher.transform.localScale.z
            );

            yield return null;
        }
    }

    public IEnumerator RaiseScreen(float duration)
    {
        float counter = 0;
        while (counter < duration)
        {
            counter += Time.deltaTime;
            var a = CubicBezier.EaseOut.Sample(counter / duration);

            transform.position = new Vector3(
                transform.position.x,
                -((1 - a) * height / 2) + a * height / 2,
                transform.position.z
            );

            _holePuncher.transform.localPosition = new Vector3(
                _holePuncher.transform.localPosition.x,
                (1 - a) / 2,
                _holePuncher.transform.localPosition.z
            );

            _holePuncher.transform.localScale = new Vector3(
                _holePuncher.transform.localScale.x,
                a * _holePuncherFixedScale.y,
                _holePuncher.transform.localScale.z
            );

            yield return null;
        }
    }

    public void LoadUrl(string url)
    {
        _webViewPrefab.WebView.LoadUrl(url);
    }

    public void PostMessage(string data)
    {
        _webViewPrefab.WebView.PostMessage(data);
    }

    public static int ResolvablePixels(float width, float distanceEstimate, int pixelPerDegree)
    {
        // calculates the optimal resolution along some dimension
        // width : side of billboard in unity units
        // distanceEstimate : estimated closest distance from (user) --- (billboard)
        // pixelPerDegree : resolving resolution of headset
        return (int) Math.Round(
            pixelPerDegree * (360f / (2f * Math.PI)) * 2f * Math.Atan(width / (2f * distanceEstimate))
        );
    }
}