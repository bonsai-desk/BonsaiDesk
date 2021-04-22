using System.Collections;
using UnityEngine;

public class LogoMove : MonoBehaviour
{
    public OVROverlay overlay;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void FadeOut()
    {
        Debug.Log("Fade Out");
        StartCoroutine(DelayFade());
    }

    private IEnumerator DelayFade()
    {
        yield return new WaitForSeconds(0.25f);
        overlay.enabled = false;
    }
}