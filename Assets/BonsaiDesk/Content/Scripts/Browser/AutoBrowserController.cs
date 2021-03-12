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
public class AutoBrowserController : NetworkBehaviour {
	private const float ClientJoinGracePeriod = 10f;
	private const float ClientPingTolerance = 1f;
	private const float ClientPingInterval = 0.1f;
	private const float MaxReadyUpPeriod = 10f;
	private const float VideoSyncTolerance = 1f;
	public TogglePause togglePause;
	public TabletSpot tabletSpot;
	private readonly Dictionary<uint, double> _clientsJoinedNetworkTime = new Dictionary<uint, double>();
	private readonly Dictionary<uint, double> _clientsLastPing = new Dictionary<uint, double>();
	private readonly Dictionary<uint, PlayerState> _clientsPlayerStatus = new Dictionary<uint, PlayerState>();
	private bool _allGood;
	private AutoBrowser _autoBrowser;
	private double _beginReadyUpTime;
	private double _clientLastSentPing;
	private PlayerState _clientPlayerStatus;
	private float _clientPlayerTimeStamp;
	private float _clientPlayerDuration;
	private ContentInfo _contentInfo;
	private double _contentInfoAtTime;
	private Coroutine _fetchAndReadyUp;

	[SyncVar] private float _height;
	[SyncVar] private ScrubData _idealScrub;

	private void Start() {
		// so the server runs a browser but does not sync it yet
		// it will need to be synced for streamer mode
		_autoBrowser              =  GetComponent<AutoBrowser>();
		_autoBrowser.BrowserReady += () => { SetupBrowser(); };
	}

	private void Update() {
		if (isServer) {
			HandlePlayerServer();
			HandleScreenServer();
		}

		if (isClient) {
			HandlePlayerClient();
			HandleScreenClient();
		}
	}

	public override void OnStartClient() {
		base.OnStartClient();
		_clientPlayerStatus = PlayerState.Unstarted;
	}

	public override void OnStartServer() {
		TLog("On Start Server");
		base.OnStartServer();
		_contentInfo                                  =  new ContentInfo(false, "", new Vector2(1, 1));
		
		NetworkManagerGame.Singleton.ServerAddPlayer  -= HandleServerAddPlayer;
		NetworkManagerGame.Singleton.ServerDisconnect -= HandleServerDisconnect;
		togglePause.CmdSetPausedServer                -= HandleCmdSetPausedServer;
		
		NetworkManagerGame.Singleton.ServerAddPlayer  += HandleServerAddPlayer;
		NetworkManagerGame.Singleton.ServerDisconnect += HandleServerDisconnect;
		togglePause.CmdSetPausedServer                += HandleCmdSetPausedServer;

		if (tabletSpot != null) {
			tabletSpot.SetNewVideo -= HandleSetNewVideo;
			tabletSpot.PlayVideo   -= HandlePlayVideo;
			tabletSpot.StopVideo   -= HandleStopVideo;
			
			tabletSpot.SetNewVideo += HandleSetNewVideo;
			tabletSpot.PlayVideo   += HandlePlayVideo;
			tabletSpot.StopVideo   += HandleStopVideo;
		}
	}

	public override void OnStopServer() {
		TLog("On Stop Server");
		base.OnStopServer();
	}

	private void HandleSetNewVideo(string id) {
		if (_contentInfo.Active) {
			CloseVideo();
		}
	}

	private void HandlePlayVideo(string id) {
		LoadVideo(id, 0);
	}

	private void HandleStopVideo() {
		if (_contentInfo.Active) {
			CloseVideo();
		}
	}

	private void HandleServerAddPlayer(NetworkConnection newConn) {
		var newId = newConn.identity.netId;
		TLog($"AutoBrowserController add player [{newId}]");
		_clientsJoinedNetworkTime.Add(newId, NetworkTime.time);
		if (_contentInfo.Active) {
			BeginSync("new player joined");
			var timeStamp = _idealScrub.CurrentTimeStamp(NetworkTime.time);
			TargetReloadYoutube(newConn, _contentInfo.ID, timeStamp, _contentInfo.Aspect);
			foreach (var conn in NetworkServer.connections.Values) {
				if (conn.identity.netId != newId) {
					TargetReadyUp(conn, timeStamp);
				}
			}
		}
	}

