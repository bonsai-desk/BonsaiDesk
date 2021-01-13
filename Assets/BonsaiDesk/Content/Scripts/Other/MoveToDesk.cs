using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public delegate void OrientEvent(bool oriented);

public class MoveToDesk : MonoBehaviour
{
    public Transform tableParent;

    public static event OrientEvent OrientationChanged;

    public Transform oVRCameraRig;
    public Transform centerEyeAnchor;

    //bool fixedTableHeight = false;

    public GameObject blackOverlay;
    public GameObject instructions;

    public TextMeshProUGUI instructionsText;
    public GameObject animationObject;

    public Animator animator;
    public Transform leftAnimationHand;
    public Transform rightAnimationHand;

    public Transform tableEdge;

    public Transform tableControls;

    private bool _oriented;
    private float? calculatedCenterEyeAnchor;

    private Vector3? calculatedTablePosition;
    private Quaternion? calculatedTableRotation;
    private string[] instructionsTexts;

    private float? lastHandDistance;
    private bool orientatingSelf;
    private Vector3 oVRCameraRigStartPosition;
    private Quaternion oVRCameraRigStartRotation;
    private List<PlayerOrientation> playerOrientations;
    private int state;

    public bool oriented
    {
        get => _oriented;
        set
        {
            if (_oriented != value) OrientationChanged?.Invoke(value);
            _oriented = value;
        }
    }

    private void Start()
    {
        oVRCameraRigStartPosition = oVRCameraRig.position;
        oVRCameraRigStartRotation = oVRCameraRig.rotation;

        instructionsTexts = new[]
            {instructionsText.text, "Place hands flat on table\nwith thumbs on the edge", "Move hands apart quickly"};

        //if (Application.isEditor)
        //    fixedTableHeight = true;
        //else
        //    fixedTableHeight = false;

        playerOrientations = new List<PlayerOrientation>();

        ResetPosition();
    }

    private void Update()
    {
        if (!oriented)
        {
            var distance = Vector3.Distance(instructions.transform.position, blackOverlay.transform.position);
            instructions.transform.position = Vector3.MoveTowards(instructions.transform.position,
                blackOverlay.transform.position, Mathf.Max(0.25f * Time.deltaTime, distance * Time.deltaTime / 1.5f));

            var angle = Vector3.Angle(instructions.transform.forward, blackOverlay.transform.forward);
            var newForward = Quaternion.RotateTowards(instructions.transform.rotation, blackOverlay.transform.rotation,
                Mathf.Max(10f * Time.deltaTime, angle * Time.deltaTime / 1.5f)) * Vector3.forward;
            instructions.transform.rotation = Quaternion.LookRotation(newForward, Vector3.up);

            leftAnimationHand.localPosition = new Vector3(-rightAnimationHand.localPosition.x,
                rightAnimationHand.localPosition.y, rightAnimationHand.localPosition.z);
            leftAnimationHand.localRotation = new Quaternion(rightAnimationHand.localRotation.x * -1.0f,
                rightAnimationHand.localRotation.y,
                rightAnimationHand.localRotation.z, rightAnimationHand.localRotation.w * -1.0f);
            leftAnimationHand.Rotate(180f, 0, 0);
        }
    }

