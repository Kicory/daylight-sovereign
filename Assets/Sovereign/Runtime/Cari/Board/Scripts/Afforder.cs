using System;
using UnityEngine;

namespace NoFS.DayLight.CariBoard {
   [Serializable]
   public class Afforder {

      [SerializeField]
      private string _code;

      public string code => _code;

      public Afforder(string code) {
         this._code = code;
      }
   }
}