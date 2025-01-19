using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Fundamental frequency estimation using Summation of Residual Harmonics (SRH)
// T. Drugman and A. Alwan: "Joint Robust Voicing Detection and Pitch Estimation Based on Residual Harmonics", Interspeech'11, 2011.

public class AudioPitchEstimator : MonoBehaviour
{
   [Tooltip("Lowest frequency that can estimate [Hz]")] [Range(40, 150)]
   public int frequencyMin = 40;

   [Tooltip("Highest frequency that can estimate [Hz]")] [Range(300, 1200)]
   public int frequencyMax = 600;

   [Tooltip("Number of overtones to use for estimation")] [Range(1, 8)]
   public int harmonicsToUse = 5;

   [Tooltip("Frequency bandwidth of spectral smoothing filter [Hz]\nWider bandwidth smoothes the estimation, however the accuracy decreases.")]
   public float smoothingWidth = 500;

   [Tooltip("Threshold to judge silence or not\nLarger the value, stricter the judgment.")]
   public float thresholdSRH = 7;

   const int spectrumSize = 1024;
   const int outputResolution = 200; // frequency axis resolution (decreasing this will reduce the calculation load)
   float[] spectrum = new float[spectrumSize];
   float[] specRaw = new float[spectrumSize];
   float[] specCum = new float[spectrumSize];
   float[] specRes = new float[spectrumSize];
   float[] srh = new float[outputResolution];

   public List<float> SRH => new List<float>(srh);

   /// <summary>
   /// Estimates the fundamental frequency
   /// </summary>
   /// <param name="audioSource">Input audio source</param>
   /// <returns>Fundamental frequency [Hz] (float.NaN if it does not exist)</returns>
   public float Estimate(AudioSource audioSource)
   {
      var nyquistFreq = AudioSettings.outputSampleRate / 2.0f;

      // Filter clicks.
      // float[] samples = new float[audioSource.clip.samples * audioSource.clip.channels];
      // audioSource.clip.GetData(samples, 0);
      // ProcessSimpleClickFilter(samples);
      // audioSource.clip.SetData(samples, 0);

      if (!audioSource.isPlaying) return float.NaN;
      audioSource.GetSpectrumData(spectrum, 0, FFTWindow.Hanning);

      for (int i = 0; i < spectrumSize; i++)
      {
         specRaw[i] = Mathf.Log(spectrum[i] + 1e-9f);
      }

      specCum[0] = 0;
      for (int i = 1; i < spectrumSize; i++)
      {
         specCum[i] = specCum[i - 1] + specRaw[i];
      }

      var halfRange = Mathf.RoundToInt((smoothingWidth / 2) / nyquistFreq * spectrumSize);
      for (int i = 0; i < spectrumSize; i++)
      {
         var indexUpper = Mathf.Min(i + halfRange, spectrumSize - 1);
         var indexLower = Mathf.Max(i - halfRange + 1, 0);
         var upper = specCum[indexUpper];
         var lower = specCum[indexLower];
         var smoothed = (upper - lower) / (indexUpper - indexLower);

         specRes[i] = specRaw[i] - smoothed;
      }

      float bestFreq = 0, bestSRH = 0;
      for (int i = 0; i < outputResolution; i++)
      {
         var currentFreq = (float)i / (outputResolution - 1) * (frequencyMax - frequencyMin) + frequencyMin;

         var currentSRH = GetSpectrumAmplitude(specRes, currentFreq, nyquistFreq);
         for (int h = 2; h <= harmonicsToUse; h++)
         {
            currentSRH += GetSpectrumAmplitude(specRes, currentFreq * h, nyquistFreq);

            currentSRH -= GetSpectrumAmplitude(specRes, currentFreq * (h - 0.5f), nyquistFreq);
         }

         srh[i] = currentSRH;

         if (currentSRH > bestSRH)
         {
            bestFreq = currentFreq;
            bestSRH = currentSRH;
         }
      }

      if (bestSRH < thresholdSRH) return float.NaN;

      return bestFreq;
   }

   float GetSpectrumAmplitude(float[] spec, float frequency, float nyquistFreq)
   {
      var position = frequency / nyquistFreq * spec.Length;
      var index0 = (int)position;
      var index1 = index0 + 1;
      var delta = position - index0;
      return (1 - delta) * spec[index0] + delta * spec[index1];
   }

   private void ProcessSimpleClickFilter(float[] samples)
   {
      float FILTERTHRESHOLD = 0.05f;

      for (int i = 1; i < samples.Length; i++)
      {
         if (MathF.Abs(samples[i] - samples[i - 1]) > FILTERTHRESHOLD)
         {
            samples[i] = 0;
         }
      }
   }

   private void ProcessGeneralClickFilter(float[] samples)
   {
      float FILTERTHRESHOLD = 0.05f;

      int largeWindowCount = 2074 * 2 * 2 * 2 * 2;
      int smallWindowCount = 20 * 2 * 2 * 2 * 2;

      for (int largeIndex = -largeWindowCount / 2 + smallWindowCount / 2; largeIndex <= samples.Length - largeWindowCount; largeIndex++)
      {
         float largeAvg = CalculateWindowAverageOfSquaredValues(samples, largeIndex, largeWindowCount);
         int smallIndex = largeIndex + largeWindowCount / 2 - smallWindowCount / 2;
         float smallAvg = CalculateWindowAverageOfSquaredValues(samples, smallIndex, smallWindowCount);

         if (MathF.Abs(largeAvg - smallAvg) > FILTERTHRESHOLD)
         {
            InterpolateWindow(samples, smallIndex, smallWindowCount);
         }
      }
   }

   private float CalculateWindowAverageOfSquaredValues(float[] samples, int windowStartIndex, int windowCount)
   {
      if (windowStartIndex + windowCount > samples.Length)
      {
         throw new Exception("endIndex > samples");
      }

      float sum = 0;
      for (int i = 0; i < windowCount; i++)
      {
         int index = windowStartIndex + i;
         if (index < 0)
         {
            continue;
         }

         float value = samples[index];
         sum += value * value;
      }

      return sum / windowCount;
   }

   private void InterpolateWindow(float[] samples, int windowStartIndex, int windowCount)
   {
      float x0 = windowStartIndex;
      float y0 = samples[windowStartIndex];
      float x1 = windowStartIndex + windowCount - 1;
      float y1 = samples[windowStartIndex + windowCount - 1];


      for (int i = 0; i < windowCount; i++)
      {
         samples[windowStartIndex + i] = y0 + i * (y1 - y0) / (x1 - x0);
      }
   }
}