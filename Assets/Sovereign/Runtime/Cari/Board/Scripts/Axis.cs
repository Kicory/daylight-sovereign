using System;
using UnityEngine;

namespace NoFS.DayLight.CariBoard {
   [Serializable]
   public class Axis : Compo {

      [SerializeField]
      private bool _blocking;
      public bool blocking => _blocking;

      public Axis(RectInt _rect, bool blocking) {
#if INDEV
         Debug.Assert(_rect.size.x >= 1 && _rect.size.y >= 1, "Axis의 크기는 0일 수 없음");
#endif
         rect = _rect;
         _blocking = blocking;
      }
   }
}