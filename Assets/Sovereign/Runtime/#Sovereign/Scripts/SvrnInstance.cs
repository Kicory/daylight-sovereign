using System.Threading;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;

namespace NoFS.DayLight.Sovereign {

   public abstract class SvrnInstance : MonoBehaviour {

      /// <summary>
      /// 중간에 cancel할 수 있는 <see cref="CancellationTokenSource"/>를 하나 만듦. Destroy시에 자동으로 cancel됨.
      /// </summary>
      protected CancellationTokenSource getCancelableTokenSource() {
         var cts = new CancellationTokenSource();
         cts.RegisterRaiseCancelOnDestroy(this);
         return cts;
      }

      public abstract UniTask<SvrnResult> svrnSequence(SvrnMaster master);
   }
}