using UnityEngine;

public class BlockSelectTouch : MonoBehaviour
{
    public int blockId = 0;
    public BlockSelect blockSelect;

    // Start is called before the first frame update
    //   private void Start()
    //   {
    //   }
    //
    //   // Update is called once per frame
    //   private void Update()
    //   {
    //   }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("IndexTip"))
        {
            blockSelect.SelectBlock(blockId);
        }
    }
}