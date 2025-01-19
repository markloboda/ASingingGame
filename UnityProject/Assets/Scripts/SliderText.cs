using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
   public class SliderText : MonoBehaviour
   {
      public Slider Slider;
      public string PlayerPrefKey = "unknown";
      public float DefaultValue = 8.0f;

      void Start()
      {
         Slider.value = PlayerPrefs.GetFloat(PlayerPrefKey, DefaultValue);
         OnValueChange();
      }

      public void OnValueChange()
      {
         SetText();
         PlayerPrefs.SetFloat(PlayerPrefKey, MathF.Round(Slider.value, 1));
      }

      private void SetText()
      {
         TMP_Text text = GetComponent<TMP_Text>();
         text.text = Slider.value.ToString("F1");
      }
   }
}