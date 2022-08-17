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

      [NonSerialized]
      private bool hasValidCache = false;

      [NonSerialized]
      private Compo[,] _cachedBoardMap = null;

      /// <summary>저장되어 있는 <see cref="Compo"/>들을 바탕으로 계산된 board 위 <see cref="Compo"/>들의 위치. </summary>
      private Compo[,] cachedBoardMap {
         get {
            if (!hasValidCache) {
               validateBoardCaches();
            }
            return _cachedBoardMap;
         }
      }

      /// <summary> 런타임에만 사용하는 함수 </summary>
      private void validateBoardCaches() {
         var newBoardMapCache = new Compo[_boardRect.width, _boardRect.height];
      
         foreach (Compo compo in _compos) {
            var cRect = compo.rect;

            //minx, miny가 0인 상태로 만들어주기
            cRect.x -= boardRect.xMin;
            cRect.y -= boardRect.yMin;

            for (int xx = cRect.min.x; xx < cRect.max.x; xx++) {
               for (int yy = cRect.min.y; yy < cRect.max.y; yy++) {
                  newBoardMapCache[xx, yy] = compo;
               }
            }
         }

         _cachedBoardMap = newBoardMapCache;
         hasValidCache = true;
      }

      public Compo this[int idx] => _compos[idx];
      public Compo findFirst(string compoId) => _compos.Find((compo) => !string.IsNullOrEmpty(compo.id) && compo.id.Equals(compoId));
      public Axis findFirstAxis(string axisId) => _compos.Find((compo) => compo is Axis axis && !string.IsNullOrEmpty(axis.id) && axis.id.Equals(axisId)) as Axis;
      public IEnumerable<T> getComposOfType<T>() where T : Compo => _compos.OfType<T>();

      #region MOVE

      public Axis tryProjectionFrom(Compo fromCompo, Vector2 direction) {
#if INDEV
         Debug.Assert(_compos.Contains(fromCompo), $"Board에 없는 {nameof(Compo)}가 fromCompo로 지정되었음");
#endif
         var dirVector = Vector2Int.RoundToInt(direction.normalized);
         if (dirVector.x != 0 ^ dirVector.y != 0) {
            //둘 다 0이거나 둘 이상의 input
            return null;
         }

         int mainStep;
         int startMainSearch;
         int endMainSearch;
         //PassiveAxis로 막혔을 경우 그 뒤로는 도달 못하는 매커니즘
         HashSet<int> excludedSubSearch = new HashSet<int>();
         RectInt fromRect = fromCompo.rect;

         //세로 방향 찾기
         if (dirVector.y != 0) {
            mainStep = dirVector.y;
            startMainSearch = mainStep == 1 ? fromRect.yMax : fromRect.yMin - 1;
            endMainSearch = mainStep == 1 ? boardRect.yMax : boardRect.yMin - 1;

            for (int yy = startMainSearch; yy * mainStep < endMainSearch * mainStep; yy += mainStep) {
               //좌에서 우로 훑기 (정석)
               for (int xx = fromRect.xMin; xx < fromRect.xMax; xx++) {
                  if (excludedSubSearch.Contains(xx))
                     continue;
                  
                  if (this[xx, yy] is Axis axis) {
                     if (axis.blocking) {
                        excludedSubSearch.Add(xx);
                        continue;
                     }
                     else {
                        return axis;
                     }
                  }
               }
            }
            return null;
         }
         //가로 방향 찾기
         if (dirVector.x != 0) {
            mainStep = dirVector.x;
            startMainSearch = mainStep == 1 ? fromRect.xMax : fromRect.xMin - 1;
            endMainSearch = mainStep == 1 ? boardRect.xMax : boardRect.xMin - 1;

            for (int xx = startMainSearch; xx * mainStep < endMainSearch * mainStep; xx += mainStep) {
               //위에서 아래로 훑기 (정석)
               for (int yy = fromRect.yMax - 1; yy >= fromRect.yMin; yy--) {
                  if (excludedSubSearch.Contains(yy))
                     continue;

                  if (this[xx, yy] is Axis axis) {
                     if (axis.blocking) {
                        excludedSubSearch.Add(yy);
                        continue;
                     }
                     else {
                        return axis;
                     }
                  }

               }
            }
            return null;
         }
#if INDEV
         //이미 모든 케이스를 다 처리했음...
         Debug.Assert(false);
#endif
         return null;
      }

      #endregion MOVE

      public Board() {
         _boardRect = new RectInt(-2, -2, 5, 5);
         _compos = new List<Compo>();
      }
   }
}