using System;
using System.Collections;
using System.Collections.Generic;
using OVRSimpleJSON;
using UnityEngine;
using UnityEngine.Networking;

public class AutoBrowserController : MonoBehaviour
{
    public string initialURL;
    public TogglePause togglePause;
    private AutoBrowser _autoBrowser;

    private int i;

    private void Start()
    {
        _autoBrowser = GetComponent<AutoBrowser>();
        _autoBrowser.BrowserReady += () =>
        {
            Debug.Log("_autoBrowser.BrowserReady");
            _autoBrowser.LoadUrl(initialURL);
            LoadNewVideo("PWMTDRWJqu4");
        };
        togglePause.PauseChanged += HandlePauseChange;
    }

    private void HandlePauseChange(bool paused)
    {
        var message = "{\"type\": \"video\", \"command\": \"" + (paused ? "pause" : "play") + "\"}";
        Debug.Log("[BONSAI] HandlePauseChange " + message);
        _autoBrowser.PostMessage(message);
    }

    public void ResizePlayer()
    {
        var vidIds = new List<string> {"V1bFr2SWP1I", "AqqaYs7LjlM", "V1bFr2SWP1I", "Cg0QwoHh9w4"};
        StartCoroutine(LoadNewVideo(vidIds[i]));
        i += 1;
    }

    private IEnumerator LoadNewVideo(string videoId)
    {
        var newAspect = new Vector2(16, 9);

        var videoInfoUrl = $"https://api.desk.link/youtube/{videoId}";

        const string resizePlayer = "{" +
                                    "\"type\": \"video\", " +
                                    "\"command\": \"resize\" " +
                                    "}";

        var loadVideoId = "{" +
                          "\"type\": \"video\", " +
                          "\"command\": \"load\", " +
                          $"\"video_id\": \"{videoId}\" " +
                          "}";

        using (var www = UnityWebRequest.Get(videoInfoUrl))
        {
            var req = www.SendWebRequest();

            yield return _autoBrowser.DropScreen(0.5f);
            
            _autoBrowser.PostMessage(loadVideoId);

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
            yield return new WaitForSeconds(0.05f);

            yield return _autoBrowser.RaiseScreen(0.5f);
        }
    }
}