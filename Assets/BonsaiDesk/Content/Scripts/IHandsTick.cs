using System.Collections;
using System.Collections.Generic;
using UnityEngine;

interface IHandsTick
{
    PlayerHand leftPlayerHand { get; set; }
    PlayerHand rightPlayerHand { get; set; }
    
    void Tick();
}