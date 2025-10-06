
using System.Collections.Generic;
using UnityEngine;

namespace VoiceAnxietySystem
{
    // 焦虑特征数据结构
    [System.Serializable]
    public class AnxietyFeatures
    {
        public float fillerWords;      // 填充词
        public float pauses;           // 停顿
        public float trembling;        // 颤抖
        public float speedVariation;   // 语速变化
        public float pitchInstability; // 音高不稳定
        public float repetitions;      // 重复

        public float GetTotalScore()
        {
            return (fillerWords + pauses + trembling +
                   speedVariation + pitchInstability + repetitions) / 6f;
        }
    }

    // 语音段数据
    [System.Serializable]
    public class SpeechSegment
    {
        public float startTime;
        public float duration;
        public float avgPitch;
        public float avgVolume;
        public bool isFillerWord;

        public SpeechSegment(float start, float dur, float pitch, float vol)
        {
            startTime = start;
            duration = dur;
            avgPitch = pitch;
            avgVolume = vol;
            isFillerWord = false;
        }
    }

    // 焦虑检测配置
    [System.Serializable]
    public class AnxietyConfig
    {
        [Header("检测阈值")]
        public float voiceThreshold = 0.01f;
        public float minPitchHz = 80f;
        public float maxPitchHz = 400f;

        [Header("特征权重")]
        public float fillerWordWeight = 0.25f;
        public float pauseWeight = 0.20f;
        public float trembleWeight = 0.20f;
        public float speedWeight = 0.20f;
        public float pitchWeight = 0.15f;

        [Header("时间窗口")]
        public int historySize = 100;
        public float analysisInterval = 0.05f;
    }
}