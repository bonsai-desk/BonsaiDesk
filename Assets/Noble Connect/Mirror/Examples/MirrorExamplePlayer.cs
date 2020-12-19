using UnityEngine;
using Mirror;

// A super simple example player. Use arrow keys to move
public class MirrorExamplePlayer : NetworkBehaviour
{
    void Update()
    {
        if (!isLocalPlayer) return;

        Vector3 dir = Vector3.zero;

        if (Input.GetKey(KeyCode.UpArrow)) dir = Vector3.up;
        else if (Input.GetKey(KeyCode.DownArrow)) dir = Vector3.down;
        else if (Input.GetKey(KeyCode.LeftArrow)) dir = Vector3.left;
        else if (Input.GetKey(KeyCode.RightArrow)) dir = Vector3.right;

        transform.position += dir * Time.deltaTime * 5;
    }

}