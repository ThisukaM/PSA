using UnityEngine;
using System;

public class CameraCapture : MonoBehaviour
{
    public int width = 640;
    public int height = 480;
    public int fps = 30;
    public bool compressJPEG = false;

    private WebCamTexture webcamTex;
    private Texture2D frameTex;

    public static event Action<byte[]> OnFrameCaptured;

    void Start()
    {
        if (WebCamTexture.devices.Length == 0)
        {
            Debug.LogError("No webcam detected!");
            return;
        }

        WebCamDevice device = WebCamTexture.devices[0]; // choose first webcam
        webcamTex = new WebCamTexture(device.name, width, height, fps);
        webcamTex.Play();

        frameTex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        StartCoroutine(CaptureFrames());
    }

    private System.Collections.IEnumerator CaptureFrames()
    {
        while (true)
        {
            if (webcamTex.didUpdateThisFrame)
            {
                // Copy webcam pixels to Texture2D
                frameTex.SetPixels32(webcamTex.GetPixels32());
                frameTex.Apply();

                if (compressJPEG)
                {
                    byte[] jpg = frameTex.EncodeToJPG(50);
                    OnFrameCaptured?.Invoke(jpg);
                }
                else
                {
                    Color32[] pixels = frameTex.GetPixels32();
                    byte[] bgr = new byte[pixels.Length * 3];
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        bgr[i * 3 + 0] = pixels[i].b;
                        bgr[i * 3 + 1] = pixels[i].g;
                        bgr[i * 3 + 2] = pixels[i].r;
                    }
                    OnFrameCaptured?.Invoke(bgr);
                }
            }
            yield return null; // wait until next frame
        }
    }

    void OnDisable()
    {
        if (webcamTex != null)
            webcamTex.Stop();
        if (frameTex != null)
            Destroy(frameTex);
    }
}
