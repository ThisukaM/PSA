using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace VoiceAnxietySystem
{
    public class VoiceAnxietyAnalyzer : MonoBehaviour
    {
        [Header("音频设置")]
        private AudioSource audioSource;
        private string selectedMicrophone;
        private float[] audioSamples = new float[1024];
        private float[] spectrum = new float[512];
        private int lastSamplePosition = 0;
        private const int SAMPLE_WINDOW = 1024;
        private int sampleRate = 44100;

        [Header("焦虑检测参数")]
        [Range(0, 1)] public float smoothedAnxietyLevel = 0f;
        private float currentAnxietyLevel = 0f;
        private float instantAnxietySpike = 0f;

        // 语音特征
        private float currentPitch = 0f;
        private float pitchVariation = 0f;
        private float voiceIntensity = 0f;
        private float speakingRate = 0f;
        private float pauseFrequency = 0f;

        // ========== 新增：沉默时间跟踪 ==========
        private float silenceStartTime = 0f;
        private bool isInSilence = false;
        private float continuousSilenceDuration = 0f;
        private const float ANXIETY_SILENCE_THRESHOLD = 1.0f; // Changed from 1.2f to 1.0f for clarity
        private const float NORMAL_PAUSE_THRESHOLD = 0.5f; // Increased from 0.3f - pauses under 500ms are totally normal

        // 填充词检测（增强版）
        private float fillerWordDetectionScore = 0f;
        private int consecutiveLowPitchFrames = 0;
        private float lastHighIntensityTime = 0f;
        private List<float> microPauseTimestamps = new List<float>();
        private Queue<float> recentFillerScores = new Queue<float>(); // 追踪最近的填充词分数
        private float cumulativeFillerScore = 0f; // 累积填充词分数

        // 气音特征检测
        private float breathinessScore = 0f; // 气音程度
        private float nasalityScore = 0f; // 鼻音程度
        private float lowEnergyVoiceScore = 0f; // 低能量发声
        private int hesitationFrames = 0; // 犹豫帧计数
        private float lastSpeechEndTime = 0f;
        private bool wasJustSpeaking = false;

        // 重复检测
        private Queue<VoicePattern> recentPatterns = new Queue<VoicePattern>();
        private float repetitionScore = 0f;
        private Dictionary<string, int> patternFrequency = new Dictionary<string, int>();

        // 语音节奏异常检测
        private float rhythmIrregularityScore = 0f;
        private List<float> syllableTimings = new List<float>();

        // 呼吸模式检测
        private float breathingIrregularityScore = 0f;
        private List<float> breathIntervals = new List<float>();
        private float lastBreathTime = 0f;

        // 声音紧张度检测
        private float voiceTensionScore = 0f;
        private float harmonicDistortionLevel = 0f;

        // 语速突变检测
        private float speechRateChangeScore = 0f;
        private Queue<float> recentSpeechRates = new Queue<float>();

        // 音量波动检测
        private float volumeFluctuationScore = 0f;
        private Queue<float> recentVolumes = new Queue<float>();

        // 历史数据
        private Queue<float> pitchHistory = new Queue<float>();
        private Queue<float> intensityHistory = new Queue<float>();
        private List<float> anxietyHistory = new List<float>();

        // ========== 新增：语音活动历史 ==========
        private Queue<bool> speechActivityHistory = new Queue<bool>();
        private float lastSpeechTime = 0f;

        // UI引用
        [Header("UI显示")]
        public Text anxietyText;
        public Slider anxietySlider;
        public Text pitchText;
        public Text intensityText;
        public Text statusText;
        public Text detailText;

        // 灵敏度设置
        [Header("灵敏度控制")]
        [Range(0.5f, 3.0f)] public float sensitivityMultiplier = 1.5f;
        [Range(0.01f, 0.3f)] public float responseSpeed = 0.15f;
        public bool enableInstantSpikes = true;

        // 事件
        public static event Action<float> OnAnxietyLevelChanged;
        public static event Action<string> OnFillerWordDetected;
        public static event Action<string> OnRepetitionDetected;

        // 数据记录
        private bool isRecording = false;
        private float recordingStartTime;
        private List<AnxietyDataPoint> recordedData = new List<AnxietyDataPoint>();

        // 内部类：语音模式
        private class VoicePattern
        {
            public float pitch;
            public float intensity;
            public float duration;
            public float timestamp;

            public string GetSignature()
            {
                return $"{Mathf.RoundToInt(pitch / 10)}_{Mathf.RoundToInt(intensity * 100)}_{Mathf.RoundToInt(duration * 10)}";
            }
        }

        void Start()
        {
            InitializeMicrophone();
            //InvokeRepeating(nameof(AnalyzeVoice), 0.02f, 0.02f); // 提高到50Hz分析频率
            // InvokeRepeating(nameof(DetectFillerWords), 0.03f, 0.03f); // 33Hz填充词检测

            InvokeRepeating(nameof(AnalyzeVoice), 0.05f, 0.05f);
            InvokeRepeating(nameof(DetectFillerWords), 0.1f, 0.1f);
            InvokeRepeating(nameof(AnalyzeRepetitions), 0.2f, 0.2f);
            //   InvokeRepeating(nameof(DetectHesitation), 0.05f, 0.05f); // 犹豫检测
        }

        void InitializeMicrophone()
        {
            string[] devices = Microphone.devices;

            if (devices.Length == 0)
            {
                Debug.LogError("未检测到麦克风！");
                UpdateStatus("错误：未找到麦克风");
                return;
            }

            selectedMicrophone = devices[0];
            Debug.Log($"使用麦克风: {selectedMicrophone}");

            int minFreq, maxFreq;
            Microphone.GetDeviceCaps(selectedMicrophone, out minFreq, out maxFreq);

            if (maxFreq == 0) maxFreq = 44100;
            sampleRate = maxFreq;

            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.clip = Microphone.Start(selectedMicrophone, true, 10, sampleRate);
            audioSource.loop = false;

            UpdateStatus("麦克风就绪 - 高灵敏度模式");
            Debug.Log("麦克风初始化完成 - 灵敏度增强");
        }

        void Update()
        {
            if (!Microphone.IsRecording(selectedMicrophone)) return;

            int currentPosition = Microphone.GetPosition(selectedMicrophone);

            if (currentPosition < lastSamplePosition)
            {
                lastSamplePosition = 0;
            }

            if (currentPosition - lastSamplePosition >= SAMPLE_WINDOW)
            {
                float[] samples = new float[SAMPLE_WINDOW];
                audioSource.clip.GetData(samples, lastSamplePosition);

                audioSamples = samples;
                CalculateSpectrum(samples);

                // 实时检测语音特征
                DetectMicroPauses();
                DetectVoiceTension();
                DetectBreathingPattern();
                UpdateSilenceTracking(); // 新增：更新沉默跟踪

                lastSamplePosition = currentPosition - SAMPLE_WINDOW / 2;

                float maxAmplitude = 0;
                foreach (float s in samples)
                {
                    maxAmplitude = Mathf.Max(maxAmplitude, Mathf.Abs(s));
                }

                UpdateUI();
            }
        }

        // ========== 新增方法：跟踪沉默时间 ==========
        void UpdateSilenceTracking()
        {
            bool isSpeaking = voiceIntensity > 0.005f;

            // Update speech activity history
            speechActivityHistory.Enqueue(isSpeaking);
            if (speechActivityHistory.Count > 20) speechActivityHistory.Dequeue();

            if (isSpeaking)
            {
                // Currently speaking
                if (isInSilence)
                {
                    // Recovering from silence to speech
                    continuousSilenceDuration = 0f;
                    isInSilence = false;
                }
                lastSpeechTime = Time.time;
                silenceStartTime = 0f;
            }
            else
            {
                // Not speaking
                if (!isInSilence)
                {
                    // Just started silence
                    isInSilence = true;
                    silenceStartTime = Time.time;
                }

                // Calculate continuous silence time
                if (silenceStartTime > 0)
                {
                    continuousSilenceDuration = Time.time - silenceStartTime;
                }
            }
        }

        // ========== 新增方法：判断是否在对话中 ==========
        bool IsInConversation()
        {
            // 如果最近2秒内有说话，认为是在对话中
            return Time.time - lastSpeechTime < 2.0f;
        }

        // 检测填充词
        // 检测填充词（超灵敏版本）
        void DetectFillerWords()
        {
            fillerWordDetectionScore = 0f;

            // ===== 核心检测1：极低音量的发声 =====
            // 气音通常音量很小
            if (voiceIntensity > 0.001f && voiceIntensity < 0.015f)
            {
                lowEnergyVoiceScore = Mathf.Lerp(lowEnergyVoiceScore, 60f, 0.3f);
                fillerWordDetectionScore += lowEnergyVoiceScore;

                // 如果同时音高较低，很可能是"um"或"uh"
                if (currentPitch > 0 && currentPitch < 180f)
                {
                    fillerWordDetectionScore += 35f;
                    Debug.Log($"[气音检测] 低能量+低音高 强度:{voiceIntensity:F4} 音高:{currentPitch:F1}");
                }
            }
            else
            {
                lowEnergyVoiceScore = Mathf.Lerp(lowEnergyVoiceScore, 0f, 0.1f);
            }

            // ===== 核心检测2：音高特征（更宽松的范围）=====
            if (currentPitch > 0)
            {
                // "um", "uh" 通常在 80-160Hz
                if (currentPitch >= 80f && currentPitch <= 160f)
                {
                    consecutiveLowPitchFrames++;

                    // 只需要1帧就开始计分（更灵敏）
                    if (consecutiveLowPitchFrames >= 1)
                    {
                        float frameBonus = Mathf.Min(consecutiveLowPitchFrames * 15f, 60f);
                        fillerWordDetectionScore += frameBonus;

                        // 音高越稳定，越可能是填充词
                        if (pitchVariation < 15f && pitchVariation > 0)
                        {
                            fillerWordDetectionScore += (15f - pitchVariation) * 3f;
                        }
                    }
                }
                // "ah" 可能稍高一点 160-200Hz
                else if (currentPitch > 160f && currentPitch <= 200f && voiceIntensity < 0.02f)
                {
                    fillerWordDetectionScore += 25f;
                    consecutiveLowPitchFrames = 0;
                }
                else
                {
                    consecutiveLowPitchFrames = 0;
                }
            }

            // ===== 核心检测3：气音特征分析 =====
            AnalyzeBreathiness();
            fillerWordDetectionScore += breathinessScore * 0.8f;

            // ===== 核心检测4：说话开始时的犹豫 =====
            // 刚开始说话时的低能量发声通常是填充词
            if (wasJustSpeaking == false && voiceIntensity > 0.002f)
            {
                if (Time.time - lastSpeechEndTime < 0.8f) // 0.8秒内的停顿后发声
                {
                    hesitationFrames++;
                    fillerWordDetectionScore += hesitationFrames * 12f;
                    Debug.Log($"[犹豫检测] 停顿后发声 帧数:{hesitationFrames}");
                }
                wasJustSpeaking = true;
            }
            else if (voiceIntensity < 0.002f)
            {
                if (wasJustSpeaking)
                {
                    lastSpeechEndTime = Time.time;
                    hesitationFrames = 0;
                }
                wasJustSpeaking = false;
            }

            // ===== 核心检测5：微停顿模式 =====
            // 说话过程中的极短停顿（"um...ah..."）
            if (microPauseTimestamps.Count >= 2)
            {
                var recentPauses = microPauseTimestamps.Where(t => Time.time - t < 1f).ToList();
                if (recentPauses.Count >= 2)
                {
                    fillerWordDetectionScore += 20f * recentPauses.Count;

                    // 检测停顿节奏（填充词通常有特定节奏）
                    if (recentPauses.Count >= 2)
                    {
                        float avgInterval = 0;
                        for (int i = 1; i < recentPauses.Count; i++)
                        {
                            avgInterval += recentPauses[i] - recentPauses[i - 1];
                        }
                        avgInterval /= (recentPauses.Count - 1);

                        // 0.2-0.5秒的规律停顿很可能是填充词
                        if (avgInterval > 0.2f && avgInterval < 0.5f)
                        {
                            fillerWordDetectionScore += 30f;
                        }
                    }
                }
            }

            // ===== 核心检测6：频谱特征（增强） =====
            if (spectrum != null && spectrum.Length > 0 && voiceIntensity > 0.001f)
            {
                // 分析低频能量集中度（填充词能量集中在低频）
                float lowFreqEnergy = 0f;
                float highFreqEnergy = 0f;
                int midPoint = spectrum.Length / 4;

                for (int i = 0; i < midPoint; i++)
                {
                    lowFreqEnergy += spectrum[i];
                }
                for (int i = midPoint; i < spectrum.Length; i++)
                {
                    highFreqEnergy += spectrum[i];
                }

                if (lowFreqEnergy > 0)
                {
                    float ratio = highFreqEnergy / lowFreqEnergy;
                    // 如果高频能量很少，很可能是闷声的"um"
                    if (ratio < 0.3f)
                    {
                        fillerWordDetectionScore += 40f;
                        nasalityScore = 40f;
                    }
                }
            }

            // ===== 累积分数追踪 =====
            recentFillerScores.Enqueue(fillerWordDetectionScore);
            if (recentFillerScores.Count > 10) recentFillerScores.Dequeue();

            // 如果连续多帧都有填充词迹象，增加置信度
            if (recentFillerScores.Count >= 3)
            {
                float avgScore = recentFillerScores.Average();
                if (avgScore > 20f)
                {
                    cumulativeFillerScore += avgScore * 0.1f;
                    fillerWordDetectionScore += cumulativeFillerScore;
                }
            }
            else
            {
                cumulativeFillerScore *= 0.9f; // 衰减
            }

            // ===== 触发阈值（大幅降低）=====
            if (fillerWordDetectionScore > 50f) // 从50降到30，更容易触发
            {
                // 立即且显著地增加焦虑值
                float spikeIntensity = Mathf.Min(fillerWordDetectionScore / 100f * 0.6f, 0.5f);
                instantAnxietySpike = Mathf.Max(instantAnxietySpike, spikeIntensity);

                OnFillerWordDetected?.Invoke($"气音检测 (强度: {fillerWordDetectionScore:F0}%)");
                Debug.Log($"[气音检测触发] 得分: {fillerWordDetectionScore:F1} 峰值: {spikeIntensity:F2}");

                // 增加视觉反馈的持续时间
                cumulativeFillerScore = Mathf.Min(cumulativeFillerScore + 10f, 50f);
            }

            // 限制最大值
            fillerWordDetectionScore = Mathf.Min(fillerWordDetectionScore, 100f);
        }

        void DetectMicroPauses()
        {
            if (voiceIntensity < 0.005f && intensityHistory.Count > 0)
            {
                float lastIntensity = intensityHistory.Last();
                if (lastIntensity > 0.02f)
                {
                    microPauseTimestamps.Add(Time.time);
                    if (microPauseTimestamps.Count > 10)
                    {
                        microPauseTimestamps.RemoveAt(0);
                    }
                }
            }
        }


        // 新增：分析气音程度
        void AnalyzeBreathiness()
        {
            breathinessScore = 0f;

            if (voiceIntensity < 0.001f) return;

            // 检测信噪比（气音有更多噪声成分）
            if (audioSamples != null && audioSamples.Length > 0)
            {
                // 计算过零率（气音过零率高）
                int zeroCrossings = 0;
                for (int i = 1; i < audioSamples.Length; i++)
                {
                    if ((audioSamples[i - 1] >= 0 && audioSamples[i] < 0) ||
                        (audioSamples[i - 1] < 0 && audioSamples[i] >= 0))
                    {
                        zeroCrossings++;
                    }
                }

                float zeroCrossingRate = (float)zeroCrossings / audioSamples.Length;

                // 高过零率+低音量 = 气音
                if (zeroCrossingRate > 0.1f && voiceIntensity < 0.02f)
                {
                    breathinessScore = zeroCrossingRate * 200f;
                }

                // 检测噪声成分
                //float noiseLevel = CalculateNoiseLevel(audioSamples);
                //if (noiseLevel > 0.3f)
                //{
                // breathinessScore += noiseLevel * 30f;
                //}
            }

            breathinessScore = Mathf.Min(breathinessScore, 60f);
        }

        void AnalyzeRepetitions()
        {
            if (!IsValidSpeech()) return;

            // 创建当前语音模式
            var currentPattern = new VoicePattern
            {
                pitch = currentPitch,
                intensity = voiceIntensity,
                duration = GetCurrentSpeechDuration(),
                timestamp = Time.time
            };

            recentPatterns.Enqueue(currentPattern);
            if (recentPatterns.Count > 20) recentPatterns.Dequeue();

            // 分析重复
            repetitionScore = 0f;
            patternFrequency.Clear();

            foreach (var pattern in recentPatterns)
            {
                string signature = pattern.GetSignature();
                if (!patternFrequency.ContainsKey(signature))
                    patternFrequency[signature] = 0;
                patternFrequency[signature]++;
            }

            // 检测重复模式
            foreach (var kvp in patternFrequency)
            {
                if (kvp.Value >= 3) // 同一模式出现3次以上
                {
                    repetitionScore += kvp.Value * 15f;
                    OnRepetitionDetected?.Invoke($"检测到重复模式 (次数: {kvp.Value})");
                }
            }

            repetitionScore = Mathf.Min(repetitionScore, 60f);

            // 重复会立即增加焦虑值
            if (repetitionScore > 20f)
            {
                instantAnxietySpike = Mathf.Max(instantAnxietySpike, repetitionScore / 100f * 0.3f);
            }
        }

        // 检测声音紧张度
        void DetectVoiceTension()
        {
            voiceTensionScore = 0f;

            if (!IsValidSpeech()) return;
            // 分析频谱的谐波失真
            float fundamentalFreq = currentPitch;
            if (fundamentalFreq > 0)
            {
                // 检查高频成分（紧张时会增加）
                float highFreqEnergy = 0f;
                int highFreqStart = Mathf.RoundToInt(2000f * spectrum.Length / (sampleRate / 2f));

                for (int i = highFreqStart; i < spectrum.Length; i++)
                {
                    highFreqEnergy += spectrum[i];
                }

                harmonicDistortionLevel = highFreqEnergy * 1000f;

                if (harmonicDistortionLevel > 0.5f)
                {
                    voiceTensionScore = Mathf.Min(harmonicDistortionLevel * 30f, 50f);
                }
            }
            // 声音"紧绷"的其他特征
            if (currentPitch > 250f && voiceIntensity > 0.05f)
            {
                voiceTensionScore += 20f;
            }
        }
        // 检测呼吸模式
        void DetectBreathingPattern()
        {
            breathingIrregularityScore = 0f;

            if (voiceIntensity < 0.003f && intensityHistory.Count > 10)
            {
                float avgIntensity = intensityHistory.Average();
                if (avgIntensity > 0.01f)
                {
                    float breathInterval = Time.time - lastBreathTime;
                    if (breathInterval > 0.5f && breathInterval < 5f)
                    {
                        breathIntervals.Add(breathInterval);
                        if (breathIntervals.Count > 10) breathIntervals.RemoveAt(0);

                        if (breathIntervals.Count >= 3)
                        {
                            float variance = CalculateVariance(breathIntervals.ToArray());
                            breathingIrregularityScore = Mathf.Min(variance * 20f, 40f);
                        }
                    }
                    lastBreathTime = Time.time;
                }
            }
        }
        // 分析语速变化
        void AnalyzeSpeechRateChanges()
        {
            recentSpeechRates.Enqueue(speakingRate);
            if (recentSpeechRates.Count > 10) recentSpeechRates.Dequeue();

            if (recentSpeechRates.Count >= 5)
            {
                float variance = CalculateVariance(recentSpeechRates.ToArray());
                speechRateChangeScore = Mathf.Min(variance * 15f, 40f);

                var rates = recentSpeechRates.ToArray();
                for (int i = 1; i < rates.Length; i++)
                {
                    float change = Mathf.Abs(rates[i] - rates[i - 1]);
                    if (change > 3f)
                    {
                        speechRateChangeScore += 15f;
                    }
                }
            }
        }
        // 分析音量波动
        void AnalyzeVolumeFluctuations()
        {
            recentVolumes.Enqueue(voiceIntensity);
            if (recentVolumes.Count > 15) recentVolumes.Dequeue();

            if (recentVolumes.Count >= 10 && IsValidSpeech())
            {
                float variance = CalculateVariance(recentVolumes.ToArray());
                volumeFluctuationScore = Mathf.Min(variance * 500f, 35f);

                var volumes = recentVolumes.ToArray();
                int rapidChanges = 0;
                for (int i = 1; i < volumes.Length; i++)
                {
                    if (Mathf.Abs(volumes[i] - volumes[i - 1]) > 0.01f)
                    {
                        rapidChanges++;
                    }
                }

                if (rapidChanges > volumes.Length / 2)
                {
                    volumeFluctuationScore += 20f;
                }
            }
        }

        void CalculateSpectrum(float[] samples)
        {
            spectrum = new float[512];

            float[] windowed = new float[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                float window = 0.54f - 0.46f * Mathf.Cos(2f * Mathf.PI * i / (samples.Length - 1));
                windowed[i] = samples[i] * window;
            }

            for (int i = 0; i < spectrum.Length; i++)
            {
                float sum = 0;
                for (int j = 0; j < windowed.Length - 1; j++)
                {
                    sum += Mathf.Abs(windowed[j]);
                }
                spectrum[i] = sum / windowed.Length;
            }
        }

        void AnalyzeVoice()
        {
            if (audioSamples == null || audioSamples.Length == 0) return;

            voiceIntensity = CalculateRMS(audioSamples);
            currentPitch = EstimatePitch();

            UpdateHistory();
            AnalyzeSpeechPattern();
            AnalyzeSpeechRateChanges();
            AnalyzeVolumeFluctuations();
            CalculateAnxietyLevel();

            if (isRecording)
            {
                RecordDataPoint();
            }
        }

        float CalculateRMS(float[] samples)
        {
            if (samples == null || samples.Length == 0) return 0f;

            float sum = 0f;
            foreach (float sample in samples)
            {
                sum += sample * sample;
            }
            return Mathf.Sqrt(sum / samples.Length);
        }

        float EstimatePitch()
        {
            if (audioSamples == null || audioSamples.Length < 2) return 0;

            float maxAmplitude = 0;
            foreach (float sample in audioSamples)
            {
                maxAmplitude = Mathf.Max(maxAmplitude, Mathf.Abs(sample));
            }

            if (maxAmplitude < 0.01f) return 0;

            int minPeriod = sampleRate / 400;
            int maxPeriod = sampleRate / 50;

            float maxCorr = 0;
            int bestPeriod = 0;

            for (int period = minPeriod; period < Mathf.Min(maxPeriod, audioSamples.Length / 2); period++)
            {
                float correlation = 0;
                int count = 0;

                for (int i = 0; i < audioSamples.Length - period; i++)
                {
                    correlation += audioSamples[i] * audioSamples[i + period];
                    count++;
                }

                if (count > 0)
                {
                    correlation /= count;

                    if (correlation > maxCorr)
                    {
                        maxCorr = correlation;
                        bestPeriod = period;
                    }
                }
            }

            if (bestPeriod > 0 && maxCorr > 0.3f)
            {
                return (float)sampleRate / bestPeriod;
            }

            return 0;
        }

        void UpdateHistory()
        {
            pitchHistory.Enqueue(currentPitch);
            if (pitchHistory.Count > 50) pitchHistory.Dequeue();

            intensityHistory.Enqueue(voiceIntensity);
            if (intensityHistory.Count > 50) intensityHistory.Dequeue();

            if (pitchHistory.Count >= 10)
            {
                pitchVariation = CalculateStandardDeviation(pitchHistory.ToArray());
            }
        }

        float CalculateStandardDeviation(float[] values)
        {
            if (values.Length == 0) return 0f;
            float mean = values.Average();
            float sumSquaredDiff = values.Sum(v => (v - mean) * (v - mean));
            return Mathf.Sqrt(sumSquaredDiff / values.Length);
        }

        float CalculateVariance(float[] values)
        {
            if (values.Length == 0) return 0f;
            float mean = values.Average();
            float sumSquaredDiff = values.Sum(v => (v - mean) * (v - mean));
            return sumSquaredDiff / values.Length;
        }

        void AnalyzeSpeechPattern()
        {
            bool isSpeaking = voiceIntensity > 0.01f;

            if (intensityHistory.Count >= 20)
            {
                var recent = intensityHistory.Skip(intensityHistory.Count - 20).ToArray();
                int transitions = 0;

                for (int i = 1; i < recent.Length; i++)
                {
                    if ((recent[i - 1] < 0.01f && recent[i] > 0.01f) ||
                        (recent[i - 1] > 0.01f && recent[i] < 0.01f))
                    {
                        transitions++;
                    }
                }

                speakingRate = transitions * 3f;
            }

            // Only count pauses that are actually problematic (over 1 second)
            if (!isSpeaking && intensityHistory.Count > 0 && intensityHistory.Last() > 0.01f)
            {
                // Start tracking pause, but don't immediately increase anxiety
                // This will be handled in CalculateAnxietyLevel based on duration
            }

            // Decay pause frequency slowly
            pauseFrequency = Mathf.Max(pauseFrequency - 0.01f, 0f);
        }

        void CalculateAnxietyLevel()
        {
            float anxietyFromPitch = 0f;
            float anxietyFromVariation = 0f;
            float anxietyFromIntensity = 0f;
            float anxietyFromRate = 0f;
            float anxietyFromPauses = 0f;
            float anxietyFromSilence = 0f;

            // Pitch abnormality (only when speaking)
            if (currentPitch > 0 && voiceIntensity > 0.005f)
            {
                if (currentPitch < 100f || currentPitch > 300f)
                    anxietyFromPitch = 0.6f;
                else if (currentPitch > 200f)
                    anxietyFromPitch = (currentPitch - 200f) / 100f * 0.5f;
            }

            // Pitch variation (only when speaking)
            if (voiceIntensity > 0.005f)
            {
                anxietyFromVariation = Mathf.Clamp01(pitchVariation / 50f);
            }

            // Volume abnormality - only very quiet speech during conversation is anxious
            if (IsInConversation() && voiceIntensity < 0.005f && voiceIntensity > 0.001f)
            {
                anxietyFromIntensity = 0.3f;
            }
            else if (voiceIntensity > 0.1f)
            {
                anxietyFromIntensity = 0.3f;
            }

            // Silence anxiety calculation
            if (IsInConversation() && continuousSilenceDuration > ANXIETY_SILENCE_THRESHOLD)
            {
                if (continuousSilenceDuration <= 2.0f)
                {
                    anxietyFromSilence = ((continuousSilenceDuration - ANXIETY_SILENCE_THRESHOLD) / 1.0f) * 0.3f;
                }
                else if (continuousSilenceDuration <= 3.0f)
                {
                    anxietyFromSilence = 0.3f + ((continuousSilenceDuration - 2.0f) / 1.0f) * 0.3f;
                }
                else
                {
                    anxietyFromSilence = 0.6f + Mathf.Min((continuousSilenceDuration - 3.0f) / 2.0f, 0.4f);
                }
            }

            // ==== 修改这里，使用 checkWindow 和 longPauses 变量 ====
            if (IsInConversation())
            {
                int longPauses = 0;
                float checkWindow = 5.0f; // 检查最近5秒

                // 统计最近 checkWindow 秒内超过 1 秒的暂停次数
                if (microPauseTimestamps.Count >= 2)
                {
                    for (int i = microPauseTimestamps.Count - 1; i > 0; i--)
                    {
                        float pauseDuration = microPauseTimestamps[i] - microPauseTimestamps[i - 1];
                        if (pauseDuration >= 1.0f && (Time.time - microPauseTimestamps[i]) <= checkWindow)
                        {
                            longPauses++;
                        }
                    }
                }

                // 如果最近长暂停较多，则减少 pause 的焦虑权重
                if (longPauses > 0)
                {
                    // 长暂停被认为是正常的停顿，降低焦虑分数
                    anxietyFromPauses = Mathf.Max(0f, pauseFrequency * 0.15f - longPauses * 0.05f);
                }
                else
                {
                    anxietyFromPauses = pauseFrequency * 0.2f;
                }
            }

            // Speech rate (only when speaking)
            if (voiceIntensity > 0.01f)
            {
                if (speakingRate > 6f)
                    anxietyFromRate = 0.5f;
                else if (speakingRate < 2f)
                    anxietyFromRate = 0.3f;
            }

            // Combine all factors with adjusted weights
            if (!IsInConversation() || (continuousSilenceDuration < ANXIETY_SILENCE_THRESHOLD && voiceIntensity < 0.005f))
            {
                currentAnxietyLevel = 0.05f;
            }
            else
            {
                currentAnxietyLevel = (
                    anxietyFromPitch * 0.15f +
                    anxietyFromVariation * 0.15f +
                    anxietyFromIntensity * 0.10f +
                    anxietyFromRate * 0.10f +
                    anxietyFromPauses * 0.05f +
                    anxietyFromSilence * 0.20f +
                    (fillerWordDetectionScore / 100f) * 0.10f +
                    (repetitionScore / 100f) * 0.05f +
                    (voiceTensionScore / 100f) * 0.05f +
                    (breathingIrregularityScore / 100f) * 0.03f +
                    (speechRateChangeScore / 100f) * 0.05f +
                    (volumeFluctuationScore / 100f) * 0.05f
                ) * sensitivityMultiplier;
            }

            // Apply instant spikes (only during conversation)
            if (enableInstantSpikes && instantAnxietySpike > 0 && IsInConversation())
            {
                currentAnxietyLevel = Mathf.Max(currentAnxietyLevel, currentAnxietyLevel + instantAnxietySpike);
                instantAnxietySpike *= 0.9f;

                if (instantAnxietySpike < 0.01f)
                {
                    instantAnxietySpike = 0f;
                }
            }

            // Clamp and smooth
            currentAnxietyLevel = Mathf.Clamp01(currentAnxietyLevel);

            float actualResponseSpeed = IsInConversation() ? responseSpeed : responseSpeed * 3f;
            smoothedAnxietyLevel = Mathf.Lerp(smoothedAnxietyLevel, currentAnxietyLevel, actualResponseSpeed);

            OnAnxietyLevelChanged?.Invoke(smoothedAnxietyLevel);

            anxietyHistory.Add(smoothedAnxietyLevel);
        }


        void UpdateUI()
        {
            if (anxietyText != null)
            {
                string silenceInfo = continuousSilenceDuration > 0.1f ?
                    $" (沉默: {continuousSilenceDuration:F1}s)" : "";
                anxietyText.text = $"焦虑水平: {(smoothedAnxietyLevel * 100):F1}%{silenceInfo}";

                if (smoothedAnxietyLevel < 0.3f)
                    anxietyText.color = Color.green;
                else if (smoothedAnxietyLevel < 0.6f)
                    anxietyText.color = Color.yellow;
                else
                    anxietyText.color = Color.red;
            }

            if (anxietySlider != null)
            {
                anxietySlider.value = smoothedAnxietyLevel;

                var fillImage = anxietySlider.fillRect?.GetComponent<Image>();
                if (fillImage != null)
                {
                    Color targetColor;
                    if (smoothedAnxietyLevel < 0.3f)
                        targetColor = Color.green;
                    else if (smoothedAnxietyLevel < 0.6f)
                        targetColor = Color.yellow;
                    else
                        targetColor = Color.red;

                    if (instantAnxietySpike > 0.1f)
                    {
                        float flash = Mathf.Sin(Time.time * 10f) * 0.3f + 0.7f;
                        targetColor = Color.Lerp(targetColor, Color.white, flash * instantAnxietySpike);
                    }

                    fillImage.color = targetColor;
                }
            }

            if (pitchText != null)
                pitchText.text = $"音高: {currentPitch:F1} Hz\n变化: {pitchVariation:F1}";

            if (intensityText != null)
            {
                string speakingStatus = voiceIntensity > 0.005f ? "说话中" :
                    (IsInConversation() ? "沉默中" : "待机");
                intensityText.text = $"音量: {(voiceIntensity * 100):F1}\n状态: {speakingStatus}";
            }

            if (detailText != null)
            {
                detailText.text = $"填充词: {fillerWordDetectionScore:F0}%\n" +
                                 $"重复: {repetitionScore:F0}%\n" +
                                 $"紧张度: {voiceTensionScore:F0}%\n" +
                                 $"呼吸: {breathingIrregularityScore:F0}%\n" +
                                 $"沉默时长: {continuousSilenceDuration:F1}s\n" +
                                 $"对话状态: {(IsInConversation() ? "是" : "否")}";
            }
        }

        void UpdateStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
            Debug.Log($"[状态] {message}");
        }

        bool IsValidSpeech()
        {
            return voiceIntensity > 0.005f && currentPitch > 50f;
        }

        float GetCurrentSpeechDuration()
        {
            if (intensityHistory.Count < 5) return 0f;

            int speechFrames = 0;
            foreach (float intensity in intensityHistory.Skip(intensityHistory.Count - 10))
            {
                if (intensity > 0.01f) speechFrames++;
            }

            return speechFrames * 0.05f;
        }

        public void StartRecording()
        {
            isRecording = true;
            recordingStartTime = Time.time;
            anxietyHistory.Clear();
            recordedData.Clear();
            UpdateStatus("正在记录（高灵敏度）...");
        }

        public void StopRecording()
        {
            isRecording = false;
            SaveData();
            UpdateStatus("记录已保存");
        }

        void RecordDataPoint()
        {
            recordedData.Add(new AnxietyDataPoint
            {
                timestamp = Time.time - recordingStartTime,
                anxietyLevel = smoothedAnxietyLevel,
                pitch = currentPitch,
                pitchVariation = pitchVariation,
                intensity = voiceIntensity,
                speakingRate = speakingRate,
                pauseFrequency = pauseFrequency
            });
        }

        void SaveData()
        {
            if (recordedData.Count == 0) return;

            string json = JsonUtility.ToJson(new SessionData
            {
                sessionTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                duration = Time.time - recordingStartTime,
                dataPoints = recordedData
            }, true);

            string path = System.IO.Path.Combine(
                Application.persistentDataPath,
                $"anxiety_session_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            );

            System.IO.File.WriteAllText(path, json);
            Debug.Log($"数据已保存: {path}");
        }

        public float GetAnxietyLevel() => smoothedAnxietyLevel;
        public float GetInstantSpike() => instantAnxietySpike;
        public float GetSilenceDuration() => continuousSilenceDuration; // 新增
        public bool GetIsInConversation() => IsInConversation(); // 新增

        public Dictionary<string, float> GetDetailedScores()
        {
            return new Dictionary<string, float>
            {
                { "FillerWords", fillerWordDetectionScore },
                { "Repetition", repetitionScore },
                { "VoiceTension", voiceTensionScore },
                { "Breathing", breathingIrregularityScore },
                { "SpeechRateChange", speechRateChangeScore },
                { "VolumeFluctuation", volumeFluctuationScore },
                { "SilenceDuration", continuousSilenceDuration }, // 新增
                { "InConversation", IsInConversation() ? 1f : 0f } // 新增
            };
        }

        void OnDestroy()
        {
            if (Microphone.IsRecording(selectedMicrophone))
            {
                Microphone.End(selectedMicrophone);
            }
        }
    }


}