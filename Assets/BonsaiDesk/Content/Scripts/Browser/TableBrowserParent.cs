using System;
using UnityEngine;
using Vuplex.WebView;

public class TableBrowserParent : MonoBehaviour
{
    private bool openedOnce;
    private Browser active = Browser.Table;
    public TableBrowser TableBrowser;
    public TableBrowser ContextMenu;
    public TableBrowser KeyboardBrowser;
    public TableBrowserMenu TableBrowserMenu;
    public WebBrowserParent WebBrowserParent;
    public KeyboardBrowserController KeyboardBrowserController;
    public MoveToDesk moveToDesk;
    public static TableBrowserParent Instance;
    public bool MenuAsleep { get; private set; }
    public bool ContextAsleep { get; private set; }
    
    private int _parentsReady;
    public BoxCollider contentBoxCollider;

    public GameObject closeMenuHoverButton;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

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

        KeyboardBrowserController.DismissKeyboard += HandleDismissKeyboard;

        TableBrowser.FocusInput += HandleMenuFocusInput;

        KeyboardBrowser.InputRecieved += HandleKeyboardInput;
    }

    private void HandleKeyboardInput(object sender, EventArgs<string> e)
    {
        switch (active)
        {
            case Browser.Web:
                WebBrowserParent.webBrowser.HandleKeyboardInput(e.Value);
                break;
            case Browser.Table:
                TableBrowser.HandleKeyboardInput(e.Value);
                break;
            default:
                BonsaiLogWarning($"Did not handle input for mode ({active})");
                break;
        }
    }

    private void HandleMenuFocusInput(object sender, EventArgs<bool> focus)
    {
        if (focus.Value)
        {
            KeyboardBrowserController.SetActive(true);
            TableBrowserMenu.SetRaised(true);
        }
        else
        {
            KeyboardBrowserController.SetActive(false);
            TableBrowserMenu.SetRaised(false);
        }
    }

    private void HandleDismissKeyboard()
    {
        // if menu mode, set not hidden
        switch (active)
        {
            case Browser.Web:
                WebBrowserParent.HandleToggleKeyboard();
                break;
            case Browser.Table:
                KeyboardBrowserController.SetActive(false);
                TableBrowserMenu.SetRaised(false);
                break;
            default:
                BonsaiLogWarning($"Dismiss keyboard while ({active}) not handled");
                break;
        }
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

    private void HandleOrientationChange(bool oriented)
    {
        MenuSleep();
        ContextSleep();
    }

    private void HandleCloseWeb(object _, EventArgs e)
    {
        WebBrowserParent.HandleDismissKeyboard();
        SetActive(Browser.Table);
    }

    private void HandleBrowseYouTube(object _, EventArgs e)
    {
        SetActive(Browser.Web);
    }

    public void OpenMenu()
    {
        SetActive(Browser.Table);
        MenuWake();
    }

    private void SetActive(Browser browser)
    {
        active = browser;
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
        
        closeMenuHoverButton.SetActive(!MenuAsleep);

        SetHandForInactiveBrowser();
    }

    private void MenuWake()
    {
        MenuAsleep = false;
        
        closeMenuHoverButton.SetActive(!MenuAsleep);

        if (!openedOnce)
        {
            openedOnce = true;
            TableBrowser.SetHidden(false);
        }
        else
        {
            switch (active)
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

    public bool AllMenusClosed()
    {
        return MenuAsleep && ContextAsleep;
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

    public void ToggleContextAwakeIfMenuClosed()
    {
        if (!moveToDesk.oriented)
        {
            return;
        }
        
        if (ContextAsleep)
        {
            if (MenuAsleep)
            {
                ContextWake();
            }
        }
        else
        {
            ContextSleep();
        }
    }

    public void ToggleContextAwake()
    {
        if (!moveToDesk.oriented)
        {
            return;
        }
        
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
        if (!moveToDesk.oriented)
        {
            return;
        }
        
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