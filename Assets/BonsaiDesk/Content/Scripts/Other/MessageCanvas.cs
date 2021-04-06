using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class MessageCanvas : MonoBehaviour
{
    public Canvas canvas;
    public TextMeshProUGUI textMesh;

    public void SetText(string text)
    {
        textMesh.text = text;
    }

    public void SetEnabled(bool shouldBe)
    {
        canvas.enabled = shouldBe;
    }

    public bool IsEnabled()
    {
        return canvas.enabled;
    }
}