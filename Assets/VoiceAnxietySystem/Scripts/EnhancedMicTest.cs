using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EnhancedMicTest : MonoBehaviour
{
    [Header("��Ƶ����")]
    private AudioSource audioSource;
    private float[] samples;
    private float[] spectrum;

    [Header("UI��ʾ")]
    public Text statusText;
    public Text pitchText;
    public Text volumeText;
    public Slider volumeSlider;

    [Header("��Ƶ����")]
    private float currentPitch = 0f;
    private float currentVolume = 0f;
    private float maxVolume = 0f;

    void Start()
    {
        InitializeMicrophone();
        StartCoroutine(UpdateAudio());
    }

    void InitializeMicrophone()
    {
        Debug.Log("=== ��ǿ��˷���� ===");

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("û���ҵ���˷磡");
            UpdateStatus("����δ�ҵ���˷�", Color.red);
            return;
        }

        // �г������豸
        foreach (string device in Microphone.devices)
        {
            Debug.Log($"�ҵ���˷�: {device}");
        }

        // ��ʼ��
        samples = new float[1024];
        spectrum = new float[512];

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = Microphone.Start(null, true, 1, 44100);
        audioSource.loop = true;

        while (!(Microphone.GetPosition(null) > 0)) { }

        audioSource.Play();
        audioSource.volume = 0;

        UpdateStatus("��˷����", Color.green);
    }

    IEnumerator UpdateAudio()
    {
        while (true)
        {
            AnalyzeAudio();
            yield return new WaitForSeconds(0.05f);
        }
    }

    void AnalyzeAudio()
    {
        if (!audioSource || !audioSource.isPlaying) return;

        // ��ȡ��Ƶ����
        audioSource.GetOutputData(samples, 0);
        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        // ��������
        float sum = 0;
        foreach (float s in samples)
        {
            sum += s * s;
        }
        currentVolume = Mathf.Sqrt(sum / samples.Length);
        maxVolume = Mathf.Max(maxVolume, currentVolume);

        // �������ߣ�FFT��
        if (currentVolume > 0.01f)
        {
            float maxVal = 0;
            int maxIndex = 0;

            // ��������Χ��Ѱ�ҷ�ֵ
            for (int i = 2; i < spectrum.Length / 2; i++)
            {
                if (spectrum[i] > maxVal)
                {
                    maxVal = spectrum[i];
                    maxIndex = i;
                }
            }

            if (maxVal > 0.001f)
            {
                currentPitch = maxIndex * 44100f / (spectrum.Length * 2);

                // ������������Χ
                if (currentPitch < 80 || currentPitch > 500)
                {
                    currentPitch = 0;
                }
            }
        }
        else
        {
            currentPitch = 0;
        }

        UpdateUI();

        // �������
        if (currentVolume > 0.01f)
        {
            Debug.Log($"[��Ƶ] ����:{currentVolume:F3} ����:{currentPitch:F0}Hz");
        }
    }

    void UpdateUI()
    {
        if (volumeText != null)
        {
            volumeText.text = $"����: {(currentVolume * 100):F1} / ���: {(maxVolume * 100):F1}";
        }

        if (pitchText != null)
        {
            string pitchInfo = currentPitch > 0 ? $"{currentPitch:F0} Hz" : "����";
            pitchText.text = $"����: {pitchInfo}";

            if (currentPitch > 0)
            {
                // ��ʾ���߷�Χ
                if (currentPitch < 150)
                    pitchText.text += " (����)";
                else if (currentPitch < 250)
                    pitchText.text += " (����)";
                else
                    pitchText.text += " (����)";
            }
        }

        if (volumeSlider != null)
        {
            volumeSlider.value = currentVolume * 10; // �Ŵ���ʾ
        }
    }

    void UpdateStatus(string message, Color color)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = color;
        }
        Debug.Log(message);
    }

    void OnDestroy()
    {
        if (Microphone.IsRecording(null))
        {
            Microphone.End(null);
        }
    }
}
