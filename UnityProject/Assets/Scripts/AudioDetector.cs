using System;
using System.Linq;
using Note_Recognizer;
using UnityEngine;

namespace Assets.Scripts
{
   public class AudioDetector : MonoBehaviour
   {
      public int Duration = 2;

      public AudioSource AudioSource;

      public float GetFrequencyFromMicrophone()
      {
         return GetFrequencyFromAudioClip(AudioSource);
      }

      public void StartMicrophone()
      {
         AudioSource.clip = Microphone.Start(string.Empty, true, Duration, AudioSettings.outputSampleRate);
         AudioSource.loop = true;
         while (!(Microphone.GetPosition(null) > 0))
         {
         }

         AudioSource.Play();
      }

      public void StopMicrophone()
      {
         AudioSource.Stop();
         Microphone.End(null);
      }

      private float GetFrequencyFromAudioClip(AudioSource audioSource)
      {
         AudioPitchEstimator pitchEstimator = gameObject.GetComponent<AudioPitchEstimator>();
         float micThreshold = PlayerPrefs.GetFloat("MicThreshold", 8.0f);
         pitchEstimator.thresholdSRH = micThreshold;
         float frequency = pitchEstimator.Estimate(audioSource);
         return frequency;
      }
   }
}