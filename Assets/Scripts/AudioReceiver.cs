using UnityEngine;

public class AudioReceiver : MonoBehaviour
{
    void OnEnable()
    {
        MicCapture.OnAudioChunkCaptured += HandleAudioChunk;
    }

    void OnDisable()
    {
        MicCapture.OnAudioChunkCaptured -= HandleAudioChunk;
    }

    void HandleAudioChunk(float[] samples)
    {
        Debug.Log("Received audio chunk of length: " + samples.Length);

        // TODO: Send to ML model, WebSocket, or file

        //THIS IS FOR TESTING (FROM GPT), try shouting into mic and the avg volume number should be higher
        //just to test for if this live function is working,
        //should be able to maybe call server from here for mic at least.
        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
            sum += Mathf.Abs(samples[i]);
        float avg = sum / samples.Length;

        Debug.Log($"🎧 Received {samples.Length} samples. Avg volume: {avg:F4}");
        //can delete later ^^^^^^
    }
}
