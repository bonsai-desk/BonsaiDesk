using UnityEngine;

public class PinchSpawn : MonoBehaviour
{
    public GameObject blockPrefab;

    private Transform[] fingernails;
    private float[] lastSpawnTime = new float[] {0, 0};

    public int leftSelectedBlockid = 0;
    public int rightSelectedBlockid = 0;

    private void Start()
    {
        fingernails = new Transform[4];
        fingernails[0] = GameObject.Find("l_thumb_fingernail_marker").transform;
        fingernails[1] = GameObject.Find("l_index_fingernail_marker").transform;
        fingernails[2] = GameObject.Find("r_thumb_fingernail_marker").transform;
        fingernails[3] = GameObject.Find("r_index_fingernail_marker").transform;
    }

    // void Update()
    // {
    //     if (PlayerHands.hands.menuUsedBy == OVRSkeleton.SkeletonType.None)
    //     {
    //         //checkPinch(PlayerHands.hands.left);
    //         checkPinch(PlayerHands.hands.right);
    //     }
    // }

    // void checkPinch(PlayerHand hand)
    // {
    //     int handIndex = 0;
    //     if (hand._skeletonType == OVRSkeleton.SkeletonType.HandRight)
    //         handIndex = 1;
    //     if (hand.heldJoint == null && hand.pinching() && Time.time - lastSpawnTime[handIndex] > 0.1f)
    //     {
    //         lastSpawnTime[handIndex] = Time.time;

    //         GameObject block = Instantiate(blockPrefab);
    //         if (hand._skeletonType == OVRSkeleton.SkeletonType.HandLeft)
    //             block.GetComponent<BlockPhysics>().id = leftSelectedBlockid;
    //         else
    //             block.GetComponent<BlockPhysics>().id = rightSelectedBlockid;

    //         Transform f1 = fingernails[handIndex * 2];
    //         Transform f2 = fingernails[handIndex * 2 + 1];

    //         block.transform.position = Vector3.Lerp(f1.position, f2.position, 0.5f);

    //         block.transform.rotation = Quaternion.LookRotation(f2.position - f1.position, f2.right);
    //         block.transform.localScale = new Vector3(BlockArea.cubeScale, BlockArea.cubeScale, 0.001f);

    //         hand.connectBody(block.GetComponent<Rigidbody>());

    //         BlockArea.lockAllBlocks();

    //         WidgetManager.addWidget(block);
    //     }
    // }
}