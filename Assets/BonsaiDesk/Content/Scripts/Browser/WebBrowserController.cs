using UnityEngine;

[RequireComponent(typeof(TableBrowser))]
public class WebBrowserController : MonoBehaviour {
	public Transform screen;
	public Transform raisedTransform;
	private bool _raised;

	// Start is called before the first frame update
	private void Start() {
		SetRaised(true);
	}

	// Update is called once per frame
	private void Update() { }

	public void SetRaised(bool raised) {
		_raised = raised;
		if (_raised) {
			screen.localPosition    = raisedTransform.localPosition;
			screen.localEulerAngles = raisedTransform.localEulerAngles;
		}
		else {
			screen.localPosition    = Vector3.zero;
			screen.localEulerAngles = Vector3.zero;
		}
	}

	public void ToggleRaised() {
		SetRaised(!_raised);
	}
}