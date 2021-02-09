using System;
using Newtonsoft.Json;
using UnityEngine;
using Vuplex.WebView;

public class KeyboardBrowserController : MonoBehaviour {
	private TableBrowser _browser;

	// Start is called before the first frame update
	private void Start() {
		_browser              =  GetComponent<TableBrowser>();
		_browser.BrowserReady += () => { _browser.OnMessageEmitted(HandleJavascriptMessage); };
	}

	// Update is called once per frame
	private void Update() { }

	public event Action NavBack;
	public event Action NavForward;

	private void HandleJavascriptMessage(object _, EventArgs<string> eventArgs) {
		var message = JsonConvert.DeserializeObject<Browser.JsMessageString>(eventArgs.Value);
		if (message.Type == "command") {
			switch (message.Message) {
				case "navBack":
					NavBack?.Invoke();
					break;
				case "navForward":
					NavForward?.Invoke();
					break;
			}
		}
	}
}