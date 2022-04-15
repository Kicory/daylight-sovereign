using System;
using UnityEngine;

namespace NoFS.DayLight.CariBoard {

   public enum CompoType { None, PassiveAxis, ActiveAxis, Wire }

   /// <summary> Board 위에 올라가는 모든 요소들의 superclass </summary>
   [Serializable]
   public abstract class Compo {

      [SerializeField]
      private RectInt _rect;

      public RectInt rect { get => _rect; protected set => _rect = value; }
   }

}