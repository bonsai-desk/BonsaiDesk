using System.Collections;
using System.Collections.Generic;
using UnityEngine;

interface IHandsTick
{
    void Tick(PlayerHand leftPlayerHand, PlayerHand rightPlayerHand);
}