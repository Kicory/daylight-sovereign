using Cysharp.Threading.Tasks;

namespace NoFS.DayLight.Sovereign.Cari {
   public class SampleCariInstance : CariInstance {
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