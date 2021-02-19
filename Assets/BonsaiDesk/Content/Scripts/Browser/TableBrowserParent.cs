using System;
using UnityEngine;

public class TableBrowserParent : MonoBehaviour {
	public TableBrowser TableBrowser;
	public TableBrowserMenu TableBrowserMenu;
	public WebBrowserParent WebBrowserParent;
	private Vector3 _startTransform;
	private bool sleeped;

	// Start is called before the first frame update
	private void Start() {
		TableBrowserMenu.BrowseSite += HandleBrowseSite;
		WebBrowserParent.CloseWeb   += HandleCloseWeb;
		TableBrowser.BrowserReady += () =>
		{
			Sleep();
		};
		_startTransform = TableBrowserMenu.transform.localPosition;
	}

	// Update is called once per frame
	private void Update() { }

	private void HandleCloseWeb(object _, EventArgs e) {
		SetActive(Browser.Table);
	}

	private void HandleBrowseSite(object _, string url) {
		WebBrowserParent.LoadUrl(url);
		SetActive(Browser.Web);
	}

	private void SetActive(Browser browser) {
		switch (browser) {
			case Browser.Web:
				SetAlt(true);
				WebBrowserParent.SetActive(true);
				break;
			case Browser.Table:
				SetAlt(false);
				WebBrowserParent.SetActive(false);
				break;
			default:
				Debug.LogWarning($"[BONSAI] set browser {browser} active not handled");
				break;
		}
	}

	public void Sleep() {
		Debug.Log("sleep");
		sleeped = true;
		SetAlt(true);
		WebBrowserParent.SetActive(false);
	}

	public void Wake() {
		sleeped = false;
		SetAlt(false);
	}

	public void ToggleAwake() {
		if (sleeped) {
			Wake();
		}
		else {
			Sleep();
		}
	}

	public void SetAlt(bool alt) {
		if (alt) {
			TableBrowser.SetHidden(true);
			TableBrowserMenu.transform.localPosition = new Vector3(0f, -10f, 0f);
		}
		else {
			TableBrowser.SetHidden(false);
			TableBrowserMenu.transform.localPosition = _startTransform;
		}
	}

	private enum Browser {
		Web,
		Table
	}
}