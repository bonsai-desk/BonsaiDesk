using Mirror;

public class ServerVideoSync : NetworkBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
        InvokeRepeating(nameof(SyncUpdate), 0f, 0.1f);
    }

    private void SyncUpdate()
    {
        switch (NetworkManagerGame.Singleton.videoState)
        {
            case NetworkManagerGame.VideoState.None:
                break;

            case NetworkManagerGame.VideoState.Cued:
                bool readyToPlay = true;
                foreach (var player in NetworkManagerGame.Singleton.PlayerInfos)
                {
                    if (player.Value.youtubePlayerState != 5)
                        readyToPlay = false;
                }
                if (readyToPlay)
                {
                    NetworkManagerGame.Singleton.videoState = NetworkManagerGame.VideoState.Playing;
                    NetworkServer.SendToAll(new NetworkManagerGame.ActionMessage()
                    {
                        ActionId = 0
                    });
                }
                break;

            case NetworkManagerGame.VideoState.Playing:
                // float acceptableDifference = 1f;
                // float min = Mathf.Infinity;
                // foreach (var player in NetworkManagerGame.singleton.playerInfo)
                // {
                //     if (player.Value.youtubePlayerCurrentTime < min)
                //         min = player.Value.youtubePlayerCurrentTime;
                // }
                // foreach (var player in NetworkManagerGame.singleton.playerInfo)
                // {
                //     if (player.Value.youtubePlayerCurrentTime > min + acceptableDifference)
                //     {
                //         player.Key.Send(new NetworkManagerGame.ActionMessage()
                //         {
                //             actionId = 2
                //         });
                //     }
                //     else
                //     {
                //         player.Key.Send(new NetworkManagerGame.ActionMessage()
                //         {
                //             actionId = 1
                //         });
                //     }
                // }
                break;

            default:
                break;
        }
    }
}