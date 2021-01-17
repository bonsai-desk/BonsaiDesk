using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CustomInputModule : StandaloneInputModule
{
    [Header("Custom Input Module")] public Transform rayTransform;

    public OVRCursor m_Cursor;
    public Transform fingerTip;
    public List<Transform> screens;
    public float hoverDistance = 0.1f;
    public Camera mainCamera;
    private readonly MouseState m_MouseState = new MouseState();
    private bool inClickRegion;
    protected Dictionary<int, OVRPointerEventData> m_VRRayPointerData = new Dictionary<int, OVRPointerEventData>();
    private bool prevInClickRegion;

    // Start is called before the first frame update
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
    }

    public override void Process()
    {
        base.Process();


        ProcessMouseEvent(GetGazePointerData());
    }

    private void ProcessMouseEvent(MouseState mouseData)
    {
        var leftButtonData = mouseData.GetButtonState(PointerEventData.InputButton.Left).eventData;

        //ProcessMousePress(leftButtonData);
        ProcessMousePress(leftButtonData);
        ProcessMove(leftButtonData.buttonData);
        ProcessDrag(leftButtonData.buttonData);
    }

    protected bool GetPointerData(int id, out OVRPointerEventData data, bool create)
    {
        if (!m_VRRayPointerData.TryGetValue(id, out data) && create)
        {
            data = new OVRPointerEventData(eventSystem)
            {
                pointerId = id
            };

            m_VRRayPointerData.Add(id, data);
            return true;
        }

        return false;
    }

    private MouseState GetGazePointerData()
    {
        // Get the OVRRayPointerEventData reference
        OVRPointerEventData leftData;
        GetPointerData(-1, out leftData, true);
        leftData.Reset();

        foreach (var screen in screens)
        {
            var fingerInScreen = screen.InverseTransformPoint(fingerTip.position);

            if (FingerInBounds(fingerInScreen) &&
                -hoverDistance / 5 < -fingerInScreen.z &&
                -fingerInScreen.z < hoverDistance)
            {
                var screenHit = screen.TransformPoint(
                    new Vector3(fingerInScreen.x, fingerInScreen.y, 0)
                );
                var screenPos = mainCamera.WorldToScreenPoint(screenHit);
                var fakeRayCast = new RaycastResult
                {
                    gameObject = screen.gameObject, worldPosition = screenHit, screenPosition = screenPos
                };
                leftData.pointerCurrentRaycast = fakeRayCast;
                m_Cursor.SetCursorStartDest(fingerTip.position, screenHit, Vector3.forward);

                prevInClickRegion = inClickRegion;
                if (FingerInBounds(fingerInScreen) && Math.Abs(fingerInScreen.z) < hoverDistance / 5)
                    inClickRegion = true;
                else
                    inClickRegion = false;
                break;
            }
        }


        //Populate some default values
        leftData.button = PointerEventData.InputButton.Left;


        var fps = GetGazeButtonState();
        m_MouseState.SetButtonState(PointerEventData.InputButton.Left, fps, leftData);

        return m_MouseState;
    }

    private PointerEventData.FramePressState GetGazeButtonState()
    {
        foreach (var screen in screens)
        {
            var pressed = inClickRegion && !prevInClickRegion;
            var released = prevInClickRegion && !inClickRegion;

            if (pressed)
                return PointerEventData.FramePressState.Pressed;
            if (released)
                return PointerEventData.FramePressState.Released;
            return PointerEventData.FramePressState.NotChanged;
        }

        throw new NotImplementedException();
    }

    private bool FingerInBounds(Vector3 fingerInScreen)
    {
        return Math.Abs(fingerInScreen.x) < 0.5 && Math.Abs(fingerInScreen.y) < 0.5;
    }

    /// <summary>
    ///     Exactly the same as the code from PointerInputModule, except that we call our own
    ///     IsPointerMoving.
    ///     This would also not be necessary if PointerEventData.IsPointerMoving was virtual
    /// </summary>
    /// <param name="pointerEvent"></param>
    protected override void ProcessDrag(PointerEventData pointerEvent)
    {
        var originalPosition = pointerEvent.position;
        var moving = IsPointerMoving(pointerEvent);
        if (moving && pointerEvent.pointerDrag != null
                   && !pointerEvent.dragging
                   && ShouldStartDrag(pointerEvent))
        {
            if (pointerEvent.IsVRPointer())
                //adjust the position used based on swiping action. Allowing the user to
                //drag items by swiping on the touchpad
                pointerEvent.position = SwipeAdjustedPosition(originalPosition, pointerEvent);
            ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.beginDragHandler);
            pointerEvent.dragging = true;
        }

        // Drag notification
        if (pointerEvent.dragging && moving && pointerEvent.pointerDrag != null)
        {
            if (pointerEvent.IsVRPointer())
                pointerEvent.position = SwipeAdjustedPosition(originalPosition, pointerEvent);
            // Before doing drag we should cancel any pointer down state
            // And clear selection!
            if (pointerEvent.pointerPress != pointerEvent.pointerDrag)
            {
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

                pointerEvent.eligibleForClick = false;
                pointerEvent.pointerPress = null;
                pointerEvent.rawPointerPress = null;
            }

            ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.dragHandler);
        }
    }

    private bool IsPointerMoving(PointerEventData pointerEvent)
    {
        return true;
    }

    protected Vector2 SwipeAdjustedPosition(Vector2 originalPosition, PointerEventData pointerEvent)
    {
        return originalPosition;
#if UNITY_ANDROID && !UNITY_EDITOR
            // On android we use the touchpad position (accessed through Input.mousePosition) to modify
            // the effective cursor position for events related to dragging. This allows the user to
            // use the touchpad to drag draggable UI elements
            if (useSwipeScroll)
            {
                Vector2 delta = (Vector2)Input.mousePosition - pointerEvent.GetSwipeStart();
                if (InvertSwipeXAxis)
                    delta.x *= -1;
                return originalPosition + delta * swipeDragScale;
            }
#endif
    }

    private bool ShouldStartDrag(PointerEventData pointerEvent)
    {
        return true;
    }
}