using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public static string ReadyUpAtTime(double timeStamp)
    {
        return "{" +
               "\"type\": \"video\", " +
               "\"command\": \"readyUp\", " +
               $"\"timeStamp\": {timeStamp}" +
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

    private AutoBrowser _autoBrowser;

    private int _vidId;

    #endregion variables

    #region server vars

    private float _beginReadyUpTime = Mathf.Infinity;

    private bool _started;

    private bool _allGood;

    private readonly Dictionary<uint, double> _clientLastPingTime = new Dictionary<uint, double>();

    private readonly Dictionary<uint, bool> _clientsReady = new Dictionary<uint, bool>();

    private Coroutine _fetchAndReadyCoroutine;

    #endregion server vars

    #region client vars

    private float _height;

    private PlayerState _playerState;

    private float _lastPingSentTime;

    private float _playerCurrentTime;

    private Coroutine _clientStartAtTimeCoroutine;

    #endregion client vars

    #region syncvars

    [SyncVar] private ContentInfo _contentInfo;

    [SyncVar] private ScreenState _screenState;

    [SyncVar] private ScrubData _idealScrub;

    #endregion syncvars

    #region unity

    private void Start()
    {
        _screenState = ScreenState.Lower;
        _playerState = PlayerState.Unstarted;
        _autoBrowser = GetComponent<AutoBrowser>();

        NetworkManagerGame.Singleton.ServerDisconnect += conn =>
        {
            //TODO verify that this triggers properly
            _clientsReady.Remove(conn.identity.netId);
        };

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
        if (isServer)
        {
            // check if any of the clients did not ping recently
            // set _allGood is false if a bad ping exists
            if (_allGood)
            {
                const float pingTolerance = 1f;
                foreach (var entry in _clientLastPingTime)
                    if (NetworkTime.time - entry.Value > pingTolerance)
                    {
                        _started = false;
                        _allGood = false;
                        break;
                    }
            }

            // TODO trip the failsafe and sync the clients
            if (!_allGood && _started)
            {
                Debug.Log("[BONSAI] !_allGood && _started");
                _beginReadyUpTime = Time.time;
                _idealScrub = _idealScrub.Paused(NetworkTime.time);
                ReadyUpAtTime(_idealScrub.CurrentVideoTime(NetworkTime.time));
            }

            // if the video is not started but clients have begun to ready up
            if (!_started && !float.IsInfinity(_beginReadyUpTime))
            {
                Debug.Log("[BONSAI] ABC !_started && !float.IsInfinity(_beginReadyUpTime)");
                // if all the clients are ready, start the video
                if (AllClientsAreReady())
                {
                    Debug.Log("[BONSAI] AllClientsAreReady");
                    _allGood = true;
                    _started = true;
                    _clientLastPingTime.Clear();
                    SetScreenState(ScreenState.Raised);
                    _idealScrub = new ScrubData(0, NetworkTime.time + 0.5f);
                    //ServerPlayVideoAtTime(0, NetworkTime.time + 0.5f);
                }
                // if clients are not ready after 5 seconds, load the video again
                else if (Time.time - _beginReadyUpTime > 5f)
                {
                    Debug.Log("[BONSAI] Clients Failed to Ready Up");
                    _beginReadyUpTime = Mathf.Infinity;
                    CmdLoadVideo(_contentInfo.ID);
                }
            }
        }

        // ping the server with the client current player time
        if (isClient)
        {
            if (_playerState == PlayerState.Ready && NetworkTime.time > _idealScrub.NetworkTime)
            {
                _autoBrowser.PostMessage(YouTubeMessage.Play);
            }
            
            const float pingInterval = 0.1f;
            if (Time.time - _lastPingSentTime > pingInterval)
            {
                _lastPingSentTime = Time.time;
                if (NetworkClient.connection != null &&  NetworkClient.connection.identity != null)
                {
                    CmdPing(NetworkClient.connection.identity.netId, _playerCurrentTime);
                }
            }
        }

        #region screen height

        const float transitionTime = 0.5f;
        var browserDown = _screenState == ScreenState.Lower;
        var targetHeight = browserDown ? 0 : 1;

        // TODO remove this when ready
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

    public override void OnStartServer()
    {
        base.OnStartServer();
        togglePause.SetInteractable(false);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        var browserDown = _screenState == ScreenState.Lower;
        var targetHeight = browserDown ? 0 : 1;
        _height = targetHeight;
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

        public ScrubData Paused(double currentNetworkTime)
        {
            return new ScrubData(
                CurrentVideoTime(currentNetworkTime),
                double.PositiveInfinity
            );
        }
        public bool IsPaused()
        {
            return double.IsPositiveInfinity(NetworkTime);
        }
    }

    [Server]
    private void ServerPlayVideoAtTime(double timeStamp, double networkTime)
    {
        _allGood = true;
        _started = true;
        _clientLastPingTime.Clear();
        _idealScrub = new ScrubData(timeStamp, networkTime);
        RpcPlayVideoAtTime(timeStamp, networkTime);
    }

    [ClientRpc]
    private void RpcPlayVideoAtTime(double timeStamp, double networkTime)
    {
        if (_clientStartAtTimeCoroutine != null) StopCoroutine(_clientStartAtTimeCoroutine);
        _clientStartAtTimeCoroutine = StartCoroutine(ClientStartAtTime(new ScrubData(timeStamp, networkTime)));
    }

    private IEnumerator ClientStartAtTime(ScrubData data)
    {
        _autoBrowser.PostMessage(YouTubeMessage.Pause);
        _autoBrowser.PostMessage(YouTubeMessage.SeekTo(data.Scrub));
        while (NetworkTime.time < data.NetworkTime) yield return null;
        _autoBrowser.PostMessage(YouTubeMessage.Play);
        _clientStartAtTimeCoroutine = null;
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

    private bool AllClientsAreReady()
    {
        return _clientsReady.Values.All(status => status);
    }

    #endregion failsafe

    #region video loading

    [Command(ignoreAuthority = true)]
    public void CmdLoadVideo(string id)
    {
        Debug.Log("[BONSAI] CmdLoadVideo " + id);

        _started = false;

        if (_fetchAndReadyCoroutine != null) StopCoroutine(_fetchAndReadyCoroutine);

        _fetchAndReadyCoroutine = StartCoroutine(
            FetchYouTubeAspect(id, newAspect =>
            {
                print("[BONSAI] FetchYouTubeAspect callback");
                _contentInfo = new ContentInfo(id, newAspect);

                _clientsReady.Clear();
                foreach (var conn in NetworkServer.connections.Values) _clientsReady[conn.identity.netId] = false;

                _beginReadyUpTime = Time.time;
                RpcReadyUp(_contentInfo);
                _fetchAndReadyCoroutine = null;
            })
        );
    }

    [ClientRpc]
    private void RpcReadyUp(ContentInfo info)
    {
        var resolution = _autoBrowser.ChangeAspect(info.Aspect);

        Debug.Log("[BONSAI] RpcReadyUp " + info.ID + " resolution: " + resolution);

        //TODO _autoBrowser.PostMessage(YouTubeMessage.GoHome);
        _autoBrowser.PostMessage(YouTubeMessage.LoadVideo(info.ID, 0));
    }

    [Server]
    private void ReadyUpAtTime(double timeStamp)
    {
        RpcReadyUpAtTime(timeStamp);
    }

    [ClientRpc]
    private void RpcReadyUpAtTime(double timeStamp)
    {
        _autoBrowser.PostMessage(YouTubeMessage.ReadyUpAtTime(timeStamp));
    }

    private void CmdReady(uint id)
    {
        Debug.Log("[BONSAI] CmdReady");
        _clientsReady[id] = true;
    }

    private void OnMessageEmitted(object sender, EventArgs<string> eventArgs)
    {
        var jsonNode = JSONNode.Parse(eventArgs.Value) as JSONObject;


        if (jsonNode?["type"].Value == "infoCurrentTime")
        {
            _playerCurrentTime = jsonNode["current_time"];
            return;
        }

        Debug.Log("[BONSAI] JSON recieved " + eventArgs.Value);

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
                    CmdReady(NetworkClient.connection.identity.netId);
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

    [Server]
    private void SetScreenState(ScreenState newState)
    {
        _screenState = newState;

        //TODO probably don't need to set interactable to false if screen was already down,
        //but then the default interactable state needs to be false

        togglePause.ServerSetPaused(true);
        togglePause.SetInteractable(false);
    }

    [Command(ignoreAuthority = true)]
    public void CmdCloseVideo()
    {
        SetScreenState(ScreenState.Lower);
        RpcGoHome();
    }

    [ClientRpc]
    public void RpcGoHome()
    {
        _autoBrowser.PostMessage(YouTubeMessage.GoHome);
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
            case ScreenState.Lower:
                CmdLoadVideo(vidIds[_vidId]);
                _vidId = _vidId == vidIds.Length - 1 ? 0 : _vidId + 1;
                break;

            case ScreenState.Raised:
                CmdCloseVideo();
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
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
        Lower,
        Raised
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