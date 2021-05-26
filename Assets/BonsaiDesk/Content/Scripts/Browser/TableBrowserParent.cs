using System;
using UnityEngine;
using Vuplex.WebView;

public class TableBrowserParent : MonoBehaviour
{
    private bool openedOnce;
    private Browser preSleepActive = Browser.Table;
    public TableBrowser TableBrowser;
    public TableBrowser ContextMenu;
    public TableBrowserMenu TableBrowserMenu;
    public WebBrowserParent WebBrowserParent;
    public bool MenuAsleep { get; private set; }
    public bool ContextAsleep { get; private set; }
    
    private int _parentsReady;
    public BoxCollider contentBoxCollider;

    // Start is called before the first frame update
    private void Start()
    {
        MoveToDesk.OrientationChanged += HandleOrientationChange;
        TableBrowserMenu.BrowseYouTube += HandleBrowseYouTube;
        WebBrowserParent.CloseWeb += HandleCloseWeb;

        WebBrowserParent.BrowsersReady += HandleParentReady;
        TableBrowser.BrowserReady += HandleParentReady;
        TableBrowserMenu.Singleton.CloseMenu += HandleCloseMenu;

        ContextMenu.BrowserReady += HandleContextReady;
    }

    private void HandleContextReady(object sender, EventArgs e)
    {
        ContextSleep();
    }

    private void HandleCloseMenu(object sender, EventArgs e)
    {
        MenuSleep();
    }

    private void HandleParentReady(object sender, EventArgs e)
    {
        _parentsReady += 1;
        if (_parentsReady == 2)
        {
            MenuSleep();
            WebBrowserParent.LoadUrl("https://m.youtube.com");
        }
    }

    // Update is called once per frame
    private void Update()
    {
    }

    private void HandleOrientationChange(bool oriented)
    {
        MenuSleep();
        ContextSleep();
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

    public void MenuSleep()
    {
        MenuAsleep = true;
        
        TableBrowser.SetHidden(true);
        WebBrowserParent.SetAllHidden(true);

        SetHandForInactiveBrowser();
    }

    private void MenuWake()
    {
        MenuAsleep = false;

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
        
        SetHandsForActiveBrowser();

    }

    private void SetHandsForActiveBrowser()
    {
        InputManager.Hands.Left.ZTestOverlay();
        InputManager.Hands.Right.ZTestOverlay();
        InputManager.Hands.Left.SetPhysicsLayerForTouchScreen();
        InputManager.Hands.Right.SetPhysicsLayerForTouchScreen();
        InputManager.Hands.Left.SetPhysicsForUsingScreen(true);
        InputManager.Hands.Right.SetPhysicsForUsingScreen(true);
    }

    private void SetHandForInactiveBrowser()
    {
        if (MenuAsleep && ContextAsleep)
        {
            InputManager.Hands.Left.ZTestRegular();
            InputManager.Hands.Right.ZTestRegular();
            InputManager.Hands.Left.SetPhysicsLayerRegular();
            InputManager.Hands.Right.SetPhysicsLayerRegular();
            InputManager.Hands.Left.SetPhysicsForUsingScreen(false);
            InputManager.Hands.Right.SetPhysicsForUsingScreen(false);
        }
    }

    private void ContextWake()
    {
        ContextAsleep = false;
        contentBoxCollider.enabled = true;
        ContextMenu.SetHidden(false);
        SetHandsForActiveBrowser();
        
    }

    private void ContextSleep()
    {
        ContextAsleep = true;
        contentBoxCollider.enabled = false;
        ContextMenu.SetHidden(true);
        SetHandForInactiveBrowser();
    }

    public void ToggleContextAwake()
    {
        if (ContextAsleep)
        {
            ContextWake();
        }
        else
        {
            ContextSleep();
        }
    }

    public void ToggleAwake()
    {
        if (MenuAsleep)
        {
            MenuWake();
        }
        else
        {
            MenuSleep();
        }
    }

    private enum Browser
    {
        Web,
        Table
    }

    private static void BonsaiLog(string msg)
    {
        Debug.Log("<color=orange>BonsaiTableBrowserParent: </color>: " + msg);
    }

    private static void BonsaiLogWarning(string msg)
    {
        Debug.LogWarning("<color=orange>BonsaiTableBrowserParent: </color>: " + msg);
    }

    private void BonsaiLogError(string msg)
    {
        Debug.LogError("<color=orange>BonsaiTableBrowserParent: </color>: " + msg);
    }
}