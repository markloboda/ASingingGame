using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Random = UnityEngine.Random;
using Note_Recognizer;
using System.Linq;
using UnityEngine.Networking;

namespace Assets.Scripts
{
   public class ObstacleLevel : MonoBehaviour
   {
      public enum EMode
      {
         Random,
         AudioSource
      }

      public GameObject ObstaclePrefab;
      public GameObject FinishPrefab;
      public EMode Mode = EMode.Random;
      [Range(0f, 1f)] public float ObstacleGapSize = 0.2f;
      public float RandomObstacleDistance = 0.2f;
      public float ObstacleDeltaTimeSec = 1;
      public float FrequencyRangeOffset = 150;

      public float MaxFrequency { get; private set; } = 0;
      public float MinFrequency { get; private set; } = 0;

      public AudioClip AudioFile;

      public GameplayLogic GameplayLogic;

      public ParticleSystem ConfettiObject;

      private AudioSource _audioSource;

      private float _frequencyTolerance = 4f;
      private float _startingX = 0f;

      private float _movingSpeed = 1f;

      void Start()
      {
         Mode = (EMode)PlayerPrefs.GetInt("Game Mode");
         float difficulty = PlayerPrefs.GetFloat("Difficulty");
         ObstacleGapSize = Mathf.Lerp(0.2f, 0.5f, 1 - difficulty);
         ObstacleDeltaTimeSec = Mathf.Lerp(0.5f, 2f, 1 - difficulty);
         RandomObstacleDistance = Mathf.Lerp(0.5f, 2f, 1 - difficulty);

         if (Mode == EMode.Random)
         {
            ConstructLevel();
         }
         else
         {
            var songPath = PlayerPrefs.GetString("SongPath");

            AudioType audioType = Utils.GetAudioType(songPath.Split('.').Last());
            UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + songPath, audioType);
            www.SendWebRequest();

            while (!www.isDone)
            {
            }

            AudioFile = DownloadHandlerAudioClip.GetContent(www);

            if (AudioFile == null)
            {
               Debug.LogError("Failed to load audio file.");
               Mode = EMode.Random;
               ConstructLevel();
               return;
            }

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.clip = AudioFile;
            _audioSource.Play();
            ConstructLevel();
         }

         _startingX = transform.position.x;
         _movingSpeed = 1f;
      }

      void Update()
      {
         float positionX = (transform.position.x - _movingSpeed * Time.deltaTime);

         if (Mode == EMode.AudioSource)
         {
            if (_audioSource == null || AudioFile == null)
            {
               return;
            }

            if (!_audioSource.isPlaying && transform.position.x > 0 && positionX <= 0)
            {
               _audioSource.Play();
            }
            else if (transform.position.x > 0 && _audioSource.isPlaying)
            {
               _audioSource.Stop();
               _audioSource.time = 0;
            }
         }

         transform.position = new Vector3(positionX, 0, transform.position.z);
      }

      private void ConstructLevel()
      {
         // construct level randomly
         if (Mode == EMode.Random)
         {
            CreateRandomObstacles();
         }
         // construct level based on audio source
         else if (Mode == EMode.AudioSource)
         {
            CreateAudioSourceObstacles();
         }
      }

      private void CreateRandomObstacles()
      {
         int obstacleCount = 10;
         for (int i = 0; i < obstacleCount; i++)
         {
            Obstacle obstacle = CreateObstacle();
            obstacle.GapSize = ObstacleGapSize;
            obstacle.transform.localPosition = new Vector2(i * RandomObstacleDistance, 0);
            obstacle.YPosition = Random.Range(0.3f, 0.7f);

            MaxFrequency = 300;
            MinFrequency = 0;
         }

         Finish finish = CreateFinish();
         finish.transform.localPosition = new Vector2(obstacleCount * RandomObstacleDistance, 0);
      }

      private void CreateAudioSourceObstacles()
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

               if (keyFreq < MinFrequency)
               {
                  MinFrequency = keyFreq;
               }
            }
         }

         MaxFrequency += FrequencyRangeOffset;
         MinFrequency = Math.Max(MinFrequency - FrequencyRangeOffset, 0);

         // Create obstacles based on key frequencies.
         float xStep = 1.0f * AudioFile.samples / (AudioFile.frequency * keyFreqs.Count);
         int iStep = (int)Math.Round(ObstacleDeltaTimeSec / (1.0f * AudioFile.samples / (AudioFile.frequency * keyFreqs.Count)));
         for (int i = 0; i < keyFreqs.Count; i += iStep)
         {
            Obstacle obstacle = CreateObstacle();
            obstacle.GapSize = ObstacleGapSize;
            obstacle.transform.localPosition = new Vector2(i * xStep, 0);
            obstacle.YPosition = (keyFreqs[i] - MinFrequency) / (MaxFrequency - MinFrequency);
         }

         Finish finish = CreateFinish();
         finish.transform.localPosition = new Vector2(keyFreqs.Count * RandomObstacleDistance, 0);
      }

      private Obstacle CreateObstacle()
      {
         GameObject obstacle = Instantiate(ObstaclePrefab, transform);
         obstacle.name += Guid.NewGuid();
         obstacle.transform.localPosition = Vector2.zero;
         Obstacle obstacleComponent = obstacle.GetComponent<Obstacle>();
         return obstacleComponent;
      }

      private Finish CreateFinish()
      {
         GameObject finish = Instantiate(FinishPrefab, transform);
         finish.name += Guid.NewGuid();
         finish.transform.localPosition = Vector2.zero;
         Finish finishComponent = finish.GetComponent<Finish>();
         return finishComponent;
      }

      public void GameFinished()
      {
         ConfettiObject.gameObject.SetActive(true);
         ConfettiObject.Play();
         _movingSpeed = 0f;
         GameplayLogic.FinishedGame();
      }

      public void GameOver()
      {
         RestartLevel();
      }

      private void RestartLevel()
      {
         transform.position = new Vector3(_startingX, 0, transform.position.z);
         _audioSource.Stop();
      }
   }
}