using TMPro;
using UnityEngine;

public class DebugText : MonoBehaviour
{
    public TextMeshProUGUI text;
    public static string textString;

    //   private void Start()
    //   {
    //   }

    private void Update()
    {
        text.text = textString;
    }
}