using Mirror;
using UnityEngine;

public class BlockPhysics : MonoBehaviour
{
    private NetworkIdentity networkIdentity;

    private Rigidbody body;

    // public int id = 0;

    public float lbsForce = 70f;
    public float lbsTorque = 10f;

    private float moveForce;
    private float rotationTorque;

    private const float lbsToKg = 0.45359237f;

    private Vector3 position;
    private Quaternion rotation;

    private Transform initialParent;
    private bool lastInCubeArea = false;

    private bool touchingHand = false;

    private BlockArea myBlockArea;

    public bool debug = false;

    private int layer;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("LeftHand") || collision.gameObject.layer == LayerMask.NameToLayer("RightHand"))
        {
            touchingHand = true;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("LeftHand") || collision.gameObject.layer == LayerMask.NameToLayer("RightHand"))
        {
            touchingHand = true;
        }
    }

    private void Start()
    {
        networkIdentity = GetComponent<NetworkIdentity>();
        myBlockArea = GetComponent<BlockArea>();

        body = GetComponent<Rigidbody>();
        body.maxAngularVelocity = float.MaxValue;

        moveForce = lbsForce * lbsToKg * 9.81f;
        rotationTorque = lbsTorque * lbsToKg * 9.81f;

        initialParent = transform.parent;
    }

    private void FixedUpdate()
    {
        if (!networkIdentity.hasAuthority)
            return;

        if ((PlayerHands.hands.right.heldBody != null && PlayerHands.hands.right.heldBody.gameObject == gameObject) ||
            (PlayerHands.hands.left.heldBody != null && PlayerHands.hands.left.heldBody.gameObject == gameObject))
            touchingHand = true;

        BlockArea blockArea = null;
        Transform blockAreaTransform = null;
        bool isInCubeArea = false;
        bool isNearHole = false;
        foreach (NetworkIdentity nextOwnedObject in BlockArea.blockAreaIdentities)
        {
            if (!nextOwnedObject.hasAuthority)
                continue;
            BlockArea nextBlockArea = nextOwnedObject.GetComponent<BlockArea>();
            if (nextBlockArea != null && nextBlockArea.active && nextBlockArea != myBlockArea)
            {
                blockArea = nextBlockArea;
                blockAreaTransform = nextBlockArea.transform;
                Vector3 coord = Vector3.zero;
                foreach (var block in myBlockArea.blocks)
                    coord = block.Key;
                Vector3 blockPosition = transform.TransformPoint(coord * BlockArea.cubeScale);
                Vector3 positionLocalToCubeArea = blockAreaTransform.InverseTransformPoint(blockPosition);
                Vector3Int blockCoord = blockArea.GetBlockCoord(positionLocalToCubeArea);
                var inArea = blockArea.InCubeArea(blockCoord, myBlockArea.blocks[myBlockArea.OnlyBlock()].id);
                if (inArea.isNearHole)
                    isNearHole = true;
                isInCubeArea = inArea.isInCubeArea;
                isInCubeArea = isInCubeArea && ((transform.parent == initialParent && touchingHand) || transform.parent == blockAreaTransform);
                if (isInCubeArea)
                {
                    Vector3 bearingOffset = Vector3.zero;
                    if (blockArea.blocks.TryGetValue(blockCoord, out BlockArea.MeshBlock block))
                    {
                        if (Blocks.blocks[block.id].blockType == Block.BlockType.bearing)
                        {
                            bearingOffset = blockArea.transform.rotation * BlockArea.IntToQuat(block.forward, block.up) * Vector3.up * 0.1f * BlockArea.cubeScale;
                        }
                    }
                    position = blockArea.BlockCoordToPosition(blockCoord) + bearingOffset;
                    rotation = blockArea.GetTargetRotation(transform.rotation, blockCoord, Blocks.blocks[myBlockArea.blocks[myBlockArea.OnlyBlock()].id].blockType);

                    if (AboutEquals(blockPosition, position) && AboutEquals(transform.rotation, rotation))
                    {
                        // blockArea.lockBlocks();
                        transform.parent = initialParent;
                        body.useGravity = true;
                        myBlockArea.CmdSetActive(true);

                        transform.GetChild(1).gameObject.layer = LayerMask.NameToLayer("blockArea");
                        if (layer == -1)
                            layer = GetLayerRecursively(transform.GetChild(3).gameObject);
                        if (layer == 0)
                            SetLayerRecursively(transform.GetChild(3).gameObject, LayerMask.NameToLayer("blockArea"));
                        else
                            SetLayerRecursively(transform.GetChild(3).gameObject, layer);

                        blockArea.LockBlock(gameObject);
                        return;
                    }

                    //float distance = Vector3.Distance(transform.position, position);
                    // float scale = distance / (BlockArea.cubeScale / 2f);
                    // scale = 1 - scale;
                    // scale = BlockArea.cubeScale / 2f + BlockArea.cubeScale / 2f * scale;
                    // transform.localScale = new Vector3(scale, scale, scale);
                    AddForceTowardsTarget(blockPosition);
                    AddTorqueTowardsTarget();
                    break;
                }
            }
        }

        if ((isInCubeArea && isNearHole) || (isNearHole && touchingHand))
        {
            transform.GetChild(1).gameObject.layer = LayerMask.NameToLayer("block");
            bool sphere = true;
            foreach (var block in myBlockArea.blocks)
                sphere = Blocks.blocks[block.Value.id].hasSphere;
            if (sphere)
                SetLayerRecursively(transform.GetChild(3).gameObject, LayerMask.NameToLayer("block"));
        }
        else if (isInCubeArea && !touchingHand && Blocks.blocks[myBlockArea.blocks[myBlockArea.OnlyBlock()].id].blockType == Block.BlockType.bearing)
        {
            transform.GetChild(1).gameObject.layer = LayerMask.NameToLayer("onlyHands");
            bool sphere = true;
            foreach (var block in myBlockArea.blocks)
                sphere = Blocks.blocks[block.Value.id].hasSphere;
            if (sphere)
                SetLayerRecursively(transform.GetChild(3).gameObject, LayerMask.NameToLayer("onlyHands"));
        }
        else
        {
            transform.GetChild(1).gameObject.layer = LayerMask.NameToLayer("blockArea");
            if (layer == -1)
                layer = GetLayerRecursively(transform.GetChild(3).gameObject);
            if (layer == 0)
                SetLayerRecursively(transform.GetChild(3).gameObject, LayerMask.NameToLayer("blockArea"));
            else
                SetLayerRecursively(transform.GetChild(3).gameObject, layer);
        }

        if (isInCubeArea)
        {
            body.useGravity = false;
        }
        else
        {
            body.useGravity = true;
        }

        if (isInCubeArea != lastInCubeArea)
        {
            if (isInCubeArea)
            {
                if (blockAreaTransform != null)
                {
                    transform.parent = blockAreaTransform;
                    myBlockArea.CmdSetActive(false);
                }
                else
                    Debug.LogError("This should never print.");
            }
            else
            {
                transform.parent = initialParent;
                myBlockArea.CmdSetActive(true);
                // transform.localScale = new Vector3(BlockArea.cubeScale, BlockArea.cubeScale, BlockArea.cubeScale);
                if (blockAreaTransform != null)
                {
                    if (blockArea != null)
                        blockArea.ResetBounds();
                }
                else
                    Debug.LogError("This should never print.");
            }
        }
        lastInCubeArea = isInCubeArea;
        touchingHand = false;
    }

    private void OnEnable()
    {
        layer = -1;
        // print("c: " + transform.GetChild(3).childCount);
        // layer = getLayerRecursively(transform.GetChild(3).gameObject);
        // print(layer);
        // print(LayerMask.LayerToName(layer));
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    private int GetLayerRecursively(GameObject obj)
    {
        if (obj.layer != 0)
            return obj.layer;

        foreach (Transform child in obj.transform)
        {
            int layer = GetLayerRecursively(child.gameObject);
            if (layer != 0)
                return layer;
        }

        return 0;
    }

    private bool AboutEquals(Vector3 v1, Vector3 v2)
    {
        //+ or - 1mm
        float tolerance = 0.001f;
        float distance = (v2.x - v1.x) * (v2.x - v1.x) + (v2.y - v1.y) * (v2.y - v1.y) + (v2.z - v1.z) * (v2.z - v1.z);
        return distance < tolerance * tolerance;
    }

    private bool AboutEquals(Quaternion q1, Quaternion q2)
    {
        //+ or - 1 degree
        return Mathf.Abs(Quaternion.Dot(q1, q2)) > 0.98888889f;
    }

    // private void ResetToTarget()
    // {
    //     body.velocity = Vector3.zero;
    //     body.angularVelocity = Vector3.zero;
    //     body.MovePosition(position);
    //     body.MoveRotation(rotation);
    // }

    private void AddForceTowardsTarget(Vector3 blockPosition)
    {
        Vector3 towards = position - blockPosition;

        //speed is maintained, but direction is changed to be towards target position (not realistic)
        body.velocity = towards.normalized * body.velocity.magnitude;

        //targetForce is the force required to get a velocity that would make the object get to the target position this update tick
        Vector3 targetVelocity = towards / Time.deltaTime;
        Vector3 velocityDifference = targetVelocity - body.velocity;
        Vector3 targetForce = velocityDifference * body.mass / Time.deltaTime / 2f;
        float distance = Vector3.Distance(blockPosition, position);

        //if already at target or have enough velocity to overshoot target this update tick, allow unlimited force to decelerate (not realistic)
        if (Mathf.Approximately(distance, 0) || body.velocity.magnitude * Time.deltaTime > distance || Mathf.Approximately(moveForce, 0))
            body.AddForce(targetForce, ForceMode.Force);
        else //clamp target force to moveForce
            body.AddForce(Vector3.ClampMagnitude(targetForce, moveForce), ForceMode.Force);
    }

    private void AddTorqueTowardsTarget()
    {
        var delta = rotation * Quaternion.Inverse(body.rotation);

        delta.ToAngleAxis(out float angle, out Vector3 axis);

        // We get an infinite axis in the event that our rotation is already aligned.
        // allow instant deceleration to stop rotation (not realistic)
        if (float.IsInfinity(axis.x))
        {
            body.angularVelocity = Vector3.zero;
        }
        else
        {
            if (angle > 180f)
                angle -= 360f;

            Vector3 targetAngularVelocity = axis.normalized * Mathf.Deg2Rad * angle / Time.deltaTime;

            //angular speed is maintained, but direction is changed to be towards target rotation (not realistic)
            body.angularVelocity = targetAngularVelocity.normalized * body.angularVelocity.magnitude;

            Vector3 angularVelocityDifference = targetAngularVelocity - body.angularVelocity;

            Quaternion q = transform.rotation * body.inertiaTensorRotation;
            Vector3 targetTorque = q * Vector3.Scale(body.inertiaTensor, (Quaternion.Inverse(q) * angularVelocityDifference)) / Time.deltaTime; // / 2f;

            //if already at target, allow unlimited torque to decelerate (not realistic)
            if (Mathf.Approximately(angle, 0) || Mathf.Approximately(rotationTorque, 0))
                body.AddTorque(targetTorque, ForceMode.Force);
            else //clamp target torque to rotationForce
                body.AddTorque(Vector3.ClampMagnitude(targetTorque, rotationTorque), ForceMode.Force);
        }
    }

    // public static Vector3 TransformPointUnscaled(Transform transformIn, Vector3 position)
    // {
    //     var localToWorldMatrix = Matrix4x4.TRS(transformIn.position, transformIn.rotation, Vector3.one);
    //     return localToWorldMatrix.MultiplyPoint3x4(position);
    // }

    // public static Vector3 InverseTransformPointUnscaled(Transform transformIn, Vector3 position)
    // {
    //     var worldToLocalMatrix = Matrix4x4.TRS(transformIn.position, transformIn.rotation, Vector3.one).inverse;
    //     return worldToLocalMatrix.MultiplyPoint3x4(position);
    // }
}