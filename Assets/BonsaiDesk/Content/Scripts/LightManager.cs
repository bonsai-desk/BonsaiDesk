using System;
using System.Collections;
using UnityEngine;

public class LightManager : MonoBehaviour {
	public Texture2D[] Lights;
	public Material Garden;

	public FilterMode filterMode = FilterMode.Bilinear;
	public TextureWrapMode wrapMode = TextureWrapMode.Clamp;
	public TextureFormat textureFormat = TextureFormat.RGBA32;
	public bool mipChain;

	[Header("ring")] public float ringScale = 1f;

	public float ringFloor;
	public float ringSpeed = 1;

	[Header("Pulse Data (scale, floor, speed)")]
	public Vector3[] PulseData;

	private readonly float[] lightLevels = new float[64];

	// Start is called before the first frame update
	private void Start() {
		var x   = Lights[0];
		var arr = new Texture2DArray(x.width, x.height, Lights.Length, textureFormat, mipChain);

		arr.filterMode = filterMode;
		arr.wrapMode   = wrapMode;

		for (var i = 0; i < Lights.Length; i++) {
			arr.SetPixels(Lights[i].GetPixels(), i);
		}

		arr.Apply();
		//AssetDatabase.CreateAsset(arr, "Assets/LightsArr.tarr");
		lightLevels[0] = 1;
		lightLevels[1] = 1;
		lightLevels[2] = 0;
		lightLevels[3] = 0;
		lightLevels[4] = 0;
		lightLevels[5] = 0;
		Garden.SetInt("numLights", Lights.Length);
		Garden.SetFloatArray("lightLevels", lightLevels);
		Garden.SetTexture("Lights", arr);

		MoveToDesk.OrientationChanged += HandleOrient;

	#if UNITY_EDITOR
		if (NetworkManagerGame.Singleton.serverOnlyIfEditor) {
			StartCoroutine(FadeOnMain());
		}
	#endif
	}

	// Update is called once per frame
	private void Update() {
		//var idxs = new[] {5};
		Pulse(1, PulseData[0], 0);
		Garden.SetFloatArray("lightLevels", lightLevels);
	}

	private void HandleOrient(bool oriented) {
		if (oriented) {
			StartCoroutine(FadeOnMain());
		}
	}

	private IEnumerator FadeOnMain() {
		float x = 0;

		while (x < 1) {
			x              += 0.02f;
			lightLevels[2] =  CubicBezier.EaseIn.Sample(x);
			yield return null;
		}

		lightLevels[2] = 1f;
	}

	private static float gauss(float ringScale, float ringFloor, float ringSpeed, float x, float shift = 0) {
		return (float) (ringFloor + (1 - ringFloor) * Math.Exp(
			-ringScale * Math.Pow(
				Math.Sin(ringSpeed * x - shift), 2f
			)));
	}

	private void ring(int[] idxs) {
		var numLights = idxs.Length;
		var twoPi     = 2 * Math.PI;
		for (var i = 0; i < idxs.Length; i++) {
			lightLevels[idxs[i]] =
				gauss(ringScale, ringFloor, ringSpeed, Time.time, (float) twoPi * (i + 1) / numLights);
		}
	}

	private void Pulse(int idx, Vector3 pulseData, float offset) {
		lightLevels[idx] = gauss(pulseData[0], pulseData[1], pulseData[2],
		                         Time.time + offset);
	}
}