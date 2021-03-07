using UnityEngine;
using Vuplex.WebView;

public class BrowserSetup : MonoBehaviour {
	public bool forceEnableDebugging;
	private void Awake() {
		PreConfigureWebView();
	}

	// Start is called before the first frame update
	private void Start() { }

	// Update is called once per frame
	private void Update() { }

	private void PreConfigureWebView() {
		Debug.Log("[BONSAI] Preconfigure WebView");
		Web.SetUserAgent(true);
		if (forceEnableDebugging) {
	#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidGeckoWebView.EnableRemoteDebugging();
	#elif UNITY_EDITOR
		StandaloneWebView.EnableRemoteDebugging(8080);
	#endif
		}
		else {
	#if UNITY_EDITOR || DEVELOPMENT_BUILD
	#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidGeckoWebView.EnableRemoteDebugging();
	#elif UNITY_EDITOR
		StandaloneWebView.EnableRemoteDebugging(8080);
	#endif
	#endif
		}

	#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidGeckoWebView.SetUserPreferences(@"
            user_pref('media.autoplay.default', 0);
            user_pref('media.geckoview.autoplay.request', false);
        ");
		AndroidGeckoWebView.EnsureBuiltInExtension(
			"resource://android/assets/ublock/",
			"uBlock0@raymondhill.net"
		);
		AndroidGeckoWebView.EnsureBuiltInExtension(
			"resource://android/assets/bonsai-youtube/",
			"browser-agent@bonsaidesk.com"
		);
	#elif UNITY_EDITOR
		StandaloneWebView.SetCommandLineArguments("--autoplay-policy=no-user-gesture-required");
	#endif
	}
}