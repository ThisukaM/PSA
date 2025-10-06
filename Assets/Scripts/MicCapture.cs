using UnityEngine;
using System;
using System.Collections;

public class MicCapture : MonoBehaviour
{
    public AudioSource audioSource;
    public int sampleRate = 16000;
    public int chunkLengthSec = 1;

    private string micName;
    private bool capturing = false;
    private int lastSamplePos = 0;

    // Event so other scripts can receive audio chunks
    public static event Action<float[]> OnAudioChunkCaptured;

    void Start()
    {
        // Check available microphones
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone detected!");
            return;
        }

        micName = Microphone.devices[0];
        Debug.Log("Using microphone: " + micName);

        // Start recording
        audioSource.clip = Microphone.Start(micName, true, 10, sampleRate);
        audioSource.loop = true;
        audioSource.Play();

        capturing = true;
        StartCoroutine(StreamAudioChunks());
    }

    private IEnumerator StreamAudioChunks()
    {
        int chunkSize = sampleRate * chunkLengthSec;
        float[] samples = new float[chunkSize];

        while (capturing)
        {
            int micPos = Microphone.GetPosition(micName);

            // Only proceed when enough new samples are available
            int diff = micPos - lastSamplePos;
            if (diff < 0) diff += audioSource.clip.samples; // looped around

            if (diff >= chunkSize)
            {
                audioSource.clip.GetData(samples, lastSamplePos);
                lastSamplePos = (lastSamplePos + chunkSize) % audioSource.clip.samples;

                // Trigger the event
                OnAudioChunkCaptured?.Invoke(samples);

                Debug.Log("1second chunk sent");
            }

            yield return null;
        }
    }

    void OnDisable()
    {
        capturing = false;
        if (micName != null)
        {
            Microphone.End(micName);
        }
    }
}
