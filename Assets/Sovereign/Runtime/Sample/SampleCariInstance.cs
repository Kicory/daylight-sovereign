using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using NoFS.DayLight.CariBoard;
using UnityEngine;

namespace NoFS.DayLight.Sovereign.Cari {
   public class SampleCariInstance : CariInstance {
      [SerializeField]
      private Board board;

      [Button]
      public void doWhatever() {
         foreach(Afforder c in board.findAll("PRIME")) {
            Debug.Log(c.GetType().Name);
         }
      }

      public override async UniTask doCariVisualConventions(SvrnMaster master) {
         return;
      }

      protected override async UniTask<SvrnResult> cariSequence(SvrnMaster master) {
         var cts = getCancelableTokenSource();

         await UniTask.NextFrame(cts.Token);

         return new SvrnResult() {
            resultType = SvrnResult.SvrnResultType.Finished,
            dummy = true
         };
      }
   }
}