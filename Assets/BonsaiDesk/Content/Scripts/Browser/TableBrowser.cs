using System;
using Newtonsoft.Json;
using OVR;
using UnityEngine;
using Vuplex.WebView;

public class TableBrowser : Browser
{
    public SoundFXRef hoverSound;
    public SoundFXRef mouseDownSound;
    public SoundFXRef mouseUpSound;

    public bool disableVideo;
    public bool clickWithoutStealingFocus;
    public bool hoveringDisabled;

    protected override void Start()
    {
        base.Start();

        WebViewPrefab.DragMode = DragMode.DragToScroll;

        SetMaterialOnTop();

        BrowserReady += () =>
        {
            var view = WebViewPrefab.transform.Find("WebViewPrefabResizer/WebViewPrefabView").gameObject.GetComponent<MeshRenderer>();
            CustomInputModule.Singleton.screens.Add(view);
            CustomInputModule.Singleton.Click += HandleClickSound;
            OnMessageEmitted(HandleJavascriptMessage);
        };
        ListenersReady += () => { Debug.Log("[BONSAI] TableBrowser listeners ready"); };
    }

    private void HandleClickSound(object sender, EventArgs e)
    {
        mouseDownSound.PlaySoundAt(CustomInputModule.Singleton.cursorRoot);
    }

    public event EventHandler<EventArgs<string>> InputRecieved;

    private void HandleJavascriptMessage(object _, EventArgs<string> eventArgs)
    {
        var message = JsonConvert.DeserializeObject<JsMessageString>(eventArgs.Value);

        switch (message.Type)
        {
            case "event":
                switch (message.Message)
                {
                    case "hover":
                        hoverSound.PlaySoundAt(CustomInputModule.Singleton.cursorRoot);
                        break;
                    case "mouseDown":
                        //mouseDownSound.PlaySoundAt(CustomInputModule.Singleton.cursorRoot);
                        break;
                    case "mouseUp":
                        mouseUpSound.PlaySoundAt(CustomInputModule.Singleton.cursorRoot);
                        break;
                    case "keyPress":
                        if (InputRecieved != null)
                        {
                            InputRecieved(this, new EventArgs<string>(message.Data));
                        }

                        break;
                }

                break;
        }
    }

    public Vector2Int ChangeAspect(Vector2 newAspect)
    {
        var aspectRatio = newAspect.x / newAspect.y;
        var localScale = new Vector3(Bounds.y * aspectRatio, Bounds.y, 1);
        if (localScale.x > Bounds.x)
        {
            localScale = new Vector3(Bounds.x, Bounds.x * (1f / aspectRatio), 1);
        }

        var resolution = AutoResolution(Bounds, distanceEstimate, pixelPerDegree, newAspect);

        var res = resolution.x > resolution.y ? resolution.x : resolution.y;
        var scale = Bounds.x > Bounds.y ? Bounds.x : Bounds.y;
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

    protected override void SetupWebViewPrefab()
    {
        WebViewPrefab = WebViewPrefabCustom.Instantiate(Bounds.x, Bounds.y, new WebViewOptions
            {
                clickWithoutStealingFocus = clickWithoutStealingFocus,
                disableVideo = disableVideo
            }
        );

        Destroy(WebViewPrefab.Collider);

        WebViewPrefab.transform.localPosition = Vector3.zero;

        WebViewPrefab.transform.SetParent(screenTransform, false);

        Resizer = WebViewPrefab.transform.Find("WebViewPrefabResizer");
        WebViewView = Resizer.transform.Find("WebViewPrefabView");

        holePuncherTransform.SetParent(WebViewView, false);
        overlayTransform.SetParent(WebViewView, false);

#if UNITY_ANDROID && !UNITY_EDITOR
        WebViewView.GetComponent<MeshRenderer>().enabled = false;
#endif

        WebViewPrefab.Initialized += (sender, eventArgs) =>
        {
            WebViewPrefab.HoveringEnabled = !hoveringDisabled;
            ChangeRes(Bounds);
            //todo ChangeRes(Bounds);
            //ChangeSize(0.3f, 0.3f);
        };
        base.SetupWebViewPrefab();
    }

    public void ChangeRes(Vector2 bounds, int ppu = 2000)
    {
        WebViewPrefab.WebView.SetResolution(ppu);
        WebViewPrefab.Resize(bounds.x, bounds.y);
        var res = new Vector2Int((int) (ppu * bounds.x), (int) (ppu * bounds.y));
#if UNITY_ANDROID && !UNITY_EDITOR
		RebuildOverlay(res);
#endif
    }
}