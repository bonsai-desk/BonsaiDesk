using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RendererToggle : MonoBehaviour
{
    public SkinnedMeshRenderer renderer;
    public PlayerHand playerHand;

    private bool _initialState = true;

    void Start()
    {
        _initialState = renderer.enabled;
    }
    
    void Update()
    {
        if (!_initialState)
            return;
        
        if (renderer && playerHand)
        {
            bool tracking = playerHand.Tracking();
            if (tracking && !renderer.enabled)
                renderer.enabled = true;
            if (!tracking && renderer.enabled)
                renderer.enabled = false;
        }
    }
}
