using System.Collections;
using System.Collections.Generic;
using mixpanel;
using UnityEngine;

public class ToggleSeeHead : MonoBehaviour
{
    public Camera mainCam;
    
    private bool canSeeHead = false;
    
    public void ToggleSeeOwnHead()
    {
        Mixpanel.Track("Toggle See Own Head");
        canSeeHead = !canSeeHead;
        if (canSeeHead)
        {
            mainCam.cullingMask |= 1 << LayerMask.NameToLayer("doNotRenderHead");
        }
        else
        {
            mainCam.cullingMask &= ~(1 << LayerMask.NameToLayer("doNotRenderHead"));
        }
    }
}
