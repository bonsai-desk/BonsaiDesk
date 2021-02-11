using System;
using UnityEngine;

public class WebBrowserParent : MonoBehaviour {
	public TableBrowser webBrowser;
	public TableBrowser keyboardBrowser;
	public TableBrowser webNavBrowser;
	public KeyboardBrowserController keyboardBrowserController;
	public WebBrowserController webBrowserController;
	public WebNavBrowserController webNavBrowserController;

	// Start is called before the first frame update
	private void Start() {
		keyboardBrowser.ListenersReady += SetupKeyboardBrowser;
		webNavBrowser.BrowserReady     += SetupWebWebNavBrowser;
	}

	// Update is called once per frame
	private void Update() { }

	private void SetupWebWebNavBrowser() {
		webNavBrowserController.GoBack          += HandleGoBack;
		webNavBrowserController.GoForward       += HandleGoForward;
		webNavBrowserController.SpawnKeyboard   += HandleSpawnKeyboard;
		webNavBrowserController.DismissKeyboard += HandleDismissKeyboard;
		webNavBrowserController.CloseWeb        += HandleCloseWeb;
	}

	private void SetupKeyboardBrowser() {
		keyboardBrowser.KeyPress += HandleKeyPress;
	}

	private void HandleGoBack() {
		webBrowser.GoBack();
	}

	private void HandleGoForward() {
		webBrowser.GoForward();
	}

	private void HandleCloseWeb() {
		throw new NotImplementedException();
	}

	private void HandleSpawnKeyboard() {
		webBrowserController.SetRaised(true);
		keyboardBrowserController.SetActive(true);
	}

	private void HandleDismissKeyboard() {
		webBrowserController.SetRaised(false);
		keyboardBrowserController.SetActive(false);
	}

	private void HandleKeyPress(string key) {
		webBrowser.HandleKeyboardInput(key);
	}
}