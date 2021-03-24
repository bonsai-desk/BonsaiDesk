using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuTutorial : MonoBehaviour
{
    private void Start()
    {
        if (SaveSystem.Instance.BoolPairs.TryGetValue("FinishedMenuTutorial", out var finished))
        {
            gameObject.SetActive(!finished);
        }
    }
    
    public void DisableSelf()
    {
        gameObject.SetActive(false);
        SaveSystem.Instance.BoolPairs["FinishedMenuTutorial"] = true;
        SaveSystem.Instance.Save();
    }
}