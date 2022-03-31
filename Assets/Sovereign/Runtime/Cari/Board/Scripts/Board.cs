using System.Collections.Generic;
using UnityEngine;

namespace NoFS.DayLight.CariBoard {

   [CreateAssetMenu(fileName = "CariBoardInstance", menuName = "CARI/Make Board", order = 100)]
   public partial class Board : ScriptableObject {

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
         _boardRect = new RectInt(0, 0, 2, 2);
         _compos = new List<Compo>();
         _compos.Add(new ActiveAxis(new RectInt(0, 0, 1, 1), new Afforder("TEST")));
         _compos.Add(new PassiveAxis(new RectInt(1, 1, 1, 1), new Afforder("TEST2")));
      }

      /// <summary> 런타임에만 사용하는 함수 </summary>
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
   }
}