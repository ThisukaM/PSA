using UnityEngine;
using System;

public class CameraReceiver : MonoBehaviour
{
    public int width = 640;
    public int height = 480;

    private Texture2D displayTex;

    void OnEnable()
    {
        CameraCapture.OnFrameCaptured += HandleFrame;
    }

    void OnDisable()
    {
        CameraCapture.OnFrameCaptured -= HandleFrame;
    }

    void Start()
    {
        displayTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
    }

    void HandleFrame(byte[] data)
    {
        if (data.Length == width * height * 3)
        {
            // Raw BGR array
            Color32[] pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                byte b = data[i * 3 + 0];
                byte g = data[i * 3 + 1];
                byte r = data[i * 3 + 2];
                pixels[i] = new Color32(r, g, b, 255); // convert BGR -> RGBA
            }

            displayTex.SetPixels32(pixels);
            displayTex.Apply();
        }
        else
        {
            // Possibly JPEG data
            displayTex.LoadImage(data);
        }
    }

    void OnGUI()
    {
        // Display the texture on screen for testing
        if (displayTex != null)
        {
            GUI.DrawTexture(new Rect(10, 10, width / 2, height / 2), displayTex, ScaleMode.ScaleToFit);
        }
    }
}
