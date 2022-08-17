#if UNITY_EDITOR
using System;
using UnityEngine;

namespace NoFS.DayLight.CariBoard {
   public enum CompoType { None, Wire, Axis, Afforder, Container, Forwarder }

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
} 
#endif