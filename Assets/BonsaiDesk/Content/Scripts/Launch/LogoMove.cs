using UnityEngine;
using UnityEngine.SceneManagement;

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
        overlay.enabled = false;
    }
}