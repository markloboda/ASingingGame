using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts
{
   public class FrequencyLine : MonoBehaviour
   {
      public List<float> Frequencies;
      public float LineWidth = 0.1f;
      public float LineLength = 50;
      public float MaxPlayerFrequencyHz = 0;
      public float MinPlayerFrequencyHz = 0;

      private LineRenderer _lineRenderer;

      public void SetupLine()
      {
         if (Frequencies.Count <= 0)
         {
            return;
         }

         _lineRenderer = gameObject.GetComponent<LineRenderer>();
         if (_lineRenderer == null)
         {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.numCornerVertices = 5;
            _lineRenderer.numCapVertices = 5;
         }

         _lineRenderer.startWidth = LineWidth;
         _lineRenderer.endWidth = LineWidth;
         _lineRenderer.useWorldSpace = false;

         _lineRenderer.enabled = true;
         _lineRenderer.positionCount = Frequencies.Count;

         for (var i = 0; i < Frequencies.Count; i++)
         {
            var x = i * LineLength / Frequencies.Count;
            var y = (Frequencies[i] - MinPlayerFrequencyHz) / MaxPlayerFrequencyHz;
            _lineRenderer.SetPosition(i, new Vector2(x, y));
         }
      }
   }
}