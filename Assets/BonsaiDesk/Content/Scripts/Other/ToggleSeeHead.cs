using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleSeeHead : MonoBehaviour
{
    public Camera mainCam;
    
    private bool canSeeHead = false;
    
    public void ToggleSeeOwnHead()
    {
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
