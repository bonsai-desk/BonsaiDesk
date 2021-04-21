using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

public class LaunchManager : MonoBehaviour
{
    public static Vector3 LogoLocalPosition;
    public static Quaternion LogoLocalRotation;

    public Transform centerEyeAnchor;
    public Transform logo;

    private bool _run;

    private void Start()
    {
        StartCoroutine(LoadAsync());
    }

    private void Update()
    {
        if (!_run && centerEyeAnchor.transform.eulerAngles.sqrMagnitude != 0)
        {
            _run = true;

            var forward = centerEyeAnchor.forward;
            forward.y = 0;
            forward = forward.normalized;

            forward *= 3f;

            logo.position = centerEyeAnchor.transform.position + forward;
            logo.rotation = Quaternion.LookRotation(forward);
        }
        
        // LogoLocalPosition = centerEyeAnchor.InverseTransformPoint(logo.position);
        LogoLocalPosition = centerEyeAnchor.position - logo.position;
        // LogoLocalRotation = Quaternion.Inverse(centerEyeAnchor.rotation) * logo.transform.rotation;
    }

    private IEnumerator LoadAsync()
    {
        yield return new WaitForSeconds(3);
        var op = SceneManager.LoadSceneAsync(1);
        
        while (!op.isDone)
        {
            Debug.Log(op.progress);
            yield return null;
        }
    }
}