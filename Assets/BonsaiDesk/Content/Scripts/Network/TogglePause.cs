using System;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using Mirror;
using UnityEngine;

public class TogglePause : NetworkBehaviour
{
    public GameObject icons;
    public GameObject playIcon;
    public GameObject pauseIcon;

    [SyncVar(hook = nameof(SetPaused))] private bool paused = true;

    private bool leftPointing = false;
    private bool rightPointing = false;

    private void Start()
    {
        updateIcons(paused);
    }

    private void Update()
    {
        icons.SetActive(leftPointing || rightPointing);
    }

    [Command(ignoreAuthority = true)]
    void CmdSetPaused(bool paused)
    {
        this.paused = paused;
    }

    public void SetPaused(bool paused)
    {
        CmdSetPaused(paused);
    }

    void SetPaused(bool oldPaused, bool newPaused)
    {
        updateIcons(newPaused);
    }

    void updateIcons(bool paused)
    {
        playIcon.SetActive(paused);
        pauseIcon.SetActive(!paused);
    }

    public void leftPoint(bool pointing)
    {
        leftPointing = pointing;
    }

    public void rightPoint(bool pointing)
    {
        rightPointing = pointing;
    }
}
