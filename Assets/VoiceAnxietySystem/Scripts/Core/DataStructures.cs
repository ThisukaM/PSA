using System;
using System.Collections.Generic;

namespace VoiceAnxietySystem
{
    [Serializable]
    public class AnxietyDataPoint
    {
        public float timestamp;
        public float anxietyLevel;
        public float pitch;
        public float pitchVariation;
        public float intensity;
        public float speakingRate;
        public float pauseFrequency;
    }

    [Serializable]
    public class SessionData
    {
        public string sessionTime;
        public float duration;
        public List<AnxietyDataPoint> dataPoints;
    }

    [Serializable]
    public class AnxietySettings
    {
        public float pitchWeight = 0.25f;
        public float variationWeight = 0.25f;
        public float intensityWeight = 0.15f;
        public float rateWeight = 0.20f;
        public float pauseWeight = 0.15f;
        public float smoothingFactor = 0.1f;
    }
}