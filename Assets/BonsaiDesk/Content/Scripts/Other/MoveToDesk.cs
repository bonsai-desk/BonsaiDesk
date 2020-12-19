using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Mirror;

public class MoveToDesk : MonoBehaviour
{
    public Transform tableParent;
    private List<PlayerOrientation> playerOrientations;
    private bool orientatingSelf = false;
    private bool _oriented;

    private bool oriented
    {
        get { return _oriented; }
        set
        {
            _oriented = value;

            foreach (NetworkConnection conn in NetworkServer.connections.Values)
            {
                if (conn.connectionId != NetworkConnection.LocalConnectionId)
                {
                    foreach (NetworkIdentity obj in conn.clientOwnedObjects)
                    {
                        obj.gameObject.SetActive(value);
                    }
                }
            }
        }
    }

    private float? lastHandDistance = null;

    public Transform oVRCameraRig;
    public Transform centerEyeAnchor;

    //bool fixedTableHeight = false;

    public GameObject blackOverlay;
    private Vector3 oVRCameraRigStartPosition;
    private Quaternion oVRCameraRigStartRotation;
    public GameObject instructions;

    public TextMeshProUGUI instructionsText;
    private string[] instructionsTexts;
    public GameObject animationObject;

    public Animator animator;
    private int state = 0;
    public Transform leftAnimationHand;
    public Transform rightAnimationHand;

    public Transform tableEdge;

    private Vector3? calculatedTablePosition = null;
    private Quaternion? calculatedTableRotation = null;
    private float? calculatedCenterEyeAnchor = null;

    public Transform tableControls;

    public void SetTableEdge(Transform tableEdge)
    {
        this.tableEdge = tableEdge;
        if (tableEdge != null && tableControls != null)
        {
            tableControls.position = tableEdge.position;
            tableControls.rotation = tableEdge.rotation;
            GetComponent<DeskController>().UpdateHolePositionsInShader();
        }
        ApplyCalculatedTableOrientation(false);
    }

    private void Start()
    {
        oriented = false;
        oVRCameraRigStartPosition = oVRCameraRig.position;
        oVRCameraRigStartRotation = oVRCameraRig.rotation;

        instructionsTexts = new string[] { instructionsText.text, "Place hands flat on table\nwith thumbs on the edge", "Move hands apart quickly" };

        //if (Application.isEditor)
        //    fixedTableHeight = true;
        //else
        //    fixedTableHeight = false;

        playerOrientations = new List<PlayerOrientation>();

        ResetPosition();

        OVRManager.HMDUnmounted += ResetPosition;
    }

    public void ResetPosition()
    {
        oriented = false;
        blackOverlay.SetActive(true);
        instructions.SetActive(true);
        animationObject.SetActive(true);
        oVRCameraRig.position = oVRCameraRigStartPosition;
        oVRCameraRig.rotation = oVRCameraRigStartRotation;
        instructions.transform.position = blackOverlay.transform.position;
        instructions.transform.rotation = Quaternion.LookRotation(blackOverlay.transform.forward, Vector3.up);
    }

    private void Update()
    {
        if (!oriented)
        {
            float distance = Vector3.Distance(instructions.transform.position, blackOverlay.transform.position);
            instructions.transform.position = Vector3.MoveTowards(instructions.transform.position, blackOverlay.transform.position, Mathf.Max(0.25f * Time.deltaTime, distance * Time.deltaTime / 1.5f));

            float angle = Vector3.Angle(instructions.transform.forward, blackOverlay.transform.forward);
            Vector3 newForward = Quaternion.RotateTowards(instructions.transform.rotation, blackOverlay.transform.rotation, Mathf.Max(10f * Time.deltaTime, angle * Time.deltaTime / 1.5f)) * Vector3.forward;
            instructions.transform.rotation = Quaternion.LookRotation(newForward, Vector3.up);

            leftAnimationHand.localPosition = new Vector3(-rightAnimationHand.localPosition.x, rightAnimationHand.localPosition.y, rightAnimationHand.localPosition.z);
            leftAnimationHand.localRotation = new Quaternion(rightAnimationHand.localRotation.x * -1.0f, rightAnimationHand.localRotation.y,
                rightAnimationHand.localRotation.z, rightAnimationHand.localRotation.w * -1.0f);
            leftAnimationHand.Rotate(180f, 0, 0);
        }
    }

    private void UpdateState(int newState)
    {
        if (state != newState)
        {
            state = newState;
            animator.SetInteger("State", state);
            animator.SetTrigger("ChangeState");
            instructionsText.text = instructionsTexts[state];
        }
    }

