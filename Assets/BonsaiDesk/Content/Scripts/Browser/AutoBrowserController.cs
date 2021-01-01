using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using OVRSimpleJSON;
using UnityEngine;
using UnityEngine.Networking;
using Vuplex.WebView;
using Random = System.Random;

[RequireComponent(typeof(AutoBrowser))]
public class AutoBrowserController : NetworkBehaviour
{
    public string hotReloadUrl;
    public TogglePause togglePause;
    private readonly (float, float) _delayClamp = (0.1f, 0.75f);

    private readonly double desyncTolerance = 0.2;

    private AutoBrowser _autoBrowser;

    [SyncVar(hook = nameof(OnSetContentId))]
    private string _contentId;

    private Data _myScrub;

    private int _numClientsReporting;

    private ClientScrubCollector _scrubCollector = new ClientScrubCollector();
    [SyncVar] private string _started;

    [SyncVar] private ScreenState _screenState;
    private PlayerState _playerState;

    [SyncVar(hook = nameof(OnSetWorstScrub))]
    private Data _worstScrub = new Data(10e10, 0);

    private void Start()
    {
        _screenState = ScreenState.Neutral;
        _playerState = PlayerState.Unstarted;

        _autoBrowser = GetComponent<AutoBrowser>();
        _autoBrowser.BrowserReady += () =>
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _autoBrowser.LoadUrl(hotReloadUrl);
#else
            _autoBrowser.LoadHTML(BonsaiUI.Html);
#endif

            togglePause.PauseChangedClient += OnPauseChangeClient;
            togglePause.PauseChangedServer += OnPauseChangeServer;
            _autoBrowser.OnMessageEmitted(OnMessageEmitted);
        };
    }

    private string LoadVideoIdMessage(string videoId)
    {
        return "{" +
               "\"type\": \"video\", " +
               "\"command\": \"load\", " +
               $"\"video_id\": \"{videoId}\" " +
               "}";
    }

    private string PauseMessage()
    {
        return "{\"type\": \"video\", \"command\": \"pause\"}";
    }

    private string PlayMessage()
    {
        return "{\"type\": \"video\", \"command\": \"play\"}";
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
                _myScrub = new Data(jsonNode["current_time"], NetworkTime.time);
                CmdClientPlaying(_myScrub);
                break;

            case "PAUSED":
                _playerState = PlayerState.Paused;
                //_myScrub = new Data(jsonNode["current_time"], NetworkTime.time);
                break;
            
            case "BUFFERING":
                _playerState = PlayerState.Buffering;
                break;
            
            case "VIDEOCUED":
                _playerState = PlayerState.VideoCued;
                break;
                
        }
    }

    private void OnSetWorstScrub(Data oldWorstScrub, Data newWorstScrub)
    {
        Debug.Log("[BONSAI] OnSetWorstScrub scrub: " + newWorstScrub.Scrub +" networktime: " + newWorstScrub.NetworkTime);
        var now = NetworkTime.time;
        var myTime = _myScrub.CurrentVideoTime(now);
        var worstTime = newWorstScrub.CurrentVideoTime(now);
        var deSync = myTime - worstTime;

        if (!(deSync > desyncTolerance) || _playerState != PlayerState.Playing) return;
        
        Debug.Log("[BONSAI] OnSetWorstScrub desync=" + deSync);

        _autoBrowser.PostMessage(PauseMessage());
        StartCoroutine(PlayAfter(NetworkTime.time + deSync));
    }

    private void OnSetContentId(string oldVideoId, string newVideoId)
    {
        Debug.Log("[BONSAI] OnSetVideoId " + oldVideoId + "->" + newVideoId);
        StartCoroutine(newVideoId == "" ? ReturnToNeutral() : LoadVideo(newVideoId));
    }

    [Command(ignoreAuthority = true)]
    private void CmdClientPlaying(Data data)
    {
        Debug.Log("[BONSAI] CmdClientPlaying scrub: " + data.Scrub + " time: " + data.NetworkTime);
        _scrubCollector.Include(data);
        _worstScrub = _scrubCollector.Worst;
    }

    [Command(ignoreAuthority = true)]
    private void CmdSetState(ScreenState newState)
    {
        _screenState = newState;
    }

    [Command(ignoreAuthority = true)]
    public void CmdSetContentId(string id)
    {
        _contentId = id;
    }

    private IEnumerator PlayAfter(double startAfterNetworkTime)
    {
        Debug.Log("[BONSAI] (now-startAfterNetworkTime) = " + (float) (NetworkTime.time - startAfterNetworkTime));
        while (NetworkTime.time < startAfterNetworkTime) yield return null;
        Debug.Log("[BONSAI] (now-startAfterNetworkTime) = " + (float) (NetworkTime.time - startAfterNetworkTime));
        _autoBrowser.PostMessage(PlayMessage());
    }

    public void ToggleVideo()
    {
        var rnd = new Random();
        //var vidIds = new List<string> {"V1bFr2SWP1I", "AqqaYs7LjlM", "jNQXAC9IVRw", "Cg0QwoHh9w4", "kJQP7kiw5Fk"};
        var vidIds = new List<string> {"HPoZ42JKhuc", "zvpVRTobCC0", "16GeJe0Mjh4", "p1skpV2fhN0"};

        switch (_screenState)
        {
            case ScreenState.Neutral:
                CmdSetContentId(vidIds[rnd.Next(0, vidIds.Count)]);
                CmdSetState(ScreenState.YouTube);
                break;

            case ScreenState.YouTube:
                CmdSetContentId("");
                CmdSetState(ScreenState.Neutral);
                break;

            case ScreenState.Twitch:
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private IEnumerator LoadVideo(string videoId)
    {
        var newAspect = new Vector2(16, 9);

        var videoInfoUrl = $"https://api.desk.link/youtube/{videoId}";

        const string resizePlayer = "{" +
                                    "\"type\": \"video\", " +
                                    "\"command\": \"resize\" " +
                                    "}";


        using (var www = UnityWebRequest.Get(videoInfoUrl))
        {
            var req = www.SendWebRequest();

            _autoBrowser.PostMessage(LoadVideoIdMessage(videoId));

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

            _autoBrowser.ChangeAspect(newAspect);
            yield return new WaitForSeconds(0.05f);

            // TODO verify that window resize has finished
            _autoBrowser.PostMessage(resizePlayer);
            yield return new WaitForSeconds(0.1f);

            yield return _autoBrowser.RaiseScreen(0.5f);
        }
    }

    private IEnumerator ReturnToNeutral()
    {
        //TODO togglePause.CmdSetPaused(true);
        yield return _autoBrowser.DropScreen(1f);
        _autoBrowser.PostMessage(LoadVideoIdMessage(""));
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

    private enum ScreenState
    {
        Neutral,
        YouTube,
        Twitch
    }

    private enum PlayerState
    {
        Unstarted, Ended, Playing, Paused, Buffering, VideoCued
        
    }

    public struct Data
    {
        public double Scrub;
        public double NetworkTime;

        public double CurrentVideoTime(double currentNetworkTime)
        {
            return Scrub + (currentNetworkTime - NetworkTime);
        }

        public Data(double scrub, double networkTime)
        {
            Scrub = scrub;
            NetworkTime = networkTime;
        }
    }

    public class ClientScrubCollector
    {
        public Data Worst = new Data(10e10, 0);

        public void Include(Data data)
        {
            if (!(data.Scrub < Worst.Scrub)) return;
            Worst = data;
        }

        public void Reset()
        {
            Worst.Scrub = 10e10;
            Worst.NetworkTime = 0;
        }
    }
}
