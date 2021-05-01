using System;
using Newtonsoft.Json;
using UnityEngine;
using Vuplex.WebView;

[RequireComponent(typeof(TableBrowser))]
public class ContextBrowserController : MonoBehaviour
{
    public enum Block
    {
        None,
        Wood,
        Orange,
        Green,
        Brown,
        Pink,
        LightPurple,
        DarkPurple,
        Violet,
        LightNeutral,
        DarkNeutral
    }

    public enum Hand
    {
        Left,
        Right
    }

    public Block LeftBlockActive = Block.None;
    public Block RightBlockActive = Block.None;

    public bool LeftBlockBreak;
    public bool RightBlockBreak;

    private TableBrowser _browser;

    // Start is called before the first frame update
    private void Start()
    {
        _browser = GetComponent<TableBrowser>();
        _browser.ListenersReady += SetupBrowser;
        _browser.BrowserReady += (sender, _) => { _browser.OnMessageEmitted(HandleJavascriptMessage); };
    }


    public event Action InfoChange;

    private void HandleJavascriptMessage(object sender, EventArgs<string> e)
    {
        var message = JsonConvert.DeserializeObject<Browser.JsMessageString>(e.Value);
        switch (message.Type)
        {
            case "command":
                switch (message.Message)
                {
                    case "toggleBlockBreakHand":
                        var toggleBlockBreakHand = message.Data == "left" ? Hand.Left : Hand.Right;
                        if (toggleBlockBreakHand == Hand.Left)
                        {
                            LeftBlockBreak = !LeftBlockBreak;
                            InfoChange?.Invoke();
                        }

                        if (toggleBlockBreakHand == Hand.Right)
                        {
                            RightBlockBreak = !RightBlockBreak;
                            InfoChange?.Invoke();
                        }

                        break;
                        
                    case "toggleBlockActive":
                        var toggleHand = message.Data == "left" ? Hand.Left : Hand.Right;
                        if (toggleHand == Hand.Left)
                        {
                            LeftBlockActive = LeftBlockActive == Block.None ? Block.Wood : Block.None;
                            InfoChange?.Invoke();
                        }

                        if (toggleHand == Hand.Right)
                        {
                            RightBlockActive = RightBlockActive == Block.None ? Block.Wood : Block.None;
                            InfoChange?.Invoke();
                        }

                        break;
                    case "changeActiveBlock":
                        var data = JsonConvert.DeserializeObject<ActiveBlockString>(message.Data);
                        var hand = data.Hand == "left" ? Hand.Left : Hand.Right;
                        var block = (Block) data.BlockId;
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

                        InfoChange?.Invoke();
                        break;
                }

                break;
        }
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

    private struct ActiveBlockString
    {
        public string Hand;
        public int BlockId;
    }
}