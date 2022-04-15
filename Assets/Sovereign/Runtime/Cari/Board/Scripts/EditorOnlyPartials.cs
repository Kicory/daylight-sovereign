#if UNITY_EDITOR
using System;
using UnityEngine;

namespace NoFS.DayLight.CariBoard {
   public partial class Board {

      [SerializeField, Range(10, 100)]
#pragma warning disable CS0414
      private float _cellSize = 10;
#pragma warning restore CS0414

      [SerializeField]
      private CompoType _creatingCompoType;

      public void invalidateBoardMap() {
         hasValidCache = false;
      }
   }

   public abstract partial class Axis {
#pragma warning disable CS0414
      [SerializeField]
      private Afforder.Type _afType = default;
#pragma warning restore CS0414
   }
} 
#endif