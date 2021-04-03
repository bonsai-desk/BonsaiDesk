using UnityEngine;
using Vuplex.WebView;

public class BrowserSetup : MonoBehaviour
{
    public bool forceEnableDebugging;

    private void Awake()
    {
        PreConfigureWebView();
    }

    // Start is called before the first frame update
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
    }

    private void PreConfigureWebView()
    {
        BonsaiLog("Preconfigure WebView");
        Web.SetUserAgent(true);
        if (forceEnableDebugging)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidGeckoWebView.EnableRemoteDebugging();
#elif UNITY_EDITOR
            StandaloneWebView.EnableRemoteDebugging(8080);
#endif
        }
        else
        {
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
			"resource://android/assets/adguard-3.5.34/",
			"adguardadblocker@adguard.com"
		);
		AndroidGeckoWebView.EnsureBuiltInExtension(
			"resource://android/assets/bonsai-youtube/",
			"browser-agent@bonsaidesk.com"
		);
#elif UNITY_EDITOR
        StandaloneWebView.SetCommandLineArguments("--autoplay-policy=no-user-gesture-required");
#endif
    }

    private void BonsaiLog(string msg)
    {
        Debug.Log("<color=orange>BonsaiBrowserSetup: </color>: " + msg);
    }

    private void BonsaiLogWarning(string msg)
    {
        Debug.LogWarning("<color=orange>BonsaiBrowserSetup: </color>: " + msg);
    }

    private void BonsaiLogError(string msg)
    {
        Debug.LogError("<color=orange>BonsaiBrowserSetup: </color>: " + msg);
    }
}