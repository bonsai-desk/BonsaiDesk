using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TableBrowser))]
public class TableBrowserController : MonoBehaviour
{
    private TableBrowser _tableBrowser;
    // Start is called before the first frame update
    void Start()
    {
        _tableBrowser = GetComponent<TableBrowser>();
        _tableBrowser.BrowserReady +=  SetupBrowser;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SetupBrowser()
    {
        
    }
}
