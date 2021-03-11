using UnityEngine;

[RequireComponent(typeof(TableBrowser))]
public class KeyboardBrowserController : MonoBehaviour {
	public Transform screen;
	private TableBrowser _browser;
	public Transform altTransform;
	private bool _alt;

	// Start is called before the first frame update
	private void Start() {
		_browser              =  GetComponent<TableBrowser>();
		_browser.ListenersReady += SetupBrowser;
	}

	// Update is called once per frame
	private void Update() { }

	private void SetupBrowser() {
		_browser.PostMessage(Browser.BrowserMessage.NavKeyboard);
		SetActive(false);
	}

	public void SetActive(bool active) {
		_browser.SetHidden(!active);
		SetAlt(!active);
	}

	public void SetAlt(bool alt) {
		_alt = alt;
		if (_alt) {
			screen.localPosition    = altTransform.localPosition;
			screen.localEulerAngles = altTransform.localEulerAngles;
		}
		else {
			screen.localPosition    = Vector3.zero;
			screen.localEulerAngles = Vector3.zero;
		}
	}
}