using System;
using System.Collections;
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

    public Hand handActive = Hand.Right;

    public BlockBreakHand.BreakMode ActiveHandMode => handActive == Hand.Left ? LeftHandMode : RightHandMode;

    public BlockBreakHand blockBreakHandLeft;
    public BlockBreakHand blockBreakHandRight;

    private TableBrowser _browser;

    public BlockBreakHand.BreakMode LeftHandMode => blockBreakHandLeft.HandBreakMode;
    public BlockBreakHand.BreakMode RightHandMode => blockBreakHandRight.HandBreakMode;

    private const string DefaultLeftActive = "wood1";
    private const string DefaultRightActive = "";

    public string LeftBlockActive { get; private set; } = DefaultLeftActive;

    public string RightBlockActive { get; private set; } = DefaultRightActive;

    public bool LeftBlockBreak { get; }

    public bool RightBlockBreak { get; }

    // Start is called before the first frame update
    private void Start()
    {
        _browser = GetComponent<TableBrowser>();
        _browser.ListenersReady += SetupBrowser;
        _browser.BrowserReady += (sender, _) => { _browser.OnMessageEmitted(HandleJavascriptMessage); };

        if (SaveSystem.Instance.IntPairs.TryGetValue("ContextHand", out var value))
        {
            handActive = (Hand) value;
        }

        if (SaveSystem.Instance.StringPairs.TryGetValue("LeftBlockActive", out var leftBlockActive))
        {
            LeftBlockActive = leftBlockActive;
        }
        else
        {
            LeftBlockActive = DefaultLeftActive;
        }

        SetLeftSpawner(LeftBlockActive);

        if (SaveSystem.Instance.StringPairs.TryGetValue("RightBlockActive", out var rightBlockActive))
        {
            RightBlockActive = rightBlockActive;
        }
        else
        {
            RightBlockActive = DefaultRightActive;
        }

        SetRightSpawner(RightBlockActive);

        StartCoroutine(SetInitialBlocks());
    }

    private IEnumerator SetInitialBlocks()
    {
        while (!NetworkBlockSpawn.InstanceLeft || !NetworkBlockSpawn.InstanceRight)
        {
            yield return null;
        }

        NetworkBlockSpawn.InstanceLeft.SetSpawnBlockName(LeftBlockActive);
        NetworkBlockSpawn.InstanceRight.SetSpawnBlockName(RightBlockActive);
    }

    public event Action InfoChange;

    private void SetHand(string hand)
    {
        if (hand == "left")
        {
            handActive = Hand.Left;
            if (blockBreakHandLeft.HandBreakMode == BlockBreakHand.BreakMode.None)
            {
                blockBreakHandLeft.SetBreakMode(blockBreakHandRight.HandBreakMode);
                blockBreakHandRight.SetBreakMode(BlockBreakHand.BreakMode.None);
            }
        }

        if (hand == "right")
        {
            handActive = Hand.Right;
            SaveSystem.Instance.IntPairs["ContextHand"] = (int) Hand.Right;
            if (blockBreakHandRight.HandBreakMode == BlockBreakHand.BreakMode.None)
            {
                blockBreakHandRight.SetBreakMode(blockBreakHandLeft.HandBreakMode);
                blockBreakHandLeft.SetBreakMode(BlockBreakHand.BreakMode.None);
            }
        }

        SaveSystem.Instance.IntPairs["ContextHand"] = (int) handActive;
    }

    private void SetHandMode(BlockBreakHand.BreakMode handMode)
    {
        if (handActive == Hand.Left)
        {
            blockBreakHandLeft.SetBreakMode(handMode);
        }
        else
        {
            blockBreakHandRight.SetBreakMode(handMode);
        }
    }

    private void HandleJavascriptMessage(object sender, EventArgs<string> e)
    {
        var message = JsonConvert.DeserializeObject<Browser.JsMessageString>(e.Value);
        switch (message.Type)
        {
            case "command":
                switch (message.Message)
                {
                    case "setHand":
                        SetHand(message.Data);
                        InfoChange.Invoke();
                        break;

                    case "setHandMode":
                        var handModeData = JsonConvert.DeserializeObject<BlockBreakHand.BreakMode>(message.Data);
                        SetHandMode(handModeData);
                        InfoChange.Invoke();
                        break;
                    case "changeActiveBlock":
                        var data = JsonConvert.DeserializeObject<ActiveBlockString>(message.Data);
                        var hand = data.Hand == "left" ? Hand.Left : Hand.Right;
                        var blockName = data.BlockName;
                        if (blockName != string.Empty && Blocks.GetBlock(blockName) == null)
                        {
                            BonsaiLogWarning($"Block {blockName} does not exist in blocks");
                        }

                        var printName = blockName == string.Empty ? "(no block)" : blockName;
                        BonsaiLog($"changeActiveBlock: {hand} {printName}");
                        switch (hand)
                        {
                            case Hand.Left:
                                LeftBlockActive = blockName;
                                SaveSystem.Instance.StringPairs["LeftBlockActive"] = blockName;
                                SetLeftSpawner(blockName);

                                break;
                            case Hand.Right:
                                RightBlockActive = blockName;
                                SaveSystem.Instance.StringPairs["RightBlockActive"] = blockName;
                                SetRightSpawner(blockName);

                                break;
                        }

                        InfoChange?.Invoke();
                        break;
                }

                break;
        }
    }

    private void SetRightSpawner(string blockName)
    {
        if (NetworkBlockSpawn.InstanceRight)
        {
            NetworkBlockSpawn.InstanceRight.SetSpawnBlockName(blockName);
        }
    }

    private void SetLeftSpawner(string blockName)
    {
        if (NetworkBlockSpawn.InstanceLeft)
        {
            NetworkBlockSpawn.InstanceLeft.SetSpawnBlockName(blockName);
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
        public BlockBreakHand.BreakMode Mode;
    }
}