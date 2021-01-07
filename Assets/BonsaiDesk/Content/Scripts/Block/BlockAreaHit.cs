using Mirror;
using UnityEngine;

public class BlockAreaHit : MonoBehaviour
{
    private NetworkIdentity networkIdentity;

    private BlockArea cubeArea;
    public GameObject blockPrefab;

    private void Start()
    {
        networkIdentity = GetComponent<NetworkIdentity>();
        cubeArea = transform.GetComponent<BlockArea>();
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!(networkIdentity.isServer && networkIdentity.connectionToClient == null || networkIdentity.hasAuthority))
            return;

        if (collision.contactCount > 0)
        {
            bool left = (PlayerHands.hands.left.deleteMode || PlayerHands.hands.left.deleteAllMode) && collision.gameObject.CompareTag("PointerTip") && collision.gameObject.layer == LayerMask.NameToLayer("LeftHand");
            bool right = (PlayerHands.hands.right.deleteMode || PlayerHands.hands.right.deleteAllMode) && collision.gameObject.CompareTag("PointerTip") && collision.gameObject.layer == LayerMask.NameToLayer("RightHand");
            if (left || right)
            {
                ContactPoint contact = collision.GetContact(0);
                Vector3 blockPosition = contact.point;
                if (!contact.thisCollider.CompareTag("Bearing"))
                    blockPosition += contact.normal * (BlockArea.cubeScale / 2f);
                else
                    blockPosition += contact.normal * (BlockArea.cubeScale * 0.025f);
                Vector3 positionLocalToCubeArea = cubeArea.transform.InverseTransformPoint(blockPosition);
                Vector3Int blockCoord = cubeArea.GetBlockCoord(positionLocalToCubeArea);
                if (cubeArea.blocks.ContainsKey(blockCoord))
                {
                    float timeToBreak = 0.575f;
                    float timeToBreakAll = 1.25f;
                    if (left && PlayerHands.hands.left.deleteMode || right && PlayerHands.hands.right.deleteMode)
                        cubeArea.DamageBlock(blockCoord, Time.deltaTime * (1f / timeToBreak));
                    if (left && PlayerHands.hands.left.deleteAllMode || right && PlayerHands.hands.right.deleteAllMode)
                    {
                        if (cubeArea.blocks.Count > 1)
                            cubeArea.DamageAll(collision.contacts[0].point, Time.deltaTime * (1f / timeToBreakAll));
                        else
                            cubeArea.DamageBlock(blockCoord, Time.deltaTime * (1f / timeToBreak));
                    }
                }
            }
        }
    }
}