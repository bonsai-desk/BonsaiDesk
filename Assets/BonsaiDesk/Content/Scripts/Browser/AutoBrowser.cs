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

    private GameObject _holePuncher;
    private OVROverlay _overlay;
    private WebViewPrefab _webViewPrefab;

    private void Start()
    {
        // Set screen to below desk
        transform.position = new Vector3(transform.position.x, -height / 2, transform.position.z);

        // Enable autoplay

        // Create hole-puncher surface
        _holePuncher = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _holePuncher.transform.SetParent(transform, false);
#if UNITY_ANDROID && !UNITY_EDITOR
        _holePuncher.GetComponent<Renderer>().material = holePuncherMaterial;
#else
        _holePuncher.GetComponent<Renderer>().material = dummyMaterial;
#endif
        
        // Spin up WebView prefab
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidGeckoWebView.EnableRemoteDebugging();
        AndroidGeckoWebView.SetUserPreferences(@"
            user_pref('media.autoplay.default', 0);
            user_pref('media.geckoview.autoplay.request', false);
        ");
#endif
        _webViewPrefab = WebViewPrefab.Instantiate(aspect.x / aspect.y * height, height);
        _webViewPrefab.Visible = false;
        _webViewPrefab.Initialized +=
            (sender, eventArgs) =>
            {
                ChangeAspect(aspect);
                BrowserReady?.Invoke();
            };
    }

    #region interface

    public event BrowserReadyEvent BrowserReady;

    public void ChangeAspect(Vector2 newAspect)
    {
        aspect = newAspect;

        var resolution = Resolution();

        _webViewPrefab.WebView.SetResolution(1);
        _webViewPrefab.WebView.Resize(resolution.x, resolution.y);

        RebuildOverlay();
    }

    public IEnumerator DropScreen(float duration)
    {
        float counter = 0;
        while (counter < duration)
        {
            counter += Time.deltaTime;
            var a = CubicBezier.EaseIn.Sample(counter / duration);

            transform.position = new Vector3(
                transform.position.x, 
                (1 - a) * height / 2 + a * -height / 2,
                transform.position.z);

            _holePuncher.transform.localPosition = new Vector3(
                _holePuncher.transform.localPosition.x,
                a / 2,
                _holePuncher.transform.localPosition.z
            );

            _holePuncher.transform.localScale = new Vector3(
                _holePuncher.transform.localScale.x,
                1 - a,
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
                a,
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

    #endregion interface

    #region private methods

    private void RebuildOverlay()
    {
        var resolution = Resolution();
        Destroy(_overlay);
        _overlay = gameObject.AddComponent<OVROverlay>();
        _overlay.externalSurfaceWidth = resolution.x;
        _overlay.externalSurfaceHeight = resolution.y;
        _overlay.transform.localScale = new Vector3(aspect.x / aspect.y * height, height, 1);
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
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log("[BONSAI] SetSurface" + _overlay.externalSurfaceObject + ", WebView " + _webViewPrefab.WebView);
        (_webViewPrefab.WebView as AndroidGeckoWebView).SetSurface(_overlay.externalSurfaceObject);
        Debug.Log("[BONSAI] Done SetSurface");
#endif
        yield break;
    }

    private Vector2Int Resolution()
    {
        return autoSetResolution
            ? AutoResolution(height, distanceEstimate, pixelPerDegree, aspect)
            : ResolutionFromY(yResolution, aspect);
    }

    #endregion private methods

    #region static methods

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