	private void HandleServerDisconnect(NetworkConnection conn) {
		var id = conn.identity.netId;
		TLog($"AutoBrowserController remove player [{id}]");
		_clientsJoinedNetworkTime.Remove(id);
		_clientsLastPing.Remove(id);
		_clientsPlayerStatus.Remove(id);
	}

	private void HandleCmdSetPausedServer(bool paused) {
		if (!_contentInfo.Active) {
			Debug.LogWarning("Ignoring attempt to toggle pause status when content is not active");
			return;
		}

		if (paused) {
			// todo set the toggle pause inactive now
			_idealScrub = _idealScrub.Pause(NetworkTime.time);
			var timeStamp = _idealScrub.CurrentTimeStamp(NetworkTime.time);
			TLog($"Paused scrub at timestamp {timeStamp}");
			RpcReadyUp(timeStamp);
		}
		else {
			// todo set the togglepause to activate when this starts
			var timeStamp = _idealScrub.CurrentTimeStamp(NetworkTime.time);
			BeginSync("toggled play");
			RpcReadyUp(timeStamp);
		}
	}

	private void HandlePlayerClient() {
		// post play message if paused/ready and player is behind ideal scrub
		if (_clientPlayerStatus == PlayerState.Ready && _idealScrub.IsStarted(NetworkTime.time)) {
			_autoBrowser.PostMessage(YouTubeMessage.Play);
		}

		// ping the server with the current timestamp
		if (_contentInfo.Active &&
		    NetworkTime.time - _clientLastSentPing > ClientPingInterval &&
		    NetworkClient.connection != null && NetworkClient.connection.identity != null) {
			var id        = NetworkClient.connection.identity.netId;
			var now       = NetworkTime.time;
			var timeStamp = _clientPlayerTimeStamp;
			CmdPingAndCheckTimeStamp(id, now, timeStamp);
		}
	}

	private void HandleScreenClient() {
		_autoBrowser.SetHeight(_height);
	}

	private void BeginSync(string reason = "no reason provided") {
		TLog($"Beginning the sync process because [{reason}]");
		togglePause.SetInteractable(false);
		_allGood = false;
		_clientsLastPing.Clear();
		_clientsPlayerStatus.Clear();
		_beginReadyUpTime = NetworkTime.time;
		if (_idealScrub.Active) {
			_idealScrub = _idealScrub.Pause(NetworkTime.time);
		}
	}

	private void HandlePlayerServer() {
		if (_contentInfo.Active == false) {
			return;
		}

		if (_allGood && BadPingExists()) {
			BeginSync("a bad ping");
			RpcReadyUp(_idealScrub.CurrentTimeStamp(NetworkTime.time));
		}

		if (!_allGood) {
			if (AllClientsReportPlayerStatus(PlayerState.Ready)) {
				var networkTimeToUnpause = NetworkTime.time + 1;
				TLog($"All clients report ready, un-pausing the scrub at network time {networkTimeToUnpause}");
				_idealScrub = _idealScrub.UnPauseAtNetworkTime(networkTimeToUnpause);
				// todo this could become interactable at networkTimeToUnpause
				togglePause.SetInteractable(true);
				_allGood = true;
			}
			else if (NetworkTime.time - _beginReadyUpTime > MaxReadyUpPeriod) {
				var (numFailed, failedIdsStr) = FailedToMatchStatus(_clientsPlayerStatus, PlayerState.Ready);
				HardReload(
					$"[{numFailed}/{NetworkServer.connections.Count}] clients failed to ready up [{failedIdsStr}]");
			}
		}
	}

	private static (int, string) FailedToMatchStatus(Dictionary<uint, PlayerState> _clientsPlayerStatus,
	                                                 PlayerState playerState) {
		var failedNetIds = new HashSet<string>();

		foreach (var info in _clientsPlayerStatus.Where(info => info.Value != playerState)) {
			failedNetIds.Add($"{info.Key} {playerState}");
		}

		return (failedNetIds.Count, string.Join(", ", failedNetIds));
	}

