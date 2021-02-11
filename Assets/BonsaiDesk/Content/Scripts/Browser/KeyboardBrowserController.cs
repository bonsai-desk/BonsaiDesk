using UnityEngine;

[RequireComponent(typeof(TableBrowser))]
public class KeyboardBrowserController : MonoBehaviour {
	public Transform screen;
	private TableBrowser _browser;

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
	}
}