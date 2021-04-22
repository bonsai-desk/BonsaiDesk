using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.SceneManagement;

public class LaunchManager : MonoBehaviour
{
    public static LaunchManager Instance;
    public Transform centerEyeAnchor;
    public Transform logo;
    public LogoMove logoMove;

    private bool _run;

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }

        StartCoroutine(LoadAsync());
    }

    private void Update()
    {
        if (!_run && centerEyeAnchor.transform.eulerAngles.sqrMagnitude != 0)
        {
            _run = true;
            AlignWithView();
        }
    }

    private void AlignWithView()
    {
        var forward = centerEyeAnchor.forward;
        forward.y = 0;
        forward = forward.normalized;

        forward *= 3f;

        logo.position = centerEyeAnchor.transform.position + forward;
        logo.rotation = Quaternion.LookRotation(-forward);
    }

    private IEnumerator LoadAsync()
    {
        var op = SceneManager.LoadSceneAsync(1);

        while (!op.isDone)
        {
            yield return null;
        }
    }
}