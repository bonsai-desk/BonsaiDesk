using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using OVRSimpleJSON;
using UnityEngine;
using UnityEngine.Networking;
using Vuplex.WebView;

[RequireComponent(typeof(AutoBrowser))]
public class NewAutoBrowserController : NetworkBehaviour
{
    private const float ClientJoinGracePeriod = 10f;
    private const float ClientPingTolerance = 1f;
    private const float ClientPingInterval = 0.1f;
    private const float MaxReadyUpPeriod = 10f;
    private const float VideoSyncTolerance = 0.2f;
    public bool useBuiltHtml = true;
    public string hotReloadUrl;
    public TogglePause togglePause;
    private readonly Dictionary<uint, double> _clientsJoinedNetworkTime = new Dictionary<uint, double>();
    private readonly Dictionary<uint, double> _clientsLastPing = new Dictionary<uint, double>();
    private readonly Dictionary<uint, PlayerState> _clientsPlayerStatus = new Dictionary<uint, PlayerState>();
    private bool _allGood;
    private AutoBrowser _autoBrowser;
    private double _beginReadyUpTime;
    private PlayerState _clientPlayerState;
    private float _clientPlayerTimeStamp;
    private double _clientLastSentPing;
    private bool _serverRecievedBadTimestampInPing;
    


    [SyncVar] private ContentInfo _contentInfo;
    [SyncVar] private ScrubData _idealScrub;

    // Start is called before the first frame update
    private void Start()
    {
        if (isClient) _autoBrowser.BrowserReady += SetupBrowser;
    }

    // Update is called once per frame
    private void Update()
    {
        if (isClient)
        {
            HandlePlayerClient();
            //todo HandleScreenClient();
        }

        if (isServer)
        {
            HandlePlayerServer();
            //todo HandleScreenServer();
        }
    }


    private void HandlePlayerClient()
    {
        // post play message if paused/ready and player is behind ideal scrub
        if (_clientPlayerState == PlayerState.Ready && _idealScrub.Active &&
            _clientPlayerTimeStamp < _idealScrub.CurrentTimeStamp(NetworkTime.time))
        {
            TLog("Post play to javascript");
            _autoBrowser.PostMessage(YouTubeMessage.Play);
        }

        // ping the server with the current timestamp
        if (_contentInfo.Active && 
            NetworkTime.time - _clientLastSentPing > ClientPingInterval &&
            NetworkClient.connection != null && NetworkClient.connection.identity != null)
        {
            var id = NetworkClient.connection.identity.netId;
            var now = NetworkTime.time;
            var timeStamp = _clientPlayerTimeStamp;
            CmdPingAndCheckTimeStamp(id, now, timeStamp);
        }
    }

    private void HandleScreenClient()
    {
        throw new NotImplementedException();
    }

    private void HandlePlayerServer()
    {
        if (_contentInfo.Active == false) return;

        if (_allGood)
            if (BadPingExists() || _serverRecievedBadTimestampInPing)
            {
                _serverRecievedBadTimestampInPing = false;
                TLog("Beginning the sync process");
                _allGood = false;
                _clientsLastPing.Clear();
                _clientsPlayerStatus.Clear();
                _beginReadyUpTime = NetworkTime.time;
                _idealScrub = _idealScrub.Pause(NetworkTime.time);

                var timeStamp = _idealScrub.CurrentTimeStamp(NetworkTime.time);
                RpcReadyUp(timeStamp);
            }

        if (!_allGood)
        {
            if (AllClientsReportPlayerStatus(PlayerState.Ready))
            {
                TLog("All clients report ready, un-pausing the scrub");
                _idealScrub = _idealScrub.UnPauseAtNetworkTime(NetworkTime.time + 0.5);
                _allGood = true;
            }
            else if (NetworkTime.time - _beginReadyUpTime > MaxReadyUpPeriod)
            {
                TLog("Some clients failed to ready up, doing a hard reset");
                _clientsLastPing.Clear();
                _clientsPlayerStatus.Clear();
                _beginReadyUpTime = NetworkTime.time;
                _idealScrub = _idealScrub.Pause(NetworkTime.time);

                var timeStamp = _idealScrub.CurrentTimeStamp(NetworkTime.time);
                RpcReloadYouTube(_contentInfo.ID, timeStamp);
            }
        }
    }

    private bool BadPingExists()
    {
        var aBadPing = false;

        foreach (var entry in _clientsLastPing)
            if (!ClientInGracePeriod(entry.Key) && !ClientPingedRecently(entry.Value))
            {
                TLog($"Bad ping for client [{entry.Key}]");
                aBadPing = true;
            }

        return aBadPing;
    }

    private void HandleScreenServer()
    {
        throw new NotImplementedException();
    }

    private void SetupBrowser()
    {
#if !DEVELOPMENT_BUILD
            _autoBrowser.LoadHtml(BonsaiUI.Html);
#else
        if (useBuiltHtml)
            _autoBrowser.LoadHtml(BonsaiUI.Html);
        else
            _autoBrowser.LoadUrl(hotReloadUrl);
#endif
        _autoBrowser.OnMessageEmitted(HandleJavascriptMessage);
    }