	private bool BadPingExists() {
		var aBadPing = false;

		foreach (var entry in _clientsLastPing) {
			if (!ClientInGracePeriod(entry.Key) && !ClientPingedRecently(entry.Value)) {
				TLog($"Bad ping for client [{entry.Key}]");
				aBadPing = true;
			}
		}

		return aBadPing;
	}

	private void HandleScreenServer() {
		const float transitionTime = 0.5f;
		var         browserDown    = !_contentInfo.Active || NetworkTime.time - _contentInfoAtTime < 1.5;
		var         targetHeight   = browserDown ? 0 : 1;

		if (!Mathf.Approximately(_height, targetHeight)) {
			var easeFunction = browserDown ? CubicBezier.EaseOut : CubicBezier.EaseIn;
			var t            = easeFunction.SampleInverse(_height);
			var step         = 1f / transitionTime * Time.deltaTime;
			t       = Mathf.MoveTowards(t, targetHeight, step);
			_height = easeFunction.Sample(t);
		}
	}

	private void SetupBrowser(bool restart = false) {
		Debug.Log("setup browser");
		if (!restart) {
			_autoBrowser.OnMessageEmitted(HandleJavascriptMessage);
		}
	}

	private void HandleJavascriptMessage(object _, EventArgs<string> eventArgs) {
		var json = JSONNode.Parse(eventArgs.Value) as JSONObject;

		if (json?["type"] != "infoCurrentTime") {
			TLog($"Received JSON {eventArgs.Value} at {NetworkTime.time}");
		}

		if (json?["current_time"] != null) {
			_clientPlayerTimeStamp      = json["current_time"];
			_clientPlayerDuration = json["duration"];
		}

		switch (json?["type"].Value) {
			case "infoCurrentTime":
				return;
			case "error":
				Debug.LogError(Tag() + $"Javascript error [{json["error"].Value}]");
				return;
			case "stateChange":
				switch ((string) json["message"]) {
					case "READY":
						_clientPlayerStatus = PlayerState.Ready;
						break;
					case "PAUSED":
						_clientPlayerStatus = PlayerState.Paused;
						break;
					case "PLAYING":
						_clientPlayerStatus = PlayerState.Playing;
						break;
					case "BUFFERING":
						_clientPlayerStatus = PlayerState.Buffering;
						break;
					case "ENDED":
						_clientPlayerStatus = PlayerState.Ended;
						break;
				}

				CmdUpdateClientPlayerStatus(NetworkClient.connection.identity.netId, _clientPlayerStatus);
				break;
		}
	}

	private bool ClientInGracePeriod(uint id) {
		return ClientJoinGracePeriod > NetworkTime.time - _clientsJoinedNetworkTime[id];
	}

	private static bool ClientPingedRecently(double pingTime) {
		return ClientPingTolerance > NetworkTime.time - pingTime;
	}

	private bool ClientVideoIsSynced(double timeStamp) {
		var whereTheyShouldBe = _idealScrub.CurrentTimeStamp(NetworkTime.time);
		var whereTheyAre      = timeStamp;
		var synced            = Math.Abs(whereTheyAre - whereTheyShouldBe) < VideoSyncTolerance;
		if (!synced) {
			TLog(
				$"Client reported timestamp {whereTheyAre} which is not within {VideoSyncTolerance} seconds of {whereTheyShouldBe}");
		}

		return synced;
	}

	private void ReloadYouTube(string id, double timeStamp, Vector2 aspect) {
		TLog($"NavHome then load {id} at {timeStamp}");
		var resolution = _autoBrowser.ChangeAspect(aspect);
		_autoBrowser.PostMessages(new[] {
			YouTubeMessage.NavHome,
			YouTubeMessage.LoadYouTube(id, timeStamp, resolution.x, resolution.y)
		});
	}

	private bool AllClientsReportPlayerStatus(PlayerState playerState) {
		if (_clientsPlayerStatus.Count != NetworkServer.connections.Count) {
			return false;
		}

		return _clientsPlayerStatus.Values.All(status => status == playerState);
	}

	public void ButtonReloadBrowser() {
		SetupBrowser(true);
	}

	[Server]
	public void ButtonHardReload() {
		HardReload("pressed the hard reload button");
	}

