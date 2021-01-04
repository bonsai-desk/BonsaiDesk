using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using OVRSimpleJSON;
using UnityEngine;
using UnityEngine.Networking;
using Vuplex.WebView;

public class YouTubeMessage
{
    public static string Pause = "{\"type\": \"video\", \"command\": \"pause\"}";

    public static string Play = "{\"type\": \"video\", \"command\": \"play\"}";

    public static string GoHome = "{" +
                                  "\"type\": \"nav\", " +
                                  "\"command\": \"goHome\" " +
                                  "}";

    public static string SeekTo(double time)
    {
        return "{" +
               "\"type\": \"video\", " +
               "\"command\": \"seekTo\", " +
               $"\"seekTime\": {time}" +
               "}";
    }
    
    public static string LoadVideo(string id, float ts)
    {
        return "{" +
               "\"type\": \"nav\", " +
               "\"command\": \"push\", " +
               $"\"path\": \"/youtube/{id}/{ts}\"" +
               "}";
    }
}

public class PingUtils
{
    private static float GetDelay(double worstPing, (float, float) delayClamp)
    {
        return Mathf.Clamp(
            (float) (1.5 * worstPing), delayClamp.Item1, delayClamp.Item2);
    }

    private static double Sigma3Ping()
    {
        return NetworkTime.rtt / 2 + 3 * (NetworkTime.rttSd / 2);
    }
}

[RequireComponent(typeof(AutoBrowser))]
public class AutoBrowserController : NetworkBehaviour
{
    #region variables

    public bool useBuiltHtml = true;

    public string hotReloadUrl;

    public TogglePause togglePause;

    private readonly Dictionary<uint, double> _clientLastPingTime = new Dictionary<uint, double>();

    private bool _allGood;

    private AutoBrowser _autoBrowser;

    private float _height;

    private ScrubData _idealScrub;

    private Coroutine _idealScrubRoutine;

    private float _lastPingSentTime;

    private int _numClientsReporting;

    private float _playerCurrentTime;

    private PlayerState _playerState;

    private int _vidId;

    #endregion variables

    #region syncvars

    [SyncVar(hook = nameof(OnSetContentInfo))]
    private ContentInfo _contentInfo;

    [SyncVar(hook = nameof(OnSetScreenState))]
    private ScreenState _screenState;

    #endregion syncvars

    #region unity

    private void Start()
    {
        _screenState = ScreenState.Neutral;
        _playerState = PlayerState.Unstarted;
        _autoBrowser = GetComponent<AutoBrowser>();

        _autoBrowser.BrowserReady += () =>
        {
#if !DEVELOPMENT_BUILD
            _autoBrowser.LoadHtml(BonsaiUI.Html);
#else
            if (useBuiltHtml)
                _autoBrowser.LoadHtml(BonsaiUI.Html);
            else
                _autoBrowser.LoadUrl(hotReloadUrl);
#endif
            _autoBrowser.OnMessageEmitted(OnMessageEmitted);
        };
    }

    private void Update()
    {
        // check if any of the clients did not ping recently
        if (isServer && _allGood)
        {
            const float pingTolerance = 1f;
            foreach (var entry in _clientLastPingTime)
                if (NetworkTime.time - entry.Value > pingTolerance)
                {
                    _allGood = false;
                    break;
                }
        }

        // trip the failsafe and sync the clients
        if (!_allGood) ServerPlayVideoAtTime(0, NetworkTime.time + 0.5);

        // ping the server with the client current player time
        if (isClient)
        {
            const float pingInterval = 0.1f;
            if (Time.time - _lastPingSentTime > pingInterval)
            {
                _lastPingSentTime = Time.time;
                CmdPing(NetworkClient.connection.identity.netId, _playerCurrentTime);
            }
        }

        #region screen height

        const float transitionTime = 0.5f;
        var browserDown = _screenState == ScreenState.Neutral;
        var targetHeight = browserDown ? 0 : 1;
        
        // TODO
        browserDown = false;
        targetHeight = 1;

        if (!Mathf.Approximately(_height, targetHeight))
        {
            var easeFunction = browserDown ? CubicBezier.EaseOut : CubicBezier.EaseIn;
            var t = easeFunction.SampleInverse(_height);
            var step = 1f / transitionTime * Time.deltaTime;
            t = Mathf.MoveTowards(t, targetHeight, step);
            _height = easeFunction.Sample(t);
        }

        _autoBrowser.SetHeight(_height);

        if (isServer)
            if (Mathf.Approximately(_height, 1) && !togglePause.Interactable)
                togglePause.SetInteractable(true);

        #endregion
    }

    #endregion unity

    #region failsafe

    public struct ScrubData
    {
        public double Scrub;
        public double NetworkTime;

        public double CurrentVideoTime(double currentNetworkTime)
        {
            return Scrub + (currentNetworkTime - NetworkTime);
        }

        public ScrubData(double scrub, double networkTime)
        {
            Scrub = scrub;
            NetworkTime = networkTime;
        }
    }

    [Server]
    private void ServerPlayVideoAtTime(double timeStamp, double networkTime)
    {
        _clientLastPingTime.Clear();
        _idealScrub = new ScrubData(timeStamp, networkTime);
        // TODO
        //RpcPlayVideoAtTime(timeStamp, networkTime);
        _allGood = true;
    }

    [ClientRpc]
    private void RpcPlayVideoAtTime(double timeStamp, double networkTime)
    {
        if (_idealScrubRoutine != null) StopCoroutine(_idealScrubRoutine);
        _idealScrubRoutine = StartCoroutine(ClientStartAtTime(new ScrubData(timeStamp, networkTime)));
    }

