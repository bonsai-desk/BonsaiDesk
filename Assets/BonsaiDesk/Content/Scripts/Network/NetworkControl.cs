using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Management;

public class NetworkControl : MonoBehaviour
{
    public bool serverOnlyIfEditor = false;

    public GameObject player;

    public GameObject server;

    public OVRInputModule oVRInputModule;

    // Start is called before the first frame update
    private void Awake()
    {
        if (Application.isEditor && !serverOnlyIfEditor)
        {
            StartCoroutine(startCoroutine());
        }
        else
        {
            if (Application.isEditor && serverOnlyIfEditor)
            {
                server.SetActive(true);
                player.SetActive(false);
                oVRInputModule.enabled = false;
                oVRInputModule.gameObject.AddComponent<StandaloneInputModule>();
            }
            init();
        }
    }

    private IEnumerator startCoroutine()
    {
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
        XRGeneralSettings.Instance.Manager.StartSubsystems();
        init();
    }

    private void init()
    {
        if (Application.isEditor && serverOnlyIfEditor)
        {
            startServer();
        }
        else
        {
            startHost();
            // join();
        }
    }

    public void startHost()
    {
        NetworkManagerGame.singleton.StartHost();
    }

    public void startServer()
    {
        NetworkManagerGame.singleton.StartServer();
    }

    public void join()
    {
        NetworkManagerGame.singleton.networkAddress = "192.168.1.126";
        NetworkManagerGame.singleton.StartClient();
    }
}