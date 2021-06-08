using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class MenuHand : MonoBehaviour, IHandTick
{
    public PlayerHand playerHand { get; set; }

    public AngleToObject angleToHead;
    public TableBrowserParent tableBrowserParent;
    public List<GameObject> menuObjects;
    public Material overlayUIMaterial;
    public Image[] images;
    public Material overlayTextMaterial;
    public TextMeshProUGUI[] texts;
    public GameObject returnToVoidHoverButton;

    private bool _isInit = false;

    private void Init()
    {
        overlayUIMaterial = new Material(overlayUIMaterial);
        overlayUIMaterial.renderQueue = (int) RenderQueue.Transparent;
        for (int i = 0; i < images.Length; i++)
        {
            images[i].material = overlayUIMaterial;
        }

        overlayTextMaterial = new Material(overlayTextMaterial);
        overlayTextMaterial.renderQueue = (int) RenderQueue.Transparent;
        for (int i = 0; i < texts.Length; i++)
        {
            texts[i].fontSharedMaterial = overlayTextMaterial;
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
        
        var menuOpen = tableBrowserParent && (!tableBrowserParent.MenuAsleep || !tableBrowserParent.ContextAsleep);
        overlayUIMaterial.renderQueue = menuOpen ? (int) RenderQueue.Overlay + 3 : (int) RenderQueue.Transparent;
        overlayTextMaterial.renderQueue = menuOpen ? (int) RenderQueue.Overlay + 3 : (int) RenderQueue.Transparent;

        var angleBelowThreshold = angleToHead.AngleBelowThreshold();
        var playing = Application.isFocused && Application.isPlaying || Application.isEditor;
        var oriented = MoveToDesk.Singleton.oriented;
        var active = angleBelowThreshold && playerHand.HandComponents.TrackingRecently && playing && oriented;
        if (returnToVoidHoverButton)
        {
            returnToVoidHoverButton.SetActive(active);
        }
        if (playerHand.skeletonType == OVRSkeleton.SkeletonType.HandLeft && tableBrowserParent && !tableBrowserParent.ContextAsleep)
        {
            active = false;
        }
        if (playerHand.skeletonType == OVRSkeleton.SkeletonType.HandRight && tableBrowserParent && !tableBrowserParent.MenuAsleep)
        {
            active = false;
        }
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