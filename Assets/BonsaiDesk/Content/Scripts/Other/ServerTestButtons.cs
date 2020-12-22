using UnityEngine;

public class ServerTestButtons : MonoBehaviour
{
    // Start is called before the first frame update
    //   private void Start()
    //   {
    //   }
    //
    //   // Update is called once per frame
    //   private void Update()
    //   {
    //   }

    public void PrintPlayerInfo()
    {
        print("----------");
        bool first = true;
        foreach (var player in NetworkManagerGame.singleton.playerInfos)
        {
            if (!first)
                print("---");
            print(player.Value.spot);
            print(player.Value.youtubePlayerState);
            print(player.Value.youtubePlayerCurrentTime);
            first = false;
        }
    }
}