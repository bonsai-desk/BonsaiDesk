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
    
    private AutoBrowser _autoBrowser;
    [SyncVar(hook = nameof(OnSetContentId))] private string _contentId;
    private int _numClientsReady;
    [SyncVar] private string _scrub;
    [SyncVar] private string _started;

    [SyncVar] private PlayerState _state;

    #region unity
    
    private void Start()
    {
        _state = PlayerState.Neutral;

        _autoBrowser = GetComponent<AutoBrowser>();
        _autoBrowser.BrowserReady += () =>
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _autoBrowser.LoadUrl(hotReloadUrl);
#else
            _autoBrowser.LoadHTML(BonsaiUI.Html);
#endif

            togglePause.PauseChanged += OnPauseChange;
            _autoBrowser.OnMessageEmitted(OnMessageEmitted);
        };
    }
    
    #endregion unity
    
    #region handlers
    
    private void OnPauseChange(bool paused)
    {
        var message = "{\"type\": \"video\", \"command\": \"" + (paused ? "pause" : "play") + "\"}";
        _autoBrowser.PostMessage(message);
    }
    
    private void OnSetContentId(string oldVideoId, string newVideoId)
    {
        Debug.Log("[BONSAI] OnSetVideoId " + oldVideoId + "->" + newVideoId);
        StartCoroutine(newVideoId == "" ? ReturnToNeutral() : LoadVideo(newVideoId));
    }
    
    private void OnMessageEmitted(object sender, EventArgs<string> eventArgs)
    {
        var jsonNode = JSONNode.Parse(eventArgs.Value) as JSONObject;

        Debug.Log("[BONSAI] JSON recieved " + eventArgs.Value);

        if ((string) jsonNode?["type"] != "stateChange" || jsonNode["message"] is null) return;

        switch ((string) jsonNode["message"])
        {
            case "PAUSED_AFTER_INITIAL_BUFFER":
                CmdClientIsReady();
                break;
        }
    }
    
    #endregion handlers
    
    #region commands
    
    [Command(ignoreAuthority = true)]
    private void CmdSetState(PlayerState newState)
    {
        _state = newState;
    }
    
    [Command(ignoreAuthority = true)]
    public void CmdSetContentId(string id)
    {
        _contentId = id;
    }

    [Command(ignoreAuthority = true)]
    public void CmdClientIsReady()
    {
        _numClientsReady += 1;
        if (_numClientsReady != NetworkServer.connections.Count) return;
        _numClientsReady = 0;
        RpcPlay();
    }

    [ClientRpc]
    public void RpcPlay()
    {
        _autoBrowser.PostMessage(PlayVideoMessage());
    }

    #endregion commands
    
    #region actions 
    
    public void ToggleVideo()
    {
        var rnd = new Random();
        //var vidIds = new List<string> {"V1bFr2SWP1I", "AqqaYs7LjlM", "jNQXAC9IVRw", "Cg0QwoHh9w4", "kJQP7kiw5Fk"};
        var vidIds = new List<string> {"HPoZ42JKhuc", "zvpVRTobCC0", "16GeJe0Mjh4", "p1skpV2fhN0"};

        switch (_state)
        {
            case PlayerState.Neutral:
                CmdSetContentId(vidIds[rnd.Next(0, vidIds.Count)]);
                CmdSetState(PlayerState.YouTube);
                break;

            case PlayerState.YouTube:
                CmdSetContentId("");
                CmdSetState(PlayerState.Neutral);
                break;

            case PlayerState.Twitch:
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
        //TODO reset to paused
        //togglePause.CmdSetPaused(true);
        yield return _autoBrowser.DropScreen(1f);
        _autoBrowser.PostMessage(LoadVideoIdMessage(""));
    }
    
    #endregion actions 
    
    private string PlayVideoMessage()
    {
        return "{" +
               "\"type\": \"video\", " +
               "\"command\": \"play\" " +
               "}";
    }
    
    private string LoadVideoIdMessage(string videoId)
    {
        return "{" +
               "\"type\": \"video\", " +
               "\"command\": \"load\", " +
               $"\"video_id\": \"{videoId}\" " +
               "}";
    }

    private enum PlayerState
    {
        Neutral,
        YouTube,
        Twitch
    }
}