	[Server]
	private void HardReload(string reason = "no reason provided") {
		TLog($"Initiating a hard reload because [{reason}]");
		_clientsLastPing.Clear();
		_clientsPlayerStatus.Clear();
		_beginReadyUpTime = NetworkTime.time;
		_idealScrub       = _idealScrub.Pause(NetworkTime.time);

		var timeStamp = _idealScrub.CurrentTimeStamp(NetworkTime.time);
		togglePause.ServerSetPaused(false);
		RpcReloadYouTube(_contentInfo.ID, timeStamp, _contentInfo.Aspect);
	}

	[Server]
	private void LoadVideo(string id, double timeStamp) {
		togglePause.ServerSetPaused(false);

		TLog($"Fetching info for video {id}");

		if (_fetchAndReadyUp != null) {
			StopCoroutine(_fetchAndReadyUp);
		}

		_fetchAndReadyUp = StartCoroutine(FetchYouTubeAspect(id, aspect =>
		{
			TLog($"Fetched aspect ({aspect.x},{aspect.y}) for video ({id})");

			_contentInfo       = new ContentInfo(true, id, aspect);
			_contentInfoAtTime = NetworkTime.time;
			_idealScrub        = ScrubData.PausedAtScrub(timeStamp);

			BeginSync("new video");
			RpcReloadYouTube(id, timeStamp, aspect);

			_fetchAndReadyUp = null;
		}));
	}

	[Command(ignoreAuthority = true)]
	private void CmdPingAndCheckTimeStamp(uint id, double networkTime, double timeStamp) {
		_clientsLastPing[id] = NetworkTime.time;

		// TODO could use networkTime of timeStamp to account for rtt
		if (_allGood && !ClientInGracePeriod(id) && !ClientVideoIsSynced(timeStamp)) {
			BeginSync("Client reported a bad timestamp in ping");
			RpcReadyUp(_idealScrub.CurrentTimeStamp(NetworkTime.time));
		}
	}

	[Command(ignoreAuthority = true)]
	private void CmdUpdateClientPlayerStatus(uint id, PlayerState playerState) {
		TLog($"Client [{id}] is {playerState}");
		_clientsPlayerStatus[id] = playerState;
	}

	[Command(ignoreAuthority = true)]
	private void CmdLoadVideo(string id, double timeStamp) {
		LoadVideo(id, timeStamp);
	}

	[Command(ignoreAuthority = true)]
	private void CmdCloseVideo() {
		CloseVideo();
	}

	[Server]
	private void CloseVideo() {
		// todo set paused
		// todo lower the screen
		_contentInfo = new ContentInfo(false, "", new Vector2(1, 1));
		togglePause.SetInteractable(false);
		RpcGoHome();
	}

	[TargetRpc]
	private void TargetReloadYoutube(NetworkConnection target, string id, double timeStamp, Vector2 aspect) {
		TLog("[Target RPC] ReloadYouTube");
		ReloadYouTube(id, timeStamp, aspect);
	}

	[ClientRpc]
	private void RpcReloadYouTube(string id, double timeStamp, Vector2 aspect) {
		TLog("[RPC] ReloadYouTube");
		ReloadYouTube(id, timeStamp, aspect);
	}

	[ClientRpc]
	private void RpcReadyUp(double timeStamp) {
		TLog($"[RPC] Ready up at timestamp {timeStamp}");
		_autoBrowser.PostMessage(YouTubeMessage.ReadyUpAtTime(timeStamp));
	}

	[TargetRpc]
	private void TargetReadyUp(NetworkConnection target, double timeStamp) {
		TLog($"[TARGET RPC] Ready up at {timeStamp}");
		_autoBrowser.PostMessage(YouTubeMessage.ReadyUpAtTime(timeStamp));
	}

	[ClientRpc]
	private void RpcGoHome() {
		TLog("Navigating home");
		_autoBrowser.PostMessage(YouTubeMessage.NavHome);
	}

