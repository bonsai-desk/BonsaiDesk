using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(InputManager))]
public class CustomInputModule : StandaloneInputModule {
	[Header("Custom Input Module")] public static CustomInputModule Singleton;

	public Vector3 cursorRoot;
	public OVRCursor m_Cursor;
	public bool drawcursor;
	public List<Transform> screens;
	public float hoverDistance = 0.1f;
	public float clickDistance = 0.075f / 2;
	public Camera mainCamera;
	public float angleDragThreshold = 1;
	private readonly MouseState m_MouseState = new MouseState();
	private Active handActive = Active.Right;
	private bool inClickRegion;
	protected Dictionary<int, OVRPointerEventData> m_VRRayPointerData = new Dictionary<int, OVRPointerEventData>();
	private bool prevInClickRegion;

	protected override void Awake() {
		if (Singleton == null) {
			Singleton = this;
		}
	}

	public override void Process() {
		base.Process();

		ProcessMouseEvent(GetGazePointerData());
	}

	public event EventHandler Click;

	private void ProcessMouseEvent(MouseState mouseData) {
		var leftButtonData = mouseData.GetButtonState(PointerEventData.InputButton.Left).eventData;

		ProcessMousePress(leftButtonData);
		ProcessMove(leftButtonData.buttonData);
		ProcessDrag(leftButtonData.buttonData);
	}

	protected bool GetPointerData(int id, out OVRPointerEventData data, bool create) {
		if (!m_VRRayPointerData.TryGetValue(id, out data) && create) {
			data = new OVRPointerEventData(eventSystem) {
				pointerId = id
			};

			m_VRRayPointerData.Add(id, data);
			return true;
		}

		return false;
	}

	private MouseState GetGazePointerData() {
		// Get the OVRRayPointerEventData reference
		OVRPointerEventData leftData;
		GetPointerData(-1, out leftData, true);
		leftData.Reset();

		prevInClickRegion = inClickRegion;
		inClickRegion     = false;

		var foundScreen = false;
		foreach (var screen in screens) {
			var leftFingerInScreen  = screen.InverseTransformPoint(InputManager.Hands.physicsFingerTipPositions[1]);
			var rightFingerInScreen = screen.InverseTransformPoint(InputManager.Hands.physicsFingerTipPositions[6]);

			var leftInBounds  = FingerInBounds(leftFingerInScreen);
			var rightInBounds = FingerInBounds(rightFingerInScreen);

			var leftValid  = leftFingerInScreen.z <= 0.04 && leftInBounds;
			var rightValid = rightFingerInScreen.z <= 0.04 && rightInBounds;

			var leftHover  = -leftFingerInScreen.z < hoverDistance && leftValid;
			var rightHover = -rightFingerInScreen.z < hoverDistance && rightValid;

			var leftInClick  = -leftFingerInScreen.z < clickDistance && leftValid;
			var rightInClick = -rightFingerInScreen.z < clickDistance && rightValid;

			var fingerInScreen = leftFingerInScreen;
			if (!leftValid || rightValid && rightFingerInScreen.z > leftFingerInScreen.z) {
				fingerInScreen = rightFingerInScreen;
			}

			cursorRoot = screen.TransformPoint(fingerInScreen);

			var leftClick  = leftInClick && !rightInClick;
			var rightClick = !leftInClick && rightInClick;
			if (leftClick || rightClick) {
				if (!prevInClickRegion && Click != null) {
					Click(this, new EventArgs());
				}
				
				inClickRegion = true;
			}

			if (!leftValid && rightValid || rightClick) {
				handActive = Active.Right;
			}

			if (leftValid && !rightValid || leftClick) {
				handActive = Active.Left;
			}

			if (leftValid || rightValid) {
				foundScreen = true;
			}

			if (handActive == Active.Right && rightHover) {
				ProcessCursor(rightFingerInScreen, screen);
				ProcessRay(rightFingerInScreen, screen, leftData, leftValid, rightValid);

				break;
			}

			if (handActive == Active.Left && leftHover) {
				ProcessCursor(leftFingerInScreen, screen);
				ProcessRay(leftFingerInScreen, screen, leftData, leftValid, rightValid);
				break;
			}

			if (drawcursor) {
				m_Cursor.SetCursorStartDest(Vector3.zero, Vector3.zero, Vector3.zero);
			}
		}

		if (!foundScreen) {
			var fakeRayCast = new RaycastResult();
			leftData.pointerCurrentRaycast = fakeRayCast;

			InputManager.Hands.Left.SetHandColliderActiveForScreen(true);
			InputManager.Hands.Right.SetHandColliderActiveForScreen(true);
		}

		//Populate some default values
		leftData.button = PointerEventData.InputButton.Left;

		var fps = GetGazeButtonState();
		m_MouseState.SetButtonState(PointerEventData.InputButton.Left, fps, leftData);

		return m_MouseState;
	}

