using System;
using UnityEngine;

namespace NoFS.DayLight.CariBoard {
   [Serializable]
   public abstract partial class Axis : Compo {

      [SerializeReference]
      private Afforder _afforder;

      public Afforder afforder => _afforder;
      public Afforder.Type afType => afforder.type();
      public string afforderCode => afforder?.code ?? null;

      public Axis(RectInt _rect, Afforder _afforder) {
#if INDEV
         Debug.Assert(_rect.size.x >= 1 && _rect.size.y >= 1, "Axis의 크기는 0일 수 없음");
#endif
         this.rect = _rect;
         this._afforder = _afforder;
      }
   }

   [Serializable]
   public class PassiveAxis : Axis {

      public PassiveAxis(RectInt _rect, Afforder _afforder) : base(_rect, _afforder) {
      }
   }

   [Serializable]
   public class ActiveAxis : Axis {

      public ActiveAxis(RectInt _rect, Afforder _afforder) : base(_rect, _afforder) {
      }
   }
}