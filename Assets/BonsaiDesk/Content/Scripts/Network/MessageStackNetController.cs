using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class MessageStackNetController : NetworkBehaviour
{
    public static MessageStackNetController Instance;

    private void Awake()
    {
        Instance = this;
    }
    
    [Command(ignoreAuthority = true)]
    public void CmdMsg(string msg, MessageStack.MessageType messageType)
    {
        ServerMsg(msg, messageType);
    }

    [ClientRpc]
    private void RpcMsg(string msg, MessageStack.MessageType messageType)
    {
        print("RpcMsg: " + msg);
        MessageStack.Singleton.AddMessage(msg, messageType);
    }

    [Server]
    public void ServerMsg(string msg, MessageStack.MessageType messageType)
    {
        print("ServerMsg: " + msg);
        RpcMsg(msg, messageType);
    }
}
