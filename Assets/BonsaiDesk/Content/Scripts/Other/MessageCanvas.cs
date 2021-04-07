using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageCanvas : MonoBehaviour
{
    public Canvas canvas;
    public TextMeshProUGUI textMesh;

    public Image img1;
    public Image img2;
    private bool _shouldDestroy;
    private float _t;

    public void Update()
    {
        var target = _shouldDestroy ? 1f : 0f;
        _t = Mathf.MoveTowards(_t, target, Time.deltaTime / 0.25f);
        var opacity = 1 - CubicBezier.EaseOut.Sample(_t);
        textMesh.alpha = opacity;
        img1.color = new Color(img1.color.r, img1.color.g, img1.color.b, opacity);
        img2.color = new Color(img2.color.r, img2.color.g, img2.color.b, opacity);

        if (_shouldDestroy && Mathf.Approximately(_t, 1))
        {
            Destroy(gameObject);
        }
    }

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

    public void SelfDestruct()
    {
        _shouldDestroy = true;
    }

    public bool IsDestructing()
    {
        return _shouldDestroy;
    }
}