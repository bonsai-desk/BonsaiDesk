using System;
using Newtonsoft.Json;
using UnityEngine;
using Vuplex.WebView;

[RequireComponent(typeof(TableBrowser))]
public class KeyboardBrowserController : MonoBehaviour
{
    public Transform screen;
    public Transform altTransform;
    private bool _alt;
    private TableBrowser _browser;

    // Start is called before the first frame update
    private void Start()
    {
        _browser = GetComponent<TableBrowser>();
        _browser.ListenersReady += SetupBrowser;
    }

    // Update is called once per frame
    private void Update() { }

    public event Action DismissKeyboard;

    private void SetupBrowser()
    {
        _browser.PostMessage(Browser.BrowserMessage.NavKeyboard);
        SetActive(false);
        _browser.SetHidden(true);
        _browser.OnMessageEmitted(HandleJavascriptMessage);
    }

    private void HandleJavascriptMessage(object sender, EventArgs<string> eventArgs)
    {
        var message = JsonConvert.DeserializeObject<Browser.JsMessageString>(eventArgs.Value);
        if (message.Type == "command")
        {
            switch (message.Message)
            {
                case "dismissKeyboard":
                    DismissKeyboard?.Invoke();
                    break;
            }
        }
    }

    public void SetActive(bool active)
    {
        _browser.SetHidden(!active);
        SetAlt(!active);
    }

    public void SetAlt(bool alt)
    {
        _alt = alt;
        if (_alt)
        {
            screen.localPosition = altTransform.localPosition;
            screen.localEulerAngles = altTransform.localEulerAngles;
        }
        else
        {
            screen.localPosition = Vector3.zero;
            screen.localEulerAngles = Vector3.zero;
        }
    }
}