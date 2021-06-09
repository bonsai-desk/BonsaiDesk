using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PublicRoomBlock : MonoBehaviour
{
    public void Close()
    {
        
    }
    
    public void Silence()
    {
        TableBrowserMenu.Singleton.SilencePublicRoomNotifications();
        gameObject.GetComponent<AutoAuthority>().CmdDestroy();
    }
    
    public void Go()
    {
        TableBrowserMenu.Singleton.NavToPublicRooms();
        gameObject.GetComponent<AutoAuthority>().CmdDestroy();
    }
}