    private void LateUpdate()
    {
        if (oriented)
            return;

        if (!InputManager.Hands.Tracking())
        {
            orientatingSelf = false;
            UpdateState(0);
        }
        else
        {
            var oVRCameraRigPosition = oVRCameraRig.position;
            var oVRCameraRigRotation = oVRCameraRig.rotation;

            //move player position and rotation to the desk
            var thumbDifference = InputManager.Hands.targetFingerTipPositions[0] -
                                  InputManager.Hands.targetFingerTipPositions[5];
            var angle = Mathf.Atan2(thumbDifference.x, thumbDifference.z) * Mathf.Rad2Deg + 90f;
            var angleRotation = Quaternion.AngleAxis(-angle, Vector3.up);
            var averageThumbPosition =
                (InputManager.Hands.targetFingerTipPositions[0] + InputManager.Hands.targetFingerTipPositions[5]) / 2f;

            oVRCameraRigPosition += new Vector3(-averageThumbPosition.x, 0, -averageThumbPosition.z);
            oVRCameraRigPosition = angleRotation * oVRCameraRigPosition;
            oVRCameraRigRotation = angleRotation * oVRCameraRigRotation;

            //calculate desk height
            float averageHeight = 0;
            var min = float.MaxValue;
            var max = float.MinValue;
            for (var i = 1; i < 5; i++)
            {
                averageHeight += InputManager.Hands.Left.TargetFingerTips[i].position.y;
                averageHeight += InputManager.Hands.Right.TargetFingerTips[i].position.y;
                if (InputManager.Hands.Left.TargetFingerTips[i].position.y < min)
                    min = InputManager.Hands.Left.TargetFingerTips[i].position.y;
                if (InputManager.Hands.Left.TargetFingerTips[i].position.y > max)
                    max = InputManager.Hands.Left.TargetFingerTips[i].position.y;
                if (InputManager.Hands.Right.TargetFingerTips[i].position.y < min)
                    min = InputManager.Hands.Right.TargetFingerTips[i].position.y;
                if (InputManager.Hands.Right.TargetFingerTips[i].position.y > max)
                    max = InputManager.Hands.Right.TargetFingerTips[i].position.y;
            }

            var diff = max - min;
            averageHeight /= 8f;
            averageHeight -= 0.0025f;

            var thumbDistance = thumbDifference.magnitude;
            float speed = 0;
            if (lastHandDistance != null)
                speed = (thumbDistance - (float) lastHandDistance) / Time.deltaTime;
            lastHandDistance = thumbDistance;

            if (!oriented)
            {
                if (diff < 0.0375f)
                    UpdateState(2);
                else
                    UpdateState(1);
            }

            if (speed > 0.75f && diff < 0.0375f)
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
                    var totalDistance = playerOrientations[playerOrientations.Count - 1].thumbDistance -
                                        playerOrientations[0].thumbDistance;
                    if (totalDistance >= 0.1f)
                    {
                        var start = Mathf.RoundToInt(playerOrientations.Count * 0.25f) + 1;
                        var end = Mathf.RoundToInt(playerOrientations.Count * 0.75f) - 1;
                        var averagePosition = new Vector3(0, 0, 0);
                        var rotations = new Queue<Quaternion>();
                        float averageAverageHeight = 0;
                        float numOrientations = 0;
                        for (var i = start; i <= end; i++)
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

                        var averageRotation = AverageQuaternion(rotations);

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

    private void ApplyCalculatedTableOrientation(bool updateCenterEyeAnchor)
    {
        if (calculatedTablePosition == null || calculatedTableRotation == null ||
            calculatedCenterEyeAnchor == null && !updateCenterEyeAnchor)
            return;
        
        InputManager.Hands.Left.PhysicsHandController.SetCapsulesActiveTarget(false);
        InputManager.Hands.Right.PhysicsHandController.SetCapsulesActiveTarget(false);

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
        {
            Debug.LogError("No table edge");
        }
        
        InputManager.Hands.Left.PhysicsHandController.ResetFingerJoints();
        InputManager.Hands.Right.PhysicsHandController.ResetFingerJoints();
        InputManager.Hands.UpdateHandTargets();
        InputManager.Hands.Left.PhysicsHandController.SetCapsulesActiveTarget(true);
        InputManager.Hands.Right.PhysicsHandController.SetCapsulesActiveTarget(true);
    }

    private Quaternion AverageQuaternion(Queue<Quaternion> quaternions)
    {
        var avgr = Vector4.zero;
        foreach (var quaternion in quaternions)
            Math3d.AverageQuaternion(ref avgr, quaternion, quaternions.Peek(), quaternions.Count);
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