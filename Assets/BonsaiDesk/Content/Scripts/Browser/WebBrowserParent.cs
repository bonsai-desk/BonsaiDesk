using UnityEngine;

public class WebBrowserParent : MonoBehaviour {
	public TableBrowser _web;
	public TableBrowser _keyboard;
	public KeyboardBrowserController _KeyboardBrowserController;
	public WebBrowserController _WebBrowserController;

	// Start is called before the first frame update
	private void Start() {
		_web.BrowserReady        += SetupWeb;
		_keyboard.ListenersReady += SetupKeyboard;
	}

	// Update is called once per frame
	private void Update() { }

	private void SetupKeyboard() {
		Debug.Log("[BONSAI] Browser Keyboard Ready");
		_keyboard.KeyPress                         += HandleKeyPress;
		_KeyboardBrowserController.NavBack         += HandleGoBack;
		_KeyboardBrowserController.NavForward      += HandleGoForward;
		_KeyboardBrowserController.SpawnKeyboard   += HandleSpawnKeyboard;
		_KeyboardBrowserController.DismissKeyboard += HandleDismissKeyboard;
		_keyboard.PostMessage(Browser.BrowserMessage.NavKeyboard);
		_keyboard.SetHidden(false);
	}

	public void HandleGoBack() {
		_web.GoBack();
		
	}

	public void HandleGoForward() {
		_web.GoForward();
	}

	public void HandleSpawnKeyboard() {
		_WebBrowserController.SetRaised(true);
	}

	public void HandleDismissKeyboard() {
		_WebBrowserController.SetRaised(false);
	}

	private void SetupWeb() {
		Debug.Log("[BONSAI] Web Ready");
		_web.SetHidden(false);
	}

	private void HandleKeyPress(string key) {
		_web.HandleKeyboardInput(key);
	}
}