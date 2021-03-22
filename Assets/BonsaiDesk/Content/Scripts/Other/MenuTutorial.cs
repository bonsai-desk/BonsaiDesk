using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuTutorial : MonoBehaviour
{
    private void Start()
    {
        if (SaveSystem.Instance.GetBool("FinishedMenuTutorial"))
        {
            gameObject.SetActive(false);
        }
    }
    
    public void DisableSelf()
    {
        gameObject.SetActive(false);
        SaveSystem.Instance.SetBool("FinishedMenuTutorial", true);
        SaveSystem.Instance.Save();
    }
}