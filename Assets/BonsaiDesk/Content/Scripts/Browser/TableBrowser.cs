using System;
using Newtonsoft.Json;
using UnityEngine;
using Vuplex.WebView;

public class TableBrowser : NewBrowser
{
    public string initialUrl;
    public CustomInputModule customInputModule;

    public OVR.SoundFXRef hoverSound;
    public OVR.SoundFXRef mouseDownSound;
    public OVR.SoundFXRef mouseUpSound;

    protected override void Start()
    {
        base.Start();
        _webViewPrefab.DragMode = DragMode.DragToScroll;
        _webViewPrefab.InitialUrl = initialUrl;
        BrowserReady += () =>
        {
            OnMessageEmitted(HandleJavascriptMessage);
            _webViewPrefab.WebView.LoadProgressChanged += NavToMenu;
            var view = _webViewPrefab.transform.Find("WebViewPrefabResizer/WebViewPrefabView");
            CustomInputModule.Singleton.screens.Add(view);
        };
    }

    private void NavToMenu(object sender, ProgressChangedEventArgs eventArgs)
    {
        if (eventArgs.Type == ProgressChangeType.Finished) PostMessage(BrowserMessage.NavToMenu);
    }

    public override Vector2Int ChangeAspect(Vector2 newAspect)
    {
        var aspectRatio = newAspect.x / newAspect.y;
        var localScale = new Vector3(_bounds.y * aspectRatio, _bounds.y, 1);
        if (localScale.x > _bounds.x) localScale = new Vector3(_bounds.x, _bounds.x * (1f / aspectRatio), 1);

        var resolution = AutoResolution(_bounds, distanceEstimate, pixelPerDegree, newAspect);

        var res = resolution.x > resolution.y ? resolution.x : resolution.y;
        var scale = _bounds.x > _bounds.y ? _bounds.x : _bounds.y;
        var resScaled = res / scale;

        _webViewPrefab.WebView.SetResolution(resScaled);
        _webViewPrefab.Resize(_bounds.x, _bounds.y);

        Debug.Log($"[BONSAI] ChangeAspect resolution {resolution}");

        boundsTransform.localScale = localScale;

#if UNITY_ANDROID && !UNITY_EDITOR
        RebuildOverlay(resolution);
#endif

        return resolution;
    }

    private void HandleJavascriptMessage(object _, EventArgs<string> eventArgs)
    {
        var message = JsonConvert.DeserializeObject<JSMessage>(eventArgs.Value);
        
        Debug.Log($"[BONSAI] JS Message: {message.Type} {message.Message}" );

        switch (message.Type)
        {
            case "command":
                throw new NotImplementedException();
            case "event":
                switch (message.Message)
                {
                    case "hover":
                        hoverSound.PlaySoundAt(customInputModule.cursorRoot);
                        break;
                    case "mouseDown":
                        mouseDownSound.PlaySoundAt(customInputModule.cursorRoot);
                        break;
                    case "mouseUp":
                        mouseUpSound.PlaySoundAt(customInputModule.cursorRoot);
                        break;
                }

                break;
        }
    }

    private class JSMessage
    {
        public string Type;
        public string Message;
    }
}