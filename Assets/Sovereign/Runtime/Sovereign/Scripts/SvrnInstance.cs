using System.Threading;
using Cysharp.Threading.Tasks;
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

      /// <summary>
      /// 프레임 단위로 기다려야 하는 경우에, 매우 짧은 시간간격(~5ms)으로
      /// 일어나는 일들의 속도를 제어하기 어렵다. 이런 경우에 여러 프레임을
      /// 연속으로 쉬고 난 뒤에는 동시에 이벤트가 두 번 일어나는 식으로
      /// 속도를 맞추어야 한다. 이를 위해 구현된 함수.
      /// </summary>
      /// <param name="interval">원하는 시간 간격 (초) (주로 매우 짧음)</param>
      /// <param name="overWaited">함수 scope 바깥에서 관리되어야 하는 변수이며, 처음에는 0의 값을 가져야 한다.</param>
      /// <returns>새로운 <paramref name="overWaited"/> 값</returns>
      /// <remarks>
      /// 긴 인터벌도 지원한다.
      /// <code>
      /// float overWaited = 0;
      /// ...
      ///   overWaited = await waitNextFrameIfNecessary(0.005f, overWaited, token); // 5ms
      ///   ...
      /// </code>
      /// </remarks>
      protected async UniTask<double> waitNextFramesIfNecessary(double interval, double overWaited, CancellationToken token) {
         while (overWaited < interval) {
            await UniTask.NextFrame(token);
            overWaited += Time.deltaTime;
         }
         return overWaited - interval;
      }

      public abstract UniTask<SvrnResult> svrnSequence(SvrnMaster master);
   }
}