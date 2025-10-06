using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EnhancedMicTest : MonoBehaviour
{
    [Header("音频设置")]
    private AudioSource audioSource;
    private float[] samples;
    private float[] spectrum;

    [Header("UI显示")]
    public Text statusText;
    public Text pitchText;
    public Text volumeText;
    public Slider volumeSlider;

    [Header("音频数据")]
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
        Debug.Log("=== 增强麦克风测试 ===");

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("没有找到麦克风！");
            UpdateStatus("错误：未找到麦克风", Color.red);
            return;
        }

        // 列出所有设备
        foreach (string device in Microphone.devices)
        {
            Debug.Log($"找到麦克风: {device}");
        }

        // 初始化
        samples = new float[1024];
        spectrum = new float[512];

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = Microphone.Start(null, true, 1, 44100);
        audioSource.loop = true;

        while (!(Microphone.GetPosition(null) > 0)) { }

        audioSource.Play();
        audioSource.volume = 0;

        UpdateStatus("麦克风就绪", Color.green);
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

        // 获取音频数据
        audioSource.GetOutputData(samples, 0);
        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        // 计算音量
        float sum = 0;
        foreach (float s in samples)
        {
            sum += s * s;
        }
        currentVolume = Mathf.Sqrt(sum / samples.Length);
        maxVolume = Mathf.Max(maxVolume, currentVolume);

        // 计算音高（FFT）
        if (currentVolume > 0.01f)
        {
            float maxVal = 0;
            int maxIndex = 0;

            // 在人声范围内寻找峰值
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

                // 限制在人声范围
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

        // 调试输出
        if (currentVolume > 0.01f)
        {
            Debug.Log($"[音频] 音量:{currentVolume:F3} 音高:{currentPitch:F0}Hz");
        }
    }

    void UpdateUI()
    {
        if (volumeText != null)
        {
            volumeText.text = $"音量: {(currentVolume * 100):F1} / 最大: {(maxVolume * 100):F1}";
        }

        if (pitchText != null)
        {
            string pitchInfo = currentPitch > 0 ? $"{currentPitch:F0} Hz" : "静音";
            pitchText.text = $"音高: {pitchInfo}";

            if (currentPitch > 0)
            {
                // 显示音高范围
                if (currentPitch < 150)
                    pitchText.text += " (低音)";
                else if (currentPitch < 250)
                    pitchText.text += " (中音)";
                else
                    pitchText.text += " (高音)";
            }
        }

        if (volumeSlider != null)
        {
            volumeSlider.value = currentVolume * 10; // 放大显示
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
