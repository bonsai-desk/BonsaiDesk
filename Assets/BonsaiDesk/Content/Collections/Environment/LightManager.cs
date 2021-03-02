using System;
using UnityEngine;

public class LightManager : MonoBehaviour {
	public Texture2D[] Lights;
	public Material Garden;

	public FilterMode filterMode = FilterMode.Bilinear;
	public TextureWrapMode wrapMode = TextureWrapMode.Clamp;
	public TextureFormat textureFormat = TextureFormat.RGBA32;
	public bool mipChain;
	private readonly float[] lightLevels = new float[64];
	
	public float ringScale = 1f;
	public float ringFloor = 0f;
	public float ringSpeed = 1;

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
		lightLevels[2] = 1;
		lightLevels[3] = 1;
		lightLevels[4] = 1;
		Garden.SetInt("numLights", Lights.Length);
		Garden.SetFloatArray("lightLevels", lightLevels);
		Garden.SetTexture("Lights", arr);
	}

	// Update is called once per frame
	private void Update() {
		var idxs = new [] {2,3,4};
		ring(idxs);
		Garden.SetFloatArray("lightLevels", lightLevels);
	}

	private void ring(int[] idxs) {
		var numLights   = idxs.Length;
		var twoPi = 2 * Math.PI;
		for (var i = 0; i < idxs.Length; i++) {
			lightLevels[idxs[i]] = (float) (ringFloor + (1-ringFloor) * Math.Exp(
				-ringScale * Math.Pow(
					Math.Sin(ringSpeed * Time.time - twoPi * (i + 1) / numLights),
					2f)));
		}
	}
}