using System;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

public delegate void OrientEvent(bool oriented);

public class MoveToDesk : MonoBehaviour {
    public static MoveToDesk Singleton;
    public Transform tableParent;

    public static event OrientEvent OrientationChanged;

    public Transform oVRCameraRig;
    public Transform centerEyeAnchor;

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

    public Transform handDemo;
    public Transform tableGhost;
    public TextMeshProUGUI tableGhostText;
    private bool _lastHandsOnEdge = false;
    private List<Material> _tableMaterials;
    private float _tableAlpha = 1f;

    public bool oriented
    {
        get => _oriented;
        set
        {
            if (_oriented != value) OrientationChanged?.Invoke(value);
            _oriented = value;
        }
    }

    public void Awake() {
        if (Singleton == null) {
            Singleton = this;
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

        _tableMaterials = new List<Material>();
        var tableRenderers = tableGhost.GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in tableRenderers)
        {
            _tableMaterials.Add(renderer.material);
        }

        playerOrientations = new List<PlayerOrientation>();

        ResetPosition();
    }

    private bool WristsPointedOut()
    {
        var headForward = centerEyeAnchor.forward;
        headForward.y = 0;

        var lwf = InputManager.Hands.Left.PlayerHand.wrist.forward;
        lwf.y = 0;
        var rwf = InputManager.Hands.Right.PlayerHand.wrist.forward;
        lwf.y = 0;

        return Vector3.Angle(headForward, lwf) < 45f && Vector3.Angle(headForward, rwf) < 45f;
    }

    private bool HandsPointedTowards()
    {
        var left = InputManager.Hands.Left.PlayerHand.thumbDirection.forward;
        var right = InputManager.Hands.Right.PlayerHand.thumbDirection.forward;

        var toRight = InputManager.Hands.Right.PlayerHand.thumbDirection.position
                      - InputManager.Hands.Left.PlayerHand.thumbDirection.position;

        var toLeft = -toRight;

        return Vector3.Angle(left, toRight) < 30f && Vector3.Angle(right, toLeft) < 30f;
    }

    private bool HandsOnEdge()
    {
        var lp = InputManager.Hands.Left.PlayerHand.palm;
        var rp = InputManager.Hands.Left.PlayerHand.palm;

        const float palmAngle = 20f;
        var palmsOriented = Vector3.Angle(lp.forward, Vector3.down) < palmAngle &&
                            Vector3.Angle(rp.forward, Vector3.down) < palmAngle;

        var palmDifferenceValid = Mathf.Abs(lp.position.y - rp.position.y) < 0375f;

        var wristsValid = WristsPointedOut();
        var handsPointing = HandsPointedTowards();

        var palm = InputManager.Hands.Left.PlayerHand.GetGesture(PlayerHand.Gesture.WeakPalm)
                   && InputManager.Hands.Right.PlayerHand.GetGesture(PlayerHand.Gesture.WeakPalm);

        var handsValid = palmsOriented && palmDifferenceValid && wristsValid && handsPointing && palm;

        return handsValid;
    }

    private const float TableLerpTime = 0.35f;
    private float _tableLerp = 0;
    private Vector3 _tableStartPosition;
    private Vector3 _tableEndPosition;

    private void MoveTableGhost()
    {
        //set instructions active
        handDemo.gameObject.SetActive(true);

        //calculate rotation of the hand demo
        var eyeForward = centerEyeAnchor.forward;
        eyeForward.y = 0;

        var handDemoForward = handDemo.forward;
        handDemoForward.y = 0;

        float angle = Vector3.Angle(handDemoForward, eyeForward);

        var handDemoTargetRotation = Quaternion.LookRotation(eyeForward, Vector3.up);

        handDemo.rotation =
            Quaternion.RotateTowards(handDemo.rotation, handDemoTargetRotation, angle * 1.75f * Time.deltaTime);
        // handDemo.position = centerEyeAnchor.position;
        var handDemoPositionDifference = Vector3.Distance(handDemo.position, centerEyeAnchor.position);
        if (handDemoPositionDifference > 1f)
        {
            handDemo.position = centerEyeAnchor.position;
        }

        handDemo.position = Vector3.MoveTowards(handDemo.position, centerEyeAnchor.position,
            handDemoPositionDifference * 3f * Time.deltaTime);

        //if hands are on edge of real table
        bool handOnEdge = HandsOnEdge();
        if (handOnEdge)
        {
            UpdateState(1);

            _tableAlpha = Mathf.MoveTowards(_tableAlpha, 1f, Time.deltaTime * (1f / TableLerpTime));

            tableGhost.gameObject.SetActive(true);
            tableGhostText.text = "<--- swipe apart --->";

            var leftThumb = InputManager.Hands.physicsFingerTipPositions[0];
            var rightThumb = InputManager.Hands.physicsFingerTipPositions[5];
            rightThumb.y = leftThumb.y;

            var tableRotation = Quaternion.LookRotation(leftThumb - rightThumb, Vector3.up) *
                                Quaternion.AngleAxis(90f, Vector3.up);

            var averageThumbPosition = (leftThumb + rightThumb) / 2f;
            var tableDepthOffset = tableRotation * (Vector3.forward * 0.75f / 2f);

            var tablePosition = averageThumbPosition + tableDepthOffset;
            tablePosition.y = (InputManager.Hands.Left.PlayerHand.palm.position.y +
                               InputManager.Hands.Right.PlayerHand.palm.position.y) / 2f;

            if (_lastHandsOnEdge)
            {
                // _tableLerp += Time.deltaTime * (1f / TableLerpTime);

                _tableLerp = CubicBezier.EaseOut.MoveTowards01(_tableLerp, TableLerpTime, true);
                tableGhost.position = Vector3.Lerp(_tableStartPosition, tablePosition, _tableLerp);

                // float tablePositionDifference = Vector3.Distance(tableGhost.position, tablePosition);
                // tableGhost.position = Vector3.MoveTowards(tableGhost.position, tablePosition,
                //     tablePositionDifference * 3f * Time.deltaTime);
                float tableAngleDifference = Quaternion.Angle(tableGhost.rotation, tableRotation);
                tableGhost.rotation = Quaternion.RotateTowards(tableGhost.rotation, tableRotation,
                    tableAngleDifference * 3f * Time.deltaTime);
            }
            else
            {
                tableGhost.position = tablePosition + Vector3.down * 0.25f + tableRotation * Vector3.forward * 0.0f;
                // tableGhost.position = tablePosition;
                tableGhost.rotation = tableRotation;

                // _tableLerp = 0;
                _tableStartPosition = tableGhost.position;
            }
        }
        else
        {
            UpdateState(0);

            _tableAlpha = Mathf.MoveTowards(_tableAlpha, 0f, Time.deltaTime * (1f / TableLerpTime));

            if (_lastHandsOnEdge)
            {
                _tableEndPosition = tableGhost.position;
            }
            
            _tableLerp = CubicBezier.EaseOut.MoveTowards01(_tableLerp, TableLerpTime, false);
            tableGhost.position = Vector3.Lerp(_tableStartPosition, _tableEndPosition, _tableLerp);

            // tableGhost.gameObject.SetActive(false);
            tableGhostText.text = "Place your thumbs on the\n edge of your <i><b>real</b></i> desk";
        }

        leftAnimationHand.localPosition = new Vector3(-rightAnimationHand.localPosition.x,
            rightAnimationHand.localPosition.y, rightAnimationHand.localPosition.z);

        _tableMaterials[0].color = new Color(1f, 0.9561622f, 0.8726415f, _tableAlpha);
        _tableMaterials[1].color = new Color(1f, 1f, 1f, _tableAlpha);

        _lastHandsOnEdge = handOnEdge;
    }

