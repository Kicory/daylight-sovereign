using System.Threading;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;

namespace NoFS.DayLight.Sovereign {

   public abstract class SvrnInstance : MonoBehaviour {

      [SerializeField, InfoBox("메시를 그릴 때 사용할 메티리얼, 갯수에 따라 subMeshCount도 정해진다")]
      protected Material[] materials;

      //함수 짤 것: 점 A를 중심으로 점 B를 각도 C만큼 회전시킨 점 얻기

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