//Cristian Pop - https://boxophobic.com/

using UnityEngine;
using UnityEditor;

public class HeightFogShaderGUI : ShaderGUI
{
    Material material;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
    {
        base.OnGUI(materialEditor, props);

        material = materialEditor.target as Material;

        SetBlendProps();
    }

    void SetBlendProps()
    {
        if (material.HasProperty("_FogAxisMode"))
        {
            if (material.GetInt("_FogAxisMode") == 0)
            {
                material.SetVector("_FogAxisOption", new Vector4(1, 0, 0, 0));
            }
            else if (material.GetInt("_FogAxisMode") == 1)
            {
                material.SetVector("_FogAxisOption", new Vector4(0, 1, 0, 0));
            }
            else if (material.GetInt("_FogAxisMode") == 2)
            {
                material.SetVector("_FogAxisOption", new Vector4(0, 0, 1, 0));
            }
        }

        if (material.HasProperty("_DirectionalMode"))
        {
            if (material.GetInt("_DirectionalMode") == 0)
            {
                material.SetFloat("_DirectionalModeBlend", 0.0f);
            }
            else if (material.GetInt("_DirectionalMode") == 1)
            {
                material.SetFloat("_DirectionalModeBlend", 1.0f);
            }
        }

        if (material.HasProperty("_NoiseMode"))
        {
            if (material.GetInt("_NoiseMode") == 0)
            {
                material.SetFloat("_NoiseModeBlend", 0.0f);
            }
            else if (material.GetInt("_NoiseMode") == 2)
            {
                material.SetFloat("_NoiseModeBlend", 1.0f);
            }
        }
    }
}
