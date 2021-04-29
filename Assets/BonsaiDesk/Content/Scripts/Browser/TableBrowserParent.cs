using System;
using UnityEngine;
using Vuplex.WebView;

public class TableBrowserParent : MonoBehaviour
{
    private bool openedOnce;
    private Browser preSleepActive = Browser.Table;
    public TableBrowser TableBrowser;
    public TableBrowserMenu TableBrowserMenu;
    public WebBrowserParent WebBrowserParent;
    public bool sleeped { get; private set; }
    private int parentsReady;

    // Start is called before the first frame update
    private void Start()
    {
        MoveToDesk.OrientationChanged += HandleOrientationChange;
        TableBrowserMenu.BrowseYouTube += HandleBrowseYouTube;
        WebBrowserParent.CloseWeb += HandleCloseWeb;

        WebBrowserParent.BrowsersReady += HandleParentReady;
        TableBrowser.BrowserReady += HandleParentReady;
        TableBrowserMenu.Singleton.CloseMenu += HandleCloseMenu;
    }

    private void HandleCloseMenu(object sender, EventArgs e)
    {
        Sleep();
    }

    private void HandleParentReady(object sender, EventArgs e)
    {
        parentsReady += 1;
        if (parentsReady == 2)
        {
            Sleep();
            WebBrowserParent.LoadUrl("https://m.youtube.com");
        }
    }

    // Update is called once per frame
    private void Update()
    {
    }

    private void HandleOrientationChange(bool oriented)
    {
        Sleep();
    }

    private void HandleCloseWeb(object _, EventArgs e)
    {
        SetActive(Browser.Table);
    }

    private void HandleBrowseYouTube(object _, EventArgs e)
    {
        SetActive(Browser.Web);
    }

    private void SetActive(Browser browser)
    {
        preSleepActive = browser;
        switch (browser)
        {
            case Browser.Web:
                TableBrowser.SetHidden(true);
                WebBrowserParent.SetAllHidden(false);
                break;
            case Browser.Table:
                TableBrowser.SetHidden(false);
                WebBrowserParent.SetAllHidden(true);
                break;
            default:
                BonsaiLogWarning($"Set browser {browser} active not handled");
                break;
        }
    }

    public void Sleep()
    {
        BonsaiLog("Sleep");
        
        sleeped = true;
        TableBrowser.SetHidden(true);
        WebBrowserParent.SetAllHidden(true);

        InputManager.Hands.Left.ZTestRegular();
        InputManager.Hands.Right.ZTestRegular();
        InputManager.Hands.Left.SetPhysicsLayerRegular();
        InputManager.Hands.Right.SetPhysicsLayerRegular();
    }

    public void Wake()
    {
        sleeped = false;

        if (!openedOnce)
        {
            openedOnce = true;
            TableBrowser.SetHidden(false);
        }
        else
        {
            switch (preSleepActive)
            {
                case Browser.Web:
                    WebBrowserParent.SetAllHidden(false);
                    break;
                case Browser.Table:
                    TableBrowser.SetHidden(false);
                    break;
                default:
                    TableBrowser.SetHidden(false);
                    break;
            }
        }
        

        InputManager.Hands.Left.ZTestOverlay();
        InputManager.Hands.Right.ZTestOverlay();
        InputManager.Hands.Left.SetPhysicsLayerForTouchScreen();
        InputManager.Hands.Right.SetPhysicsLayerForTouchScreen();
    }

    public void ToggleAwake()
    {
        if (sleeped)
        {
            Wake();
        }
        else
        {
            Sleep();
        }
    }

    private enum Browser
    {
        Web,
        Table
    }

    private void BonsaiLog(string msg)
    {
        Debug.Log("<color=orange>BonsaiTableBrowserParent: </color>: " + msg);
    }

    private void BonsaiLogWarning(string msg)
    {
        Debug.LogWarning("<color=orange>BonsaiTableBrowserParent: </color>: " + msg);
    }

    private void BonsaiLogError(string msg)
    {
        Debug.LogError("<color=orange>BonsaiTableBrowserParent: </color>: " + msg);
    }
}