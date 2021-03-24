using System;
using Mirror;
using UnityEngine;

public class YouTubeSpawner : NetworkBehaviour {
	public static YouTubeSpawner Singleton;
	public GameObject VideoPrefab;

	private void Awake() {
		if (Singleton == null) {
			Singleton = this;
		}
	}

	[Command(ignoreAuthority = true)]
	public void CmdSpawnYT(Vector3 position, Vector3 headPosition,  string id) {
		var atHead = (headPosition - position);
		var theta  = Mathf.Atan(atHead.z/atHead.x);
		var angle  = - 360 * theta / (2 * Mathf.PI) + 270;
		var rot  = Quaternion.Euler(0, angle, 0);
		// todo this crashes when called without being a host/client
		var spawnedObject = Instantiate(VideoPrefab, position, rot);
		NetworkServer.Spawn(spawnedObject);
		spawnedObject.GetComponent<TabletControl>().videoId = id;
	}
}