using System.Collections;
using System.Collections.Generic;
using UnityEngine;

interface IHandTick
{
    PlayerHand playerHand { get; set; }

    void Tick();
}