﻿using System;
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
	public TableBrowserParent tableBrowserParent;

	// Start is called before the first frame update
	private void Start() {
		keyboardBrowser.ListenersReady += SetupKeyboardBrowser;
		webNavBrowser.BrowserReady     += SetupWebWebNavBrowser;
	}

	// Update is called once per frame
	private void Update() { }

	public event EventHandler CloseWeb;

	public void SetAllHidden(bool hidden) {
		webBrowser.SetHidden(hidden);
		keyboardBrowser.SetHidden(hidden);
		webNavBrowser.SetHidden(hidden);
		if (hidden) {
			LoadUrl("about:blank");
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
		tableBrowserParent.Sleep();
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
		var spawnedObject = Instantiate(VideoPrefab, position, Quaternion.AngleAxis(-90, Vector3.up));
		NetworkServer.Spawn(spawnedObject);
		spawnedObject.GetComponent<TabletControl>().videoId = id;
	}

	public void DummySpawn() {
		CmdSpawnYT(videoSpawnLocation.position, "niS_Fpy_2-U");
	}
}