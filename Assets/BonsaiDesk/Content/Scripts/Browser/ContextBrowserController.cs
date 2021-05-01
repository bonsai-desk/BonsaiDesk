using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Vuplex.WebView;

[RequireComponent(typeof(TableBrowser))]
public class ContextBrowserController : MonoBehaviour
{
    public Block LeftBlockActive = Block.None;
    public Block RightBlockActive = Block.None;
    
    private TableBrowser _browser;
    // Start is called before the first frame update
    void Start()
    {
        _browser = GetComponent<TableBrowser>();
        _browser.ListenersReady += SetupBrowser;
        _browser.BrowserReady += (sender, _) =>
        {
            _browser.OnMessageEmitted(HandleJavascriptMessage);
        };
    }

    public event Action<Hand, Block> ChangeActiveBlock;

    private void HandleJavascriptMessage(object sender, EventArgs<string> e)
    {
        var message = JsonConvert.DeserializeObject<Browser.JsMessageString>(e.Value);
        switch (message.Type)
        {
            case "command":
                switch (message.Message)
                {
                    case "changeActiveBlock":
                        var data = JsonConvert.DeserializeObject<ActiveBlockString>(message.Data);
                        var hand = data.Hand == "left" ? Hand.Left : Hand.Right;
                        var block = (Block)data.BlockId;
                        if (!Enum.IsDefined(typeof(Block), data.BlockId))
                        {
                            BonsaiLogWarning("Failed to match block id, defaulting to None");
                        }
                        BonsaiLog($"changeActiveBlock: {hand} {block}");
                        switch (hand)
                        {
                            case Hand.Left:
                                LeftBlockActive = block;
                                break;
                            case Hand.Right:
                                RightBlockActive = block;
                                break;
                        }

                        ChangeActiveBlock?.Invoke(hand, block);
                        break;
                }

                break;
        }
    }

    public enum Hand
    {
        Left, Right
    }

    public enum Block
    {
        None, Wood, Orange, Green, Purple, Red
    }

    private struct ActiveBlockString
    {
        public string Hand;
        public int BlockId;
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
    
    private void BonsaiLog(string msg)
    {
        Debug.Log("<color=orange>BonsaiContextBrowser: </color>: " + msg);
    }
    
    private void BonsaiLogWarning(string msg)
    {
        Debug.LogWarning("<color=orange>BonsaiContextBrowser: </color>: " + msg);
    }
    
}