    private void LateUpdate()
    {
        if (!PlayerHands.hands.Tracking())
        {
            orientatingSelf = false;
            UpdateState(0);
        }
        else
        {
            Vector3 oVRCameraRigPosition = oVRCameraRig.position;
            Quaternion oVRCameraRigRotation = oVRCameraRig.rotation;

            //move player position and rotation to the desk
            Vector3 thumbDifference = PlayerHands.hands.fingerTipPositions[0] - PlayerHands.hands.fingerTipPositions[5];
            float angle = Mathf.Atan2(thumbDifference.x, thumbDifference.z) * Mathf.Rad2Deg + 90f;
            Quaternion angleRotation = Quaternion.AngleAxis(-angle, Vector3.up);
            Vector3 averageThumbPosition = (PlayerHands.hands.fingerTipPositions[0] + PlayerHands.hands.fingerTipPositions[5]) / 2f;

            oVRCameraRigPosition += new Vector3(-averageThumbPosition.x, 0, -averageThumbPosition.z);
            oVRCameraRigPosition = angleRotation * oVRCameraRigPosition;
            oVRCameraRigRotation = angleRotation * oVRCameraRigRotation;

            //calculate desk height
            float averageHeight = 0;
            float min = float.MaxValue;
            float max = float.MinValue;
            for (int i = 1; i < 5; i++)
            {
                averageHeight += PlayerHands.hands.left.fingerTips[i].position.y;
                averageHeight += PlayerHands.hands.right.fingerTips[i].position.y;
                if (PlayerHands.hands.left.fingerTips[i].position.y < min)
                    min = PlayerHands.hands.left.fingerTips[i].position.y;
                if (PlayerHands.hands.left.fingerTips[i].position.y > max)
                    max = PlayerHands.hands.left.fingerTips[i].position.y;
                if (PlayerHands.hands.right.fingerTips[i].position.y < min)
                    min = PlayerHands.hands.right.fingerTips[i].position.y;
                if (PlayerHands.hands.right.fingerTips[i].position.y > max)
                    max = PlayerHands.hands.right.fingerTips[i].position.y;
            }
            float diff = max - min;
            averageHeight /= 8f;
            averageHeight -= 0.0025f;

            float thumbDistance = thumbDifference.magnitude;
            float speed = 0;
            if (lastHandDistance != null)
                speed = (thumbDistance - (float)lastHandDistance) / Time.deltaTime;
            lastHandDistance = thumbDistance;

            if (!oriented)
            {
                if (diff < 0.025f)
                    UpdateState(2);
                else
                    UpdateState(1);
            }

            if (speed > 0.75f && diff < 0.025f)
            {
                if (!orientatingSelf)
                {
                    orientatingSelf = true;
                    playerOrientations.Clear();
                }

                PlayerOrientation orientation;
                orientation.oVRCameraRigPosition = oVRCameraRigPosition;
                orientation.averageHeight = averageHeight;
                orientation.oVRCameraRigRotation = oVRCameraRigRotation;
                orientation.thumbDistance = thumbDistance;
                playerOrientations.Add(orientation);
            }
            else
            {
                orientatingSelf = false;
                if (playerOrientations.Count >= 4)
                {
                    float totalDistance = playerOrientations[playerOrientations.Count - 1].thumbDistance - playerOrientations[0].thumbDistance;
                    if (totalDistance >= 0.1f)
                    {
                        int start = Mathf.RoundToInt(playerOrientations.Count * 0.25f) + 1;
                        int end = Mathf.RoundToInt(playerOrientations.Count * 0.75f) - 1;
                        Vector3 averagePosition = new Vector3(0, 0, 0);
                        Queue<Quaternion> rotations = new Queue<Quaternion>();
                        float averageAverageHeight = 0;
                        float numOrientations = 0;
                        for (int i = start; i <= end; i++)
                        {
                            numOrientations++;
                            averagePosition += playerOrientations[i].oVRCameraRigPosition;
                            rotations.Enqueue(playerOrientations[i].oVRCameraRigRotation);
                            averageAverageHeight += playerOrientations[i].averageHeight;
                        }
                        averagePosition /= numOrientations;
                        averageAverageHeight /= numOrientations;

                        //if (fixedTableHeight)
                        averagePosition += new Vector3(0, -averageAverageHeight + tableParent.position.y, 0);
                        //else
                        //    tableParent.position = new Vector3(0, averageAverageHeight, 0);

                        Quaternion averageRotation = AverageQuaternion(rotations);

                        calculatedTablePosition = averagePosition;
                        calculatedTableRotation = averageRotation;

                        ApplyCalculatedTableOrientation(true);

                        playerOrientations.Clear();

                        blackOverlay.SetActive(false);
                        instructions.SetActive(false);
                        animationObject.SetActive(false);
                        oriented = true;
                    }
                }
            }
        }
    }

    private void ApplyCalculatedTableOrientation(bool updateCenterEyeAnchor)
    {
        if (calculatedTablePosition == null || calculatedTableRotation == null || (calculatedCenterEyeAnchor == null && !updateCenterEyeAnchor))
            return;

        oVRCameraRig.position = calculatedTablePosition.Value;
        oVRCameraRig.rotation = calculatedTableRotation.Value;

        //center horizontally based on head position
        if (updateCenterEyeAnchor)
            calculatedCenterEyeAnchor = centerEyeAnchor.position.x;
        oVRCameraRig.position += new Vector3(-calculatedCenterEyeAnchor.Value, 0, 0);

        if (tableEdge != null)
        {
            oVRCameraRig.RotateAround(Vector3.zero, Vector3.up, tableEdge.eulerAngles.y);
            oVRCameraRig.position += tableEdge.position;
        }
        else
            Debug.LogError("No table edge");
    }

    private Quaternion AverageQuaternion(Queue<Quaternion> quaternions)
    {
        Vector4 avgr = Vector4.zero;
        foreach (Quaternion quaternion in quaternions)
        {
            Math3d.AverageQuaternion(ref avgr, quaternion, quaternions.Peek(), quaternions.Count);
        }
        return new Quaternion(avgr.x, avgr.y, avgr.z, avgr.w);
    }

    private struct PlayerOrientation
    {
        public Vector3 oVRCameraRigPosition;
        public float averageHeight;
        public Quaternion oVRCameraRigRotation;
        public float thumbDistance;
    }
}