﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Serialization;
using Vuplex.WebView;
using static AutoBrowserController;

[RequireComponent(typeof(TableBrowser))]
public class TableBrowserMenu : MonoBehaviour
{
    public enum LightState
    {
        Bright,
        Vibes
    }

    private const float PostRoomInfoEvery = 1f;
    public static TableBrowserMenu Singleton;
    public AutoBrowserController autoBrowserController;
    public float postMediaInfoEvery = 0.5f;
    [FormerlySerializedAs("_browser")] public TableBrowser browser;
    public bool canPost;
    private float _postMediaInfoLast;
    private float _postRoomInfoLast;

    private void Awake()
    {
        if (Singleton == null)
        {
            Singleton = this;
        }
    }

    private void Start()
    {
        browser = GetComponent<TableBrowser>();
        browser.BrowserReady += SetupBrowser;
        browser.ListenersReady += HandleListenersReady;
        NetworkManagerGame.Singleton.InfoChange += HandleNetworkInfoChange;
        OVRManager.HMDUnmounted += () => { browser.SetHidden(true); };
    }

    public void Update()
    {
        if (Time.time - _postMediaInfoLast > postMediaInfoEvery)
        {
            _postMediaInfoLast = Time.time;
            PostMediaInfo(autoBrowserController.GetMediaInfo());
        }

        if (Time.time - _postRoomInfoLast > PostRoomInfoEvery)
        {
            _postRoomInfoLast = Time.time;
            PostPlayerInfo(NetworkManagerGame.Singleton.PlayerInfos);
            if (canPost)
            {
                PostNetworkInfo();
            }

            PostExperimentalInfo();
            PostAppInfo();
        }
    }

    private void HandleNetworkInfoChange(object sender, EventArgs e)
    {
        PostNetworkInfo();
    }

    private void PostNetworkInfo()
    {
        var networkInfo = new NetworkInfo
        {
            Online = NetworkManagerGame.Singleton.Online,
            NetworkAddress = NetworkManagerGame.Singleton.GetNetworkAddress(),
            RoomOpen = NetworkManagerGame.Singleton.roomOpen,
            Mode = NetworkManagerGame.Singleton.mode
        };
        var csm = new CsMessageKeyType<NetworkInfo>
        {
            Data = new KeyType<NetworkInfo>
            {
                Key = "NetworkInfo", Val = networkInfo
            }
        };
        SerializeAndPost(csm);
    }

    private void SerializeAndPost(object obj)
    {
        var message = JsonConvert.SerializeObject(obj);
        browser.PostMessage(message);
    }

    private void SetupBrowser(object sender, EventArgs eventArgs)
    {
        browser.OnMessageEmitted(HandleJavascriptMessage);
    }

    private void HandleListenersReady()
    {
        BonsaiLog("Navigate to menu");
        browser.PostMessage(Browser.BrowserMessage.NavToMenu);
        canPost = true;
    }

    private void HandleJavascriptMessage(object _, EventArgs<string> eventArgs)
    {
        var message = JsonConvert.DeserializeObject<Browser.JsMessageString>(eventArgs.Value);

        switch (message.Type)
        {
            case "command":
                switch (message.Message)
                {
                    case "requestMicrophone":
                        RequestMicrophone();
                        break;
                    case "togglePinchPull":
                        TogglePinchPull();
                        _postRoomInfoLast = Mathf.NegativeInfinity;
                        break;
                    case "toggleBlockBreak":
                        ToggleBlockBreak();
                        _postRoomInfoLast = Mathf.NegativeInfinity;
                        break;
                    case "closeMenu":
                        CloseMenu?.Invoke(this, new EventArgs());
                        break;
                    case "joinRoom":
                        var roomData = JsonConvert.DeserializeObject<RoomData>(message.Data);
                        BonsaiLog($"[BONSAI] Message JoinRoom {message.Data}");
                        JoinRoom?.Invoke(roomData);
                        break;
                    case "leaveRoom":
                        LeaveRoom?.Invoke();
                        break;
                    case "openRoom":
                        OpenRoom?.Invoke();
                        break;
                    case "closeRoom":
                        CloseRoom?.Invoke();
                        break;
                    case "browseYouTube":
                        if (BrowseSite != null)
                        {
                            BrowseSite(this, "https://m.youtube.com");
                        }

                        break;
                    case "seekPlayer":
                        var ts = float.Parse(message.Data);
                        SeekPlayer?.Invoke(this, ts);
                        break;
                    case "restartVideo":
                        RestartVideo?.Invoke(this, new EventArgs());
                        break;
                    case "kickConnectionId":
                        // todo what happens when this fails?
                        var id = JsonConvert.DeserializeObject<int>(message.Data);
                        KickConnectionId?.Invoke(id);
                        break;
                    case "setVolume":
                        var level = JsonConvert.DeserializeObject<float>(message.Data);
                        SetVolumeLevel?.Invoke(this, level);
                        break;
                    case "lightsChange":
                        if (LightChange != null)
                        {
                            switch (message.Data)
                            {
                                case "vibes":
                                    LightChange.Invoke(this, LightState.Vibes);
                                    break;
                                case "bright":
                                    LightChange.Invoke(this, LightState.Bright);
                                    break;
                            }
                        }

                        break;
                    case "pauseVideo":
                        PauseVideo?.Invoke(this, new EventArgs());
                        break;
                    case "playVideo":
                        PlayVideo?.Invoke(this, new EventArgs());
                        break;
                    case "ejectVideo":
                        EjectVideo?.Invoke(this, new EventArgs());
                        break;
                }

                break;

            case "event":
                break;
        }
    }

