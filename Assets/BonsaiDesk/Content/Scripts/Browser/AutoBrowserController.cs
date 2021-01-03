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

    public static string SeekTo(double time)
    {
        return "{" +
               "\"type\": \"video\", " +
               "\"command\": \"seekTo\", " +
               $"\"seekTime\": {time}" +
               "}";
    }

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

    private bool _allGood = false;
    private AutoBrowser _autoBrowser;

    private readonly Dictionary<uint, double> _clientLastPingTime = new Dictionary<uint, double>();

    [SyncVar(hook = nameof(OnSetContentInfo))]
    private ContentInfo _contentInfo;

    private float _height;

    private float _lastPingSentTime;
    private ScrubData _myScrub;
    private int _numClientsReporting;

    private double _playAfter = Mathf.Infinity;
    private PlayerState _playerState;

    [SyncVar(hook = nameof(OnSetScreenState))]
    private ScreenState _screenState;

    private int _vidId;

    [SyncVar(hook = nameof(OnSetWorstScrub))]
    private ScrubData _worstScrub = new ScrubData(0, Mathf.NegativeInfinity);

    private float playDelay = 0.1f;

    private float _playerCurrentTime;

    private ScrubData _idealScrub;

    private Coroutine _idealScrubRoutine;

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
        if (isServer && _allGood)
        {
            var pingTolerance = 1f;
            foreach (var entry in _clientLastPingTime)
                if (NetworkTime.time - entry.Value > pingTolerance)
                {
                    _allGood = false;
                    break;
                }
        }

        if (!_allGood)
        {
            ServerPlayVideoAtTime(0, NetworkTime.time + 0.5);
        }

        if (isClient)
        {
            var pingInterval = 0.1f;
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

    [Server]
    private void ServerPlayVideoAtTime(float timeStamp, double networkTime)
    {
        _clientLastPingTime.Clear();
        _idealScrub = new ScrubData(timeStamp, networkTime);
        RpcPlayVideoAtTime(timeStamp, networkTime);
        _allGood = true;
    }

    [ClientRpc]
    private void RpcPlayVideoAtTime(float timeStamp, double networkTime)
    {
        if (_idealScrubRoutine != null) {
            StopCoroutine(_idealScrubRoutine);
        }
        _idealScrubRoutine = StartCoroutine(ClientStartAtTime(new ScrubData(timeStamp, networkTime)));

    }

    private IEnumerator ClientStartAtTime(ScrubData data)
    {
        _autoBrowser.PostMessage(YouTubeMessage.Pause);
        _autoBrowser.PostMessage(YouTubeMessage.SeekTo(data.Scrub));
        while (NetworkTime.time < data.NetworkTime)
        {
            yield return null;
        }
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
                CmdSetScreenState(ScreenState.Neutral);
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

    private void OnPauseChangeServer(bool paused)
    {
        return;
        Debug.Log("Reset Scrub Collector");
        _scrubCollector.Reset();
        if (!paused) _worstScrub = new ScrubData(_worstScrub.Scrub, NetworkTime.time + 200f);
    }

    private void OnPauseChangeClient(bool paused)
    {
        return;
        Debug.Log("[BONSAI] OnPauseChangeClient: pause=" + paused);
        if (paused)
        {
            _autoBrowser.PostMessage(YouTubeMessage.Pause);
        }
        else
        {
            var now = NetworkTime.time;
            var myTime = _myScrub.Scrub;
            var worstTime = _worstScrub.CurrentVideoTime(now);
            var deSync = myTime - worstTime;
            _playAfter = now + deSync;

            Debug.Log("[BONSAI] ClientPlay myTime ");
            Debug.Log("[BONSAI] ClientPlay playAfter " + _playAfter);
        }
    }

    private void OnMessageEmitted(object sender, EventArgs<string> eventArgs)
    {
        var jsonNode = JSONNode.Parse(eventArgs.Value) as JSONObject;

        Debug.Log("[BONSAI] JSON recieved " + eventArgs.Value);

        if (jsonNode?["type"].Value == "infoCurrentTime")
        {
            _playerCurrentTime = jsonNode["current_time"];
            return;
        }

        if ((string) jsonNode?["type"] != "stateChange" || jsonNode["message"] is null) return;

        switch ((string) jsonNode["message"])
        {
            case "UNSTARTED":
                _playerState = PlayerState.Unstarted;
                _myScrub = new ScrubData(0, Mathf.NegativeInfinity);
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
                _myScrub = new ScrubData(jsonNode["current_time"], Mathf.NegativeInfinity);
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

    [Command(ignoreAuthority = true)]
    private void CmdIncludeScrub(ScrubData scrubData)
    {
        Debug.Log("[BONSAI] CmdIncludeScrub scrub: " + scrubData.Scrub + " time: " + scrubData.NetworkTime);
        _scrubCollector.Include(scrubData);
        _worstScrub = _scrubCollector.Worst;
    }

    private void OnSetWorstScrub(ScrubData oldWorstScrub, ScrubData newWorstScrub)
    {
        Debug.Log("[BONSAI] OnSetWorstScrub scrub: " + newWorstScrub.Scrub + " networktime: " +
                  newWorstScrub.NetworkTime);
        var now = NetworkTime.time;
        var myTime = _myScrub.CurrentVideoTime(now);
        var worstTime = newWorstScrub.CurrentVideoTime(now);
        var deSync = myTime - worstTime;

        if (deSync < desyncTolerance || _playerState != PlayerState.Playing) return;

        Debug.Log("[BONSAI] OnSetWorstScrub desync=" + deSync);

        _autoBrowser.PostMessage(YouTubeMessage.Pause);

        _playAfter = NetworkTime.time + deSync;
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

    private void OnSetContentInfo(ContentInfo oldInfo, ContentInfo newInfo)
    {
        var resolution = _autoBrowser.ChangeAspect(newInfo.Aspect);

        Debug.Log("[BONSAI] OnSetContentInfo " + oldInfo.ID + "->" + newInfo.ID + " resolution: " + resolution);

        _autoBrowser.PostMessage(YouTubeMessage.SetContent(newInfo.ID, resolution));
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