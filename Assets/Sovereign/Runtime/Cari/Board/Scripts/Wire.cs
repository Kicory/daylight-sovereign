using System;
using UnityEngine;

namespace NoFS.DayLight.CariBoard {
   [Serializable]
   public class Wire : Compo {

      public Wire(RectInt rect) {
         this.rect = rect;
      }
   }
}