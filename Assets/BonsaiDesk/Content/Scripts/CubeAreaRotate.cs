using UnityEngine;

public class CubeAreaRotate : MonoBehaviour
{
    private int rotation = 0;

    // Start is called before the first frame update
    //   private void Start()
    //   {
    //   }
    //
    //   // Update is called once per frame
    //   private void Update()
    //   {
    //   }

    public void RotateLeft()
    {
        if (!LeanTween.isTweening(gameObject))
        {
            rotation++;
            if (rotation > 3)
                rotation = 0;
            LeanTween.rotateY(gameObject, rotation * 90f, 0.5f);
        }
    }

    public void RotateRight()
    {
        if (!LeanTween.isTweening(gameObject))
        {
            rotation--;
            if (rotation < 0)
                rotation = 3;
            LeanTween.rotateY(gameObject, rotation * 90f, 0.5f);
        }
    }
}