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
        foreach (var player in NetworkManagerGame.Singleton.PlayerInfos)
        {
            if (!first)
                print("---");
            print(player.Value.spot);
            first = false;
        }
    }
}