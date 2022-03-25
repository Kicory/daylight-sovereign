using Cysharp.Threading.Tasks;

namespace NoFS.DayLight.Sovereign.Cari {

   public abstract class CariInstance : SvrnInstance {

      public override sealed async UniTask<SvrnResult> svrnSequence(SvrnMaster master) {
         await doCariVisualConventions(master);
         return await cariSequence(master);
      }

      /// <summary>
      /// 반드시 지켜져야 하는 CARI의 visual convention을 구현함 (i.e., 백그라운드 렌더 방식)
      /// </summary>
      /// <remarks>https://docs.google.com/document/d/1u2MpWpFM4yZOD_lYdYsYSwquP3UnULySNq9KpSPR7Zk/edit#heading=h.o5osnl1u422w</remarks>
      public abstract UniTask doCariVisualConventions(SvrnMaster master);

      protected abstract UniTask<SvrnResult> cariSequence(SvrnMaster master);
   }
}