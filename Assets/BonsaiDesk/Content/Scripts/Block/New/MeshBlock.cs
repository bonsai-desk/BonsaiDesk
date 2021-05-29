using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshBlock
{
    public int positionInList;
    public float health;

    public int framesSinceLastDamage;
    public GameObject blockGameObject;
    public Material material;
    // public Joint connected;

    public MeshBlock(int positionInList, GameObject blockGameObject, Material material)
    {
        this.positionInList = positionInList;
        health = 1;
        framesSinceLastDamage = 100;

        this.blockGameObject = blockGameObject;
        this.material = material;

        // this.connected = connected;
    }
}