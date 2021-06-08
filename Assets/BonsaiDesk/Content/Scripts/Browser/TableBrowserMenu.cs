using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        Bright = 0,
        Vibes = 1
    }

    private const float PostRoomInfoEvery = 1f;
    public static TableBrowserMenu Singleton;
    public ContextBrowserController contextBrowserController;
    public TableBrowser contextBrowser;
    public AutoBrowserController autoBrowserController;
    public float postMediaInfoEvery = 0.5f;

    [HideInInspector] [FormerlySerializedAs("_browser")]
    public TableBrowser browser;

    [HideInInspector] public bool canPost;

    public Transform screen;
    public Transform raisedTransform;
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
        contextBrowserController.InfoChange += HandleChangeBlockActive;
        StartCoroutine(PostEventually(PostBlockList));
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
            PostSocialInfo();
            PostContextInfo();
        }
    }

    private void HandleChangeBlockActive()
    {
        PostContextInfo();
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
            MyNetworkAddress = NetworkManagerGame.Singleton.GetMyNetworkAddress(),
            RoomOpen = NetworkManagerGame.Singleton.roomOpen,
            Mode = NetworkManagerGame.Singleton.mode,
            PublicRoom = NetworkManagerGame.Singleton.publicRoom,
            Full = NetworkManagerGame.Singleton.RoomFull,
            Connecting = NetworkManagerGame.Singleton.connecting
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
                    case "openPublicRoom":
                        OpenRoom?.Invoke(true);
                        var resetNav = JsonConvert.DeserializeObject<bool>(message.Data);
                        if (resetNav)
                        {
                            browser.PostMessage(Browser.BrowserMessage.NavToMenu);
                        }

                        break;
                    case "openPrivateRoom":
                        OpenRoom?.Invoke(false);
                        browser.PostMessage(Browser.BrowserMessage.NavToMenu);
                        break;
                    case "closeRoom":
                        CloseRoom?.Invoke();
                        break;
                    case "browseYouTube":
                        // todo remove this
                        if (BrowseYouTube != null)
                        {
                            BrowseYouTube(this, new EventArgs());
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
                    case "layoutChange":
                        var isClient = NetworkManagerGame.Singleton.mode == NetworkManagerMode.ClientOnly;
                        var guyInRoom = NetworkManagerGame.Singleton.PlayerInfos.Count > 1;
                        if (isClient || guyInRoom)
                        {
                            switch (message.Data)
                            {
                                case "across":
                                    LayoutChange?.Invoke(this, SpotManager.Layout.Opposite);
                                    break;
                                case "sideBySide":
                                    LayoutChange?.Invoke(this, SpotManager.Layout.Side);
                                    break;
                            }
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
                    case "buildsRefresh":
                        RefreshBuilds();
                        break;
                    case "stageBuild":
                        StageBuild(message.Data);
                        break;
                    case "deleteBuild":
                        PostDeleteBuild(message.Data);
                        break;
                    case "spawnBuild":
                        SpawnBuild(message.Data);
                        break;
                    case "spawnBuildById":
                        SpawnBuildFromId(message.Data);
                        break;
                    case "saveBuild":
                        SaveBuild(message.Data);
                        break;
                }

                break;

            case "event":
                break;
        }
    }

    private void SpawnBuildFromId(string messageData)
    {
        if (!string.IsNullOrEmpty(messageData))
        {
            var spawned = BlockObjectSpawner.Instance.SpawnFromFileName(messageData);
            if (spawned)
            {
                TableBrowserParent.Instance.MenuSleep();
            }
            else
            {
                MessageStack.Singleton.AddMessage("Empty Block Message Data", MessageStack.MessageType.Bad);
            }
        }
        else
        {
            MessageStack.Singleton.AddMessage("Empty Block Message Data", MessageStack.MessageType.Bad);
        }
    }

    private void SaveBuild(string messageData)
    {
        if (!string.IsNullOrEmpty(messageData))
        {
            var saved = BlockObjectFileReader.SaveStagedBlockObject(messageData);
            if (saved)
            {
                PostBlockList();
                PostStagedSavedOk();
            }
        }
        else
        {
            MessageStack.Singleton.AddMessage("Can't save empty build");
        }
    }

    private void PostStagedSavedOk()
    {
        var data = new BuildsSaved {SavedOk = true};
        var msg = Message(data, "Builds");
        browser.PostMessage(msg);
    }

    private void StageBuild(string buildId)
    {
        var blockObject = BlockObjectFileReader.LoadFileIntoBlockObjectFile(buildId);
        PostStaging(blockObject);
    }

    public static string ByteArrayToString(byte[] ba)
    {
        //  https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa
        var hex = new StringBuilder(ba.Length * 2);
        foreach (var b in ba)
        {
            hex.AppendFormat("{0:x2}", b);
        }

        return hex.ToString();
    }

    public static byte[] StringToByteArray(string hex)
    {
        //  https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa
        var NumberChars = hex.Length;
        var bytes = new byte[NumberChars / 2];
        for (var i = 0; i < NumberChars; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }

        return bytes;
    }

    private BuildInfo ConvertBlockObject(BlockObjectFileReader.BlockObjectFile blockObject)
    {
        var bytes = Encoding.ASCII.GetBytes(blockObject.Content);
        var hex = ByteArrayToString(bytes);
        return new BuildInfo(blockObject.FileName, hex, blockObject.DisplayName);
    }

    private void PostBlockList()
    {
        var blockObjects = BlockObjectFileReader.GetBlockObjectFiles();
        var buildInfos = blockObjects.Select(ConvertBlockObject).ToArray();
        var BlockList = new Builds
        {
            List = buildInfos
        };
        browser.PostMessage(Message(BlockList, "Builds"));
    }

    private void PostStaging(BlockObjectFileReader.BlockObjectFile blockObject)
    {
        var buildInfo = ConvertBlockObject(blockObject);
        var buildStaging = new BuildStaging {Staging = buildInfo};
        var msg = Message(buildStaging, "Builds");
        browser.PostMessage(msg);
    }

    private void PostDeleteBuild(string buildId)
    {
        var deleted = BlockObjectFileReader.DeleteFile(buildId);
        if (deleted)
        {
            PostBlockList();
        }
        else
        {
            MessageStack.Singleton.AddMessage("Failed to Delete", MessageStack.MessageType.Bad);
        }
    }

    private void SpawnBuild(string data)
    {
        var bytes = StringToByteArray(data);
        var content = Encoding.ASCII.GetString(bytes);
        if (!string.IsNullOrEmpty(content))
        {
            // todo need a check here
            BlockObjectSpawner.Instance.SpawnFromString(content);
            TableBrowserParent.Instance.MenuSleep();
        }
        else
        {
            MessageStack.Singleton.AddMessage("Empty Block Object", MessageStack.MessageType.Bad);
        }
    }

    private void RefreshBuilds()
    {
        BlockObjectFileReader.GetBlockObjectFiles();
    }

    private static void RequestMicrophone()
    {
        Permission.RequestUserPermission(Permission.Microphone);
    }

    private void TogglePinchPull()
    {
        var pinchPullEnabled = InputManager.Hands.Left.PlayerHand.GetIHandTick<PinchPullHand>().pinchPullEnabled;
        var newPinchPullState = !pinchPullEnabled; //toggle state
        InputManager.Hands.Left.PlayerHand.GetIHandTick<PinchPullHand>().pinchPullEnabled = newPinchPullState;
        InputManager.Hands.Right.PlayerHand.GetIHandTick<PinchPullHand>().pinchPullEnabled = newPinchPullState;
        SaveSystem.Instance.BoolPairs["PinchPullEnabled"] = newPinchPullState;
        SaveSystem.Instance.Save();
    }

    private void ToggleBlockBreak()
    {
        var blockBreakActive = InputManager.Hands.Right.PlayerHand.GetIHandTick<BlockBreakHand>().HandBreakMode != BlockBreakHand.BreakMode.None;
        InputManager.Hands.Right.PlayerHand.GetIHandTick<BlockBreakHand>()
                    .SetBreakMode(blockBreakActive ? BlockBreakHand.BreakMode.None : BlockBreakHand.BreakMode.Single);
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

    private void PostContextInfo()
    {
        var contextInfo = new ContextInfo
        {
            HandActive = contextBrowserController.handActive,
            HandMode = contextBrowserController.ActiveHandMode,
            LeftBlockActive = contextBrowserController.LeftBlockActive,
            RightBlockActive = contextBrowserController.RightBlockActive
        };
        contextBrowser.PostMessage(Message(contextInfo, "ContextInfo"));
    }

    private string Message<T>(T info, string KeyName)
    {
        var kvs = new KeyType<T> {Key = KeyName, Val = info};
        var jsMessage = new CsMessageKeyType<T> {Data = kvs};
        return JsonConvert.SerializeObject(jsMessage);
    }

    private void Post<T>(T info, string KeyName)
    {
        var kvs = new KeyType<T> {Key = KeyName, Val = info};
        var jsMessage = new CsMessageKeyType<T> {Data = kvs};
        var message = JsonConvert.SerializeObject(jsMessage);
        browser.PostMessage(message);
    }

    public void PostAuthInfo(AuthInfo authInfo)
    {
        StartCoroutine(PostEventually(() => { Post(authInfo, "AuthInfo"); }));
    }

    private IEnumerator PostEventually(Action a)
    {
        while (!canPost)
        {
            yield return new WaitForSeconds(0.1f);
        }

        a();
    }

    private void PostSocialInfo()
    {
        var socialInfo = new SocialInfo
        {
            UserName = NetworkManagerGame.Singleton.UserName()
        };
        Post(socialInfo, "SocialInfo");
    }

    private void PostExperimentalInfo()
    {
        var experimentalInfo = new ExperimentalInfo
        {
            PinchPullEnabled = InputManager.Hands.Left.PlayerHand.GetIHandTick<PinchPullHand>().pinchPullEnabled,
            BlockBreakEnabled = InputManager.Hands.Right.PlayerHand.GetIHandTick<BlockBreakHand>().HandBreakMode != BlockBreakHand.BreakMode.None
        };
        Post(experimentalInfo, "ExperimentalInfo");
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

    public void SetRaised(bool raised)
    {
        if (raised)
        {
            screen.localPosition = raisedTransform.localPosition;
            screen.localEulerAngles = raisedTransform.localEulerAngles;
        }
        else
        {
            screen.localPosition = Vector3.zero;
            screen.localEulerAngles = Vector3.zero;
        }
    }

    public static event Action<RoomData> JoinRoom;
    public static event Action LeaveRoom;
    public static event Action<bool> OpenRoom;
    public static event Action CloseRoom;
    public static event Action<int> KickConnectionId;

    public event EventHandler CloseMenu;
    public event EventHandler PlayVideo;
    public event EventHandler PauseVideo;
    public event EventHandler EjectVideo;
    public event EventHandler RestartVideo;
    public event EventHandler<float> SeekPlayer;

    public event EventHandler BrowseYouTube;

    public event EventHandler<float> SetVolumeLevel;

    public event EventHandler<LightState> LightChange;

    public event EventHandler<SpotManager.Layout> LayoutChange;

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

    public void NavToSaveDraft()
    {
        // nav to menu first in case they are already on the draft page
        // this re-triggers the modal to pop up again
        browser.PostMessage(Browser.BrowserMessage.NavToMenu);
        browser.PostMessage(Browser.BrowserMessage.NavToSaveDraft);
        TableBrowserParent.Instance.OpenMenu();
    }

    private struct BuildsSaved
    {
        public bool SavedOk;
    }

    private class BuildInfo
    {
        public string Data;
        public string Id;
        public string Name;

        public BuildInfo(string id, string data, string name)
        {
            Id = id;
            Data = data;
            Name = name;
        }
    }

    private struct Builds
    {
        public BuildInfo[] List;
    }

    private struct BuildStaging
    {
        public BuildInfo Staging;
    }

    private class ContextInfo
    {
        public ContextBrowserController.Hand HandActive;
        public BlockBreakHand.BreakMode HandMode;
        public string LeftBlockActive;
        public string RightBlockActive;
    }

    private struct NetworkInfo
    {
        public bool Online;
        public string NetworkAddress;
        public string MyNetworkAddress;
        public bool RoomOpen;
        public NetworkManagerMode Mode;
        public bool PublicRoom;
        public bool Full;
        public bool Connecting;
    }

    private class ExperimentalInfo
    {
        public bool BlockBreakEnabled;
        public bool PinchPullEnabled;
    }

    private class SocialInfo
    {
        public string UserName;
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

    public struct AuthInfo
    {
        public ulong UserId;
        public string Nonce;
        public string Build; // mobile or desktop
    }

    public struct UserInfo
    {
        public string UserName;
    }
}