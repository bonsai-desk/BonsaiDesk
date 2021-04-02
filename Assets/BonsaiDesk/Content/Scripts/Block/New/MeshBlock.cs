using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshBlock
{
    public int positionInList;
    public float health;

    public int framesSinceLastDamage;
    // public GameObject blockObject;
    // public MeshRenderer meshRenderer;
    // public Joint connected;

    public MeshBlock(int positionInList)
    {
        this.positionInList = positionInList;
        health = 1;
        framesSinceLastDamage = 100;

        // this.damagedThisFrame = false;
        // this.framesSinceLastDamage = 0;
        // this.blockObject = blockObject;
        // this.meshRenderer = meshRenderer;
        // this.connected = connected;
    }
}