using System.Linq;
using System.Windows.Forms;
using SFB;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Application = UnityEngine.Application;
using Color = UnityEngine.Color;
using Button = UnityEngine.UI.Button;


namespace Assets.Scripts
{
   public class MainMenu : MonoBehaviour
   {
      private ObstacleLevel.EMode _selectedMode = ObstacleLevel.EMode.Random;

      private string _filePath;

      private Color defaultButtonColor;

      void Start()
      {
         Image chooseSongButton = GameObject.Find("ChooseSongButton")?.GetComponent<Image>();
         defaultButtonColor = new Color(24.0f / 255, 120.0f / 255, 161.0f / 255);
      }

      void Update()
      {
         Image chooseSongButton = GameObject.Find("ChooseSongButton")?.GetComponent<Image>();
         Image randomButton = GameObject.Find("RandomButton")?.GetComponent<Image>();

         if (chooseSongButton != null && randomButton != null)
         {
            float val = 50f / 255;
            if (_selectedMode == ObstacleLevel.EMode.Random)
            {
               chooseSongButton.color = defaultButtonColor;
               randomButton.color = defaultButtonColor + new Color(val, val, val);
            }
            else if (_selectedMode == ObstacleLevel.EMode.AudioSource)
            {
               chooseSongButton.color = defaultButtonColor + new Color(val, val, val);
               randomButton.color = defaultButtonColor;
            }
            else
            {
            }
         }
      }

      public void PlayGame()
      {
         if (_selectedMode == ObstacleLevel.EMode.AudioSource)
         {
            if (string.IsNullOrEmpty(_filePath))
            {
               Debug.LogError("No song file selected");
               _selectedMode = ObstacleLevel.EMode.Random;
            }
            else
            {
               PlayerPrefs.SetString("SongPath", _filePath);
               PlayerPrefs.Save();
               UnityEngine.SceneManagement.SceneManager.LoadScene("Gameplay Scene");
               GameObject[] objects = SceneManager.GetActiveScene().GetRootGameObjects();
            }
         }

         PlayerPrefs.SetInt("Game Mode", (int)_selectedMode);
         SceneManager.LoadScene("Gameplay Scene");
      }

      public void QuitGame()
      {
         Application.Quit();
      }

      public void SelectAudioSourceMode()
      {
         _filePath = StandaloneFileBrowser.OpenFilePanel("Choose Song File", "",
            new[] { new ExtensionFilter("Audio Files", Utils.GetAvailableAudioFormats()) },
            false).FirstOrDefault();

         if (string.IsNullOrEmpty(_filePath))
         {
            Debug.LogError("No song file selected");
            SelectRandomMode();
         }
         else
         {
            _selectedMode = ObstacleLevel.EMode.AudioSource;
         }
      }

      public void SelectRandomMode()
      {
         _selectedMode = ObstacleLevel.EMode.Random;
      }
   }
}