using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuHand : MonoBehaviour, IHandTick
{
    public PlayerHand playerHand { get; set; }

    public GameObject menuPrefab;
    public AngleToObject angleToHead;
    public TableBrowserParent tableBrowserParent;
    public List<GameObject> menuObjects;

    private bool _isInit = false;

    private void Init()
    {
        if (menuPrefab)
        {
            menuObjects.Add(Instantiate(menuPrefab, transform));
        }
        SetMenuState(false);
    }

    public void Tick()
    {
        if (!_isInit)
        {
            _isInit = true;
            Init();
        }

        var angleBelowThreshold = angleToHead.AngleBelowThreshold();
        var menuOpen = tableBrowserParent && !tableBrowserParent.MenuAsleep;
        var playing = Application.isFocused && Application.isPlaying || Application.isEditor;
        var oriented = MoveToDesk.Singleton.oriented;
        var active = angleBelowThreshold && playerHand.HandComponents.TrackingRecently && !menuOpen && playing && oriented;
        SetMenuState(active);
    }

    public void TurnOffMenus()
    {
        SetMenuState(false);
    }

    private void SetMenuState(bool active)
    {
        for (int i = 0; i < menuObjects.Count; i++)
        {
            menuObjects[i].SetActive(active);
        }
    }
}