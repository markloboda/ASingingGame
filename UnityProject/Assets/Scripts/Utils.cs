using UnityEngine;

namespace Assets.Scripts
{
   static class Utils
   {
      public static string[] GetAvailableAudioFormats()
      {
         return new[] { "mp3", "wav", "flac", "aac", "ogg", "aiff" };
      }

      public static AudioType GetAudioType(string extension)
      {
         switch (extension)
         {
            case "mp3":
               return AudioType.MPEG;
            case "wav":
               return AudioType.WAV;
            case "flac":
               return AudioType.OGGVORBIS;
            case "aac":
               return AudioType.ACC;
            case "ogg":
               return AudioType.OGGVORBIS;
            case "aiff":
               return AudioType.AIFF;
            default:
               return AudioType.UNKNOWN;
         }
      }
   }
}