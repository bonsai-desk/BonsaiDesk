using System;
using System.Collections;
using Mirror;
using OVRSimpleJSON;
using UnityEngine;
using UnityEngine.Networking;
using Vuplex.WebView;

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

    [SyncVar] private ScreenState _screenState;

    [SyncVar] private string _started;

    [SyncVar(hook = nameof(OnSetWorstScrub))]
    private ScrubData _worstScrub = new ScrubData(10e10, 0);

    private int _vidId;

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
                CmdSetState(ScreenState.Neutral);
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
        Debug.Log("Reset Scrub Collector");
        _scrubCollector.Reset();
    }

    private void OnPauseChangeClient(bool paused)
    {
        Debug.Log("[BONSAI] OnPauseChangeClient: pause=" + paused);
        if (paused)
            _autoBrowser.PostMessage(PauseMessage());
        else
            _autoBrowser.PostMessage(PlayMessage());
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
                CmdClientPlaying(_myScrub);
                break;

            case "PAUSED":
                _playerState = PlayerState.Paused;
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

        _autoBrowser.PostMessage(PauseMessage());
        StartCoroutine(PlayAfter(NetworkTime.time + deSync));
    }

    [Command(ignoreAuthority = true)]
    private void CmdClientPlaying(ScrubData scrubData)
    {
        Debug.Log("[BONSAI] CmdClientPlaying scrub: " + scrubData.Scrub + " time: " + scrubData.NetworkTime);
        _scrubCollector.Include(scrubData);
        _worstScrub = _scrubCollector.Worst;
    }

    [Command(ignoreAuthority = true)]
    private void CmdSetState(ScreenState newState)
    {
        _screenState = newState;

        //TODO probably don't need to set interactable to false if screen was already down,
        //but then the default interactable state needs to be false
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

    private void OnSetContentInfo(ContentInfo oldInfo, ContentInfo newInfo)
    {
        var resolution = _autoBrowser.ChangeAspect(newInfo.Aspect);

        Debug.Log("[BONSAI] OnSetContentInfo " + oldInfo.ID + "->" + newInfo.ID + " resolution: " + resolution);

        _autoBrowser.PostMessage(SetContentMessage(newInfo.ID, resolution));
    }

    private IEnumerator PlayAfter(double startAfterNetworkTime)
    {
        Debug.Log("[BONSAI] (now-startAfterNetworkTime) = " + (float) (NetworkTime.time - startAfterNetworkTime));
        while (NetworkTime.time < startAfterNetworkTime) yield return null;
        Debug.Log("[BONSAI] (now-startAfterNetworkTime) = " + (float) (NetworkTime.time - startAfterNetworkTime));
        _autoBrowser.PostMessage(PlayMessage());
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

    private static string SetContentMessage(string id, Vector2Int resolution)
    {
        return "{" +
               "\"type\": \"video\", " +
               "\"command\": \"setContent\", " +
               $"\"video_id\": \"{id}\", " +
               $"\"x\": {resolution.x}," +
               $"\"y\": {resolution.y}" +
               "}";
    }

    private static string PauseMessage()
    {
        return "{\"type\": \"video\", \"command\": \"pause\"}";
    }

    private static string PlayMessage()
    {
        return "{\"type\": \"video\", \"command\": \"play\"}";
    }

    private static float GetDelay(double worstPing, (float, float) delayClamp)
    {
        return Mathf.Clamp(
            (float) (1.5 * worstPing), delayClamp.Item1, delayClamp.Item2);
    }

    private static double Sigma3Ping()
    {
        return NetworkTime.rtt / 2 + 3 * (NetworkTime.rttSd / 2);
    }

    private readonly struct ContentInfo
    {
        public readonly string ID;
        public readonly Vector2 Aspect;

        public ContentInfo(string id, Vector2 aspect)
        {
            this.ID = id;
            this.Aspect = aspect;
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
        public ScrubData Worst = new ScrubData(10e10, 0);

        public void Include(ScrubData scrubData)
        {
            if (!(scrubData.Scrub < Worst.Scrub)) return;
            Worst = scrubData;
        }

        public void Reset()
        {
            Worst.Scrub = 10e10;
            Worst.NetworkTime = 0;
        }
    }
}