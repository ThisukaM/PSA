using UnityEngine;

public class MicCapture : MonoBehaviour
{
    public AudioSource audioSource; // assign an AudioSource in the inspector
    private string micName;

    void Start()
    {
        // List all available microphones
        foreach (var device in Microphone.devices)
        {
            Debug.Log("Microphone found: " + device);
        }

        if (Microphone.devices.Length > 0)
        {
            micName = Microphone.devices[0]; // choose first mic
            Debug.Log("Using microphone: " + micName);

            // Start recording from mic
            // Args: device name, loop, lengthSec, frequency
            audioSource.clip = Microphone.Start(micName, true, 10, 44100);
            audioSource.loop = true;

            // Wait until recording starts
            while (!(Microphone.GetPosition(micName) > 0)) { }

            // Play back the mic input in real-time
            audioSource.Play();
        }
        else
        {
            Debug.LogError("No microphone detected!");
        }
    }

    void OnDisable()
    {
        if (micName != null)
        {
            Microphone.End(micName);
        }
    }
}
