using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine;
using UnityEngine.XR.Management;
using Application = UnityEngine.Application;

public class CursedNetworkManager : MonoBehaviour
{
    public ulong remoteIdY;
    public ulong remoteIdX;
    public ulong connectedId;
    public bool attemptedToJoin;
    private readonly List<string> lines = new List<string>();
    public User user;
    private float _lastPinged;
    private bool _shouldLog;

    private void Start()
    {
        if (Application.isEditor)
        {
            StartCoroutine(StartXR());
        }

        Core.AsyncInitialize().OnComplete(InitCallback);

        Net.SetPeerConnectRequestCallback(PeerConnectRequestCallback);
        Net.SetConnectionStateChangedCallback(ConnectionStateChangedCallback);
    }

    // Update is called once per frame
    private void Update()
    {
        var x = OVRInput.GetDown(OVRInput.RawButton.X) || Input.GetKeyDown(KeyCode.X);
        var y = OVRInput.GetDown(OVRInput.RawButton.Y) || Input.GetKeyDown(KeyCode.Y);
        var a = OVRInput.GetDown(OVRInput.RawButton.A) || Input.GetKeyDown(KeyCode.A);
        var b = OVRInput.GetDown(OVRInput.RawButton.B) || Input.GetKeyDown(KeyCode.B);

        if (b)
        {
            if (!_shouldLog)
            {
                OculusLog("Beginning recording pings, press A to dump");
                _shouldLog = true;
            }
        }
        
        
        if (user != null && !attemptedToJoin && x)
        {
            attemptedToJoin = true;
            ConnectTo(remoteIdX);
        }

        if (user != null && !attemptedToJoin && y)
        {
            attemptedToJoin = true;
            ConnectTo(remoteIdY);
        }

        if (a)
        {
            OculusLog("Dumping pings");
            DumpPings();
        }

        if (_shouldLog && Time.realtimeSinceStartup - _lastPinged >= 0.1f)
        {
            _lastPinged = Time.realtimeSinceStartup;
            Ping();
        }
    }

    public void OnApplicationQuit()
    {
        StopXR();
    }

    public void DumpPings()
    {
        var epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var cur_time = (int) (DateTime.UtcNow - epochStart).TotalSeconds;
        var logFileName = "ping_" + cur_time + ".log";
        var logPath = Path.Combine(Application.persistentDataPath, logFileName);

        File.WriteAllLines(logPath, lines);
    }

    private void AddTime(double gameTime, float ping)
    {
        lines.Add($"{gameTime} {ping}");
    }

    public void Ping()
    {
        var cur_time = Time.realtimeSinceStartup;

        Net.Ping(connectedId).OnComplete(a =>
        {
            if (a.IsError)
            {
                //OculusLogWarning(a.GetError().Message);
                //AddTime(cur_time, -20000);
            }
            else
            {
                if (a.Data.IsTimeout)
                {
                    //OculusLogWarning("Timeout");
                    //AddTime(cur_time, -20000);
                }
                else
                {
                    //OculusLog($"Ping {a.Data.PingTimeUsec}");
                    AddTime(cur_time, a.Data.PingTimeUsec);
                }
            }
        });
    }

    public void ConnectTo(ulong id)
    {
        OculusLog($"Connect to {id}");
        connectedId = id;
        Net.Connect(id);
    }

    private IEnumerator StartXR()
    {
        OculusLog("Initializing XR");
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            OculusLogError("Initializing XR Failed. Check Editor or Player log for details.");
        }
        else
        {
            OculusLog("Starting XR");
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }
    }

    private void StopXR()
    {
        if (XRGeneralSettings.Instance.Manager.isInitializationComplete)
        {
            OculusLog("Stopping XR");
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        }
    }

    private void InitCallback(Message<PlatformInitialize> msg)
    {
        if (msg.IsError)
        {
            TerminateWithError(msg);
            return;
        }

        Users.GetLoggedInUser().OnComplete(HandleGetLoggedInUser);
    }

    private void HandleGetLoggedInUser(Message<User> msg)
    {
        if (msg.IsError)
        {
            TerminateWithError(msg);
            return;
        }

        user = msg.Data;

        OculusLog($"Logged in with user {msg.Data.ID} {msg.Data.OculusID}");
    }

    private void ConnectionStateChangedCallback(Message<NetworkingPeer> msg)
    {
        Debug.LogFormat("Connection state to {0} changed to {1}", msg.Data.ID, msg.Data.State);
        if (msg.Data.State == PeerConnectionState.Connected)
        {
            connectedId = msg.Data.ID;
        }
    }

    private void PeerConnectRequestCallback(Message<NetworkingPeer> msg)
    {
        Debug.LogFormat("Connection request from {0}, authorized is {1}", msg.Data.ID, msg.Data.ID);
        Net.Accept(msg.Data.ID);
    }

    private void TerminateWithError(Message msg)
    {
        OculusLogError($"Error {msg.GetError().Message}");
        Application.Quit();
    }

    private void OculusLog(string msg)
    {
        Debug.Log("<color=orange>OculusNetwork: </color>: " + msg);
    }

    private void OculusLogWarning(string msg)
    {
        Debug.LogWarning("<color=orange>OculusNetwork: </color>: " + msg);
    }

    private void OculusLogError(string msg)
    {
        Debug.LogError("<color=orange>OculusNetwork: </color>: " + msg);
    }
}