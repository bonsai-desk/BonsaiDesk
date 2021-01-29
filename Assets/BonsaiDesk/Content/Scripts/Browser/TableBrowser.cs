using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Newtonsoft.Json;
using OVR;
using UnityEngine;
using Vuplex.WebView;

public class TableBrowser : NewBrowser {
	public string initialUrl;
	public CustomInputModule customInputModule;
	public bool useBuiltHtml;

	public SoundFXRef hoverSound;
	public SoundFXRef mouseDownSound;
	public SoundFXRef mouseUpSound;

	protected override void Start() {
		base.Start();
		_webViewPrefab.DragMode = DragMode.DragToScroll;

	#if UNITY_EDITOR || DEVELOPMENT_BUILD
		if (useBuiltHtml) {
			initialUrl = "streaming-assets://build/index.html";
		}
	#else
		initialUrl = "streaming-assets://build/index.html";
	#endif

		_webViewPrefab.InitialUrl = initialUrl;
		BrowserReady += () =>
		{
			OnMessageEmitted(HandleJavascriptMessage);
			var view = _webViewPrefab.transform.Find("WebViewPrefabResizer/WebViewPrefabView");
			CustomInputModule.Singleton.screens.Add(view);
		};
		ListenersReady += NavToMenu;
	}

	public event Action<RoomData> JoinRoom;
	public event Action LeaveRoom;
	public event Action KickAll;
	public event Action<int> KickConnectionId;

	private void NavToMenu() {
		PostMessage(BrowserMessage.NavToMenu);
	}

	public override Vector2Int ChangeAspect(Vector2 newAspect) {
		var aspectRatio = newAspect.x / newAspect.y;
		var localScale  = new Vector3(_bounds.y * aspectRatio, _bounds.y, 1);
		if (localScale.x > _bounds.x) {
			localScale = new Vector3(_bounds.x, _bounds.x * (1f / aspectRatio), 1);
		}

		var resolution = AutoResolution(_bounds, distanceEstimate, pixelPerDegree, newAspect);

		var res       = resolution.x > resolution.y ? resolution.x : resolution.y;
		var scale     = _bounds.x > _bounds.y ? _bounds.x : _bounds.y;
		var resScaled = res / scale;

		_webViewPrefab.WebView.SetResolution(resScaled);
		_webViewPrefab.Resize(_bounds.x, _bounds.y);

		Debug.Log($"[BONSAI] ChangeAspect resolution {resolution}");

		boundsTransform.localScale = localScale;

	#if UNITY_ANDROID && !UNITY_EDITOR
        RebuildOverlay(resolution);
	#endif

		return resolution;
	}

	private void HandleJavascriptMessage(object _, EventArgs<string> eventArgs) {
		var message = JsonConvert.DeserializeObject<JsMessageString>(eventArgs.Value);

		Debug.Log($"[BONSAI] JS Message: {message.Type} {message.Message}");

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
					case "kickAll":
						KickAll?.Invoke();
						break;
					case "kickConnectionId":
						// todo what happens when this fails?
						var id = JsonConvert.DeserializeObject<int>(message.Data);
						KickConnectionId?.Invoke(id);
						break;
				}

				break;

			case "event":
				switch (message.Message) {
					case "hover":
						hoverSound.PlaySoundAt(customInputModule.cursorRoot);
						break;
					case "mouseDown":
						mouseDownSound.PlaySoundAt(customInputModule.cursorRoot);
						break;
					case "mouseUp":
						mouseUpSound.PlaySoundAt(customInputModule.cursorRoot);
						break;
				}

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
		PostMessage(message);
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
		PostMessage(message);
	}

	public void PostPlayerInfo(Dictionary<NetworkConnection, NetworkManagerGame.PlayerInfo> playerInfos) {
		var data = playerInfos.Select(entry => new PlayerData(){Name="Player", ConnectionId = entry.Key.connectionId}).ToArray();

		var csMessage = new CsMessageKeyType<PlayerData[]> {Data = new KeyType<PlayerData[]>{Key="player_info", Val=data}};
		
		var message   = JsonConvert.SerializeObject(csMessage);
		PostMessage(message);
	}

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

	private struct KeyVal {
		public string Key;
		public string Val;
	}

	public struct RoomData {
		public string id;
		public string ip_address;
		public int pinged;
		public int port;
	}

}