    private static void RequestMicrophone()
    {
        Permission.RequestUserPermission(Permission.Microphone);
    }

    private void TogglePinchPull()
    {
        var pinchPullEnabled = InputManager.Hands.Left.PlayerHand.GetIHandTick<PinchPullHand>().pinchPullEnabled;
        InputManager.Hands.Left.PlayerHand.GetIHandTick<PinchPullHand>().pinchPullEnabled = !pinchPullEnabled;
        InputManager.Hands.Right.PlayerHand.GetIHandTick<PinchPullHand>().pinchPullEnabled = !pinchPullEnabled;
    }

    private void ToggleBlockBreak()
    {
        var blockBreakActive = InputManager.Hands.Right.PlayerHand.GetIHandTick<BlockBreakHand>().BreakModeActive;
        InputManager.Hands.Right.PlayerHand.GetIHandTick<BlockBreakHand>().SetBreakMode(!blockBreakActive);
    }

    private void PostMediaInfo(MediaInfo mediaInfo)
    {
        var kv = new KeyType<MediaInfo> {Key = "MediaInfo", Val = mediaInfo};
        var jsMessage = new CsMessageKeyType<MediaInfo>
        {
            Data = kv
        };
        SerializeAndPost(jsMessage);
    }

    private void PostAppInfo()
    {
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
        const string build = "DEVELOPMENT";
    #else
        const string build = "PRODUCTION";
    #endif
        var appInfo = new AppInfo
        {
            Version = Application.version,
            BuildId = NetworkManagerGame.Singleton.BuildId,
            MicrophonePermission = Permission.HasUserAuthorizedPermission(Permission.Microphone),
            Build = build
        };
        var kvs = new KeyType<AppInfo> {Key = "AppInfo", Val = appInfo};
        var jsMessage = new CsMessageKeyType<AppInfo> {Data = kvs};
        SerializeAndPost(jsMessage);
    }

    private void PostExperimentalInfo()
    {
        var experimentalInfo = new ExperimentalInfo
        {
            PinchPullEnabled = InputManager.Hands.Left.PlayerHand.GetIHandTick<PinchPullHand>().pinchPullEnabled,
            BlockBreakEnabled = InputManager.Hands.Right.PlayerHand.GetIHandTick<BlockBreakHand>().BreakModeActive
        };

        var kvs = new KeyType<ExperimentalInfo> {Key = "ExperimentalInfo", Val = experimentalInfo};
        var jsMessage = new CsMessageKeyType<ExperimentalInfo> {Data = kvs};
        var message = JsonConvert.SerializeObject(jsMessage);
        browser.PostMessage(message);
    }

    private void PostKvs(KeyVal[] kvs)
    {
        var jsMessage = new CsMessageKeyVals
        {
            Data = kvs
        };
        var message = JsonConvert.SerializeObject(jsMessage);
        browser.PostMessage(message);
    }

    private void PostPlayerInfo(Dictionary<NetworkConnection, NetworkManagerGame.PlayerInfo> playerInfos)
    {
        var data = playerInfos.Select(entry => new PlayerData {Name = entry.Value.OculusId, ConnectionId = entry.Key.connectionId}).ToArray();
        var csMessage = new CsMessageKeyType<PlayerData[]> {Data = new KeyType<PlayerData[]> {Key = "PlayerInfos", Val = data}};
        SerializeAndPost(csMessage);
    }

    public static event Action<RoomData> JoinRoom;
    public static event Action LeaveRoom;
    public static event Action OpenRoom;
    public static event Action CloseRoom;
    public static event Action<int> KickConnectionId;

    public event EventHandler CloseMenu;
    public event EventHandler PlayVideo;
    public event EventHandler PauseVideo;
    public event EventHandler EjectVideo;
    public event EventHandler RestartVideo;
    public event EventHandler<float> SeekPlayer;

    public event EventHandler<string> BrowseSite;
    
    public event EventHandler<float> SetVolumeLevel;

    public event EventHandler<LightState> LightChange;

    private void BonsaiLog(string msg)
    {
        Debug.Log("<color=orange>BonsaiTableBrowserMenu: </color>: " + msg);
    }

    private void BonsaiLogWarning(string msg)
    {
        Debug.LogWarning("<color=orange>BonsaiTableBrowserMenu: </color>: " + msg);
    }

    private void BonsaiLogError(string msg)
    {
        Debug.LogError("<color=orange>BonsaiTableBrowserMenu: </color>: " + msg);
    }

    private struct NetworkInfo
    {
        public bool Online;
        public string NetworkAddress;
        public bool RoomOpen;
        public NetworkManagerMode Mode;
    }

    private class ExperimentalInfo
    {
        public bool BlockBreakEnabled;
        public bool PinchPullEnabled;
    }

    private class AppInfo
    {
        public string Build;
        public int BuildId;
        public bool MicrophonePermission;
        public string Version;
    }

    private class CsMessageKeyType<T>
    {
        public KeyType<T> Data;
        public string Message = "pushStoreSingle";
        public string Type = "command";
    }

    private class KeyType<T>
    {
        public string Key;
        public T Val;
    }

    private struct PlayerData
    {
        public string Name;
        public int ConnectionId;
    }

    private class CsMessageKeyVals
    {
        public KeyVal[] Data;
        public string Message = "pushStore";
        public string Type = "command";
    }

    private struct KeyVal
    {
        public string Key;
        public string Val;
    }

    public struct RoomData
    {
        public string id;
        public string ip_address;
        public string network_address;
        public int pinged;
        public int port;
    }

    public struct UserInfo
    {
        public string UserName;
    }
}