using System;
using UnityEngine;

namespace NoFS.DayLight.CariBoard {
   [Serializable]
   public abstract class Actor : Axis {
      [SerializeField]
      private bool _locked;
      public bool locked => _locked;

      protected Actor(RectInt _rect, bool blocking, bool locked) : base(_rect, blocking) {
         _locked = locked;
      }
   }

   [Serializable]
   public class Afforder : Actor {
      public Afforder(RectInt _rect, bool blocking, bool locked) : base(_rect, blocking, locked) {
      }
   }

   [Serializable]
   public class Container : Actor {
      public Container(RectInt _rect, bool blocking, bool locked) : base(_rect, blocking, locked) {
      }
   }
}