using Mirror;
using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine;
using Application = UnityEngine.Application;

public class SocialManager : NetworkBehaviour
{
    public static SocialManager Singleton;
    public TableBrowserMenu BrowserMenu;
    public float postUserInfoEvery = 0.1f;
    private float _postInfoLast;
    private float postedUserInfoLast;
    private bool reportWhenReady;
    public User User;
    private (bool, TableBrowserMenu.UserInfo) userInfo;

    // Start is called before the first frame update
    private void Start()
    {
        if (Singleton == null)
        {
            Singleton = this;
        }

        NetworkManagerGame.Singleton.ServerAddPlayer -= HandleServerAddPlayer;
        NetworkManagerGame.Singleton.ServerAddPlayer += HandleServerAddPlayer;

        Core.AsyncInitialize().OnComplete(InitCallback);
    }

    // Update is called once per frame
    private void Update()
    {
        MaybePostInfo();
        if (reportWhenReady && User != null)
        {
            Users.GetLoggedInUser().OnComplete(msg =>
            {
                if (msg.IsError)
                {
                    TerminateWithError(msg);
                    return;
                }

                BonsaiLog($"Logged in with Oculus as user: {msg.Data.OculusID}");
                CmdSetUserInfo(new NetworkManagerGame.UserInfo(msg.Data.OculusID));
            });
            reportWhenReady = false;
        }
    }

    private void MaybePostInfo()
    {
        if (Time.time - postedUserInfoLast > postUserInfoEvery)
        {
            if (userInfo.Item1)
            {
                BrowserMenu.PostUserInfo(userInfo.Item2);
                postedUserInfoLast = Time.time;
            }
        }
    }

    private void HandleGetLoggedInUser(Message<User> msg)
    {
        if (msg.IsError)
        {
            TerminateWithError(msg);
            return;
        }

        User = msg.Data;
        var userInfoClass = new TableBrowserMenu.UserInfo {UserName = "testname"};
        userInfo = (true, userInfoClass);
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

    private void TerminateWithError(Message msg)
    {
        BonsaiLogError($"Error {msg.GetError().Message}");
        Application.Quit();
    }

    [TargetRpc]
    private void TargetReportUserInfo(NetworkConnection conn)
    {
        var id = NetworkClient.connection.identity.netId;
        if (User != null)
        {
            BonsaiLog($"Report as user : {User.OculusID}");
            CmdSetUserInfo(new NetworkManagerGame.UserInfo(User.OculusID));
        }
        else
        {
            reportWhenReady = true;
        }
    }

    [Command(ignoreAuthority = true)]
    private void CmdSetUserInfo(NetworkManagerGame.UserInfo user, NetworkConnectionToClient sender = null)
    {
        var id = sender.identity.netId;
        NetworkManagerGame.Singleton.UpdateUserInfo(id, user);
        BonsaiLog($"Received user info (id={id}) ({user.DisplayName})");
    }

    private void HandleServerAddPlayer(NetworkConnection networkConnection, bool isLanOnly)
    {
        TargetReportUserInfo(networkConnection);
    }

    private void BonsaiLog(string msg)
    {
        Debug.Log("<color=orange>BonsaiSocial: </color>: " + msg);
    }

    private void BonsaiLogWarning(string msg)
    {
        Debug.LogWarning("<color=orange>BonsaiSocial: </color>: " + msg);
    }

    private void BonsaiLogError(string msg)
    {
        Debug.LogError("<color=orange>BonsaiSocial: </color>: " + msg);
    }
}