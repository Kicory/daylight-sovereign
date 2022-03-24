using Cysharp.Threading.Tasks;

namespace NoFS.DayLight.Sovereign.Cari {

   public abstract class CariInstance : SvrnInstance {

      public override sealed async UniTask<SvrnResult> svrnSequence(SvrnMaster master) {
         await doCariVisualConventions(master);
         return await cariSequence(master);
      }

      private async UniTask doCariVisualConventions(SvrnMaster master) {
         await UniTask.Delay(1000);
         return;
      }

      protected abstract UniTask<SvrnResult> cariSequence(SvrnMaster master);
   }
}