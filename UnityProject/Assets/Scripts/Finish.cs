using UnityEngine;

namespace Assets.Scripts
{
   public class Finish : MonoBehaviour
   {
      void OnCollisionEnter2D(Collision2D collision)
      {
         GameFinished();
      }

      public void GameFinished()
      {
         GetComponentInParent<ObstacleLevel>().GameFinished();
      }
   }
}