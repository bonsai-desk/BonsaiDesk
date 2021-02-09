using System;
using UnityEngine;

public class WebBrowserParent : MonoBehaviour {
	public TableBrowser _web;
	public TableBrowser _keyboard;

	// Start is called before the first frame update
	private void Start() {
		_web.BrowserReady        += SetupWeb;
		_keyboard.ListenersReady += SetupKeyboard;
	}

	// Update is called once per frame
	private void Update() { }

	private void SetupKeyboard() {
		Debug.Log("[BONSAI] Browser Keyboard Ready");
		_keyboard.KeyPress += HandleKeyPress;
		_keyboard.PostMessage(Browser.BrowserMessage.NavKeyboard);
		_keyboard.ToggleHidden();
	}

	private void SetupWeb() {
		Debug.Log("[BONSAI] Web Ready");
		//_web.PostMessage(Browser.BrowserMessage.NavHome);
		_web.ToggleHidden();
	}

	private void HandleKeyPress(string key) {
		_web.HandleKeyboardInput(key);
	}
}