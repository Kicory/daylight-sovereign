#if UNITY_EDITOR
using System;
using UnityEngine;

namespace NoFS.DayLight.CariBoard {
   public partial class Board {

      [SerializeField, Range(10, 60)]
      private float _cellSize = 10;

      public CompoInfo[,] forceRemakeCachedBoardMap() {
         var compoIndexMap = new CompoInfo[boardRect.width, boardRect.height];
         for (int idx = 0; idx < _compos.Count; idx++) {
            Compo compo = _compos[idx];
            var cRect = compo.rect;
            //minx, miny가 0인 상태로 만들어주기 (배열)
            cRect.x -= boardRect.min.x;
            cRect.y -= boardRect.min.y;

            for (int xx = cRect.min.x; xx < cRect.max.x; xx++) {
               for (int yy = cRect.min.y; yy < cRect.max.y; yy++) {
                  try {
                     if (compoIndexMap[xx, yy] != null) {
                        Debug.LogWarning($"({xx}, {yy})에서 겹침 있는 것 같음...");
                     }
                     compoIndexMap[xx, yy] = new CompoInfo() {
                        indexInBoardList = idx,
                        type = compo.GetType()
                     };
                  }
                  catch (IndexOutOfRangeException) {
                     Debug.LogError($"Board를 벗어난 {nameof(Compo)}가 있음: ({xx}, {yy})");
                  }
               }
            }
         }
         return compoIndexMap;
      }

      public class CompoInfo {
         public int indexInBoardList;
         public Type type;
      }
   }
} 
#endif