using System;
using Newtonsoft.Json;
using UnityEngine;
using Vuplex.WebView;

[RequireComponent(typeof(TableBrowser))]
public class KeyboardBrowserController : MonoBehaviour {
	public Transform screen;
	public Transform altTransform;
	private bool _alt;
	private TableBrowser _browser;

	// Start is called before the first frame update
	private void Start() {
		_browser = GetComponent<TableBrowser>();
		_browser.BrowserReady += () =>
		{
			_browser.OnMessageEmitted(HandleJavascriptMessage);
			SetAlt(true);
		};
	}

	// Update is called once per frame
	private void Update() { }

	public void SetAlt(bool alt) {
		_alt = alt;
		if (_alt) {
			screen.localPosition    = altTransform.localPosition;
			screen.localEulerAngles = altTransform.localEulerAngles;
			_browser.ChangeRes(new Vector2(0.1f, 0.15f));
		}
		else {
			screen.localPosition    = Vector3.zero;
			screen.localEulerAngles = Vector3.zero;
			_browser.ChangeRes(_browser.Bounds);
		}
	}

	public void ToggleAlt() {
		SetAlt(!_alt);
	}

	public event Action NavBack;
	public event Action NavForward;
	public event Action CloseWeb;
	public event Action SpawnKeyboard;
	public event Action DismissKeyboard;

	private void HandleJavascriptMessage(object _, EventArgs<string> eventArgs) {
		var message = JsonConvert.DeserializeObject<Browser.JsMessageString>(eventArgs.Value);
		if (message.Type == "command") {
			switch (message.Message) {
				case "closeWeb":
					CloseWeb?.Invoke();
					break;
				case "navBack":
					NavBack?.Invoke();
					break;
				case "navForward":
					NavForward?.Invoke();
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