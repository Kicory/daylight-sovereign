using System;
using UnityEngine;

namespace NoFS.DayLight.CariBoard {
   
   /// <summary> Board 위에 올라가는 모든 요소들의 superclass </summary>
   [Serializable]
   public abstract class Compo {

      [SerializeField]
      private string _id;
      public string id => _id;

      [SerializeField]
      private RectInt _rect;

      public RectInt rect { get => _rect; protected set => _rect = value; }
   }
}