	private void ProcessCursor(Vector3 fingerInScreen, Transform screen) {
		var fingerInScreen0Z = fingerInScreen;
		fingerInScreen0Z.z = 0;
		if (drawcursor) {
			m_Cursor.SetCursorStartDest(screen.TransformPoint(fingerInScreen),
										screen.TransformPoint(fingerInScreen0Z), -Vector3.forward);
		}
	}

	private void ProcessRay(Vector3 fingerInScreen, Transform screen, OVRPointerEventData leftData, bool leftValid,
	                          bool rightValid) {
		var screenHit = screen.TransformPoint(
			new Vector3(fingerInScreen.x, fingerInScreen.y, 0)
		);
		var screenPos = mainCamera.WorldToScreenPoint(screenHit);
		var fakeRayCast = new RaycastResult {
			gameObject = screen.gameObject, worldPosition = screenHit, screenPosition = screenPos
		};

		leftData.pointerCurrentRaycast = fakeRayCast;

		InputManager.Hands.Left.SetHandColliderActiveForScreen(!leftValid);
		InputManager.Hands.Right.SetHandColliderActiveForScreen(!rightValid);
	}

	private PointerEventData.FramePressState GetGazeButtonState() {
		foreach (var screen in screens) {
			var pressed  = inClickRegion && !prevInClickRegion;
			var released = prevInClickRegion && !inClickRegion;

			if (pressed) {
				return PointerEventData.FramePressState.Pressed;
			}

			if (released) {
				return PointerEventData.FramePressState.Released;
			}

			return PointerEventData.FramePressState.NotChanged;
		}

		return PointerEventData.FramePressState.Released;
		//throw new NotImplementedException();
	}

	private bool FingerInBounds(Vector3 fingerInScreen) {
		return Math.Abs(fingerInScreen.x) < 0.5 && Math.Abs(fingerInScreen.y) < 0.5;
	}

	/// <summary>
	///     Exactly the same as the code from PointerInputModule, except that we call our own
	///     IsPointerMoving.
	///     This would also not be necessary if PointerEventData.IsPointerMoving was virtual
	/// </summary>
	/// <param name="pointerEvent"></param>
	protected override void ProcessDrag(PointerEventData pointerEvent) {
		var originalPosition = pointerEvent.position;
		var moving           = IsPointerMoving(pointerEvent);
		if (moving && pointerEvent.pointerDrag != null
		           && !pointerEvent.dragging
		           && ShouldStartDrag(pointerEvent)) {
			if (pointerEvent.IsVRPointer())
				//adjust the position used based on swiping action. Allowing the user to
				//drag items by swiping on the touchpad
			{
				pointerEvent.position = SwipeAdjustedPosition(originalPosition, pointerEvent);
			}

			ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.beginDragHandler);
			pointerEvent.dragging = true;
		}

		// Drag notification
		if (pointerEvent.dragging && moving && pointerEvent.pointerDrag != null) {
			if (pointerEvent.IsVRPointer()) {
				pointerEvent.position = SwipeAdjustedPosition(originalPosition, pointerEvent);
			}

			// Before doing drag we should cancel any pointer down state
			// And clear selection!
			if (pointerEvent.pointerPress != pointerEvent.pointerDrag) {
				ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

				pointerEvent.eligibleForClick = false;
				pointerEvent.pointerPress     = null;
				pointerEvent.rawPointerPress  = null;
			}

			ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.dragHandler);
		}
	}

	private bool IsPointerMoving(PointerEventData pointerEvent) {
		return true;
	}

	protected Vector2 SwipeAdjustedPosition(Vector2 originalPosition, PointerEventData pointerEvent) {
		return originalPosition;
//   #if UNITY_ANDROID && !UNITY_EDITOR
//               // On android we use the touchpad position (accessed through Input.mousePosition) to modify
//               // the effective cursor position for events related to dragging. This allows the user to
//               // use the touchpad to drag draggable UI elements
//               if (useSwipeScroll)
//               {
//                   Vector2 delta = (Vector2)Input.mousePosition - pointerEvent.GetSwipeStart();
//                   if (InvertSwipeXAxis)
//                       delta.x *= -1;
//                   return originalPosition + delta * swipeDragScale;
//               }
//   #endif
	}

	private bool ShouldStartDrag(PointerEventData pointerEvent) {
		var cameraPos  = mainCamera.transform.position;
		var pressDir   = (pointerEvent.pointerPressRaycast.worldPosition - cameraPos).normalized;
		var currentDir = (pointerEvent.pointerCurrentRaycast.worldPosition - cameraPos).normalized;
		return Vector3.Dot(pressDir, currentDir) < Mathf.Cos(Mathf.Deg2Rad * angleDragThreshold);
	}

	private enum Active {
		Left,
		Right
	}
}