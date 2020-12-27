using UnityEngine;
using Vuplex.WebView;
using UnityEngine.XR;

class OculusWebViewDemo : MonoBehaviour {

    WebViewPrefab _webViewPrefab;
    Keyboard _keyboard;

    public Vector3 pos;
    public string initURL;

    void Start() {
        
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidGeckoWebView.EnableRemoteDebugging();
        AndroidGeckoWebView.SetUserPreferences(@"
            user_pref('media.autoplay.default', 0);
            user_pref('media.geckoview.autoplay.request', false);
        ");
#endif
        
           //user_pref('media.autoplay.blocking_policy', 0);

        // Create a 0.6 x 0.4 instance of the prefab.
        _webViewPrefab = WebViewPrefab.Instantiate(0.6f, 0.4f);
        _webViewPrefab.transform.parent = transform;
        _webViewPrefab.transform.localPosition = new Vector3(0, 0f, 0.6f);
        _webViewPrefab.transform.LookAt(transform);
        _webViewPrefab.Initialized += (sender, e) => {
            _webViewPrefab.WebView.LoadUrl(initURL);
        };

        // Add the keyboard under the main webview.
        _keyboard = Keyboard.Instantiate();
        _keyboard.transform.parent = _webViewPrefab.transform;
        _keyboard.transform.localPosition = new Vector3(0, -0.41f, 0);
        _keyboard.transform.localEulerAngles = new Vector3(0, 0, 0);
        // Hook up the keyboard so that characters are routed to the main webview.
        _keyboard.InputReceived += (sender, e) => _webViewPrefab.WebView.HandleKeyboardInput(e.Value);
    }

    void Update() {

        transform.position = Camera.main.transform.position + pos;
    }
}
