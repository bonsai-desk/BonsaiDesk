using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Vuplex.WebView;

public class Browser : MonoBehaviour {
	public Vector2 startingAspect = new Vector2(16, 9);
	public Material holePuncherMaterial;
	public float distanceEstimate = 1;
	public int pixelPerDegree = 16;
	public Transform boundsTransform;
	public Transform overlayTransform;
	public Transform holePuncherTransform;
	public Transform screenTransform;
	public string initialUrl;
	public bool useBuiltHtml;
	public Vector2 Bounds;
	public DragMode dragMode;
	private GameObject _boundsObject;
	private OVROverlay _overlay;
	private bool _postedListenersReady;
	private bool _renderEnabled = true;
	protected Transform Resizer;
	protected WebViewPrefabCustom WebViewPrefab;
	protected Transform WebViewView;

	protected virtual void Start() {
		Debug.Log("browser start");

		CacheTransforms();

		// WebView preconfiguring is done once in the BrowserSetup class

		SetupWebViewPrefab();

		SetupHolePuncher();
	}

	public event Action BrowserReady;

	public event Action ListenersReady;

	private void SetupHolePuncher() {
	#if UNITY_ANDROID && !UNITY_EDITOR
        holePuncherTransform.GetComponent<Renderer>().sharedMaterial = holePuncherMaterial;
	#else
		holePuncherTransform.GetComponent<MeshRenderer>().enabled = false;
	#endif
	}

	private void CacheTransforms() {
		Bounds = boundsTransform.transform.localScale.xy();
	}

	protected void RebuildOverlay(Vector2Int resolution) {
		Destroy(_overlay);
		_overlay                       = overlayTransform.gameObject.AddComponent<OVROverlay>();
		_overlay.externalSurfaceWidth  = resolution.x;
		_overlay.externalSurfaceHeight = resolution.y;

		_overlay.currentOverlayType = OVROverlay.OverlayType.Underlay;
		_overlay.isExternalSurface  = true;
		StartCoroutine(UpdateAndroidSurface());
	}

	private IEnumerator UpdateAndroidSurface() {
	#if UNITY_ANDROID && !UNITY_EDITOR
        while (_overlay.externalSurfaceObject == IntPtr.Zero || WebViewPrefab.WebView == null)
        {
            Debug.Log("[BONSAI] while WebView not setup\nexternalSurfaceObject: <" + _overlay.externalSurfaceObject + ">\nWebView: <" + WebViewPrefab.WebView + ">");
            yield return null;
        }

        Debug.Log("[BONSAI] SetSurface" + _overlay.externalSurfaceObject + ", WebView " + WebViewPrefab.WebView);
        (WebViewPrefab.WebView as AndroidGeckoWebView).SetSurface(_overlay.externalSurfaceObject);
        Debug.Log("[BONSAI] Done SetSurface");
	#endif
		yield break;
	}

	protected virtual void SetupWebViewPrefab() {
		WebViewPrefab.Initialized += (sender, eventArgs) =>
		{
			Debug.Log("[BONSAI] Browser Initialized");
			WebViewPrefab.WebView.MessageEmitted += HandleJavaScriptMessage;
			WebViewPrefab.DragMode               =  dragMode;
			BrowserReady?.Invoke();
		};
	#if UNITY_EDITOR || DEVELOPMENT_BUILD
		if (useBuiltHtml) {
			initialUrl = "streaming-assets://build/index.html";
		}
	#else
		initialUrl = "streaming-assets://build/index.html";
	#endif

		WebViewPrefab.InitialUrl = initialUrl;
	}

	private void HandleJavaScriptMessage(object _, EventArgs<string> eventArgs) {
		var message = JsonConvert.DeserializeObject<JsMessageString>(eventArgs.Value);

		switch (message.Type) {
			case "event":
				switch (message.Message) {
					case "listenersReady":
						Debug.Log($"[BONSAI] invoke listeners ready ... {_postedListenersReady}");
						if (!useBuiltHtml) {
							// todo: for some reason when using a hot reload url
							// the app posts listeners-ready twice so we just check it here
							if (!_postedListenersReady) {
								ListenersReady?.Invoke();
							}
							else {
								Debug.LogWarning("[BONSAI] Browser trying to post listeners twice, ignoring");
							}
						}
						else {
							ListenersReady?.Invoke();
						}

						_postedListenersReady = true;

						break;
				}

				break;
		}
	}

