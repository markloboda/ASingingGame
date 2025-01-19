using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;
using Note_Recognizer;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Rendering;
using Vector3 = UnityEngine.Vector3;

namespace Assets.Scripts
{
   public class LineLevel : MonoBehaviour
   {
      public FrequencyLine Line;
      public AudioClip AudioFile;

      private AudioSource _audioSource;
      private float _frequencyTolerance = 4f;

      public float MaxFrequency { get; private set; } = 0;
      public float MinFrequency { get; private set; } = 0;

      void Start()
      {
         if (AudioFile == null)
         {
            return;
         }

         _audioSource = gameObject.AddComponent<AudioSource>();
         _audioSource.clip = AudioFile;
         _audioSource.Play();
         ConstructLevel();
      }

      void Update()
      {
         if (_audioSource == null || AudioFile == null)
         {
            return;
         }

         float positionX = transform.position.x - 1 * Time.deltaTime;

         if (!_audioSource.isPlaying && transform.position.x > 0 && positionX <= 0)
         {
            _audioSource.Play();
         }
         else if (transform.position.x > 0 && _audioSource.isPlaying)
         {
            _audioSource.Stop();
            _audioSource.time = 0;
         }

         transform.position = new Vector3(positionX, 0, transform.position.z);
      }

      private void ConstructLevel()
      {
         float[] samples = new float[AudioFile.samples];
         AudioFile.GetData(samples, 0);

         float[] monoSamples = new float[AudioFile.samples / AudioFile.channels];
         for (int i = 0; i < monoSamples.Length; i++)
         {
            monoSamples[i] = samples
               .Skip(i * AudioFile.channels)
               .Take(AudioFile.channels)
               .Average();
         }

         DSPEngine dspEngine = new DSPEngine();
         dspEngine.WindowSize = 4096;
         dspEngine.StepSize = 2048;
         dspEngine.Tolerance = 7;
         dspEngine.SampleRate = AudioFile.frequency;
         dspEngine.Data = monoSamples;

         dspEngine.Start();

         // Filter out frequencies when same not and get min and max.
         MaxFrequency = 0;
         MinFrequency = float.MaxValue;
         float prevFreq = float.MinValue;
         List<float> keyFreqs = new List<float>();
         foreach (float keyFreq in dspEngine.KeyFreqs)
         {
            if (Math.Abs(keyFreq - prevFreq) > _frequencyTolerance)
            {
               keyFreqs.Add(keyFreq);
               prevFreq = keyFreq;

               if (keyFreq > MaxFrequency)
               {
                  MaxFrequency = keyFreq;
               }

               if (keyFreq < MinFrequency && keyFreq > 0)
               {
                  MinFrequency = keyFreq;
               }
            }
         }

         MaxFrequency += 100f;

         Line.Frequencies = keyFreqs;
         Line.LineLength = 1.0f * AudioFile.samples / AudioFile.frequency;
         Line.MaxPlayerFrequencyHz = MaxFrequency;
         Line.MinPlayerFrequencyHz = MinFrequency;

         Line.SetupLine();
      }
   }
}