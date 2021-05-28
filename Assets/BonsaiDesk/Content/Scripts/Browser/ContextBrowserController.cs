using System;
using Newtonsoft.Json;
using UnityEngine;
using Vuplex.WebView;

[RequireComponent(typeof(TableBrowser))]
public class ContextBrowserController : MonoBehaviour
{
    public enum Hand
    {
        Left,
        Right
    }

    private string _leftHandMode = "";
    private string _rightHandMode = "";

    public string LeftHandMode => _leftHandMode;
    public string RightHandMode => _rightHandMode;

    private string _leftBlockActive = "wood1";
    private string _rightBlockActive = string.Empty;

    private bool _leftBlockBreak;
    private bool _rightBlockBreak;
    
    public string LeftBlockActive => _leftBlockActive;
    public string RightBlockActive => _rightBlockActive;
    public bool LeftBlockBreak => _leftBlockBreak;
    public bool RightBlockBreak => _rightBlockBreak;

    private TableBrowser _browser;

    // Start is called before the first frame update
    private void Start()
    {
        _browser = GetComponent<TableBrowser>();
        _browser.ListenersReady += SetupBrowser;
        _browser.BrowserReady += (sender, _) => { _browser.OnMessageEmitted(HandleJavascriptMessage); };
        
        NetworkBlockSpawn.InstanceLeft.SetSpawnBlockName(LeftBlockActive);
        NetworkBlockSpawn.InstanceRight.SetSpawnBlockName(RightBlockActive);
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
                    case "setHandMode":
                        var handModeData = JsonConvert.DeserializeObject<HandMode>(message.Data);
                        if (handModeData.Hand == "left")
                        {
                            _leftHandMode = handModeData.Mode;
                        }
                        if (handModeData.Hand == "right")
                        {
                            _rightHandMode = handModeData.Mode;
                        }
                        
                        InfoChange.Invoke();
                        break;

                    case "toggleBlockActive":
                        var toggleHand = message.Data == "left" ? Hand.Left : Hand.Right;
                        if (toggleHand == Hand.Left)
                        {
                            _leftBlockActive = string.IsNullOrEmpty(_leftBlockActive) ? "wood1" : string.Empty;
                            NetworkBlockSpawn.InstanceLeft.SetSpawnBlockName(_leftBlockActive);
                            InfoChange?.Invoke();
                        }

                        if (toggleHand == Hand.Right)
                        {
                            _rightBlockActive = string.IsNullOrEmpty(_rightBlockActive) ? "wood1" : string.Empty;
                            NetworkBlockSpawn.InstanceRight.SetSpawnBlockName(_rightBlockActive);
                            InfoChange?.Invoke();
                        }

                        break;
                    case "changeActiveBlock":
                        var data = JsonConvert.DeserializeObject<ActiveBlockString>(message.Data);
                        var hand = data.Hand == "left" ? Hand.Left : Hand.Right;
                        var blockName = data.BlockName;
                        if (blockName != string.Empty && Blocks.GetBlock(blockName) == null)
                        {
                            BonsaiLogWarning($"Block {blockName} does not exist in blocks");
                        }

                        BonsaiLog($"changeActiveBlock: {hand} {blockName}");
                        switch (hand)
                        {
                            case Hand.Left:
                                _leftBlockActive = blockName;
                                NetworkBlockSpawn.InstanceLeft.SetSpawnBlockName(blockName);
                                break;
                            case Hand.Right:
                                _rightBlockActive = blockName;
                                NetworkBlockSpawn.InstanceRight.SetSpawnBlockName(blockName);
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
        public string BlockName;
    }

    private struct HandMode
    {
        public string Hand;
        public string Mode;
    }
}