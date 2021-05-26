using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Vuplex.WebView;

public class Browser : MonoBehaviour
{
    [HideInInspector] public bool Initialized;
    public Vector2 startingAspect = new Vector2(16, 9);
    public float distanceEstimate = 1;
    public int pixelPerDegree = 16;
    public Transform boundsTransform;
    public Transform overlayTransform;
    public Transform holePuncherTransform;
    public Transform stencilTransform;
    public Transform screenTransform;
    public string initialUrl;
    public bool useBuiltHtml;
    public Vector2 Bounds;
    public DragMode dragMode;
    public WebViewPrefabCustom WebViewPrefab;
    [FormerlySerializedAs("Hidden")] public bool hidden;
    private GameObject _boundsObject;
    private OVROverlay _overlay;
    private bool _postedListenersReady;
    protected Material holePuncherMaterial;
    protected Transform Resizer;
    protected Transform WebViewView;
    public Transform WebViewTransform;
    public int compositionDepth = 0;

    protected virtual void Start()
    {
        BonsaiLog("Start browser");

        holePuncherMaterial = new Material(Resources.Load<Material>("OnTopUnderlay"));

        CacheTransforms();

        // WebView preconfiguring is done once in the BrowserSetup class

        SetupWebViewPrefab();

        WebViewTransform = WebViewView.transform;

        SetupHolePuncher();
    }

    public event EventHandler BrowserReady;

    public event Action ListenersReady;

    public void SetMaterialOnTop()
    {
        if (WebViewPrefab)
        {
            WebViewPrefab.SetMaterialOnTop();
        }

        holePuncherMaterial.renderQueue = (int) RenderQueue.Overlay;
        holePuncherMaterial.SetInt("_ZTest", (int) CompareFunction.Always);
    }

    public void SetMaterialRegular()
    {
        if (WebViewPrefab)
        {
            WebViewPrefab.SetMaterialRegular();
        }

        holePuncherMaterial.renderQueue = (int) RenderQueue.Geometry + 1;
        holePuncherMaterial.SetInt("_ZTest", (int) CompareFunction.LessEqual);
    }

    private void SetupHolePuncher()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        holePuncherTransform.GetComponent<Renderer>().sharedMaterial = holePuncherMaterial;
		WebViewView.GetComponent<MeshRenderer>().enabled = false;
#else
        holePuncherTransform.GetComponent<MeshRenderer>().enabled = false;
#endif
    }

    private void CacheTransforms()
    {
        Bounds = boundsTransform.transform.localScale.xy();
    }

    protected void RebuildOverlay(Vector2Int resolution)
    {
        Destroy(_overlay);
        _overlay = overlayTransform.gameObject.AddComponent<OVROverlay>();
        _overlay.externalSurfaceWidth = resolution.x;
        _overlay.externalSurfaceHeight = resolution.y;
        _overlay.compositionDepth = compositionDepth;
        

        _overlay.currentOverlayType = OVROverlay.OverlayType.Underlay;
        _overlay.isExternalSurface = true;
        StartCoroutine(UpdateAndroidSurface());
    }

    private IEnumerator UpdateAndroidSurface()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        while (_overlay.externalSurfaceObject == IntPtr.Zero || WebViewPrefab.WebView == null)
        {
            BonsaiLog("While WebView not setup\nexternalSurfaceObject: <" + _overlay.externalSurfaceObject + ">\nWebView: <" + WebViewPrefab.WebView + ">");
            yield return null;
        }

        BonsaiLog("SetSurface" + _overlay.externalSurfaceObject + ", WebView " + WebViewPrefab.WebView);
        (WebViewPrefab.WebView as AndroidGeckoWebView).SetSurface(_overlay.externalSurfaceObject);
        BonsaiLog("SetSurface complete");
