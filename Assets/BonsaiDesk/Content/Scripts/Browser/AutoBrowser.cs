using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Vuplex.WebView;

public delegate void BrowserReadyEvent();

public class AutoBrowser : MonoBehaviour
{
    public float width = 1;
    
    public Vector2 aspect = new Vector2(16, 9);

    public int xResolution = 850;
    public bool autoSetResolution;
    public float distanceEstimate = 1;
    public int pixelPerDegree = 16;

    public Material holePuncherMaterial;
    public Material dummyMaterial;
    private GameObject _holePuncher;
    private Vector3 _holePuncherFixedScale = new Vector3(0.995f, 0.995f, 1f);

    private OVROverlay _overlay;

    private Vector2Int _resolution;

    private WebViewPrefab _webViewPrefab;
    
    public event BrowserReadyEvent BrowserReady;

    private void Start()
    {
        _resolution = autoSetResolution
            ? GetAutoResolution(width, distanceEstimate, pixelPerDegree, aspect)
            : GetResolutionFromX(xResolution, aspect);

        
        
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


        _webViewPrefab = WebViewPrefab.Instantiate(width, width * aspect.y / aspect.x);
        _webViewPrefab.Visible = false;
        _webViewPrefab.Initialized +=
            (sender, eventArgs) => StartCoroutine(SetupWebView(sender, eventArgs));
    }

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
        return new Vector3(width, width * aspect.y / aspect.x, 1);
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
    
    public IEnumerator SetNewAspect(Vector2 newAspect)
    {
        float height;
        float initialY;
        
        height = GetScale().y;
        initialY = transform.position.y;
        yield return DropHolePunch(1f, height, initialY);
        
        aspect = newAspect;
        
        _resolution = autoSetResolution
            ? GetAutoResolution(width, distanceEstimate, pixelPerDegree, aspect)
            : GetResolutionFromX(xResolution, aspect);
        
        _webViewPrefab.WebView.SetResolution(1);
        _webViewPrefab.WebView.Resize(_resolution.x, _resolution.y);
        
        yield return new WaitForSeconds(0.5f);
        
        SetOverlay();
        
        transform.position =
            new Vector3(transform.position.x, -transform.localScale.y / 2, transform.position.z);
        
        yield return new WaitForSeconds(0.5f);

        height = GetScale().y;
        initialY = transform.position.y;
        
        yield return RaiseHolePunch(1f, height, initialY);
    }

    private IEnumerator DropHolePunch(float duration, float height, float initialY)
    {
        float counter = 0;
        while (counter < duration)
        {
            counter += Time.deltaTime;
            var a = CubicBezier.EaseIn.Sample(counter / duration);
            Debug.Log(initialY);
            transform.position = new Vector3(transform.position.x, (1 - a) * initialY + a * -height/2, transform.position.z);
            yield return null;
        }
        
    }
    
    private IEnumerator RaiseHolePunch(float duration, float height, float initialY)
    {
        float counter = 0;
        while (counter < duration)
        {
            counter += Time.deltaTime;
            var a = CubicBezier.EaseOut.Sample(counter / duration);
            transform.position = new Vector3(transform.position.x, (1 - a) * initialY + a * height/2, transform.position.z);
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
            pixelPerDegree * (360f / (2 * Math.PI)) * 2 * Math.Atan(width / (2 * distanceEstimate))
        );
    }
}