	private static IEnumerator FetchYouTubeAspect(string videoId, Action<Vector2> callback) {
		var newAspect = new Vector2(16, 9);

		var videoInfoUrl = $"https://api.desk.link/youtube/{videoId}";

		using (var www = UnityWebRequest.Get(videoInfoUrl)) {
			var req = www.SendWebRequest();

			yield return req;

			if (!(www.isHttpError || www.isNetworkError)) {
				var jsonNode = JSONNode.Parse(www.downloadHandler.text) as JSONObject;
				if (jsonNode?["width"] != null && jsonNode["height"] != null) {
					var width  = (float) jsonNode["width"];
					var height = (float) jsonNode["height"];
					newAspect = new Vector2(width, height);
				}
			}
		}

		callback(newAspect);
	}

	private string Tag() {
		switch (isClient) {
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

	private void TLog(string message) {
		Debug.Log(Tag() + message);
	}

	private enum PlayerState {
		Unstarted,
		Ready,
		Paused,
		Playing,
		Buffering,
		Ended
	}

	private static class YouTubeMessage {
		public const string Play = "{\"type\": \"video\", \"command\": \"play\"}";
		public const string Pause = "{\"type\": \"video\", \"command\": \"pause\"}";

		public const string MaskOn = "{" + "\"type\": \"video\", " + "\"command\": \"maskOn\" " + "}";
		public const string MaskOff = "{" + "\"type\": \"video\", " + "\"command\": \"maskOff\" " + "}";
		public static readonly string NavHome = PushPath("/home");

		private static string PushPath(string path) {
			return "{" +
			       "\"type\": \"nav\", " +
			       "\"command\": \"push\", " +
			       $"\"path\": \"{path}\"" +
			       "}";
		}

		public static string LoadYouTube(string id, double ts, int x = 0, int y = 0) {
			var resQuery = "";
			if (x != 0 && y != 0) {
				resQuery = $"?x={x}&y={y}";
			}

			return "{" +
			       "\"type\": \"nav\", " +
			       "\"command\": \"push\", " +
			       $"\"path\": \"/youtube/{id}/{ts}{resQuery}\"" +
			       "}";
		}

		public static string ReadyUpAtTime(double timeStamp) {
			return "{" +
			       "\"type\": \"video\", " +
			       "\"command\": \"readyUp\", " +
			       $"\"timeStamp\": {timeStamp}" +
			       "}";
		}
	}

	private readonly struct ContentInfo {
		public readonly bool Active;
		public readonly string ID;
		public readonly Vector2 Aspect;

		public ContentInfo(bool active, string id, Vector2 aspect) {
			Active = active;
			ID     = id;
			Aspect = aspect;
		}
	}

	public readonly struct ScrubData {
		public readonly double Scrub;
		public readonly double NetworkTimeActivated;
		public readonly bool Active;

		private ScrubData(double scrub, double networkTimeActivated, bool active) {
			Scrub                = scrub;
			NetworkTimeActivated = networkTimeActivated;
			Active               = active;
		}

		public static ScrubData PausedAtScrub(double scrub) {
			return new ScrubData(scrub, -1, false);
		}

		public ScrubData Pause(double networkTime) {
			return new ScrubData(
				CurrentTimeStamp(networkTime), -1, false
			);
		}

		public ScrubData UnPauseAtNetworkTime(double networkTime) {
			if (Active) {
				Debug.LogError("Scrub should be paused before resuming");
			}

			return new ScrubData(Scrub, networkTime, true);
		}

		public double CurrentTimeStamp(double networkTime) {
			if (!Active || networkTime - NetworkTimeActivated < 0) {
				return Scrub;
			}

			return Scrub + (networkTime - NetworkTimeActivated);
		}

		public bool IsStarted(double networkTime) {
			return Active && networkTime > NetworkTimeActivated;
		}
	}

	public class MediaInfo {
		public bool Active;
		public string Name;
		public bool Paused;
		public float Scrub;
		public float Duration;

		public MediaInfo() {
			Active               = false;
			Name                 = "None";
			Paused               = true;
			Scrub                = 0f;
			Duration             = 1f;
		}
	}

	public MediaInfo GetMediaInfo () {
		if (_contentInfo.Active) {
			return new MediaInfo {
				Active=true, 
				Name = "youtube." + _contentInfo.ID, 
				Paused = !_idealScrub.Active, 
				Scrub = _clientPlayerTimeStamp,
				Duration = _clientPlayerDuration
			};

		}
		return new MediaInfo();
	}
	
}