#endif
        yield break;
    }

    protected virtual void SetupWebViewPrefab()
    {
        WebViewPrefab.Initialized += (sender, eventArgs) =>
        {
            Initialized = true;
            BonsaiLog("Browser initialized");
            WebViewPrefab.WebView.MessageEmitted += HandleJavaScriptMessage;
            WebViewPrefab.DragMode = dragMode;
            BrowserReady?.Invoke(this, new EventArgs());
        };

        if (useBuiltHtml)
        {
            initialUrl = "streaming-assets://build/index.html";
        }

        WebViewPrefab.InitialUrl = initialUrl;
    }

    private void HandleJavaScriptMessage(object _, EventArgs<string> eventArgs)
    {
        var message = JsonConvert.DeserializeObject<JsMessageString>(eventArgs.Value);
        
        switch (message.Type)
        {
            case "event":
                switch (message.Message)
                {
                    case "stoppedScrolling":
                        WebViewPrefab.StopResidualScrolling();
                        break;
                    case "listenersReady":
                        // todo: for some reason when using a hot reload url
                        // the app posts listeners-ready twice so we just check it here
                        if (!_postedListenersReady)
                        {
                            ListenersReady?.Invoke();
                            _postedListenersReady = true;
                        }
                        else
                        {
                            BonsaiLog("Browser trying to post listeners twice, ignoring");
                        }


                        break;
                }

                break;
        }
    }

    public void SetHidden(bool choice)
    {
        hidden = choice;
        var renderEnabled = !choice;

        stencilTransform.gameObject.SetActive(!choice);

#if UNITY_ANDROID && !UNITY_EDITOR
		if (_overlay != null) {
			_overlay.hidden = choice;
		}
        holePuncherTransform.GetComponent<MeshRenderer>().enabled = renderEnabled;
#else
        WebViewView.GetComponent<MeshRenderer>().enabled = renderEnabled;
#endif
    }

    public void LoadUrl(string url)
    {
        if (WebViewPrefab.WebView != null)
        {
            WebViewPrefab.WebView.LoadUrl(url);
        }
        else
        {
            BonsaiLogWarning("Tried to load url when WebView is null");
        }
    }

    public void LoadHtml(string html)
    {
        WebViewPrefab.WebView.LoadHtml(html);
    }

    public void PostMessage(string data)
    {
        WebViewPrefab.WebView?.PostMessage(data);
    }

    public void GoBack()
    {
        WebViewPrefab.WebView.GoBack();
    }

    public void GoForward()
    {
        WebViewPrefab.WebView.GoForward();
    }

    public void HandleKeyboardInput(string key)
    {
        BonsaiLog($"HandleKeyboardInput {key} {WebViewPrefab.WebView}");
        WebViewPrefab.WebView.HandleKeyboardInput(key);
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
        WebViewPrefab.WebView.MessageEmitted += messageEmitted;
    }

    private static int ResolvablePixels(float height, float distanceEstimate, int pixelPerDegree)
    {
        // calculates the optimal resolution along some dimension
        // height : side of billboard in unity units
        // distanceEstimate : estimated closest distance from (user) --- (billboard)
        // pixelPerDegree : resolving resolution of headset
        return (int) Math.Round(pixelPerDegree * (360f / (2f * Math.PI)) * 2f * Math.Atan(height / (2f * distanceEstimate)));
    }

    protected static Vector2Int AutoResolution(Vector2 span, float distance, int ppd, Vector2 aspect)
    {
        Vector2Int resolution;

        if (span.y > span.x)
        {
            var res = ResolvablePixels(span.y, distance, ppd);
            resolution = ResolutionFromY(res, aspect);
        }
        else
        {
            var res = ResolvablePixels(span.x, distance, ppd);
            resolution = ResolutionFromX(res, aspect);
        }

        return resolution;
    }

    private static Vector2Int ResolutionFromX(int xResolution, Vector2 aspect)
    {
        return new Vector2Int(xResolution, (int) (aspect.y / aspect.x * xResolution));
    }

    private static Vector2Int ResolutionFromY(int yResolution, Vector2 aspect)
    {
        return new Vector2Int((int) (aspect.x / aspect.y * yResolution), yResolution);
    }

    public void SetVolume(float level)
    {
        level = Mathf.Clamp(level, 0, 1);
        var js = $"document.querySelectorAll('video, audio').forEach(mediaElement => mediaElement.volume = {level})";
        WebViewPrefab.WebView?.ExecuteJavaScript(js);
    }

    public static class BrowserMessage
    {
        public static readonly string NavToMenu = PushPath("/menu/home");
        public static readonly string NavHome = PushPath("/home");
        public static readonly string NavKeyboard = PushPath("/keyboard");
        public static readonly string NavWebNav = PushPath("/webnav");
        public static readonly string NavContext = PushPath("/context");

        private static string PushPath(string path)
        {
            return "{" + "\"type\": \"nav\", " + "\"command\": \"push\", " + $"\"path\": \"{path}\"" + "}";
        }
    }

    public struct JsMessageString
    {
        public string Data;
        public string Message;
        public string Type;
    }

    private void BonsaiLog(string msg)
    {
        Debug.Log("<color=orange>BonsaiBrowser: </color>: " + msg);
    }

    private void BonsaiLogWarning(string msg)
    {
        Debug.LogWarning("<color=orange>BonsaiBrowser: </color>: " + msg);
    }

    private void BonsaiLogError(string msg)
    {
        Debug.LogError("<color=orange>BonsaiBrowser: </color>: " + msg);
    }
}