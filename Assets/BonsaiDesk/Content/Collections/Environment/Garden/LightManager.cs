using System;
using UnityEngine;

public class LightManager : MonoBehaviour {
	public Texture2D[] Lights;
	public Material Garden;

	public FilterMode filterMode = FilterMode.Bilinear;
	public TextureWrapMode wrapMode = TextureWrapMode.Clamp;
	public TextureFormat textureFormat = TextureFormat.RGBA32;
	public bool mipChain;

	[Header("ring")]
	public float ringScale = 1f;
	public float ringFloor;
	public float ringSpeed = 1;
	private readonly float[] lightLevels = new float[64];
	
	[Header("desk")]
	public float deskScale = 1f;
	public float deskFloor;
	public float deskSpeed = 1;

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
	}

	// Update is called once per frame
	private void Update() {
		var idxs = new[] {5};
		ring(idxs);
		desk();
		Garden.SetFloatArray("lightLevels", lightLevels);
	}

	private static float gauss(float ringScale, float ringFloor, float ringSpeed, float x, float shift=0) {
		return (float) (ringFloor + (1 - ringFloor) * Math.Exp(
			-ringScale * Math.Pow(
				Math.Sin(ringSpeed * x - shift), 2f
			)));
	}

	private void ring(int[] idxs) {
		var numLights = idxs.Length;
		var twoPi     = 2 * Math.PI;
		for (var i = 0; i < idxs.Length; i++) {
			lightLevels[idxs[i]] = gauss(ringScale, ringFloor, ringSpeed, Time.time, (float) twoPi * (i + 1) / numLights);
		}
	}

	private void desk() {
		lightLevels[1] = gauss(deskScale, deskFloor, deskSpeed, Time.time);
	}
}