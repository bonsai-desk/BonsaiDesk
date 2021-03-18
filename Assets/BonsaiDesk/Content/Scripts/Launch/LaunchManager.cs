﻿using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

public class LaunchManager : MonoBehaviour {
	public bool debug;
	// Start is called before the first frame update
	private void Start() {
		StartCoroutine(StartXR());
		if (!debug) {
			StartCoroutine(LoadAsync());
		}
	}

	// Update is called once per frame
	private void Update() { }

	public void LoadLevel() { }
	
	private void OnApplicationQuit() {
		StopXR();
	}
	
	private void StopXR() {
		if (XRGeneralSettings.Instance.Manager.isInitializationComplete) {
			Debug.Log("Stopping XR...");

			XRGeneralSettings.Instance.Manager.StopSubsystems();
			XRGeneralSettings.Instance.Manager.DeinitializeLoader();
			Debug.Log("XR stopped completely.");
		}
	}

	private IEnumerator LoadAsync() {
		var op = SceneManager.LoadSceneAsync(1);
		while (!op.isDone) {
			Debug.Log(op.progress);
			yield return null;
		}
	}

	private IEnumerator StartXR() {
		Debug.Log("Initializing XR...");
		yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

		if (XRGeneralSettings.Instance.Manager.activeLoader == null) {
			Debug.LogError("Initializing XR Failed. Check Editor or Player log for details.");
		}
		else {
			Debug.Log("Starting XR...");
			XRGeneralSettings.Instance.Manager.StartSubsystems();
		}
	}
}