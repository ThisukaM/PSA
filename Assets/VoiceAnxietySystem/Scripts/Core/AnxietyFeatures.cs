
using System.Collections.Generic;
using UnityEngine;

namespace VoiceAnxietySystem
{
    // �����������ݽṹ
    [System.Serializable]
    public class AnxietyFeatures
    {
        public float fillerWords;      // ����
        public float pauses;           // ͣ��
        public float trembling;        // ����
        public float speedVariation;   // ���ٱ仯
        public float pitchInstability; // ���߲��ȶ�
        public float repetitions;      // �ظ�

        public float GetTotalScore()
        {
            return (fillerWords + pauses + trembling +
                   speedVariation + pitchInstability + repetitions) / 6f;
        }
    }

    // ����������
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

    // ���Ǽ������
    [System.Serializable]
    public class AnxietyConfig
    {
        [Header("�����ֵ")]
        public float voiceThreshold = 0.01f;
        public float minPitchHz = 80f;
        public float maxPitchHz = 400f;

        [Header("����Ȩ��")]
        public float fillerWordWeight = 0.25f;
        public float pauseWeight = 0.20f;
        public float trembleWeight = 0.20f;
        public float speedWeight = 0.20f;
        public float pitchWeight = 0.15f;

        [Header("ʱ�䴰��")]
        public int historySize = 100;
        public float analysisInterval = 0.05f;
    }
}