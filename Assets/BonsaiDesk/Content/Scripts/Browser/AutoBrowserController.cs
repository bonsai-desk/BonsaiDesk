using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Oculus.Platform;
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

    private double _beginReadyUpTime = Mathf.Infinity;

    private const float ReadyUpTimeout = 5;

    private bool _allGood;

    private readonly Dictionary<uint, double> _clientLastPingTime = new Dictionary<uint, double>();

    private readonly Dictionary<uint, bool> _clientsReadyStatus = new Dictionary<uint, bool>();

    private Coroutine _fetchAndReadyCoroutine;

    #endregion server vars

    #region client vars

    private float _height;

    private PlayerState _playerState;

    private float _lastPingSentTime;

    private float _playerCurrentTime;

    private Coroutine _clientStartAtTimeCoroutine;

    private bool _contentActive;

    private bool _postedPlayMessage;

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
            _clientsReadyStatus.Remove(conn.identity.netId);
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
        if (isServer && _contentActive)
        {
            // check if any of the clients did not ping recently
            // set _allGood is false if a bad ping exists
            if (_allGood)
            {
                const float pingTolerance = 1f;
                foreach (var entry in _clientLastPingTime)
                    if (NetworkTime.time - entry.Value > pingTolerance)
                    {
                        Debug.Log("[BONSAI SERVER] Trigger Not All Good");
                        _allGood = false;
                        _clientLastPingTime.Clear();
                        break;
                    }
            }
            // things are not all good
            else
            {
                // tell the clients to begin readying up since we have not started the process
                if (double.IsInfinity(_beginReadyUpTime))
                {
                    Debug.Log($"[BONSAI SERVER] Ready up initiated at NetworkTime {NetworkTime.time}");

                    _beginReadyUpTime = NetworkTime.time;

                    _clientsReadyStatus.Clear();
                    foreach (var conn in NetworkServer.connections.Values)
                        _clientsReadyStatus.Add(conn.identity.netId, false);

                    if (!_idealScrub.IsPaused() && NetworkTime.time > _idealScrub.NetworkTime)
                    {
                        var oldScrub = _idealScrub.Scrub;
                        var oldStart = _idealScrub.NetworkTime;

                        _idealScrub = _idealScrub.Paused(NetworkTime.time);
                        var currentVideoTime = _idealScrub.CurrentVideoTime(NetworkTime.time);

                        RpcReadyUpAtTime(currentVideoTime);

                        Debug.Log("[BONSAI SERVER] Ideal scrub was not paused when beginning ready up process. " +
                                  $"Timestamp ({oldScrub}) started ({NetworkTime.time - oldStart}) seconds ago, " +
                                  $"new timestamp: ({_idealScrub.Scrub}=={currentVideoTime})"
                        );
                    }
                    else
                    {
                        Debug.Log($"[BONSAI] Server ideal scrub was paused at ({_idealScrub.Scrub}), continuing...");
                    }
                }
                // handle clients readying up
                else
                {
                    // if all the clients are ready, start the video
                    if (AllClientsAreReady())
                    {
                        Debug.Log($"[BONSAI SERVER] All ({_clientsReadyStatus.Count}) clients are ready at NetworkTime: {NetworkTime.time} with {ReadyUpTimeout - (NetworkTime.time - _beginReadyUpTime)} seconds to spare");
                        SetScreenState(ScreenState.Raised);
                        _allGood = true;
                        _clientLastPingTime.Clear();
                        _beginReadyUpTime = Mathf.Infinity;
                        _idealScrub = _idealScrub.StartPlayingWhen(NetworkTime.time + 0.5);
                        Debug.Log($"[BONSAI SERVER] Clients should start playing scrub ({_idealScrub.Scrub}) at NetworkTime ({_idealScrub.NetworkTime})");
                    }
                    // if clients are not ready after 5 seconds, load the video again
                    else if (NetworkTime.time - _beginReadyUpTime > ReadyUpTimeout)
                    {
                        var failedNetIds = new HashSet<string>();
                        foreach (var info in _clientsReadyStatus.Where(info => !info.Value))
                        {
                            failedNetIds.Add(info.Key.ToString());
                        }

                        var failedNetIdsStr = string.Join(", ", failedNetIds);
                        
                        Debug.Log($"[BONSAI SERVER] ({failedNetIds.Count}/{_clientsReadyStatus.Count}) Clients failed to ready up netIds=[{failedNetIdsStr}]");
                        _beginReadyUpTime = Mathf.Infinity;
                        _allGood = false;
                        _clientLastPingTime.Clear();
                    }
                }
            }
        }

        // ping the server with the client current player time
        if (isClient)
        {
            if (_playerState != PlayerState.Ready && _postedPlayMessage)
            {
                _postedPlayMessage = false;
            }
            
            if (_playerState == PlayerState.Ready && !_idealScrub.IsPaused() &&
                NetworkTime.time > _idealScrub.NetworkTime && !_postedPlayMessage)
            {
                Debug.Log($"[BONSAI CLIENT] (netId={NetworkClient.connection.identity.netId}) been ready, " +
                $"now playing scrub: {_idealScrub.Scrub} at NetworkTime: ({_idealScrub.NetworkTime})~=({NetworkTime.time})");
                _autoBrowser.PostMessage(YouTubeMessage.Play);
                _postedPlayMessage = true;
            } 

            const float pingInterval = 0.1f;
            if (Time.time - _lastPingSentTime > pingInterval)
            {
                _lastPingSentTime = Time.time;
                if (NetworkClient.connection != null && NetworkClient.connection.identity != null)
                    CmdPing(NetworkClient.connection.identity.netId, _playerCurrentTime);
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
            if (IsPaused()) return Scrub;
            return Scrub + (currentNetworkTime - NetworkTime);
        }

        public ScrubData(double scrub, double networkTime)
        {
            Scrub = scrub;
            NetworkTime = networkTime;
        }

        public static ScrubData PausedAtScrub(double scrub)
        {
            return new ScrubData(scrub, double.PositiveInfinity);
        }

        public ScrubData Paused(double currentNetworkTime)
        {
            return new ScrubData(
                CurrentVideoTime(currentNetworkTime),
                double.PositiveInfinity
            );
        }

        public ScrubData StartPlayingWhen(double networkTime)
        {
            if (!IsPaused())
            {
                throw new Exception("Scrub should be paused before resuming");
            }

            return new ScrubData(Scrub, networkTime);
        }

        public bool IsPaused()
        {
            return double.IsPositiveInfinity(NetworkTime);
        }
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
        if (_clientsReadyStatus.Count == 0) Debug.LogError("No clients in AllClientsAreReady");
        if (_clientsReadyStatus.Count != NetworkServer.connections.Count)
            Debug.LogError("_clientsReady mismatch NetServer connections");
        return _clientsReadyStatus.Values.All(status => status);
    }

    #endregion failsafe

    #region video loading

    [Command(ignoreAuthority = true)]
    private void CmdLoadVideo(string id)
    {
        Debug.Log("[BONSAI] CmdLoadVideo " + id);

        if (_fetchAndReadyCoroutine != null) StopCoroutine(_fetchAndReadyCoroutine);

        _fetchAndReadyCoroutine = StartCoroutine(
            FetchYouTubeAspect(id, newAspect =>
            {
                print($"[BONSAI SERVER] Fetched aspect ({newAspect.x},{newAspect.y}) for video ({id})");

                _allGood = false;

                _contentActive = true;

                _contentInfo = new ContentInfo(id, newAspect);

                _idealScrub = ScrubData.PausedAtScrub(0);

                RpcLoadVideo(_contentInfo, 0);

                _fetchAndReadyCoroutine = null;
            })
        );
    }

    [ClientRpc]
    private void RpcLoadVideo(ContentInfo info, float ts)
    {
        var resolution = _autoBrowser.ChangeAspect(info.Aspect);

        Debug.Log($"[BONSAI RPC] Load New YouTube Video ({info.ID}) with resolution: {resolution}");

        //TODO _autoBrowser.PostMessage(YouTubeMessage.GoHome);

        _autoBrowser.PostMessage(YouTubeMessage.LoadVideo(info.ID, ts));
    }

    [ClientRpc]
    private void RpcReadyUpAtTime(double timeStamp)
    {
        _autoBrowser.PostMessage(YouTubeMessage.ReadyUpAtTime(timeStamp));
    }

    [Command(ignoreAuthority = true)]
    private void CmdReady(uint id)
    {
        Debug.Log($"[BONSAI SERVER] Client (netId={id}) is ready at NetworkTime={NetworkTime.time}");
        _clientsReadyStatus[id] = true;
    }

    private void OnMessageEmitted(object sender, EventArgs<string> eventArgs)
    {
        var jsonNode = JSONNode.Parse(eventArgs.Value) as JSONObject;


        if (jsonNode?["type"].Value == "infoCurrentTime")
        {
            _playerCurrentTime = jsonNode["current_time"];
            return;
        }

        Debug.Log($"[BONSAI] (netId={NetworkClient.connection.identity.netId}) JSON recieved " + eventArgs.Value);

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
    private void CmdCloseVideo()
    {
        _contentActive = false;
        _contentInfo = new ContentInfo();
        SetScreenState(ScreenState.Lower);
        RpcGoHome();
    }

    [ClientRpc]
    private void RpcGoHome()
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