    private void Update()
    {
        //TODO figure out why it is possible to be at the desk while not oriented
        if (!oriented && Vector3.Distance(oVRCameraRig.position, Vector3.zero) < 100f)
        {
            ResetPosition();
            return;
        }

        if (oriented)
        {
            handDemo.gameObject.SetActive(false);
            tableGhost.gameObject.SetActive(false);
            return;
        }

        MoveTableGhost();

        var distance = Vector3.Distance(instructions.transform.position, blackOverlay.transform.position);
        instructions.transform.position = Vector3.MoveTowards(instructions.transform.position,
            blackOverlay.transform.position, Mathf.Max(0.25f * Time.deltaTime, distance * Time.deltaTime / 1.5f));

        var instructionsAngle = Vector3.Angle(instructions.transform.forward, blackOverlay.transform.forward);
        var newForward = Quaternion.RotateTowards(instructions.transform.rotation, blackOverlay.transform.rotation,
            Mathf.Max(10f * Time.deltaTime, instructionsAngle * Time.deltaTime / 1.5f)) * Vector3.forward;
        instructions.transform.rotation = Quaternion.LookRotation(newForward, Vector3.up);

        // leftAnimationHand.localPosition = new Vector3(-rightAnimationHand.localPosition.x,
        //     rightAnimationHand.localPosition.y, rightAnimationHand.localPosition.z);
        // leftAnimationHand.localRotation = new Quaternion(rightAnimationHand.localRotation.x * -1.0f,
        //     rightAnimationHand.localRotation.y,
        //     rightAnimationHand.localRotation.z, rightAnimationHand.localRotation.w * -1.0f);
        // leftAnimationHand.Rotate(180f, 0, 0);

        if (!InputManager.Hands.Tracking())
        {
            orientatingSelf = false;
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

            if (speed > 0.5f && diff < 0.0375f)
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
                    if (totalDistance >= 0.08f)
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
                        averageAverageHeight += 0.005f;

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
                        handDemo.gameObject.SetActive(false);
                        tableGhost.gameObject.SetActive(false);
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

    public void ResetIfOriented()
    {
        if (oriented)
        {
            ResetPosition();
        }
    }

    public void ResetPosition()
    {
        oriented = false;
        blackOverlay.SetActive(true);
        instructions.SetActive(true);
        animationObject.SetActive(true);
        oVRCameraRig.position = oVRCameraRigStartPosition;
        oVRCameraRig.rotation = oVRCameraRigStartRotation;

        var eyeForward = centerEyeAnchor.forward;
        eyeForward.y = 0;
        var handDemoForward = handDemo.forward;
        handDemoForward.y = 0;
        handDemo.rotation = Quaternion.LookRotation(eyeForward, Vector3.up);
        handDemo.position = centerEyeAnchor.position;

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
            var tableEdgePosition = tableEdge.position;
            tableEdgePosition.y = 0;
            oVRCameraRig.position += tableEdgePosition;
        }
        else
        {
            Debug.LogError("No table edge");
        }

        InputManager.Hands.UpdateHandTargets(false);

        InputManager.Hands.Left.PhysicsHandController.ResetFingerJoints();
        InputManager.Hands.Right.PhysicsHandController.ResetFingerJoints();

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