using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Serialization;
using Vuplex.WebView;

[RequireComponent(typeof(TableBrowser))]
public class TableBrowserMenu : MonoBehaviour {
	public AutoBrowserController autoBrowserController;
	public float postMediaInfoEvery = 0.5f;
	private float _postMediaInfoLast;
	private TableBrowser _browser;
	

	private void Start() {
		_browser                =  GetComponent<TableBrowser>();
		_browser.BrowserReady   += SetupBrowser;
		_browser.ListenersReady += NavToMenu;
		OVRManager.HMDUnmounted += () => { _browser.SetHidden(true); };
	}

	public void Update() {
		if (Time.time - _postMediaInfoLast > postMediaInfoEvery) {
			PostMediaInfo(autoBrowserController.GetMediaInfo());
			_postMediaInfoLast = Time.time;
		}
	}

	private void SetupBrowser() {
		_browser.OnMessageEmitted(HandleJavascriptMessage);
	}

	private void NavToMenu() {
		Debug.Log("[BONSAI] nav to menu");
		_browser.PostMessage(Browser.BrowserMessage.NavToMenu);
	}

	private void HandleJavascriptMessage(object _, EventArgs<string> eventArgs) {
		var message = JsonConvert.DeserializeObject<Browser.JsMessageString>(eventArgs.Value);

		switch (message.Type) {
			case "command":
				switch (message.Message) {
					case "joinRoom":
						var roomData = JsonConvert.DeserializeObject<RoomData>(message.Data);
						Debug.Log($"[BONSAI] Join Room {message.Data}");
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
						if (BrowseSite != null) {
							BrowseSite(this, "https://m.youtube.com");
						}

						break;
					case "seekPlayer":
						var ts = float.Parse(message.Data);
						Debug.Log($"seekPlayer {ts}");
						autoBrowserController.CmdReadyUp(ts);

						break;
					case "kickConnectionId":
						// todo what happens when this fails?
						var id = JsonConvert.DeserializeObject<int>(message.Data);
						KickConnectionId?.Invoke(id);
						break;
				}

				break;

			case "event":
				break;
		}
	}

	public void PostNetworkState(string state) {
		KeyVal[] kvs = {
			new KeyVal {Key = "network_state", Val = state}
		};
		var jsMessage = new CsMessageKeyVals {
			Type = "command", Message = "pushStore", Data = kvs
		};
		var message = JsonConvert.SerializeObject(jsMessage);
		_browser.PostMessage(message);
	}

	private void PostMediaInfo(AutoBrowserController.MediaInfo mediaInfo) {
		var kv = new KeyType<AutoBrowserController.MediaInfo> {Key = "media_info", Val = mediaInfo};
		var jsMessage = new CsMessageKeyType<AutoBrowserController.MediaInfo>() {
			Data = kv
		};
		var message = JsonConvert.SerializeObject(jsMessage);
		_browser.PostMessage(message);
	}

	public void PostRoomOpen(bool open) {
		var kv        = new KeyType<bool> {Key           = "room_open", Val = open};
		var jsMessage = new CsMessageKeyType<bool> {Data = kv};
		var message   = JsonConvert.SerializeObject(jsMessage);
		_browser.PostMessage(message);
	}

	public void PostRoomInfo(string ipAddress, string port) {
		KeyVal[] kvs = {
			new KeyVal {Key = "ip_address", Val = ipAddress},
			new KeyVal {Key = "port", Val       = port}
		};
		var jsMessage = new CsMessageKeyVals {
			Data = kvs
		};
		var message = JsonConvert.SerializeObject(jsMessage);
		_browser.PostMessage(message);
	}

	public void PostKvs(KeyVal[] kvs) {
		var jsMessage = new CsMessageKeyVals {
			Data = kvs
		};
		var message = JsonConvert.SerializeObject(jsMessage);
		_browser.PostMessage(message);
	}

	public void PostPlayerInfo(Dictionary<NetworkConnection, NetworkManagerGame.PlayerInfo> playerInfos) {
		var data = playerInfos
		           .Select(entry => new PlayerData
			                   {Name = entry.Value.userInfo.DisplayName, ConnectionId = entry.Key.connectionId})
		           .ToArray();

		var csMessage = new CsMessageKeyType<PlayerData[]>
			{Data = new KeyType<PlayerData[]> {Key = "player_info", Val = data}};

		var message = JsonConvert.SerializeObject(csMessage);
		_browser.PostMessage(message);
	}

	public void PostUserInfo(UserInfo userInfo) {
		var data = new KeyType<UserInfo> {Key = "user_info", Val = userInfo};
		var csMessage = new CsMessageKeyType<UserInfo> {Data = data};
		var message   = JsonConvert.SerializeObject(csMessage);
		_browser.PostMessage(message);
	}

	public event Action<RoomData> JoinRoom;
	public event Action LeaveRoom;
	public event Action OpenRoom;
	public event Action CloseRoom;
	public event Action<int> KickConnectionId;

	public event EventHandler<string> BrowseSite;

	public event EventHandler<float> SeekPlayer;

	private class CsMessageKeyType<T> {
		public KeyType<T> Data;
		public string Message = "pushStoreSingle";
		public string Type = "command";
	}

	private class KeyType<T> {
		public string Key;
		public T Val;
	}

	public struct PlayerData {
		public string Name;
		public int ConnectionId;
	}

	private class CsMessageKeyVals {
		public KeyVal[] Data;
		public string Message = "pushStore";
		public string Type = "command";
	}

	public struct KeyVal {
		public string Key;
		public string Val;
	}

	public struct RoomData {
		public string id;
		public string ip_address;
		public int pinged;
		public int port;
	}

	public struct UserInfo {
		public string UserName;
	}
}