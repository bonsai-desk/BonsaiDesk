using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TableBrowser))]
public class ContextBrowserController : MonoBehaviour
{
    private TableBrowser _browser;
    // Start is called before the first frame update
    void Start()
    {
        _browser = GetComponent<TableBrowser>();
        _browser.ListenersReady += SetupBrowser;
    }

    private void SetupBrowser()
    {
        _browser.PostMessage(Browser.BrowserMessage.NavContext);
        //SetActive(false);
        //_browser.SetHidden(true);
    }
    
    public void SetActive(bool active)
    {
        _browser.SetHidden(!active);
    }
    
}
