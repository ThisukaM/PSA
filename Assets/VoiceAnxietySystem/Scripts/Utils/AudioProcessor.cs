using UnityEngine;

namespace VoiceAnxietySystem
{
    public static class AudioProcessor
    {
        public static float CalculatePitchAutocorrelation(float[] samples, int sampleRate)
        {
            int minPeriod = sampleRate / 400; // 400 Hz max
            int maxPeriod = sampleRate / 50;  // 50 Hz min

            float maxCorr = 0;
            int bestPeriod = 0;

            for (int period = minPeriod; period < maxPeriod && period < samples.Length / 2; period++)
            {
                float corr = 0;
                for (int i = 0; i < samples.Length - period; i++)
                {
                    corr += samples[i] * samples[i + period];
                }

                if (corr > maxCorr)
                {
                    maxCorr = corr;
                    bestPeriod = period;
                }
            }

            return bestPeriod > 0 ? (float)sampleRate / bestPeriod : 0;
        }

        public static float[] ApplyHammingWindow(float[] samples)
        {
            float[] windowed = new float[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                float window = 0.54f - 0.46f * Mathf.Cos(2f * Mathf.PI * i / (samples.Length - 1));
                windowed[i] = samples[i] * window;
            }
            return windowed;
        }

        public static float CalculateZeroCrossingRate(float[] samples)
        {
            int crossings = 0;
            for (int i = 1; i < samples.Length; i++)
            {
                if ((samples[i] >= 0 && samples[i - 1] < 0) ||
                    (samples[i] < 0 && samples[i - 1] >= 0))
                {
                    crossings++;
                }
            }
            return crossings / (float)samples.Length;
        }
    }
}