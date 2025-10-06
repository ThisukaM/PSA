using UnityEngine;
using UnityEngine.UI;

public class SimpleMicTest : MonoBehaviour
{
    private AudioSource audioSource;
    private float[] samples = new float[1024];
    public Text debugText; // ��ѡ��������ʾ��UI��

    void Start()
    {
        // �г�������˷�
        Debug.Log("=== ����˷���� ===");
        foreach (string device in Microphone.devices)
        {
            Debug.Log($"��˷��豸: {device}");
        }

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("û���ҵ���˷磡");
            return;
        }

        // ��ʼ����˷�
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = Microphone.Start(null, true, 1, 44100);
        audioSource.loop = true;

        // �ȴ���˷�׼����
        while (!(Microphone.GetPosition(null) > 0)) { }

        // ���ţ�������Ϊ0��ֻ���ڶ�ȡ���ݣ�
        audioSource.Play();
        audioSource.volume = 0;

        Debug.Log("��˷���Կ�ʼ���������˷�˵��...");
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
                string msg = $"[��˷�] ƽ������: {average * 100:F2}, ��ֵ: {max * 100:F2}";
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