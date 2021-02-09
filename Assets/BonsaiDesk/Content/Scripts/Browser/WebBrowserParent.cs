using UnityEngine;

public class WebBrowserParent : MonoBehaviour {
	public TableBrowser _web;
	public TableBrowser _keyboard;
	public KeyboardBrowserController _KeyboardBrowserController;

	// Start is called before the first frame update
	private void Start() {
		_web.BrowserReady        += SetupWeb;
		_keyboard.ListenersReady += SetupKeyboard;
	}

	// Update is called once per frame
	private void Update() { }

	private void SetupKeyboard() {
		Debug.Log("[BONSAI] Browser Keyboard Ready");
		_keyboard.KeyPress                    += HandleKeyPress;
		_KeyboardBrowserController.NavBack    += () => { _web.GoBack(); };
		_KeyboardBrowserController.NavForward += () => { _web.GoForward(); };
		_keyboard.PostMessage(Browser.BrowserMessage.NavKeyboard);
		_keyboard.SetHidden(false);
	}

	private void SetupWeb() {
		Debug.Log("[BONSAI] Web Ready");
		_web.SetHidden(false);
	}

	private void HandleKeyPress(string key) {
		_web.HandleKeyboardInput(key);
	}
}