    private void HandleJavascriptMessage(object _, EventArgs<string> eventArgs)
    {
        var json = JSONNode.Parse(eventArgs.Value) as JSONObject;
        
        if (json?["current_time"] != null)
        {
            _clientPlayerTimeStamp = json["current_time"];
        }
        
        switch (json?["type"].Value)
        {
            case "infoCurrentTime":
                return;
            case "error":
                Debug.LogError(Tag() + $"Javascript error {json["error"].Value}");
                return;
            case "stateChange":
                switch ((string) json["message"])
                {
                    case "READY":
                        _clientPlayerState = PlayerState.Ready;
                        break;
                    case "PAUSED":
                        _clientPlayerState = PlayerState.Ready;
                        break;
                    case "PLAYING":
                        _clientPlayerState = PlayerState.Ready;
                        break;
                    case "BUFFERING":
                        _clientPlayerState = PlayerState.Ready;
                        break;
                    case "ENDED":
                        _clientPlayerState = PlayerState.Ready;
                        break;
                }
                break;
        }
    }

    private bool ClientInGracePeriod(uint id)
    {
        return ClientJoinGracePeriod > NetworkTime.time - _clientsJoinedNetworkTime[id];
    }

    private static bool ClientPingedRecently(double pingTime)
    {
        return ClientPingTolerance > NetworkTime.time - pingTime;
    }

    private bool ClientVideoIsSynced(double timeStamp)
    {
        var whereTheyShouldBe = _idealScrub.CurrentTimeStamp(NetworkTime.time);
        var whereTheyAre = timeStamp;
        return Math.Abs(whereTheyAre - whereTheyShouldBe) < VideoSyncTolerance;
    }


    [Command(ignoreAuthority = true)]
    private void CmdPingAndCheckTimeStamp(uint id, double networkTime, double timeStamp)
    {
        _clientsLastPing[id] = NetworkTime.time;

        // TODO could use networkTime of timeStamp to account for rtt
        // TODO also this could just populate a dictionary that is checked in the handle
        if (_allGood && !ClientInGracePeriod(id) && !ClientVideoIsSynced(timeStamp))
        {
            TLog("Client reported a bad timestamp in ping");
            _serverRecievedBadTimestampInPing = true;
        }
    }

    [Command(ignoreAuthority = true)]
    private void CmdUpdateClientPlayerState(uint id, PlayerState playerState)
    {
        throw new NotImplementedException();
    }

    [Command(ignoreAuthority = true)]
    private void CmdLoadVideo(string videoId, double timeStamp)
    {
        throw new NotImplementedException();
    }

    [Command(ignoreAuthority = true)]
    private void CmdGoHome()
    {
        throw new NotImplementedException();
    }


    [ClientRpc]
    private void RpcReloadYouTube(string id, double timeStamp)
    {
        _autoBrowser.PostMessages(new[]
        {
            YouTubeMessage.NavHome,
            YouTubeMessage.LoadYouTube(id, timeStamp)
        });
    }

    [ClientRpc]
    private void RpcReadyUp(double timeStamp)
    {
        TLog($"Post ready up at {timeStamp}");
        _autoBrowser.PostMessage(YouTubeMessage.ReadyUpAtTime(timeStamp));
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

    private bool AllClientsReportPlayerStatus(PlayerState playerState)
    {
        if (_clientsPlayerStatus.Count != NetworkServer.connections.Count) return false;
        return _clientsPlayerStatus.Values.All(status => status == playerState);
    }


    private string Tag()
    {
        switch (isClient)
        {
            case true when isServer:
                return NetworkClient.connection.isReady
                    ? $"[BONSAI HOST {NetworkClient.connection.identity.netId}] "
                    : "[BONSAI HOST] ";
            case true:
                return NetworkClient.connection.isReady
                    ? $"[BONSAI CLIENT {NetworkClient.connection.identity.netId}] "
                    : "[BONSAI CLIENT] ";
            default:
                return "[BONSAI SERVER] ";
        }
    }

    private void TLog(string message)
    {
        Debug.Log(Tag() + message);
    }

    private enum BrowserState
    {
        Home,
        YouTube
    }

    private enum PlayerState
    {
        Ready,
        Paused,
        Playing,
        Buffering,
        Ended
    }

    private static class YouTubeMessage
    {
        public const string Play = "{\"type\": \"video\", \"command\": \"play\"}";
        public const string Pause = "{\"type\": \"video\", \"command\": \"pause\"}";
        public static readonly string NavHome = PushPath("/home");

        private static string PushPath(string path)
        {
            return "{" +
                   "\"type\": \"nav\", " +
                   "\"command\": \"push\", " +
                   $"\"path\": \"{path}\"" +
                   "}";
        }

        public static string LoadYouTube(string id, double ts)
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

    private readonly struct ContentInfo
    {
        public readonly bool Active;
        public readonly string ID;
        public readonly Vector2 Aspect;

        public ContentInfo(bool active, string id, Vector2 aspect)
        {
            Active = active;
            ID = id;
            Aspect = aspect;
        }
    }

    public readonly struct ScrubData
    {
        private readonly double _scrub;
        private readonly double _networkTimeActivated;
        public readonly bool Active;

        private ScrubData(double scrub, double networkTimeActivated, bool active)
        {
            _scrub = scrub;
            _networkTimeActivated = networkTimeActivated;
            Active = active;
        }

        public static ScrubData PausedAtScrub(double scrub)
        {
            return new ScrubData(scrub, -1, false);
        }

        public ScrubData Pause(double networkTime)
        {
            return new ScrubData(
                CurrentTimeStamp(networkTime), -1, false
            );
        }

        public ScrubData UnPauseAtNetworkTime(double networkTime)
        {
            if (Active) Debug.LogError("Scrub should be paused before resuming");
            return new ScrubData(_scrub, networkTime, true);
        }

        public double CurrentTimeStamp(double networkTime)
        {
            if (!Active || networkTime - _networkTimeActivated < 0) return _scrub;
            return _scrub + (networkTime - _networkTimeActivated);
        }
    }
}