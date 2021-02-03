using UnityEngine;

public class WebBrowserParent : MonoBehaviour {
	public TableBrowser _web;
	public TableBrowser _keyboard;

	// Start is called before the first frame update
	private void Start() {
		_web.ListenersReady      += SetupWeb;
		_keyboard.ListenersReady += SetupKeyboard;
	}

	// Update is called once per frame
	private void Update() { }

	private void SetupKeyboard() {
		_keyboard.PostMessage(Browser.BrowserMessage.NavKeyboard);
		_keyboard.ToggleHidden();
	}

	private void SetupWeb() {
		_web.PostMessage(Browser.BrowserMessage.NavHome);
		_web.ToggleHidden();
	}
}