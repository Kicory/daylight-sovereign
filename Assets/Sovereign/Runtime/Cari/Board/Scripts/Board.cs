using UnityEngine;

namespace NoFS.DayLight.CariBoard {

   [CreateAssetMenu(fileName = "CariBoardInstance", menuName = "CARI/Make Board", order = 100)]
   public class Board : ScriptableObject {

      [SerializeField]
      private RectInt _boardRect;

#if UNITY_EDITOR

      [SerializeField, Range(10, 60)]
      private float _cellSize;

#endif

      public RectInt boardRect => _boardRect;

      public Board() {
         _boardRect = new RectInt(-5, -15, 50, 30);
      }
   }

   [System.Serializable]
   public class Axis {

      [SerializeField]
      private RectInt _rect;

      [SerializeField]
      private Afforder _afforder;

      public RectInt rect => _rect;
      public Afforder afforder => _afforder;

      [System.NonSerialized]
      public readonly Board board;

      public Axis(Board _board, RectInt _rect, Afforder _afforder) {
         this.board = _board;
#if INDEV
         Debug.Assert(_board.boardRect.Contains(_rect.min) && _board.boardRect.Contains(_rect.max), "Axis의 크기가 Board를 벗어남");
         Debug.Assert(_rect.size.x >= 1 && _rect.size.y >= 1, "Axis의 크기는 0일 수 없음");
#endif
         this._rect = _rect;
         this._afforder = _afforder;
      }
   }

   [System.Serializable]
   public class Afforder {

      [SerializeField]
      private string _code;

      public string code => _code;

      [System.NonSerialized]
      public readonly Axis axis;

      public Afforder(Axis axis, string code) {
#if INDEV
         Debug.Assert(axis != null && code != null, $"Afforder의 Axis({axis})와 code({code})는 null일 수 없음");
#endif
         this.axis = axis;
         this._code = code;
      }
   }
}