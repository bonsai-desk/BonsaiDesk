using System;
using UnityEngine;

public class TableBrowserParent : MonoBehaviour {
	public TableBrowser TableBrowser;
	public TableBrowserMenu TableBrowserMenu;
	public WebBrowserParent WebBrowserParent;
	public bool sleeped { get; private set; }

	// Start is called before the first frame update
	private void Start() {
		MoveToDesk.OrientationChanged += HandleOrientationChange;
		TableBrowserMenu.BrowseSite   += HandleBrowseSite;
		WebBrowserParent.CloseWeb     += HandleCloseWeb;
		TableBrowser.BrowserReady     += Sleep;
	}

	// Update is called once per frame
	private void Update() { }

	private void HandleOrientationChange(bool oriented) {
		Sleep();
	}

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
				TableBrowser.SetHidden(true);
				WebBrowserParent.SetAllHidden(false);
				break;
			case Browser.Table:
				TableBrowser.SetHidden(false);
				WebBrowserParent.SetAllHidden(true);
				break;
			default:
				Debug.LogWarning($"[BONSAI] set browser {browser} active not handled");
				break;
		}
	}

	public void Sleep() {
		Debug.Log("[BONSAI] Sleep all table browsers");
		sleeped = true;
		TableBrowser.SetHidden(true);
		WebBrowserParent.SetAllHidden(true);

		InputManager.Hands.Left.ZTestRegular();
		InputManager.Hands.Right.ZTestRegular();
		InputManager.Hands.Left.SetPhysicsLayerRegular();
		InputManager.Hands.Right.SetPhysicsLayerRegular();
	}

	public void Wake() {
		sleeped = false;
		TableBrowser.SetHidden(false);

		InputManager.Hands.Left.ZTestOverlay();
		InputManager.Hands.Right.ZTestOverlay();
		InputManager.Hands.Left.SetPhysicsLayerForTouchScreen();
		InputManager.Hands.Right.SetPhysicsLayerForTouchScreen();
	}

	public void ToggleAwake() {
		if (sleeped) {
			Wake();
		}
		else {
			Sleep();
		}
	}

	private enum Browser {
		Web,
		Table
	}
}