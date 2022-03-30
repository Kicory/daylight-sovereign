using System;
using System.Collections.Generic;
using UnityEngine;

namespace NoFS.DayLight.CariBoard {

   [CreateAssetMenu(fileName = "CariBoardInstance", menuName = "CARI/Make Board", order = 100)]
   public class Board : ScriptableObject {

      [SerializeField]
      private RectInt _boardRect;
      [SerializeReference]
      private List<Compo> _compos;

      public RectInt boardRect => _boardRect;

      /// <summary>
      /// 저장되어 있는 <see cref="Compo"/>들을 바탕으로 계산된 board 위 <see cref="Compo"/>들의 위치.
      /// </summary>
      public Compo[,] cachedBoardMap { get; private set; } = null;

      public Board() {
         _boardRect = new RectInt(0, 0, 1, 1);
         _compos = new List<Compo>();
         _compos.Add(new Axis(new RectInt(0, 0, 1, 1), new Afforder("TEST")));
      }

      /// <summary>
      /// 런타임에만 사용하는 함수
      /// </summary>
      private void makeCachedBoardMap() {
         cachedBoardMap = new Compo[_boardRect.width, _boardRect.height];
         foreach (Compo compo in _compos) {
            var cRect = compo.rect;
            for (int xx = 0; xx < cRect.width; xx++) {
               for (int yy = 0; yy < cRect.height; yy++) {
                  cachedBoardMap[xx, yy] = compo;
               }
            }
         }
      }



#if UNITY_EDITOR
      [SerializeField, Range(10, 60)]
      private float _cellSize = 10;

      public CompoInfo[,] forceRemakeCachedBoardMap() {
         var compoIndexMap = new CompoInfo[_boardRect.width, _boardRect.height];
         for (int idx = 0; idx < _compos.Count; idx++) {
            Compo compo = _compos[idx];
            var cRect = compo.rect;
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
#endif
   }

#if UNITY_EDITOR
   public class CompoInfo {
      public int indexInBoardList;
      public Type type;
   }
#endif

   /// <summary>
   /// Board 위에 올라가는 모든 요소들의 superclass
   /// </summary>
   [Serializable]
   public abstract class Compo {
      [SerializeField]
      private RectInt _rect;

      public RectInt rect { get => _rect; protected set => _rect = value; }
   }

   [Serializable]
   public class Wire : Compo {
   }

   [Serializable]
   public class Axis : Compo {
      [SerializeField]
      private Afforder _afforder;

      public Afforder afforder => _afforder;
      public string afforderCode => afforder?.code ?? null;

      public Axis(RectInt _rect, Afforder _afforder) {
#if INDEV
         Debug.Assert(_rect.size.x >= 1 && _rect.size.y >= 1, "Axis의 크기는 0일 수 없음");
#endif
         this.rect = _rect;
         this._afforder = _afforder;
      }
   }

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