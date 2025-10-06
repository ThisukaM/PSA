using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace VoiceAnxietySystem
{
    public class AdvancedAnxietyAnalyzer : MonoBehaviour
    {
        [Header("��Ƶ����")]
        private AudioSource audioSource;
        private string selectedMicrophone;
        private float[] audioBuffer;
        private int bufferSize = 4096; // ���󻺳����Ա���õķ���
        private int sampleRate = 44100;

        [Header("ʵʱ��Ƶ����")]
        private float currentVolume = 0f;
        private float currentPitch = 0f;
        private bool isSpeaking = false;
        private float silenceDuration = 0f;

        [Header("��������")]
        [SerializeField] private float fillerWordScore = 0f; // ���ʵ÷�
        [SerializeField] private float pauseScore = 0f; // ͣ�ٵ÷�
        [SerializeField] private float trembleScore = 0f; // �����÷�
        [SerializeField] private float speedVariationScore = 0f; // ���ٱ仯�÷�
        [SerializeField] private float pitchInstabilityScore = 0f; // ���߲��ȶ��÷�

        [Header("���ǵȼ�")]
        [Range(0, 100)] public float anxietyLevel = 0f;
        public string anxietyStatus = "����";

        // ����ģʽ���
        private Queue<float> volumeHistory = new Queue<float>(100);
        private Queue<float> pitchHistory = new Queue<float>(100);
        private List<float> speechSegments = new List<float>();
        private List<float> pauseDurations = new List<float>();

        // ���ʼ��
        private float lastSpeechTime = 0f;
        private int shortUtteranceCount = 0;
        private int fillerWordCount = 0;

        // ���ٷ���
        private float speechStartTime = 0f;
        private int syllableCount = 0;
        private float currentSpeechRate = 0f;

        // UI
        [Header("UI��ʾ")]
        public Text anxietyLevelText;
        public Text anxietyStatusText;
        public Text featureText;
        public Slider anxietySlider;
        public Image sliderFill;

        // ����
        [Header("������Ϣ")]
        public bool enableDebugLog = true;
        public Text debugText;

        // FFT���
        private FFTWindow fftWindow = FFTWindow.BlackmanHarris;
        private float[] spectrumData;

        void Start()
        {
            InitializeAudio();
            StartCoroutine(AnalyzeAudioRoutine());
        }

        void InitializeAudio()
        {
            // �����˷�
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("δ�ҵ���˷��豸��");
                return;
            }

            selectedMicrophone = Microphone.devices[0];
            Debug.Log($"ʹ����˷�: {selectedMicrophone}");

            // ��ʼ����Ƶ���
            audioSource = gameObject.AddComponent<AudioSource>();
            audioBuffer = new float[bufferSize];
            spectrumData = new float[bufferSize / 2];

            // ��ʼ¼��
            audioSource.clip = Microphone.Start(selectedMicrophone, true, 10, sampleRate);
            audioSource.loop = true;

            // �ȴ���˷��ʼ��
            while (!(Microphone.GetPosition(selectedMicrophone) > 0)) { }

            audioSource.Play();
            audioSource.volume = 0; // �������ţ�ֻ�������ݷ���

            Debug.Log("��Ƶϵͳ��ʼ�����");
        }

        IEnumerator AnalyzeAudioRoutine()
        {
            yield return new WaitForSeconds(0.5f); // �ȴ���ʼ��

            while (true)
            {
                AnalyzeAudio();
                yield return new WaitForSeconds(0.05f); // 20Hz������
            }
        }

        void AnalyzeAudio()
        {
            if (audioSource == null || !audioSource.isPlaying) return;

            // ��ȡ��Ƶ����
            audioSource.GetOutputData(audioBuffer, 0);
            audioSource.GetSpectrumData(spectrumData, 0, fftWindow);

            // ������Ƶ����
            AnalyzeVolume();
            AnalyzePitch();

            // ��������
            DetectSpeechActivity();

            // ������������
            AnalyzeFillerWords();
            AnalyzePauses();
            AnalyzeTremble();
            AnalyzeSpeechRate();
            AnalyzePitchStability();

            // �������役��ˮƽ
            CalculateAnxietyLevel();

            // ����UI
            UpdateUI();
        }

        void AnalyzeVolume()
        {
            // RMS��������
            float sum = 0;
            for (int i = 0; i < audioBuffer.Length; i++)
            {
                sum += audioBuffer[i] * audioBuffer[i];
            }
            currentVolume = Mathf.Sqrt(sum / audioBuffer.Length);

            // ��¼��ʷ
            volumeHistory.Enqueue(currentVolume);
            if (volumeHistory.Count > 100) volumeHistory.Dequeue();
        }

        void AnalyzePitch()
        {
            // ʹ��FFT�ҵ���Ƶ��
            float maxVal = 0;
            int maxIndex = 0;

            // ֻ������Ƶ�ʷ�Χ������ (80-400 Hz)
            int minBin = Mathf.FloorToInt(80f * spectrumData.Length * 2 / sampleRate);
            int maxBin = Mathf.FloorToInt(400f * spectrumData.Length * 2 / sampleRate);

            for (int i = minBin; i < Mathf.Min(maxBin, spectrumData.Length); i++)
            {
                if (spectrumData[i] > maxVal)
                {
                    maxVal = spectrumData[i];
                    maxIndex = i;
                }
            }

            // ֻ�������㹻����ʱ�Ÿ�������
            if (currentVolume > 0.01f && maxVal > 0.001f)
            {
                currentPitch = maxIndex * sampleRate / (spectrumData.Length * 2f);

                // ʹ�������߲�ֵ��߾���
                if (maxIndex > 0 && maxIndex < spectrumData.Length - 1)
                {
                    float y1 = spectrumData[maxIndex - 1];
                    float y2 = spectrumData[maxIndex];
                    float y3 = spectrumData[maxIndex + 1];
                    float x0 = (y3 - y1) / (2 * (2 * y2 - y1 - y3));
                    currentPitch = (maxIndex + x0) * sampleRate / (spectrumData.Length * 2f);
                }
            }
            else
            {
                currentPitch = 0;
            }

            // ��¼��ʷ
            if (currentPitch > 0)
            {
                pitchHistory.Enqueue(currentPitch);
                if (pitchHistory.Count > 100) pitchHistory.Dequeue();
            }
        }

        void DetectSpeechActivity()
        {
            bool wasSpeaking = isSpeaking;
            isSpeaking = currentVolume > 0.01f && currentPitch > 80f;

            // ���˵����ʼ�ͽ���
            if (!wasSpeaking && isSpeaking)
            {
                // ��ʼ˵��
                speechStartTime = Time.time;

                // ��¼����ʱ��
                if (silenceDuration > 0.1f)
                {
                    pauseDurations.Add(silenceDuration);
                    if (pauseDurations.Count > 20) pauseDurations.RemoveAt(0);
                }
                silenceDuration = 0;
            }
            else if (wasSpeaking && !isSpeaking)
            {
                // ����˵��
                float speechDuration = Time.time - speechStartTime;
                if (speechDuration > 0.1f)
                {
                    speechSegments.Add(speechDuration);
                    if (speechSegments.Count > 20) speechSegments.RemoveAt(0);

                    // ���̴ٷ��������������ʣ�
                    if (speechDuration < 0.5f)
                    {
                        shortUtteranceCount++;
                    }
                }
            }

            // �ۼƾ���ʱ��
            if (!isSpeaking)
            {
                silenceDuration += Time.deltaTime;
            }
        }

        void AnalyzeFillerWords()
        {
            // ���ڶ̴ٷ���������ģʽ�������
            if (speechSegments.Count >= 5)
            {
                float avgSegmentLength = speechSegments.Average();
                int recentShortUtterances = speechSegments.Skip(Math.Max(0, speechSegments.Count - 5))
                    .Count(s => s < 0.5f);

                // �����������̴١�Ƶ�������߽ϵ�
                fillerWordScore = 0;

                // �̴ٷ�������
                if (recentShortUtterances >= 2)
                {
                    fillerWordScore += 30f * (recentShortUtterances / 5f);
                }

                // ���ƽ��Ƭ�γ��Ⱥ̣ܶ������кܶ���ԥ
                if (avgSegmentLength < 1.0f)
                {
                    fillerWordScore += 20f;
                }

                // ���"��"��"��"������������ͨ���ϵ���ƽ�ȣ�
                if (isSpeaking && currentPitch > 0 && currentPitch < 150f)
                {
                    if (pitchHistory.Count > 5)
                    {
                        float pitchVariance = CalculateVariance(pitchHistory.Skip(pitchHistory.Count - 5).ToArray());
                        if (pitchVariance < 100f) // ����ƽ��
                        {
                            fillerWordScore += 10f;
                        }
                    }
                }

                fillerWordScore = Mathf.Clamp(fillerWordScore, 0, 100);
            }
        }

        void AnalyzePauses()
        {
            pauseScore = 0;

            if (pauseDurations.Count >= 3)
            {
                float avgPause = pauseDurations.Average();
                float maxPause = pauseDurations.Max();

                // ��ͣ��
                if (maxPause > 2.0f)
                {
                    pauseScore += 30f;
                }

                // Ƶ��ͣ��
                if (avgPause > 0.5f)
                {
                    pauseScore += 20f * Mathf.Min(avgPause, 2f);
                }

                // �����ɵ�ͣ��
                float pauseVariance = CalculateVariance(pauseDurations.ToArray());
                if (pauseVariance > 0.5f)
                {
                    pauseScore += 20f;
                }
            }

            // ��ǰ���ڳ�ʱ��ͣ��
            if (!isSpeaking && silenceDuration > 1.5f)
            {
                pauseScore += 10f * Mathf.Min(silenceDuration / 3f, 1f);
            }

            pauseScore = Mathf.Clamp(pauseScore, 0, 100);
        }

        void AnalyzeTremble()
        {
            trembleScore = 0;

            if (volumeHistory.Count >= 20 && isSpeaking)
            {
                // ��������������������
                var recentVolumes = volumeHistory.Skip(volumeHistory.Count - 20).ToArray();
                float volumeVariance = CalculateVariance(recentVolumes);

                // ��Ƶ�ʵ������������ܱ�ʾ��������
                if (volumeVariance > 0.0001f)
                {
                    trembleScore += 30f * Mathf.Min(volumeVariance * 10000f, 1f);
                }

                // ��������Ŀ��ٱ仯
                int rapidChanges = 0;
                for (int i = 1; i < recentVolumes.Length; i++)
                {
                    if (Mathf.Abs(recentVolumes[i] - recentVolumes[i - 1]) > 0.005f)
                    {
                        rapidChanges++;
                    }
                }

                if (rapidChanges > 5)
                {
                    trembleScore += 20f * (rapidChanges / 20f);
                }
            }

            trembleScore = Mathf.Clamp(trembleScore, 0, 100);
        }

        void AnalyzeSpeechRate()
        {
            speedVariationScore = 0;

            if (speechSegments.Count >= 5)
            {
                // �������ٱ仯
                var recentSegments = speechSegments.Skip(Math.Max(0, speechSegments.Count - 10)).ToArray();
                float variance = CalculateVariance(recentSegments);

                // �߷����ʾ���ٲ��ȶ�
                if (variance > 0.5f)
                {
                    speedVariationScore = 50f * Mathf.Min(variance, 1f);
                }

                // ���ͻȻ�����ٱ仯
                for (int i = 1; i < recentSegments.Length; i++)
                {
                    float ratio = recentSegments[i] / recentSegments[i - 1];
                    if (ratio > 2f || ratio < 0.5f)
                    {
                        speedVariationScore += 10f;
                    }
                }
            }

            speedVariationScore = Mathf.Clamp(speedVariationScore, 0, 100);
        }

        void AnalyzePitchStability()
        {
            pitchInstabilityScore = 0;

            if (pitchHistory.Count >= 10 && isSpeaking)
            {
                var recentPitches = pitchHistory.Skip(pitchHistory.Count - 10).ToArray();

                // �������߱仯��
                float totalChange = 0;
                for (int i = 1; i < recentPitches.Length; i++)
                {
                    totalChange += Mathf.Abs(recentPitches[i] - recentPitches[i - 1]);
                }

                float avgChange = totalChange / recentPitches.Length;

                // ������߱仯��ʾ�������ȶ�
                if (avgChange > 10f)
                {
                    pitchInstabilityScore = 50f * Mathf.Min(avgChange / 50f, 1f);
                }

                // �������ͻ�䣨����������������
                for (int i = 1; i < recentPitches.Length; i++)
                {
                    if (Mathf.Abs(recentPitches[i] - recentPitches[i - 1]) > 50f)
                    {
                        pitchInstabilityScore += 20f;
                    }
                }
            }

            pitchInstabilityScore = Mathf.Clamp(pitchInstabilityScore, 0, 100);
        }

        void CalculateAnxietyLevel()
        {
            // ��Ȩ�����ܽ���ˮƽ
            anxietyLevel =
                fillerWordScore * 0.25f +      // ����Ȩ��25%
                pauseScore * 0.20f +            // ͣ��Ȩ��20%
                trembleScore * 0.20f +          // ����Ȩ��20%
                speedVariationScore * 0.20f +   // ���ٱ仯Ȩ��20%
                pitchInstabilityScore * 0.15f;  // ���߲��ȶ�Ȩ��15%

            // ƽ������
            anxietyLevel = Mathf.Lerp(anxietyLevel, anxietyLevel, 0.1f);

            // ȷ������״̬
            if (anxietyLevel < 20)
                anxietyStatus = "����";
            else if (anxietyLevel < 40)
                anxietyStatus = "��΢����";
            else if (anxietyLevel < 60)
                anxietyStatus = "�жȽ���";
            else if (anxietyLevel < 80)
                anxietyStatus = "�߶Ƚ���";
            else
                anxietyStatus = "���Ƚ���";

            // �������
            if (enableDebugLog && isSpeaking)
            {
                Debug.Log($"[���Ƿ���] �ܷ�:{anxietyLevel:F1} ����:{fillerWordScore:F0} " +
                         $"ͣ��:{pauseScore:F0} ����:{trembleScore:F0} " +
                         $"����:{speedVariationScore:F0} ����:{pitchInstabilityScore:F0}");
            }
        }

        void UpdateUI()
        {
            if (anxietyLevelText != null)
            {
                anxietyLevelText.text = $"����ˮƽ: {anxietyLevel:F1}%";
            }

            if (anxietyStatusText != null)
            {
                anxietyStatusText.text = $"״̬: {anxietyStatus}";

                // ���ݽ��ǵȼ��ı���ɫ
                if (anxietyLevel < 30)
                    anxietyStatusText.color = Color.green;
                else if (anxietyLevel < 60)
                    anxietyStatusText.color = Color.yellow;
                else
                    anxietyStatusText.color = Color.red;
            }

            if (featureText != null)
            {
                featureText.text = $"����: {currentPitch:F0} Hz\n" +
                                  $"����: {currentVolume * 100:F1}\n" +
                                  $"˵��: {(isSpeaking ? "��" : "��")}\n" +
                                  $"����: {fillerWordScore:F0}%\n" +
                                  $"ͣ��: {pauseScore:F0}%\n" +
                                  $"����: {trembleScore:F0}%";
            }

            if (anxietySlider != null)
            {
                anxietySlider.value = anxietyLevel / 100f;

                if (sliderFill != null)
                {
                    sliderFill.color = Color.Lerp(Color.green, Color.red, anxietyLevel / 100f);
                }
            }

            if (debugText != null)
            {
                debugText.text = $"������Ϣ:\n" +
                                $"����: {currentPitch:F1} Hz\n" +
                                $"����: {currentVolume:F4}\n" +
                                $"������: {speechSegments.Count}\n" +
                                $"ͣ����: {pauseDurations.Count}\n" +
                                $"�̴���: {shortUtteranceCount}";
            }
        }

        // ���߷��������㷽��
        float CalculateVariance(float[] values)
        {
            if (values.Length == 0) return 0;

            float mean = values.Average();
            float sumOfSquares = 0;

            foreach (float value in values)
            {
                sumOfSquares += (value - mean) * (value - mean);
            }

            return sumOfSquares / values.Length;
        }

        void OnDestroy()
        {
            if (Microphone.IsRecording(selectedMicrophone))
            {
                Microphone.End(selectedMicrophone);
            }
        }

        // ��������
        public float GetAnxietyLevel() => anxietyLevel;
        public string GetAnxietyStatus() => anxietyStatus;
        public bool IsSpeaking() => isSpeaking;
    }
}