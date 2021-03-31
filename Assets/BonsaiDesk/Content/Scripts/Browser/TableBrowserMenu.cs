using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using Mirror;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Vuplex.WebView;

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
        browser.ListenersReady += HandleListnersReady;
        NetworkManagerGame.Singleton.InfoChange += HandleNetworkInfoChange;
        OVRManager.HMDUnmounted += () => { browser.SetHidden(true); };
    }

    public void Update()
    {
        if (Time.time - _postMediaInfoLast > postMediaInfoEvery)
        {
            PostMediaInfo(autoBrowserController.GetMediaInfo());
            _postMediaInfoLast = Time.time;
        }

        if (Time.time - _postRoomInfoLast > PostRoomInfoEvery)
        {
            PostNetworkInfo();
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
        var HostEndPoint = NetworkManagerGame.Singleton.HostEndPoint;
        var State = NetworkManagerGame.Singleton.State;
        var PlayerInfos = NetworkManagerGame.Singleton.PlayerInfos;
        var roomOpen = NetworkManagerGame.Singleton.roomOpen;

        if (canPost)
        {
            _postRoomInfoLast = Time.time;
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
            const string build = "DEVELOPMENT";
        #else
			const string build = "PRODUCTION";
        #endif

            PostKvs(new[]
            {
                new KeyVal {Key = "build", Val = build}
            });
            PostIsInternetGood();
            PostNetworkState(State.ToString());
            PostPlayerInfo(PlayerInfos);
            PostRoomOpen(roomOpen);
            if (HostEndPoint != null)
            {
                PostRoomInfo(HostEndPoint.Address.ToString(), HostEndPoint.Port.ToString());
            }
            else
            {
                PostRoomInfo("", "");
            }
        }
    }

    private void SetupBrowser(object sender, EventArgs eventArgs)
    {
        browser.OnMessageEmitted(HandleJavascriptMessage);
    }

    private void HandleListnersReady()
    {
        Debug.Log("[BONSAI] nav to menu");
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
                        Debug.Log($"[BONSAI] Event JoinRoom {message.Data}");
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
                    case "kickConnectionId":
                        // todo what happens when this fails?
                        var id = JsonConvert.DeserializeObject<int>(message.Data);
                        KickConnectionId?.Invoke(id);
                        break;
                    case "volumeIncrement":
                        if (VolumeChange != null)
                        {
                            VolumeChange.Invoke(this, 0.10f);
                        }

                        break;
                    case "volumeDecrement":
                        if (VolumeChange != null)
                        {
                            VolumeChange.Invoke(this, -0.10f);
                        }

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

    public void PostNetworkState(string state)
    {
        KeyVal[] kvs =
        {
            new KeyVal {Key = "network_state", Val = state}
        };
        var jsMessage = new CsMessageKeyVals
        {
            Type = "command", Message = "pushStore", Data = kvs
        };
        var message = JsonConvert.SerializeObject(jsMessage);
        browser.PostMessage(message);
    }

    private void PostMediaInfo(AutoBrowserController.MediaInfo mediaInfo)
    {
        var kv = new KeyType<AutoBrowserController.MediaInfo> {Key = "media_info", Val = mediaInfo};
        var jsMessage = new CsMessageKeyType<AutoBrowserController.MediaInfo>
        {
            Data = kv
        };
        var message = JsonConvert.SerializeObject(jsMessage);
        browser.PostMessage(message);
    }

    private class ExperimentalInfo
    {
        public bool PinchPullEnabled;
        public bool BlockBreakEnabled;
    }

    private class AppInfo
    {
        public int BundleVersionCode;
    }

    private void PostAppInfo()
    {
        var appInfo = new AppInfo() {BundleVersionCode = PlayerSettings.Android.bundleVersionCode};
        var kvs = new KeyType<AppInfo> {Key = "app_info", Val = appInfo};
        var jsMessage = new CsMessageKeyType<AppInfo>() {Data = kvs};
        var message = JsonConvert.SerializeObject(jsMessage);
        browser.PostMessage(message);
    }

    private void PostExperimentalInfo()
    {
        var experimentalInfo = new ExperimentalInfo()
        {
            PinchPullEnabled = InputManager.Hands.Left.PlayerHand.GetIHandTick<PinchPullHand>().pinchPullEnabled,
            BlockBreakEnabled = InputManager.Hands.Right.PlayerHand.GetIHandTick<BlockBreakHand>().BreakModeActive
        };

        var kvs = new KeyType<ExperimentalInfo>() {Key = "experimental_info", Val = experimentalInfo};
        var jsMessage = new CsMessageKeyType<ExperimentalInfo> {Data = kvs};
        var message = JsonConvert.SerializeObject(jsMessage);
        browser.PostMessage(message);
    }

    private void PostIsInternetGood()
    {
        var kv = new KeyType<bool> {Key = "is_internet_good", Val = NetworkManagerGame.Singleton.IsInternetGood()};
        var jsMessage = new CsMessageKeyType<bool> {Data = kv};
        var message = JsonConvert.SerializeObject(jsMessage);
        browser.PostMessage(message);
    }

    public void PostRoomOpen(bool open)
    {
        var kv = new KeyType<bool> {Key = "room_open", Val = open};
        var jsMessage = new CsMessageKeyType<bool> {Data = kv};
        var message = JsonConvert.SerializeObject(jsMessage);
        browser.PostMessage(message);
    }

    public void PostRoomInfo(string ipAddress, string port)
    {
        KeyVal[] kvs =
        {
            new KeyVal {Key = "ip_address", Val = ipAddress},
            new KeyVal {Key = "port", Val = port}
        };
        var jsMessage = new CsMessageKeyVals
        {
            Data = kvs
        };
        var message = JsonConvert.SerializeObject(jsMessage);
        browser.PostMessage(message);
    }

    public void PostKvs(KeyVal[] kvs)
    {
        var jsMessage = new CsMessageKeyVals
        {
            Data = kvs
        };
        var message = JsonConvert.SerializeObject(jsMessage);
        browser.PostMessage(message);
    }

    public void PostPlayerInfo(Dictionary<NetworkConnection, NetworkManagerGame.PlayerInfo> playerInfos)
    {
        var data = playerInfos
                   .Select(entry => new PlayerData
                               {Name = entry.Value.User.DisplayName, ConnectionId = entry.Key.connectionId})
                   .ToArray();

        var csMessage = new CsMessageKeyType<PlayerData[]>
            {Data = new KeyType<PlayerData[]> {Key = "player_info", Val = data}};

        var message = JsonConvert.SerializeObject(csMessage);
        browser.PostMessage(message);
    }

    public void PostUserInfo(UserInfo userInfo)
    {
        var data = new KeyType<UserInfo> {Key = "user_info", Val = userInfo};
        var csMessage = new CsMessageKeyType<UserInfo> {Data = data};
        var message = JsonConvert.SerializeObject(csMessage);
        browser.PostMessage(message);
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
    public event EventHandler<float> SeekPlayer;

    public event EventHandler<string> BrowseSite;

    public event EventHandler<float> VolumeChange;

    public event EventHandler<LightState> LightChange;

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

    public struct PlayerData
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

    public struct KeyVal
    {
        public string Key;
        public string Val;
    }

    public struct RoomData
    {
        public string id;
        public string ip_address;
        public int pinged;
        public int port;
    }

    public struct UserInfo
    {
        public string UserName;
    }
}