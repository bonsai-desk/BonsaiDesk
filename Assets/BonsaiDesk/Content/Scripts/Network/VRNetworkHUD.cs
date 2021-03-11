using Mirror;
using System.Collections;
using TMPro;
using UnityEngine;

public class VRNetworkHUD : MonoBehaviour
{
    public TextMeshProUGUI textMesh;

    private NetworkManagerGame manager;

    public GameObject joinButton;
    public GameObject hostButton;
    public GameObject cancelButton;
    public GameObject stopClientButton;
    public GameObject stopHostButton;

    private void Start()
    {
        manager = NetworkManagerGame.Singleton;
    }

    private void Update()
    {
        bool join = false;
        bool host = false;
        bool cancel = false;
        bool stopClient = false;
        bool stopHost = false;

        string text = "Not connected to multiplayer";

        if (!NetworkClient.isConnected && !NetworkServer.active)
        {
            //buttons
            if (!NetworkClient.active)
            {
                //client button
                host = true;
                join = true;
            }
            else
            {
                // Connecting
                text = "Connecting to " + manager.networkAddress + "..\n";

                //cancel button
                cancel = true;
            }
        }
        else
        {
            //info
            if (NetworkServer.active)
            {
                text = "Server: active. Transport: " + Transport.activeTransport + "\n";
            }
            if (NetworkClient.isConnected)
            {
                text = "Client: address=" + manager.networkAddress + "\n";
            }
        }

        // stop host if host mode
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            //stop host button
            stopHost = true;

            text += "\n[host]\n";
        }
        // stop client if client-only
        else if (NetworkClient.isConnected)
        {
            //stop client button
            stopClient = true;

            text += "\n[client]\n";
        }
        // stop server if server-only
        else if (NetworkServer.active)
        {
            //stop server button

            text += "\n[server]\n";
        }

        if (joinButton.activeSelf != join)
            StartCoroutine(SetActiveDelay(joinButton, join));
        if (hostButton.activeSelf != host)
            StartCoroutine(SetActiveDelay(hostButton, host));
        if (cancelButton.activeSelf != cancel)
            StartCoroutine(SetActiveDelay(cancelButton, cancel));
        if (stopClientButton.activeSelf != stopClient)
            StartCoroutine(SetActiveDelay(stopClientButton, stopClient));
        if (stopHostButton.activeSelf != stopHost)
            StartCoroutine(SetActiveDelay(stopHostButton, stopHost));

        if (text.CompareTo(textMesh.text) != 0)
            textMesh.text = text;
    }

    private IEnumerator SetActiveDelay(GameObject go, bool state)
    {
        if (state)
        {
            yield return new WaitForSeconds(HoleButton.shrinkTime);
            go.SetActive(true);
        }
        else
        {
            go.GetComponent<HoleButton>().DisableButton();
        }
    }

    public void Join()
    {
        manager.networkAddress = "192.168.1.126";
        manager.StartClient();
    }

    public void Cancel()
    {
        manager.StopClient();
        GameObject.Find("GameManager").GetComponent<MoveToDesk>().SetTableEdge(GameObject.Find("DefaultEdge").transform);
    }

    public void StopClient()
    {
        manager.StopClient();
        GameObject.Find("GameManager").GetComponent<MoveToDesk>().SetTableEdge(GameObject.Find("DefaultEdge").transform);
    }

    public void StopHost()
    {
        manager.StopHost();
        GameObject.Find("GameManager").GetComponent<MoveToDesk>().SetTableEdge(GameObject.Find("DefaultEdge").transform);
    }

    public void StartHost()
    {
        manager.StartHost();
    }
}