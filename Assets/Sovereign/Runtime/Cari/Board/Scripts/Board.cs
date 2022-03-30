using System;
using System.Collections.Generic;
using UnityEngine;

namespace NoFS.DayLight.CariBoard {

   [CreateAssetMenu(fileName = "CariBoardInstance", menuName = "CARI/Make Board", order = 100)]
   public class Board : ScriptableObject {

      [SerializeField]
      private RectInt _boardRect;
      [SerializeField]
      private List<Compo> _compos;

      public RectInt boardRect => _boardRect;

      /// <summary>
      /// 저장되어 있는 <see cref="Compo"/>들을 바탕으로 계산된 board 위 <see cref="Compo"/>들의 위치.
      /// </summary>
      public Compo[,] cachedBoardMap { get; private set; } = null;

      public Board() {
         _boardRect = new RectInt(0, 0, 1, 1);
         _compos = new List<Compo>();
      }

      public Compo[,] forceRemakeCachedBoardMap() {
         cachedBoardMap = new Compo[_boardRect.width, _boardRect.height];
         foreach (Compo compo in _compos) {
            var cRect = compo.rect;
            for (int xx = 0; xx < cRect.width; xx++) {
               for (int yy = 0; yy < cRect.height; yy++) {
#if INDEV
                  if (cachedBoardMap[xx, yy] != default) {
                     Debug.LogWarning($"({xx}, {yy})에서 겹침 있는 것 같음...");
                  }
                  try {
#endif
                     cachedBoardMap[xx, yy] = compo;
#if INDEV

                  } catch (IndexOutOfRangeException) {
                     Debug.LogError($"Board를 벗어난 {nameof(Compo)}가 있음: ({xx}, {yy})");
                  }
#endif
               }
            }
         }
         return cachedBoardMap;
      }

#if UNITY_EDITOR
      [SerializeField, Range(10, 60)]
      private float _cellSize;
#endif
   }

   /// <summary>
   /// Board 위에 올라가는 모든 요소들의 superclass
   /// </summary>
   [System.Serializable]
   public abstract class Compo {
      public abstract RectInt rect { get; }
   }

   [System.Serializable]
   public class Wire : Compo {
      [SerializeField]
      private Vector2Int _pos;

      public override RectInt rect => new RectInt(_pos, Vector2Int.one);
   }

   [System.Serializable]
   public class Axis : Compo {

      [SerializeField]
      private RectInt _rect;

      [SerializeField]
      private Afforder _afforder;

      public override RectInt rect => _rect;
      public Afforder afforder => _afforder;
      public string afforderCode => afforder?.code ?? null;

      public Axis(RectInt _rect, Afforder _afforder) {
#if INDEV
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

      public Afforder(string code) {
         this._code = code;
      }
   }
}