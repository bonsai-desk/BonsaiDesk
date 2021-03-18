using System;
using Newtonsoft.Json;
using UnityEngine;
using Vuplex.WebView;

[RequireComponent(typeof(TableBrowser))]
public class WebNavBrowserController : MonoBehaviour {
	private TableBrowser _browser;

	// Start is called before the first frame update
	private void Start() {
		_browser                =  GetComponent<TableBrowser>();
		_browser.ListenersReady += SetupListeners;
		_browser.BrowserReady   += SetupBrowser;
	}

	private void SetupBrowser() {
		_browser.SetHidden(true);
	}

	// Update is called once per frame
	private void Update() { }

	private void SetupListeners() {
		_browser.PostMessage(Browser.BrowserMessage.NavWebNav);
		_browser.OnMessageEmitted(HandleJavascriptMessage);
	}

	public event Action GoBack;
	public event Action GoForward;
	public event Action CloseWeb;
	public event Action SpawnKeyboard;
	public event Action DismissKeyboard;

	private void HandleJavascriptMessage(object _, EventArgs<string> eventArgs) {
		var message = JsonConvert.DeserializeObject<Browser.JsMessageString>(eventArgs.Value);
		Debug.Log(eventArgs.Value);
		if (message.Type == "command") {
			switch (message.Message) {
				case "closeWeb":
					CloseWeb?.Invoke();
					break;
				case "navBack":
					GoBack?.Invoke();
					break;
				case "navForward":
					GoForward?.Invoke();
					break;
				case "spawnKeyboard":
					SpawnKeyboard?.Invoke();
					break;
				case "dismissKeyboard":
					DismissKeyboard?.Invoke();
					break;
			}
		}
	}
}