using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NoFS.DayLight.CariBoard {

   [CreateAssetMenu(fileName = "CariBoardInstance", menuName = "CARI/Make Board", order = 100)]
   public partial class Board : ScriptableObject {

      [SerializeField]
      private RectInt _boardRect;

      [SerializeReference]
      private List<Compo> _compos;

      public RectInt boardRect => _boardRect;

      private Compo this[int xx, int yy] => cachedBoardMap[xx - boardRect.xMin, yy - boardRect.yMin];

      /// <summary>저장되어 있는 <see cref="Compo"/>들을 바탕으로 계산된 board 위 <see cref="Compo"/>들의 위치. </summary>
      public Compo[,] cachedBoardMap {
#if UNITY_EDITOR
         private
#endif
         get {
            if (_cachedBoardMap == null) {
               _cachedBoardMap = makeCachedBoardMap();
            }
            return _cachedBoardMap;
         }
#if UNITY_EDITOR
         set {
            Debug.Assert(value == null, "널로 만드는 것만 허용함. 널로 만들겠음");
            _cachedBoardMap = null;
         }
#endif
      }

      [NonSerialized]
      private Compo[,] _cachedBoardMap = null;

      /// <summary> 런타임에만 사용하는 함수 </summary>
      private Compo[,] makeCachedBoardMap() {
         var ret = new Compo[_boardRect.width, _boardRect.height];
         foreach (Compo compo in _compos) {
            var cRect = compo.rect;

            //minx, miny가 0인 상태로 만들어주기
            cRect.x -= boardRect.xMin;
            cRect.y -= boardRect.yMin;

            for (int xx = cRect.min.x; xx < cRect.max.x; xx++) {
               for (int yy = cRect.min.y; yy < cRect.max.y; yy++) {
                  ret[xx, yy] = compo;
               }
            }
         }
         return ret;
      }

      public Compo this[int idx] => _compos[idx];
      public List<Compo> this[string AfCode] => string.IsNullOrWhiteSpace(AfCode) ? null : _compos.FindAll((c) => c is Axis axis && (axis.afforder?.code?.Equals(AfCode) ?? false));
      public IEnumerable<T> getComposOfType<T>() where T : Compo => _compos.OfType<T>();

      #region MOVE

      public ActiveAxis tryMoveFrom(Compo fromCompo, Vector2 direction) {
#if INDEV
         Debug.Assert(_compos.Contains(fromCompo), $"Board에 없는 {nameof(Compo)}가 fromCompo로 지정되었음");
#endif
         var dirVector = Vector2Int.RoundToInt(direction.normalized);
         if (!(dirVector.x == 0 ^ dirVector.y == 0)) {
            //둘 다 0이거나 둘 이상의 input
            return null;
         }

         int step;
         int startSearch;
         int endSearch;
         RectInt fromRect = fromCompo.rect;
         if (dirVector.y != 0) {
            //세로 방향 찾기
            step = dirVector.y;
            startSearch = step == 1 ? fromRect.yMax : fromRect.yMin - 1;
            endSearch = step == 1 ? boardRect.yMax : boardRect.yMin - 1;

            for (int yy = startSearch; yy * step < endSearch * step; yy += step) {
               for (int xx = fromRect.xMin; xx < fromRect.xMax; xx++) {
                  //좌에서 우로 훑기 (정석)
                  if (this[xx, yy] is ActiveAxis axis) {
                     return axis;
                  }
               }
            }
            return null;
         }
         if (dirVector.x != 0) {
            //가로 방향 찾기
            step = dirVector.x;
            startSearch = step == 1 ? fromRect.xMax : fromRect.xMin - 1;
            endSearch = step == 1 ? boardRect.xMax : boardRect.xMin - 1;

            for (int xx = startSearch; xx * step < endSearch * step; xx += step) {
               for (int yy = fromRect.yMax - 1; yy >= fromRect.yMin; yy--) {
                  //위에서 아래로 훑기 (정석)
                  if (this[xx, yy] is ActiveAxis axis) {
                     return axis;
                  }
               }
            }
            return null;
         }
         //이미 모든 케이스를 다 처리했음...
         Debug.Assert(false);
         return null;
      }

      #endregion MOVE

      public Board() {
         _boardRect = new RectInt(-2, -2, 5, 5);
         _compos = new List<Compo>();
      }
   }
}