﻿using Mirror;
using UnityEngine;

public class LightNetController : NetworkBehaviour {
	[Range(0.0f, 1f)] [SyncVar] public float mainRoomLevelTarget = 1;

	[Range(0.0f, 1f)] [SyncVar] public float backRoomLevelTarget;

	private void Start() {
		TableBrowserMenu.Singleton.LightChange += HandleLightsChange;
	}

	private void HandleLightsChange(object sender, TableBrowserMenu.LightState e) {
		CmdSetLights(e);
	}

	public void Update() {
		// todo make this more efficient
		LightManager.Singleton.backRoomLevelTarget = backRoomLevelTarget;
		LightManager.Singleton.mainRoomLevelTarget = mainRoomLevelTarget;
	}

	[Command(ignoreAuthority = true)]
	private void CmdSetLights(TableBrowserMenu.LightState state) {
		switch (state) {
			case TableBrowserMenu.LightState.Bright:
				mainRoomLevelTarget = 1;
				backRoomLevelTarget = 0;
				break;
			case TableBrowserMenu.LightState.Vibes:
				mainRoomLevelTarget = 0.146f;
				backRoomLevelTarget = 0.507f;
				break;
		}
	}
}