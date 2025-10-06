using UnityEngine;
using UnityEngine.UI;

public class SimpleMicTest : MonoBehaviour
{
    private AudioSource audioSource;
    private float[] samples = new float[1024];
    public Text debugText; // 可选，用于显示在UI上

    void Start()
    {
        // 列出所有麦克风
        Debug.Log("=== 简单麦克风测试 ===");
        foreach (string device in Microphone.devices)
        {
            Debug.Log($"麦克风设备: {device}");
        }

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("没有找到麦克风！");
            return;
        }

        // 初始化麦克风
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = Microphone.Start(null, true, 1, 44100);
        audioSource.loop = true;

        // 等待麦克风准备好
        while (!(Microphone.GetPosition(null) > 0)) { }

        // 播放（音量设为0，只用于读取数据）
        audioSource.Play();
        audioSource.volume = 0;

        Debug.Log("麦克风测试开始，请对着麦克风说话...");
    }

    void Update()
    {
        if (audioSource && audioSource.isPlaying)
        {
            audioSource.GetOutputData(samples, 0);

            float sum = 0;
            float max = 0;

            foreach (float s in samples)
            {
                float abs = Mathf.Abs(s);
                sum += abs;
                max = Mathf.Max(max, abs);
            }

            float average = sum / samples.Length;

            if (average > 0.001f)
            {
                string msg = $"[麦克风] 平均音量: {average * 100:F2}, 峰值: {max * 100:F2}";
                Debug.Log(msg);

                if (debugText != null)
                {
                    debugText.text = msg;
                    debugText.color = Color.Lerp(Color.green, Color.red, average * 10);
                }
            }
        }
    }

    void OnDestroy()
    {
        if (Microphone.IsRecording(null))
        {
            Microphone.End(null);
        }
    }
}