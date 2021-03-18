using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Management;

public class StartXRStart : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(StartXR());
    }

    private IEnumerator StartXR() {
        Debug.Log("Initializing XR...");
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null) {
            Debug.LogError("Initializing XR Failed. Check Editor or Player log for details.");
        }
        else {
            Debug.Log("Starting XR...");
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }
    }
}
