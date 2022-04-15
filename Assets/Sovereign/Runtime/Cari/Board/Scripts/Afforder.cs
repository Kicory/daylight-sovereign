using System;
using UnityEngine;

namespace NoFS.DayLight.CariBoard {

   public static class AfforderHelper {
      public static Afforder.Type type(this Afforder afforder) => afforder switch {
         SquareButton => Afforder.Type.SquareButton,
         VerticalSwitch => Afforder.Type.VerticalSwitch,

         null => Afforder.Type.Empty,
         _ => Afforder.Type.NA
      };

#pragma warning disable CS8524
      //Excuse: default case를 없애서 enum entry 추가할 때마다 에러 뜨도록 함
      public static Type type(this Afforder.Type afType) => afType switch {
         Afforder.Type.SquareButton => typeof(SquareButton),
         Afforder.Type.VerticalSwitch => typeof(VerticalSwitch),
         
         Afforder.Type.Empty => throw new ArgumentException($"{afType}은 연결된 {nameof(Afforder)} 타입이 없음"),
         Afforder.Type.NA => throw new ArgumentException($"{afType}은 연결된 {nameof(Afforder)} 타입이 없음"),
      };
#pragma warning restore CS8524
   }

   public abstract partial class Afforder {
      public enum Type {
         Empty = 0,
         SquareButton = 1,
         VerticalSwitch = 2,
         NA = int.MaxValue,
      }
      
      [SerializeField]
      private string _code;

      public string code => _code;

      public Afforder(string code) {
         this._code = code;
      }

   }

   [Serializable]
   public class SquareButton : Afforder {
      [SerializeField]
      private float signalDuration = 1;

      public SquareButton(string code) : base(code) {
      }
   }

   [Serializable]
   public class VerticalSwitch : Afforder {
      [SerializeField]
      private float toughness = 0.5f;

      public VerticalSwitch(string code) : base(code) {
      }
   }
}