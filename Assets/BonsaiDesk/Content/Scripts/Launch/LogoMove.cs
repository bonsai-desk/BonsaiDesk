using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LogoMove : MonoBehaviour
{
    private void Awake()
    {
        //DontDestroyOnLoad(gameObject);
        //SceneManager.sceneLoaded += SceneLoad;
    }

    void SceneLoad(Scene scene, LoadSceneMode mode)
    {
        return;
        if (mode == LoadSceneMode.Single && SceneManager.GetActiveScene().buildIndex == 1)
        {
            var centerEyeAnchor = GameObject.Find("CenterEyeAnchor");

            // transform.position = centerEyeAnchor.transform.TransformPoint(LaunchManager.LogoLocalPosition);
            transform.position = centerEyeAnchor.transform.position +
                                 new Vector3(-LaunchManager.LogoLocalPosition.x, LaunchManager.LogoLocalPosition.y, -LaunchManager.LogoLocalPosition.z);
            // transform.rotation = centerEyeAnchor.transform.rotation * LaunchManager.LogoLocalRotation;
        }
    }
}