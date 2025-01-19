using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts
{
   public class PlayerController : MonoBehaviour
   {
      public AudioDetector Detector;

      // public LineLevel LineLevel;
      public ObstacleLevel ObstacleLevel;

      public float MoveSpeed = 3;
      public float AmplitudeThreshold = 0.4f;
      public float AmplitudeSensibility = 100;
      public bool UseRealFrequencies = false;

      private float _frequencyMeasurement;
      private float _amplitudeMeasurement;
      private float _currentY = 0f;

      public bool DebugOn = false;

      void Start()
      {
         ObstacleLevel.GameplayLogic.OnPause += GamePaused;
         ObstacleLevel.GameplayLogic.OnResume += GameResumed;

         Detector.StartMicrophone();
      }

      void Update()
      {
         float value = 0;

         float minFrequency = 100f;
         float maxFrequency = 300f;

         if (UseRealFrequencies)
         {
            minFrequency = ObstacleLevel.MinFrequency;
            maxFrequency = ObstacleLevel.MaxFrequency;
         }

         _frequencyMeasurement = Detector.GetFrequencyFromMicrophone();
         if (float.IsNaN(_frequencyMeasurement))
         {
            _frequencyMeasurement = 0;
         }

         value = (_frequencyMeasurement - minFrequency) / (maxFrequency - minFrequency);

         float desiredY = Mathf.Lerp(0, 1, value);
         _currentY = Mathf.Lerp(_currentY, desiredY, MoveSpeed * Time.deltaTime);
         transform.position = new Vector3(0, _currentY, transform.position.z);
      }

      public void GameOver()
      {
         Detector.StopMicrophone();
         Detector.StartMicrophone();
      }

      public void GameResumed()
      {
         Detector.StartMicrophone();
      }

      public void GamePaused()
      {
         Detector.StopMicrophone();
      }

      void OnGUI()
      {
         if (DebugOn)
         {
            string valueString = $"Amplitude: {_amplitudeMeasurement}\n" +
                                 $"Frequency: {_frequencyMeasurement} Hz";

            GUI.Label(new Rect(10, 10, 300, 100), valueString);
         }
      }
   }
}