	private static void PreConfigureWebView() {
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
		AndroidGeckoWebView.EnsureBuiltInExtension(
			"resource://android/assets/ublock/",
			"uBlock0@raymondhill.net"
		);
	#elif UNITY_EDITOR
		StandaloneWebView.SetCommandLineArguments("--autoplay-policy=no-user-gesture-required");
	#endif
	}

	public void SetHidden(bool hidden) {
		_renderEnabled = !hidden;

		WebViewPrefab.ClickingEnabled  = _renderEnabled;
		WebViewPrefab.ScrollingEnabled = _renderEnabled;

	#if UNITY_ANDROID && !UNITY_EDITOR
        holePuncherTransform.GetComponent<MeshRenderer>().enabled = _renderEnabled;
	#else
		WebViewView.GetComponent<MeshRenderer>().enabled = _renderEnabled;
	#endif
	}

	public void ToggleHidden() {
		SetHidden(_renderEnabled);
	}

	public void LoadUrl(string url) {
		if (WebViewPrefab.WebView != null) {
			WebViewPrefab.WebView.LoadUrl(url);
		}
		else {
			Debug.LogWarning("[BONSAI] Tried to load url when WebView is null");
		}
	}

	public void LoadHtml(string html) {
		Debug.Log("load html");
		WebViewPrefab.WebView.LoadHtml(html);
	}

	public void PostMessage(string data) {
		WebViewPrefab.WebView.PostMessage(data);
	}

	public void GoBack() {
		WebViewPrefab.WebView.GoBack();
	}

	public void GoForward() {
		WebViewPrefab.WebView.GoForward();
	}

	public void HandleKeyboardInput(string key) {
		Debug.Log($"[BONSAI] HandleKeyboardInput {key} {WebViewPrefab.WebView}");
		WebViewPrefab.WebView.HandleKeyboardInput(key);
	}

	public void PostMessages(IEnumerable<string> msgs) {
		foreach (var data in msgs) {
			PostMessage(data);
		}
	}

	public void OnMessageEmitted(EventHandler<EventArgs<string>> messageEmitted) {
		WebViewPrefab.WebView.MessageEmitted += messageEmitted;
	}

	private static int ResolvablePixels(float height, float distanceEstimate, int pixelPerDegree) {
		// calculates the optimal resolution along some dimension
		// height : side of billboard in unity units
		// distanceEstimate : estimated closest distance from (user) --- (billboard)
		// pixelPerDegree : resolving resolution of headset
		return (int) Math.Round(
			pixelPerDegree * (360f / (2f * Math.PI)) * 2f * Math.Atan(height / (2f * distanceEstimate))
		);
	}

	protected static Vector2Int AutoResolution(Vector2 span, float distance, int ppd, Vector2 aspect) {
		Vector2Int resolution;

		if (span.y > span.x) {
			var res = ResolvablePixels(span.y, distance, ppd);
			resolution = ResolutionFromY(res, aspect);
		}
		else {
			var res = ResolvablePixels(span.x, distance, ppd);
			resolution = ResolutionFromX(res, aspect);
		}

		return resolution;
	}

	private static Vector2Int ResolutionFromX(int xResolution, Vector2 aspect) {
		return new Vector2Int(xResolution, (int) (aspect.y / aspect.x * xResolution));
	}

	private static Vector2Int ResolutionFromY(int yResolution, Vector2 aspect) {
		return new Vector2Int((int) (aspect.x / aspect.y * yResolution), yResolution);
	}

	public static class BrowserMessage {
		public static readonly string NavToMenu = PushPath("/menu");
		public static readonly string NavHome = PushPath("/home");
		public static readonly string NavKeyboard = PushPath("/keyboard");
		public static readonly string NavWebNav = PushPath("/webnav");

		private static string PushPath(string path) {
			return "{" +
			       "\"type\": \"nav\", " +
			       "\"command\": \"push\", " +
			       $"\"path\": \"{path}\"" +
			       "}";
		}
	}

	public struct JsMessageString {
		public string Data;
		public string Message;
		public string Type;
	}
}