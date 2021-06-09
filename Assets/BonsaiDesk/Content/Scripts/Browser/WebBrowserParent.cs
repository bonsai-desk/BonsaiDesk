using System;
using Mirror;
using UnityEngine;
using Vuplex.WebView;

public class WebBrowserParent : MonoBehaviour
{
    public TableBrowser webBrowser;
    public TableBrowser keyboardBrowser;
    public TableBrowser webNavBrowser;
    public KeyboardBrowserController keyboardBrowserController;
    public WebBrowserController webBrowserController;
    public WebNavBrowserController webNavBrowserController;
    public Transform videoSpawnLocation;
    public TableBrowserParent tableBrowserParent;
    private int _browsersReady = 0;
    public Transform headTransform;
    private bool keyboardActive = false;

    // Start is called before the first frame update
    private void Start()
    {
        keyboardBrowser.ListenersReady += SetupKeyboardBrowser;
        webNavBrowser.BrowserReady += HandleWebNavBrowserReady;

        webBrowser.BrowserReady += HandleBrowserReady;
        webNavBrowser.BrowserReady += HandleBrowserReady;
        keyboardBrowser.BrowserReady += HandleBrowserReady;
    }

    private void HandleBrowserReady(object sender, EventArgs eventArgs)
    {
        _browsersReady += 1;
        if (_browsersReady == 3)
        {
            BrowsersReady?.Invoke(this, new EventArgs());
        }
    }

    // Update is called once per frame
    private void Update()
    {
    }

    public event EventHandler CloseWeb;
    public event EventHandler BrowsersReady;

    public void SetAllHidden(bool hidden)
    {
        webBrowser.SetHidden(hidden);
        keyboardBrowser.SetHidden(hidden);
        webNavBrowser.SetHidden(hidden);
    }

    private void HandleWebNavBrowserReady(object sender, EventArgs eventArgs)
    {
        BonsaiLog("SetupWebWebNavBrowser");
        webNavBrowserController.GoBack += HandleGoBack;
        webNavBrowserController.GoForward += HandleGoForward;
        
        webNavBrowserController.SpawnKeyboard += HandleSpawnKeyboard;
        webNavBrowserController.DismissKeyboard += HandleDismissKeyboard;
        webNavBrowserController.ToggleKeyboard += HandleToggleKeyboard;
        
        webNavBrowserController.CloseWeb += HandleCloseWeb;
        webBrowserController.SpawnYT += HandleSpawnYt;
        webBrowserController.InputFocus += HandleInputFocus;
    }


    private void HandleInputFocus(object sender, EventArgs<bool> e)
    {
        if (e.Value)
        {
            HandleSpawnKeyboard();
        }
        else
        {
            HandleDismissKeyboard();
        }
    }

    private void HandleSpawnYt(object sender, EventArgs<string> e)
    {
        BonsaiLog($"Spawn YouTube ({e.Value})");
        YouTubeSpawner.Singleton.CmdSpawnYT(videoSpawnLocation.position, headTransform.position, e.Value);
        tableBrowserParent.MenuSleep();
    }

    private void SetupKeyboardBrowser()
    {
        //BonsaiLog("SetupKeyboardBrowser");
        //keyboardBrowser.InputRecieved += (sender, e) => webBrowser.HandleKeyboardInput(e.Value);
    }

    private void HandleGoBack()
    {
        webBrowser.GoBack();
    }

    private void HandleGoForward()
    {
        webBrowser.GoForward();
    }

    private void HandleCloseWeb()
    {
        if (CloseWeb != null)
        {
            CloseWeb(this, new EventArgs());
        }
    }

    private void HandleSpawnKeyboard()
    {
        webBrowserController.SetRaised(true);
        keyboardBrowserController.SetActive(true);
    }

    public void HandleDismissKeyboard()
    {
        webBrowserController.SetRaised(false);
        keyboardBrowserController.SetActive(false);
    }
    
    public void HandleToggleKeyboard()
    {
        keyboardActive = !keyboardActive;
        if (keyboardActive)
        {
            HandleSpawnKeyboard();
        }
        else
        {
            HandleDismissKeyboard();
        }
    }

    public void LoadUrl(string url)
    {
        webBrowser.LoadUrl(url);
    }

    public void DummySpawn()
    {
        YouTubeSpawner.Singleton.CmdSpawnYT(videoSpawnLocation.position, headTransform.position, "niS_Fpy_2-U");
    }

    private void BonsaiLog(string msg)
    {
        Debug.Log("<color=orange>BonsaiWebBrowserParent: </color>: " + msg);
    }

    private void BonsaiLogWarning(string msg)
    {
        Debug.LogWarning("<color=orange>BonsaiWebBrowserParent: </color>: " + msg);
    }

    private void BonsaiLogError(string msg)
    {
        Debug.LogError("<color=orange>BonsaiWebBrowserParent: </color>: " + msg);
    }
}