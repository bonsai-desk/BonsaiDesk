using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RendererToggle : MonoBehaviour
{
    public SkinnedMeshRenderer renderer;
    public PlayerHand playerHand;
    
    void Update()
    {
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
