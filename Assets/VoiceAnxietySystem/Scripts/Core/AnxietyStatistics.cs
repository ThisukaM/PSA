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
        [Header("音频设置")]
        private AudioSource audioSource;
        private string selectedMicrophone;
        private float[] audioBuffer;
        private int bufferSize = 4096; // 增大缓冲区以便更好的分析
        private int sampleRate = 44100;

        [Header("实时音频数据")]
        private float currentVolume = 0f;
        private float currentPitch = 0f;
        private bool isSpeaking = false;
        private float silenceDuration = 0f;

        [Header("焦虑特征")]
        [SerializeField] private float fillerWordScore = 0f; // 填充词得分
        [SerializeField] private float pauseScore = 0f; // 停顿得分
        [SerializeField] private float trembleScore = 0f; // 颤抖得分
        [SerializeField] private float speedVariationScore = 0f; // 语速变化得分
        [SerializeField] private float pitchInstabilityScore = 0f; // 音高不稳定得分

        [Header("焦虑等级")]
        [Range(0, 100)] public float anxietyLevel = 0f;
        public string anxietyStatus = "放松";

        // 语音模式检测
        private Queue<float> volumeHistory = new Queue<float>(100);
        private Queue<float> pitchHistory = new Queue<float>(100);
        private List<float> speechSegments = new List<float>();
        private List<float> pauseDurations = new List<float>();

        // 填充词检测
        private float lastSpeechTime = 0f;
        private int shortUtteranceCount = 0;
        private int fillerWordCount = 0;

        // 语速分析
        private float speechStartTime = 0f;
        private int syllableCount = 0;
        private float currentSpeechRate = 0f;

        // UI
        [Header("UI显示")]
        public Text anxietyLevelText;
        public Text anxietyStatusText;
        public Text featureText;
        public Slider anxietySlider;
        public Image sliderFill;

        // 调试
        [Header("调试信息")]
        public bool enableDebugLog = true;
        public Text debugText;

        // FFT相关
        private FFTWindow fftWindow = FFTWindow.BlackmanHarris;
        private float[] spectrumData;

        void Start()
        {
            InitializeAudio();
            StartCoroutine(AnalyzeAudioRoutine());
        }

        void InitializeAudio()
        {
            // 检查麦克风
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("未找到麦克风设备！");
                return;
            }

            selectedMicrophone = Microphone.devices[0];
            Debug.Log($"使用麦克风: {selectedMicrophone}");

            // 初始化音频组件
            audioSource = gameObject.AddComponent<AudioSource>();
            audioBuffer = new float[bufferSize];
            spectrumData = new float[bufferSize / 2];

            // 开始录音
            audioSource.clip = Microphone.Start(selectedMicrophone, true, 10, sampleRate);
            audioSource.loop = true;

            // 等待麦克风初始化
            while (!(Microphone.GetPosition(selectedMicrophone) > 0)) { }

            audioSource.Play();
            audioSource.volume = 0; // 静音播放，只用于数据分析

            Debug.Log("音频系统初始化完成");
        }

        IEnumerator AnalyzeAudioRoutine()
        {
            yield return new WaitForSeconds(0.5f); // 等待初始化

            while (true)
            {
                AnalyzeAudio();
                yield return new WaitForSeconds(0.05f); // 20Hz更新率
            }
        }

        void AnalyzeAudio()
        {
            if (audioSource == null || !audioSource.isPlaying) return;

            // 获取音频数据
            audioSource.GetOutputData(audioBuffer, 0);
            audioSource.GetSpectrumData(spectrumData, 0, fftWindow);

            // 基础音频分析
            AnalyzeVolume();
            AnalyzePitch();

            // 语音活动检测
            DetectSpeechActivity();

            // 焦虑特征分析
            AnalyzeFillerWords();
            AnalyzePauses();
            AnalyzeTremble();
            AnalyzeSpeechRate();
            AnalyzePitchStability();

            // 计算总体焦虑水平
            CalculateAnxietyLevel();

            // 更新UI
            UpdateUI();
        }

        void AnalyzeVolume()
        {
            // RMS音量计算
            float sum = 0;
            for (int i = 0; i < audioBuffer.Length; i++)
            {
                sum += audioBuffer[i] * audioBuffer[i];
            }
            currentVolume = Mathf.Sqrt(sum / audioBuffer.Length);

            // 记录历史
            volumeHistory.Enqueue(currentVolume);
            if (volumeHistory.Count > 100) volumeHistory.Dequeue();
        }

        void AnalyzePitch()
        {
            // 使用FFT找到主频率
            float maxVal = 0;
            int maxIndex = 0;

            // 只在人声频率范围内搜索 (80-400 Hz)
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

            // 只有在有足够音量时才更新音高
            if (currentVolume > 0.01f && maxVal > 0.001f)
            {
                currentPitch = maxIndex * sampleRate / (spectrumData.Length * 2f);

                // 使用抛物线插值提高精度
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

            // 记录历史
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

            // 检测说话开始和结束
            if (!wasSpeaking && isSpeaking)
            {
                // 开始说话
                speechStartTime = Time.time;

                // 记录静音时长
                if (silenceDuration > 0.1f)
                {
                    pauseDurations.Add(silenceDuration);
                    if (pauseDurations.Count > 20) pauseDurations.RemoveAt(0);
                }
                silenceDuration = 0;
            }
            else if (wasSpeaking && !isSpeaking)
            {
                // 结束说话
                float speechDuration = Time.time - speechStartTime;
                if (speechDuration > 0.1f)
                {
                    speechSegments.Add(speechDuration);
                    if (speechSegments.Count > 20) speechSegments.RemoveAt(0);

                    // 检测短促发音（可能是填充词）
                    if (speechDuration < 0.5f)
                    {
                        shortUtteranceCount++;
                    }
                }
            }

            // 累计静音时长
            if (!isSpeaking)
            {
                silenceDuration += Time.deltaTime;
            }
        }

        void AnalyzeFillerWords()
        {
            // 基于短促发音和音高模式检测填充词
            if (speechSegments.Count >= 5)
            {
                float avgSegmentLength = speechSegments.Average();
                int recentShortUtterances = speechSegments.Skip(Math.Max(0, speechSegments.Count - 5))
                    .Count(s => s < 0.5f);

                // 填充词特征：短促、频繁、音高较低
                fillerWordScore = 0;

                // 短促发音比例
                if (recentShortUtterances >= 2)
                {
                    fillerWordScore += 30f * (recentShortUtterances / 5f);
                }

                // 如果平均片段长度很短，可能有很多犹豫
                if (avgSegmentLength < 1.0f)
                {
                    fillerWordScore += 20f;
                }

                // 检测"嗯"、"啊"的音高特征（通常较低且平稳）
                if (isSpeaking && currentPitch > 0 && currentPitch < 150f)
                {
                    if (pitchHistory.Count > 5)
                    {
                        float pitchVariance = CalculateVariance(pitchHistory.Skip(pitchHistory.Count - 5).ToArray());
                        if (pitchVariance < 100f) // 音高平稳
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

                // 长停顿
                if (maxPause > 2.0f)
                {
                    pauseScore += 30f;
                }

                // 频繁停顿
                if (avgPause > 0.5f)
                {
                    pauseScore += 20f * Mathf.Min(avgPause, 2f);
                }

                // 不规律的停顿
                float pauseVariance = CalculateVariance(pauseDurations.ToArray());
                if (pauseVariance > 0.5f)
                {
                    pauseScore += 20f;
                }
            }

            // 当前正在长时间停顿
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
                // 分析音量波动（颤抖）
                var recentVolumes = volumeHistory.Skip(volumeHistory.Count - 20).ToArray();
                float volumeVariance = CalculateVariance(recentVolumes);

                // 高频率的音量波动可能表示声音颤抖
                if (volumeVariance > 0.0001f)
                {
                    trembleScore += 30f * Mathf.Min(volumeVariance * 10000f, 1f);
                }

                // 检测音量的快速变化
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
                // 计算语速变化
                var recentSegments = speechSegments.Skip(Math.Max(0, speechSegments.Count - 10)).ToArray();
                float variance = CalculateVariance(recentSegments);

                // 高方差表示语速不稳定
                if (variance > 0.5f)
                {
                    speedVariationScore = 50f * Mathf.Min(variance, 1f);
                }

                // 检测突然的语速变化
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

                // 计算音高变化率
                float totalChange = 0;
                for (int i = 1; i < recentPitches.Length; i++)
                {
                    totalChange += Mathf.Abs(recentPitches[i] - recentPitches[i - 1]);
                }

                float avgChange = totalChange / recentPitches.Length;

                // 大的音高变化表示声音不稳定
                if (avgChange > 10f)
                {
                    pitchInstabilityScore = 50f * Mathf.Min(avgChange / 50f, 1f);
                }

                // 检测音高突变（可能是声音破音）
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
            // 加权计算总焦虑水平
            anxietyLevel =
                fillerWordScore * 0.25f +      // 填充词权重25%
                pauseScore * 0.20f +            // 停顿权重20%
                trembleScore * 0.20f +          // 颤抖权重20%
                speedVariationScore * 0.20f +   // 语速变化权重20%
                pitchInstabilityScore * 0.15f;  // 音高不稳定权重15%

            // 平滑处理
            anxietyLevel = Mathf.Lerp(anxietyLevel, anxietyLevel, 0.1f);

            // 确定焦虑状态
            if (anxietyLevel < 20)
                anxietyStatus = "放松";
            else if (anxietyLevel < 40)
                anxietyStatus = "轻微紧张";
            else if (anxietyLevel < 60)
                anxietyStatus = "中度焦虑";
            else if (anxietyLevel < 80)
                anxietyStatus = "高度焦虑";
            else
                anxietyStatus = "极度焦虑";

            // 调试输出
            if (enableDebugLog && isSpeaking)
            {
                Debug.Log($"[焦虑分析] 总分:{anxietyLevel:F1} 填充词:{fillerWordScore:F0} " +
                         $"停顿:{pauseScore:F0} 颤抖:{trembleScore:F0} " +
                         $"语速:{speedVariationScore:F0} 音高:{pitchInstabilityScore:F0}");
            }
        }

        void UpdateUI()
        {
            if (anxietyLevelText != null)
            {
                anxietyLevelText.text = $"焦虑水平: {anxietyLevel:F1}%";
            }

            if (anxietyStatusText != null)
            {
                anxietyStatusText.text = $"状态: {anxietyStatus}";

                // 根据焦虑等级改变颜色
                if (anxietyLevel < 30)
                    anxietyStatusText.color = Color.green;
                else if (anxietyLevel < 60)
                    anxietyStatusText.color = Color.yellow;
                else
                    anxietyStatusText.color = Color.red;
            }

            if (featureText != null)
            {
                featureText.text = $"音高: {currentPitch:F0} Hz\n" +
                                  $"音量: {currentVolume * 100:F1}\n" +
                                  $"说话: {(isSpeaking ? "是" : "否")}\n" +
                                  $"填充词: {fillerWordScore:F0}%\n" +
                                  $"停顿: {pauseScore:F0}%\n" +
                                  $"颤抖: {trembleScore:F0}%";
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
                debugText.text = $"调试信息:\n" +
                                $"音高: {currentPitch:F1} Hz\n" +
                                $"音量: {currentVolume:F4}\n" +
                                $"语音段: {speechSegments.Count}\n" +
                                $"停顿数: {pauseDurations.Count}\n" +
                                $"短促音: {shortUtteranceCount}";
            }
        }

        // 工具方法：计算方差
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

        // 公共方法
        public float GetAnxietyLevel() => anxietyLevel;
        public string GetAnxietyStatus() => anxietyStatus;
        public bool IsSpeaking() => isSpeaking;
    }
}