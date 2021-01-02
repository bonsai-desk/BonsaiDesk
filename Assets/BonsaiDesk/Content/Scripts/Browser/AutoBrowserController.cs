using System;
using System.Collections;
using Mirror;
using OVRSimpleJSON;
using UnityEngine;
using UnityEngine.Networking;
using Vuplex.WebView;

public class YouTubeMessage
{
    public static string SetContent(string id, Vector2Int resolution)
    {
        return "{" +
               "\"type\": \"video\", " +
               "\"command\": \"setContent\", " +
               $"\"video_id\": \"{id}\", " +
               $"\"x\": {resolution.x}," +
               $"\"y\": {resolution.y}" +
               "}";
    }

    public static string Pause = "{\"type\": \"video\", \"command\": \"pause\"}";

    public static string Play = "{\"type\": \"video\", \"command\": \"play\"}";
    
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
    public bool useBuiltHTML = true;

    public string hotReloadUrl;
    public TogglePause togglePause;
    private readonly (float, float) _delayClamp = (0.1f, 0.75f);
    private readonly ClientScrubCollector _scrubCollector = new ClientScrubCollector();
    private readonly double desyncTolerance = 0.2;
    private AutoBrowser _autoBrowser;

    [SyncVar(hook = nameof(OnSetContentInfo))]
    private ContentInfo _contentInfo;

    private float _height;
    private ScrubData _myScrub;
    private int _numClientsReporting;
    private PlayerState _playerState;

    [SyncVar(hook = nameof(OnSetScreenState))] private ScreenState _screenState;

    private int _vidId;

    [SyncVar(hook = nameof(OnSetWorstScrub))]
    private ScrubData _worstScrub = new ScrubData(Mathf.Infinity, 0);

    private double _playAfter = Mathf.Infinity;
    
    public void ToggleVideo()
    {
        var vidIds = new[] {"pP44EPBMb8A", "Cg0QwoHh9w4"};

        switch (_screenState)
        {
            case ScreenState.Neutral:
                CmdSetContentId(vidIds[_vidId]);
                _vidId = _vidId == vidIds.Length - 1 ? 0 : _vidId + 1;
                break;

            case ScreenState.YouTube:
                CmdSetState(ScreenState.Neutral);
                break;

            case ScreenState.Twitch:
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

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

            if (useBuiltHTML)
                _autoBrowser.LoadHtml(BonsaiUI.Html);
            else
                _autoBrowser.LoadUrl(hotReloadUrl);
#endif

            togglePause.PauseChangedClient += OnPauseChangeClient;
            togglePause.PauseChangedServer += OnPauseChangeServer;
            _autoBrowser.OnMessageEmitted(OnMessageEmitted);
        };
    }

    private void Update()
    {

        if (_playerState == PlayerState.Paused && NetworkTime.time > _playAfter)
        {
            _autoBrowser.PostMessage(YouTubeMessage.Play);
            _playAfter = Mathf.Infinity;
        }
        
        #region screen height
        
        const float transitionTime = 0.5f;
        var browserDown = _screenState == ScreenState.Neutral;
        var targetHeight = browserDown ? 0 : 1;

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
    
    private void OnPauseChangeServer(bool paused)
    {
        Debug.Log("Reset Scrub Collector");
        _scrubCollector.Reset();
    }

    private void OnPauseChangeClient(bool paused)
    {
        Debug.Log("[BONSAI] OnPauseChangeClient: pause=" + paused);
        if (paused)
            _autoBrowser.PostMessage(YouTubeMessage.Pause);
        else
            _autoBrowser.PostMessage(YouTubeMessage.Play);
    }

    private void OnMessageEmitted(object sender, EventArgs<string> eventArgs)
    {
        var jsonNode = JSONNode.Parse(eventArgs.Value) as JSONObject;

        Debug.Log("[BONSAI] JSON recieved " + eventArgs.Value);

        if ((string) jsonNode?["type"] != "stateChange" || jsonNode["message"] is null) return;

        switch ((string) jsonNode["message"])
        {
            case "UNSTARTED":
                _playerState = PlayerState.Unstarted;
                break;

            case "ENDED":
                _playerState = PlayerState.Ended;
                break;

            case "PLAYING":
                _playerState = PlayerState.Playing;
                _myScrub = new ScrubData(jsonNode["current_time"], NetworkTime.time);
                CmdIncludeScrub(_myScrub);
                break;

            case "PAUSED":
                _playerState = PlayerState.Paused;
                _myScrub = new ScrubData(jsonNode["current_time"], NetworkTime.time);
                CmdIncludeScrub(_myScrub);
                break;

            case "BUFFERING":
                _playerState = PlayerState.Buffering;
                break;

            case "VIDEOCUED":
                _playerState = PlayerState.VideoCued;
                break;
        }
    }

    private void OnSetWorstScrub(ScrubData oldWorstScrub, ScrubData newWorstScrub)
    {
        Debug.Log("[BONSAI] OnSetWorstScrub scrub: " + newWorstScrub.Scrub + " networktime: " +
                  newWorstScrub.NetworkTime);
        var now = NetworkTime.time;
        var myTime = _myScrub.CurrentVideoTime(now);
        var worstTime = newWorstScrub.CurrentVideoTime(now);
        var deSync = myTime - worstTime;

        if (!(deSync > desyncTolerance) || _playerState != PlayerState.Playing) return;

        Debug.Log("[BONSAI] OnSetWorstScrub desync=" + deSync);

        _autoBrowser.PostMessage(YouTubeMessage.Pause);

        _playAfter = NetworkTime.time + deSync;

    }
    
    private void OnSetContentInfo(ContentInfo oldInfo, ContentInfo newInfo)
    {
        var resolution = _autoBrowser.ChangeAspect(newInfo.Aspect);

        Debug.Log("[BONSAI] OnSetContentInfo " + oldInfo.ID + "->" + newInfo.ID + " resolution: " + resolution);

        _autoBrowser.PostMessage(YouTubeMessage.SetContent(newInfo.ID, resolution));
    }

    private void OnSetScreenState(ScreenState oldState, ScreenState newState)
    {
        Debug.Log("[BONSAI] OnSetScreenState " + oldState + " -> " + newState);
    }

    [Command(ignoreAuthority = true)]
    private void CmdIncludeScrub(ScrubData scrubData)
    {
        Debug.Log("[BONSAI] CmdIncludeScrub scrub: " + scrubData.Scrub + " time: " + scrubData.NetworkTime);
        _scrubCollector.Include(scrubData);
        _worstScrub = _scrubCollector.Worst;
    }

    [Command(ignoreAuthority = true)]
    private void CmdSetState(ScreenState newState)
    {
        _screenState = newState;

        //TODO probably don't need to set interactable to false if screen was already down,
        //but then the default interactable state needs to be false
        
        togglePause.ServerSetPaused(true);
        togglePause.SetInteractable(false);
    }

    [Command(ignoreAuthority = true)]
    public void CmdSetContentId(string id)
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
        Ended,
        Playing,
        Paused,
        Buffering,
        VideoCued
    }

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

    public class ClientScrubCollector
    {
        public ScrubData Worst = new ScrubData(Mathf.Infinity, 0);

        public void Include(ScrubData scrubData)
        {
            if (!(scrubData.Scrub < Worst.Scrub)) return;
            Worst = scrubData;
        }

        public void Reset()
        {
            Worst.Scrub = Mathf.Infinity;
            Worst.NetworkTime = 0;
        }
    }
}