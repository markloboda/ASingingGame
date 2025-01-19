using System;
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
   public class GameplayLogic : MonoBehaviour
   {
      enum EState
      {
         Playing,
         Paused,
         Finished
      }

      public GameObject GameplayObject;
      public Canvas PauseCanvas;
      public Canvas FinishedCanvas;

      public Action OnResume;
      public Action OnPause;

      private EState _state = EState.Playing;

      void Update()
      {
         if (Input.GetKeyDown(KeyCode.Escape))
         {
            if (_state == EState.Playing)
            {
               PauseGame();
            }
            else if (_state == EState.Paused)
            {
               ResumeGame();
            }
         }

         if (_state == EState.Finished)
         {
            // Press any key to exit.
            if (Input.anyKey)
            {
               ExitGame();
            }
         }
      }

      public void FinishedGame()
      {
         FinishedCanvas.gameObject.SetActive(true);
         _state = EState.Finished;
      }

      public void ResumeGame()
      {
         Time.timeScale = 1;
         PauseCanvas.gameObject.SetActive(false);
         GameplayObject.gameObject.SetActive(true);
         OnResume?.Invoke();
         _state = EState.Playing;
      }

      public void PauseGame()
      {
         _state = EState.Paused;
         OnPause?.Invoke();
         Time.timeScale = 0;
         PauseCanvas.gameObject.SetActive(true);
         GameplayObject.gameObject.SetActive(false);
      }

      public void ExitGame()
      {
         SceneManager.LoadScene("Main Menu");
      }
   }
}