    private IEnumerator ClientStartAtTime(ScrubData data)
    {
        _autoBrowser.PostMessage(YouTubeMessage.Pause);
        _autoBrowser.PostMessage(YouTubeMessage.SeekTo(data.Scrub));
        while (NetworkTime.time < data.NetworkTime) yield return null;
        _autoBrowser.PostMessage(YouTubeMessage.Play);
        _idealScrubRoutine = null;
    }

    [Command(ignoreAuthority = true)]
    private void CmdPing(uint id, float clientTimeStamp)
    {
        const float threshold = 1f;
        var whereTheyShouldBe = _idealScrub.CurrentVideoTime(NetworkTime.time);
        if (Math.Abs(clientTimeStamp - whereTheyShouldBe) > threshold)
        {
            _allGood = false;
            _clientLastPingTime.Clear();
            return;
        }

        _clientLastPingTime[id] = NetworkTime.time;
    }

    #endregion failsafe

    #region video loading

    private void OnMessageEmitted(object sender, EventArgs<string> eventArgs)
    {
        var jsonNode = JSONNode.Parse(eventArgs.Value) as JSONObject;

        Debug.Log("[BONSAI] JSON recieved " + eventArgs.Value);

        if (jsonNode?["type"].Value == "infoCurrentTime")
        {
            _playerCurrentTime = jsonNode["current_time"];
            return;
        }

        if (jsonNode?["type"].Value == "stateChange")
            switch ((string) jsonNode["message"])
            {
                case "UNSTARTED":
                    _playerState = PlayerState.Unstarted;
                    break;

                case "VIDEOCUED":
                    _playerState = PlayerState.VideoCued;
                    break;

                case "READY":
                    _playerState = PlayerState.Ready;
                    break;

                case "PAUSED":
                    _playerState = PlayerState.Paused;
                    break;

                case "PLAYING":
                    _playerState = PlayerState.Playing;
                    break;

                case "BUFFERING":
                    _playerState = PlayerState.Buffering;
                    // TODO notify the server
                    break;

                case "ENDED":
                    _playerState = PlayerState.Ended;
                    break;
            }
    }

    [Command(ignoreAuthority = true)]
    private void CmdSetScreenState(ScreenState newState)
    {
        _screenState = newState;

        //TODO probably don't need to set interactable to false if screen was already down,
        //but then the default interactable state needs to be false

        togglePause.ServerSetPaused(true);
        togglePause.SetInteractable(false);
    }

    private void OnSetScreenState(ScreenState oldState, ScreenState newState)
    {
        Debug.Log("[BONSAI] OnSetScreenState " + oldState + " -> " + newState);
    }

    [Command(ignoreAuthority = true)]
    public void CmdLoadVideo(string id)
    {
        StartCoroutine(
            FetchYouTubeAspect(id, newAspect =>
            {
                print("[BONSAI] FetchYouTubeAspect callback");
                _contentInfo = new ContentInfo(id, newAspect);
                _screenState = ScreenState.YouTube;
            })
        );
    }

    [Command(ignoreAuthority = true)]
    public void CmdCloseVideo()
    {
        RpcGoHome();
        _screenState = ScreenState.Neutral;
    }

    [ClientRpc]
    public void RpcGoHome()
    {
        _autoBrowser.PostMessage(YouTubeMessage.GoHome);
    }

    private void OnSetContentInfo(ContentInfo oldInfo, ContentInfo newInfo)
    {
        var resolution = _autoBrowser.ChangeAspect(newInfo.Aspect);

        Debug.Log("[BONSAI] OnSetContentInfo " + oldInfo.ID + "->" + newInfo.ID + " resolution: " + resolution);

        _autoBrowser.PostMessage(YouTubeMessage.LoadVideo(newInfo.ID, 0));
    }

    private static IEnumerator FetchYouTubeAspect(string videoId, Action<Vector2> callback)
    {
        var newAspect = new Vector2(16, 9);

        var videoInfoUrl = $"https://api.desk.link/youtube/{videoId}";

        using (var www = UnityWebRequest.Get(videoInfoUrl))
        {
            var req = www.SendWebRequest();

            yield return req;

            if (!(www.isHttpError || www.isNetworkError))
            {
                var jsonNode = JSONNode.Parse(www.downloadHandler.text) as JSONObject;
                if (jsonNode?["width"] != null && jsonNode["height"] != null)
                {
                    var width = (float) jsonNode["width"];
                    var height = (float) jsonNode["height"];
                    newAspect = new Vector2(width, height);
                }
            }
        }

        callback(newAspect);
    }

    public void ToggleVideo()
    {
        var vidIds = new[] {"pP44EPBMb8A", "Cg0QwoHh9w4"};

        switch (_screenState)
        {
            case ScreenState.Neutral:
                //CmdSetContentId(vidIds[_vidId]);
                CmdLoadVideo(vidIds[_vidId]);
                _vidId = _vidId == vidIds.Length - 1 ? 0 : _vidId + 1;
                break;

            case ScreenState.YouTube:
                CmdCloseVideo();
                //CmdSetScreenState(ScreenState.Neutral);
                break;

            case ScreenState.Twitch:
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        togglePause.SetInteractable(false);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        var browserDown = _screenState == ScreenState.Neutral;
        var targetHeight = browserDown ? 0 : 1;
        _height = targetHeight;
    }

    private readonly struct ContentInfo
    {
        public readonly string ID;
        public readonly Vector2 Aspect;

        public ContentInfo(string id, Vector2 aspect)
        {
            ID = id;
            Aspect = aspect;
        }
    }

    private enum ScreenState
    {
        Neutral,
        YouTube,
        Twitch
    }

    private enum PlayerState
    {
        Unstarted,
        VideoCued,
        Ready,
        Paused,
        Playing,
        Buffering,
        Ended
    }

    #endregion video loading
}