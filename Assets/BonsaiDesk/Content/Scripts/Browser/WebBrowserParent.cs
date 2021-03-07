using System;
using Mirror;
using UnityEngine;
using Vuplex.WebView;

public class WebBrowserParent : NetworkBehaviour {
	public TableBrowser webBrowser;
	public TableBrowser keyboardBrowser;
	public TableBrowser webNavBrowser;
	public KeyboardBrowserController keyboardBrowserController;
	public WebBrowserController webBrowserController;
	public WebNavBrowserController webNavBrowserController;
	public GameObject VideoPrefab;
	public Transform videoSpawnLocation;
	private Vector3 _altTransform;
	private Vector3 _startTransform;

	// Start is called before the first frame update
	private void Start() {
		keyboardBrowser.ListenersReady += SetupKeyboardBrowser;
		webNavBrowser.BrowserReady     += SetupWebWebNavBrowser;

		_startTransform         = transform.localPosition;
		_altTransform           = new Vector3(0f, -10f, 0f);
		transform.localPosition = _altTransform;
	}

	// Update is called once per frame
	private void Update() { }

	public event EventHandler CloseWeb;

	public void SetActive(bool active) {
		// hide and disable interaction with kb/nav
		// move webbrowser to about:blank and disable
		if (active) {
			transform.localPosition = _startTransform;
		}
		else {
			Debug.Log("[BONSAI] SetActive");
			LoadUrl("about:blank");
			transform.localPosition = _altTransform;
		}
	}

	private void SetupWebWebNavBrowser() {
		Debug.Log("[BONSAI] SetupWebWebNavBrowser");
		webNavBrowserController.GoBack          += HandleGoBack;
		webNavBrowserController.GoForward       += HandleGoForward;
		webNavBrowserController.SpawnKeyboard   += HandleSpawnKeyboard;
		webNavBrowserController.DismissKeyboard += HandleDismissKeyboard;
		webNavBrowserController.CloseWeb        += HandleCloseWeb;
		webBrowserController.SpawnYT            += HandleSpawnYt;
		webBrowserController.InputFocus         += HandleInputFocus;
	}

	private void HandleInputFocus(object sender, EventArgs<bool> e) {
		if (e.Value) {
			HandleSpawnKeyboard();
		}
		else {
			HandleDismissKeyboard();
		}
	}

	private void HandleSpawnYt(object sender, EventArgs<string> e) {
		Debug.Log($"[BONSAI] Spawn YT {e.Value}");
		CmdSpawnYT(videoSpawnLocation.localPosition, e.Value);
	}

	private void SetupKeyboardBrowser() {
		Debug.Log("[BONSAI] SetupKeyboardBrowser");
		keyboardBrowser.InputRecieved += (sender, e) => webBrowser.HandleKeyboardInput(e.Value);
	}

	private void HandleGoBack() {
		webBrowser.GoBack();
	}

	private void HandleGoForward() {
		webBrowser.GoForward();
	}

	private void HandleCloseWeb() {
		if (CloseWeb != null) {
			CloseWeb(this, new EventArgs());
		}
	}

	private void HandleSpawnKeyboard() {
		webBrowserController.SetRaised(true);
		keyboardBrowserController.SetActive(true);
	}

	private void HandleDismissKeyboard() {
		webBrowserController.SetRaised(false);
		keyboardBrowserController.SetActive(false);
	}

	public void LoadUrl(string url) {
		webBrowser.LoadUrl(url);
	}

	[Command(ignoreAuthority = true)]
	private void CmdSpawnYT(Vector3 position, string id) {
		var spawnedObject = Instantiate(VideoPrefab, position, Quaternion.identity);
		NetworkServer.Spawn(spawnedObject);
		spawnedObject.GetComponent<TabletControl>().videoId = id;
	}

	public void DummySpawn() {
		CmdSpawnYT(videoSpawnLocation.localPosition, "niS_Fpy_2-U");
	}
}