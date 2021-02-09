using System;
using Newtonsoft.Json;
using OVR;
using UnityEngine;
using Vuplex.WebView;

public class TableBrowser : Browser {

	public SoundFXRef hoverSound;
	public SoundFXRef mouseDownSound;
	public SoundFXRef mouseUpSound;

	protected override void Start() {
		base.Start();

		WebViewPrefab.DragMode = DragMode.DragToScroll;

		BrowserReady += () =>
		{
			var view = WebViewPrefab.transform.Find("WebViewPrefabResizer/WebViewPrefabView");
			CustomInputModule.Singleton.screens.Add(view);
			OnMessageEmitted(HandleJavascriptMessage);
		};
		ListenersReady += () =>
		{
			Debug.Log("[BONSAI] TableBrowser listeners ready");
		};
	}

	public event Action<string> KeyPress;

	private void HandleJavascriptMessage(object _, EventArgs<string> eventArgs) {
		var message = JsonConvert.DeserializeObject<JsMessageString>(eventArgs.Value);

		switch (message.Type) {
			case "event":
				switch (message.Message) {
					case "hover":
						hoverSound.PlaySoundAt(CustomInputModule.Singleton.cursorRoot);
						break;
					case "mouseDown":
						mouseDownSound.PlaySoundAt(CustomInputModule.Singleton.cursorRoot);
						break;
					case "mouseUp":
						mouseUpSound.PlaySoundAt(CustomInputModule.Singleton.cursorRoot);
						break;
					case "keyPress":
						KeyPress?.Invoke(message.Data);
						break;
				}

				break;
		}
	}

	public Vector2Int ChangeAspect(Vector2 newAspect) {
		var aspectRatio = newAspect.x / newAspect.y;
		var localScale  = new Vector3(Bounds.y * aspectRatio, Bounds.y, 1);
		if (localScale.x > Bounds.x) {
			localScale = new Vector3(Bounds.x, Bounds.x * (1f / aspectRatio), 1);
		}

		var resolution = AutoResolution(Bounds, distanceEstimate, pixelPerDegree, newAspect);

		var res       = resolution.x > resolution.y ? resolution.x : resolution.y;
		var scale     = Bounds.x > Bounds.y ? Bounds.x : Bounds.y;
		var resScaled = res / scale;

		WebViewPrefab.WebView.SetResolution(resScaled);
		WebViewPrefab.Resize(Bounds.x, Bounds.y);

		Debug.Log($"[BONSAI] ChangeAspect resolution {resolution}");

		boundsTransform.localScale = localScale;

	#if UNITY_ANDROID && !UNITY_EDITOR
        RebuildOverlay(resolution);
	#endif

		return resolution;
	}

	protected override void SetupWebViewPrefab() {
		WebViewPrefab = WebViewPrefabCustom.Instantiate(Bounds.x, Bounds.y);
		Destroy(WebViewPrefab.Collider);

		WebViewPrefab.transform.localPosition = Vector3.zero;

		WebViewPrefab.transform.SetParent(screenTransform, false);

		Resizer     = WebViewPrefab.transform.Find("WebViewPrefabResizer");
		WebViewView = Resizer.transform.Find("WebViewPrefabView");

		holePuncherTransform.SetParent(WebViewView, false);
		overlayTransform.SetParent(WebViewView, false);

	#if UNITY_ANDROID && !UNITY_EDITOR
        WebViewView.GetComponent<MeshRenderer>().enabled = false;
	#endif

		WebViewPrefab.Initialized += (sender, eventArgs) =>
		{
			const int ppuu = 2000;
			WebViewPrefab.WebView.SetResolution(ppuu);
			var res = new Vector2Int((int) (ppuu * Bounds.x), (int) (ppuu * Bounds.y));
			RebuildOverlay(res);
		};
		base.SetupWebViewPrefab();
	}
}