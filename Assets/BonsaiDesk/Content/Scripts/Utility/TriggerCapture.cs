// The MIT License (MIT)
// Copyright (c) 2014 Brad Nelson and Play-Em Inc.
// CaptureScreenshot is based on Brad Nelson's MIT-licensed AnimationToPng: http://wiki.unity3d.com/index.php/AnimationToPNG
// AnimationToPng is based on Twinfox and bitbutter's Render Particle to Animated Texture Scripts.

using System;
using System.IO;
using UnityEngine;

public class TriggerCapture : MonoBehaviour
{
    public bool CompatibilityMode = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            screenShot();
        }
    }

    private void screenShot()
    {
        var cam = Camera.main;
        // Set a mask to only draw only elements in this layer. e.g., capture your player with a transparent background.
        //cam.cullingMask = LayerMask.GetMask("Player");

        string filename = string.Format("Screenshots/capture_{0}.png", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff"));
        print(filename);
        int width = Screen.width;
        int height = Screen.height;
        if (!CompatibilityMode)
        {
            CaptureScreenshot.SimpleCaptureTransparentScreenshot(cam, width, height, filename);
        }
        else
        {
            CaptureScreenshot.CaptureTransparentScreenshot(cam, width, height, filename);
        }
    }
}

public static class CaptureScreenshot
{
    public static void CaptureTransparentScreenshot(Camera cam, int width, int height, string screengrabfile_path)
    {
        // This is slower, but seems more reliable.
        var bak_cam_targetTexture = cam.targetTexture;
        var bak_cam_clearFlags = cam.clearFlags;
        var bak_RenderTexture_active = RenderTexture.active;

        var tex_white = new Texture2D(width, height, TextureFormat.ARGB32, false);
        var tex_black = new Texture2D(width, height, TextureFormat.ARGB32, false);
        var tex_transparent = new Texture2D(width, height, TextureFormat.ARGB32, false);
        // Must use 24-bit depth buffer to be able to fill background.
        var render_texture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        render_texture.antiAliasing = 8;
        var grab_area = new Rect(0, 0, width, height);

        RenderTexture.active = render_texture;
        cam.targetTexture = render_texture;
        CameraClearFlags originalClearFlag = cam.clearFlags;
        cam.clearFlags = CameraClearFlags.SolidColor;

        Color originalColor = cam.backgroundColor;
        cam.backgroundColor = Color.black;
        cam.Render();
        tex_black.ReadPixels(grab_area, 0, 0);
        tex_black.Apply();

        cam.backgroundColor = Color.white;
        cam.Render();
        tex_white.ReadPixels(grab_area, 0, 0);
        tex_white.Apply();

        cam.backgroundColor = originalColor;
        cam.clearFlags = originalClearFlag;

        // Create Alpha from the difference between black and white camera renders
        for (int y = 0; y < tex_transparent.height; ++y)
        {
            for (int x = 0; x < tex_transparent.width; ++x)
            {
                float alpha = tex_white.GetPixel(x, y).r - tex_black.GetPixel(x, y).r;
                alpha = 1.0f - alpha;
                Color color;
                if (alpha == 0)
                {
                    color = Color.clear;
                }
                else
                {
                    color = tex_black.GetPixel(x, y) / alpha;
                }

                color.a = alpha;
                tex_transparent.SetPixel(x, y, color);
            }
        }

        // Encode the resulting output texture to a byte array then write to the file
        byte[] pngShot = ImageConversion.EncodeToPNG(tex_transparent);
        File.WriteAllBytes(screengrabfile_path, pngShot);

        cam.clearFlags = bak_cam_clearFlags;
        cam.targetTexture = bak_cam_targetTexture;
        RenderTexture.active = bak_RenderTexture_active;
        RenderTexture.ReleaseTemporary(render_texture);

        Texture2D.Destroy(tex_black);
        Texture2D.Destroy(tex_white);
        Texture2D.Destroy(tex_transparent);
    }

    public static void SimpleCaptureTransparentScreenshot(Camera cam, int width, int height, string screengrabfile_path)
    {
        // Depending on your render pipeline, this may not work.
        var bak_cam_targetTexture = cam.targetTexture;
        var bak_cam_clearFlags = cam.clearFlags;
        var bak_RenderTexture_active = RenderTexture.active;

        var tex_transparent = new Texture2D(width, height, TextureFormat.ARGB32, false);
        // Must use 24-bit depth buffer to be able to fill background.
        var render_texture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
        render_texture.antiAliasing = 8;
        var grab_area = new Rect(0, 0, width, height);

        RenderTexture.active = render_texture;
        cam.targetTexture = render_texture;
        CameraClearFlags originalClearFlag = cam.clearFlags;
        cam.clearFlags = CameraClearFlags.SolidColor;

        // Simple: use a clear background
        Color originalColor = cam.backgroundColor;
        cam.backgroundColor = Color.clear;
        cam.Render();
        tex_transparent.ReadPixels(grab_area, 0, 0);
        tex_transparent.Apply();

        cam.backgroundColor = originalColor;
        cam.clearFlags = originalClearFlag;

        // Encode the resulting output texture to a byte array then write to the file
        byte[] pngShot = ImageConversion.EncodeToPNG(tex_transparent);
        File.WriteAllBytes(screengrabfile_path, pngShot);

        cam.clearFlags = bak_cam_clearFlags;
        cam.targetTexture = bak_cam_targetTexture;
        RenderTexture.active = bak_RenderTexture_active;
        RenderTexture.ReleaseTemporary(render_texture);

        Texture2D.Destroy(tex_transparent);
    }
}