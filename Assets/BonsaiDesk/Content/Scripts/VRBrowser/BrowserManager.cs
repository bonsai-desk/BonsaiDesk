/**
* Copyright (c) 2020 Vuplex Inc. All rights reserved.
*
* Licensed under the Vuplex Commercial Software Library License, you may
* not use this file except in compliance with the License. You may obtain
* a copy of the License at
*
*     https://vuplex.com/commercial-library-license
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System.Globalization;
using UnityEngine;
using Vuplex.WebView;
using Vuplex.WebView.Demos;

internal class BrowserManager : MonoBehaviour
{
    public static BrowserManager instance;

    public enum StartBehaviour
    {
        LoadGoogle,
        LoadUrl,
        LoadHtml,
    }

    public enum StartVisibility
    {
        Visible,
        Hidden
    }

    public StartBehaviour startBehaviour = StartBehaviour.LoadGoogle;
    public StartVisibility startVisibility = StartVisibility.Visible;

    public string url = "";
    public TextAsset html;

    //private Timer _buttonRefreshTimer = new Timer();
    //private WebViewPrefab _controlsWebViewPrefab;

    private HardwareKeyboardListener _hardwareKeyboardListener;
    private WebViewPrefab _mainWebViewPrefab;

    private int _youtubePlayerState = -1;

    public int YoutubePlayerState
    {
        get { return _youtubePlayerState; }
        set
        {
            _youtubePlayerState = value;
            NetworkVRPlayer.self.CmdUpdateYoutubePlayerState(value);
        }
    }

    private class WebViewMessage
    {
        public string type = "unstarted";

        public string message = "";
    }

    private WebViewMessage webViewMessage;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    private void Start()
    {
        Web.SetUserAgent(true);

#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidGeckoWebView.EnableRemoteDebugging();
        AndroidGeckoWebView.SetUserPreferences(@"
            pref('media.autoplay.default', 0);
            pref('media.autoplay.enabled.user-gestures-needed', 0);
        ");
#endif

        int layer = LayerMask.NameToLayer("UI");

        // Create a 0.6 x 0.3 webview for the main web content.
        float width = 1f;
        float height = width * (9f / 16f);
        _mainWebViewPrefab = WebViewPrefab.Instantiate(width, height);
        SetLayerRecursive(_mainWebViewPrefab.gameObject, layer);
        _mainWebViewPrefab.transform.parent = transform;
        _mainWebViewPrefab.transform.localPosition = new Vector3(0, height / 2f, 0);
        _mainWebViewPrefab.transform.localEulerAngles = new Vector3(0, 180f, 0);
        _mainWebViewPrefab.Initialized += (initializedSender, initializedEventArgs) =>
        {
            switch (startBehaviour)
            {
                case StartBehaviour.LoadGoogle:
                    _mainWebViewPrefab.WebView.LoadUrl("https://www.google.com/");
                    break;

                case StartBehaviour.LoadHtml:
                    if (html != null && !string.IsNullOrEmpty(html.text))
                    {
                        _mainWebViewPrefab.WebView.LoadHtml(html.text);
                    }
                    else
                    {
                        Debug.LogError("Could not load browser html. Defaulting to loading Google.");
                        _mainWebViewPrefab.WebView.LoadUrl("https://www.google.com/");
                    }
                    break;

                case StartBehaviour.LoadUrl:
                    if (!string.IsNullOrEmpty(url))
                    {
                        _mainWebViewPrefab.WebView.LoadUrl(url);
                    }
                    else
                    {
                        Debug.LogError("Url is empty. Defaulting to loading Google.");
                        _mainWebViewPrefab.WebView.LoadUrl("https://www.google.com/");
                    }
                    break;

                default:
                    Debug.LogError("Unknown start behaviour. Defaulting to loading Google.");
                    _mainWebViewPrefab.WebView.LoadUrl("https://www.google.com/");
                    break;
            }

            // _mainWebViewPrefab.WebView.UrlChanged += MainWebView_UrlChanged;
            _mainWebViewPrefab.WebView.MessageEmitted += WebView_MessageEmitted;

            if (startVisibility == StartVisibility.Hidden)
                _mainWebViewPrefab.gameObject.SetActive(false);
        };

        // Send keys from the hardware keyboard to the main webview.
        _hardwareKeyboardListener = HardwareKeyboardListener.Instantiate();
        _hardwareKeyboardListener.InputReceived += (sender, eventArgs) =>
        {
            // Include key modifiers if the webview supports them.
            if (_mainWebViewPrefab.WebView is IWithKeyModifiers webViewWithKeyModifiers)
            {
                webViewWithKeyModifiers.HandleKeyboardInput(eventArgs.Value, eventArgs.Modifiers);
            }
            else
            {
                _mainWebViewPrefab.WebView.HandleKeyboardInput(eventArgs.Value);
            }
        };

        // // Also add an on-screen keyboard under the main webview.
        // var keyboard = Keyboard.Instantiate();
        // setLayerRecursive(keyboard.gameObject, layer);
        // keyboard.transform.parent = _mainWebViewPrefab.transform;
        // keyboard.transform.localPosition = new Vector3(0, -0.31f, 0);
        // keyboard.transform.localEulerAngles = Vector3.zero;
        // keyboard.InputReceived += (sender, eventArgs) =>
        // {
        //     _mainWebViewPrefab.WebView.HandleKeyboardInput(eventArgs.Value);
        // };

        // // Create a second webview above the first to show a UI that
        // // displays the current URL and provides back / forward navigation buttons.
        // _controlsWebViewPrefab = WebViewPrefab.Instantiate(0.6f, 0.05f);
        // setLayerRecursive(_controlsWebViewPrefab.gameObject, layer);
        // _controlsWebViewPrefab.transform.parent = _mainWebViewPrefab.transform;
        // _controlsWebViewPrefab.transform.localPosition = new Vector3(0, 0.06f, 0);
        // _controlsWebViewPrefab.transform.localEulerAngles = Vector3.zero;
        // _controlsWebViewPrefab.Initialized += (sender, eventArgs) =>
        // {
        //     _controlsWebViewPrefab.WebView.LoadHtml(CONTROLS_HTML);
        //     _controlsWebViewPrefab.WebView.MessageEmitted += Controls_MessageEmitted;
        // };

        // // Set up a timer to allow the state of the back / forward buttons to be
        // // refreshed one second after a URL change occurs.
        // _buttonRefreshTimer.AutoReset = false;
        // _buttonRefreshTimer.Interval = 1000;
        // _buttonRefreshTimer.Elapsed += ButtonRefreshTimer_Elapsed;
    }

    private void WebView_MessageEmitted(object sender, EventArgs<string> eventArgs)
    {
        webViewMessage = JsonUtility.FromJson<WebViewMessage>(eventArgs.Value);
        switch (webViewMessage.type)
        {
            case "unstarted":
                YoutubePlayerState = -1;
                print("unstarted message");
                break;

            case "ended":
                YoutubePlayerState = 0;
                print("ended message");
                NetworkVRPlayer.self.StopVideo();
                break;

            case "playing":
                YoutubePlayerState = 1;
                print("playing message");
                // NetworkVRPlayer.self.resumeVideo();
                break;

            case "paused":
                YoutubePlayerState = 2;
                print("paused message");
                // NetworkVRPlayer.self.pauseVideo();
                break;

            case "buffering":
                YoutubePlayerState = 3;
                print("buffering message");
                break;

            case "cued":
                YoutubePlayerState = 5;
                print("cued message");
                break;
            // case "videoLength":
            //     print("Video Length: " + webViewMessage.message);
            //     break;
            case "updateTime":
                // print("Time: " + webViewMessage.message);
                UpdateTime(webViewMessage.message);
                break;

            default:
                print("unknown WebView message: " + webViewMessage.type + " : " + webViewMessage.message);
                break;
        }
    }

    private void UpdateTime(string time)
    {
        float timeFloat = float.Parse(time, CultureInfo.InvariantCulture);
        print("Time Float: " + timeFloat);
        NetworkVRPlayer.self.CmdUpdateYoutubePlayerCurrentTime(timeFloat);
    }

    public void LoadVideo(string videoId)
    {
        _mainWebViewPrefab.gameObject.SetActive(true);
        SendMeessage("loadVideoById", videoId);
    }

    public void CueVideo(string videoId)
    {
        _mainWebViewPrefab.gameObject.SetActive(true);
        SendMeessage("cueVideoById", videoId);
    }

    public void PauseVideo()
    {
        SendMeessage("pause", "");
    }

    public void ResumeVideo()
    {
        SendMeessage("resume", "");
    }

    public void StartVideo()
    {
        SendMeessage("startVideo", "");
    }

    public void StopVideo()
    {
        _mainWebViewPrefab.gameObject.SetActive(false);
        SendMeessage("stopVideo", "");
    }

    private void SendMeessage(string type, string message)
    {
        _mainWebViewPrefab.WebView.PostMessage("{\"type\": \"" + type + "\", \"message\": \"" + message + "\"}");
    }

    private void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }

    // void ButtonRefreshTimer_Elapsed(object sender, ElapsedEventArgs eventArgs)
    // {
    //     // Get the main webview's back / forward state and then post a message
    //     // to the controls UI to update its buttons' state.
    //     Dispatcher.RunOnMainThread(() =>
    //     {
    //         _mainWebViewPrefab.WebView.CanGoBack(canGoBack =>
    //         {
    //             _mainWebViewPrefab.WebView.CanGoForward(canGoForward =>
    //             {
    //                 var serializedMessage = String.Format(
    //                     "{{ \"type\": \"SET_BUTTONS\", \"canGoBack\": {0}, \"canGoForward\": {1} }}",
    //                     canGoBack.ToString().ToLower(),
    //                     canGoForward.ToString().ToLower()
    //                 );
    //                 _controlsWebViewPrefab.WebView.PostMessage(serializedMessage);
    //             });
    //         });
    //     });
    // }

    // void Controls_MessageEmitted(object sender, EventArgs<string> eventArgs)
    // {
    //     var serializedMessage = eventArgs.Value;
    //     var type = BridgeMessage.ParseType(serializedMessage);
    //     if (type == "GO_BACK")
    //     {
    //         _mainWebViewPrefab.WebView.GoBack();
    //     }
    //     else if (type == "GO_FORWARD")
    //     {
    //         _mainWebViewPrefab.WebView.GoForward();
    //     }
    // }

    // void MainWebView_UrlChanged(object sender, UrlChangedEventArgs eventArgs)
    // {
    //     if (_controlsWebViewPrefab.WebView != null)
    //     {
    //         var serializedMessage = String.Format("{{ \"type\": \"SET_URL\", \"url\": \"{0}\" }}", eventArgs.Url);
    //         _controlsWebViewPrefab.WebView.PostMessage(serializedMessage);
    //     }
    //     _buttonRefreshTimer.Start();
    // }

    //     const string CONTROLS_HTML = @"
    //             <!DOCTYPE html>
    //             <html>
    //                 <head>
    //                     <!-- This transparent meta tag instructs 3D WebView to allow the page to be transparent. -->
    //                     <meta name='transparent' content='true'>
    //                     <meta charset='UTF-8'>
    //                     <style>
    //                         body {
    //                             font-family: Helvetica, Arial, Sans-Serif;
    //                             margin: 0;
    //                             height: 100vh;
    //                             color: white;
    //                         }
    //                         .controls {
    //                             display: flex;
    //                             justify-content: space-between;
    //                             align-items: center;
    //                             height: 100%;
    //                         }
    //                         .controls > div {
    //                             background-color: rgba(0, 0, 0, 0.3);
    //                             border-radius: 8px;
    //                             height: 100%;
    //                         }
    //                         .url-display {
    //                             flex: 0 0 75%;
    //                             width: 75%;
    //                             display: flex;
    //                             align-items: center;
    //                             overflow: hidden;
    //                         }
    // # url {
    //                             width: 100%;
    //                             white-space: nowrap;
    //                             overflow: hidden;
    //                             text-overflow: ellipsis;
    //                             padding: 0 15px;
    //                             font-size: 18px;
    //                         }
    //                         .buttons {
    //                             flex: 0 0 20%;
    //                             width: 20%;
    //                             display: flex;
    //                             justify-content: space-around;
    //                             align-items: center;
    //                         }
    //                         .buttons > button {
    //                             font-size: 40px;
    //                             background: none;
    //                             border: none;
    //                             outline: none;
    //                             color: white;
    //                             margin: 0;
    //                             padding: 0;
    //                         }
    //                         .buttons > button:disabled {
    //                             color: rgba(255, 255, 255, 0.3);
    //                         }
    //                         .buttons > button:last-child {
    //                             transform: scaleX(-1);
    //                         }
    //                     </style>
    //                 </head>
    //                 <body>
    //                     <div class='controls'>
    //                         <div class='url-display'>
    //                             <div id='url'></div>
    //                         </div>
    //                         <div class='buttons'>
    //                             <button id='back-button' disabled='true' onclick='vuplex.postMessage({ type: ""GO_BACK"" })'>←</button>
    //                             <button id='forward-button' disabled='true' onclick='vuplex.postMessage({ type: ""GO_FORWARD"" })'>←</button>
    //                         </div>
    //                     </div>
    //                     <script>
    //                         // Handle messages sent from C#
    //                         function handleMessage(message) {
    //                             var data = JSON.parse(message.data);
    //                             if (data.type === 'SET_URL') {
    //                                 document.getElementById('url').innerText = data.url;
    //                             } else if (data.type === 'SET_BUTTONS') {
    //                                 document.getElementById('back-button').disabled = !data.canGoBack;
    //                                 document.getElementById('forward-button').disabled = !data.canGoForward;
    //                             }
    //                         }

    //                         function attachMessageListener() {
    //                             window.vuplex.addEventListener('message', handleMessage);
    //                         }

    //                         if (window.vuplex) {
    //                             attachMessageListener();
    //                         } else {
    //                             window.addEventListener('vuplexready', attachMessageListener);
    //                         }
    //                     </script>
    //                 </body>
    //             </html>
    //         ";
}