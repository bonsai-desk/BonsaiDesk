using UnityEngine;

public class TestButtons : MonoBehaviour
{
    public GameObject browserObject;

    public void ToggleBrowserObject()
    {
        browserObject.SetActive(!browserObject.activeSelf);
    }

    public void Cue()
    {
        BrowserManager.instance.CueVideo("HmZKgaHa3Fg");
    }

    public void Resume()
    {
        BrowserManager.instance.ResumeVideo();
    }

    public void Pause()
    {
        BrowserManager.instance.PauseVideo();
    }
}