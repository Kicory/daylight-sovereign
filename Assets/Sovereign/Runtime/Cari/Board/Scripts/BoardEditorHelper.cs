#if UNITY_EDITOR
using System;
using UnityEngine;

namespace NoFS.DayLight.CariBoard {
   public partial class Board {

      [SerializeField, Range(10, 60)]
      private float _cellSize = 10;

      [SerializeField]
      private CompoType _creatingCompoType;
   }
} 
#endif