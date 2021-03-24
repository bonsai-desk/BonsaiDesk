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
	public void CmdSpawnYT(Vector3 position, string id) {
		// todo this crashes when called without being a host/client
		var spawnedObject = Instantiate(VideoPrefab, position, Quaternion.AngleAxis(-90, Vector3.up));
		NetworkServer.Spawn(spawnedObject);
		spawnedObject.GetComponent<TabletControl>().videoId = id;
	}
}