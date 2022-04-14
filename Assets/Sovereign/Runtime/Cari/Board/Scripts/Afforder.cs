using System;
using UnityEngine;

namespace NoFS.DayLight.CariBoard {

   public static class AfforderHelper {
      public static Afforder.Type type(this Afforder afforder) => afforder switch {
         ButtonAF => Afforder.Type.Button,

         null => Afforder.Type.Empty,
         _ => Afforder.Type.NA
      };
      public static Type type(this Afforder.Type afType) => afType switch {
         Afforder.Type.Button => typeof(ButtonAF),
         
         Afforder.Type.Empty => throw new ArgumentException($"{afType}은 연결된 {nameof(Afforder)} 타입이 없음"),
         Afforder.Type.NA => throw new ArgumentException($"{afType}은 연결된 {nameof(Afforder)} 타입이 없음"),
      };
   }

   public abstract partial class Afforder {
      public enum Type {
         Empty = 0,
         Button = 1,
         NA = int.MaxValue
      }
      
      [SerializeField]
      private string _code;

      public string code => _code;

      public Afforder(string code) {
         this._code = code;
      }

   }

   [Serializable]
   public class ButtonAF : Afforder {
      [SerializeField]
      private float signalDuration = 1;

      public ButtonAF(string code) : base(code) {
      }
   }
}