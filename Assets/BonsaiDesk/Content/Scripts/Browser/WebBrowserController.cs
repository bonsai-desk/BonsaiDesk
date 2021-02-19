using System;
using Newtonsoft.Json;
using UnityEngine;
using Vuplex.WebView;

[RequireComponent(typeof(TableBrowser))]
public class WebBrowserController : MonoBehaviour {
	public Transform screen;
	public Transform raisedTransform;
	private TableBrowser _browser;
	private bool _raised;

	// Start is called before the first frame update
	private void Start() {
		_browser              =  GetComponent<TableBrowser>();
		_browser.BrowserReady += SetupBrowser;
	}

	// Update is called once per frame
	private void Update() { }

	private void SetupBrowser() {
		_browser.SetHidden(false);
		_browser.OnMessageEmitted(HandleJavascriptMessage);
		SetRaised(false);
	}

	public void SetRaised(bool raised) {
		_raised = raised;
		if (_raised) {
			screen.localPosition    = raisedTransform.localPosition;
			screen.localEulerAngles = raisedTransform.localEulerAngles;
		}
		else {
			screen.localPosition    = Vector3.zero;
			screen.localEulerAngles = Vector3.zero;
		}
	}

	public void ToggleRaised() {
		SetRaised(!_raised);
	}

	public event EventHandler<EventArgs<string>> SpawnYT;

	private void HandleJavascriptMessage(object _, EventArgs<string> eventArgs) {
		var message = JsonConvert.DeserializeObject<Browser.JsMessageString>(eventArgs.Value);
		Debug.Log(eventArgs.Value);
		if (message.Type == "command") {
			switch (message.Message) {
				case "spawnYT":
					SpawnYT?.Invoke(this, new EventArgs<string>(message.Data));
					break;
			}
		}
	}
}