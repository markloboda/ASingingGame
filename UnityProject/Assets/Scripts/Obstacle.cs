using UnityEngine;

namespace Assets.Scripts
{
   [ExecuteInEditMode]
   public class Obstacle : MonoBehaviour
   {
      [Range(0, 1)] public float YPosition = 0.5f;
      [Range(0, 1)] public float GapSize = 0.2f;

      public Transform TopCollider;
      public Transform BottomCollider;

      void Update()
      {
         TopCollider.localPosition = new Vector2(0, (YPosition + 0.5f + GapSize / 2));
         BottomCollider.localPosition = new Vector2(0, (YPosition - 0.5f - GapSize / 2));
      }

      void OnCollisionEnter2D(Collision2D collision)
      {
         GameOver();
         collision.gameObject.GetComponent<PlayerController>()?.GameOver();
      }

      private void GameOver()
      {
         GetComponentInParent<ObstacleLevel>().GameOver();
      }
   }
}