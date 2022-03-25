using System.Collections.Generic;

namespace NoFS.DayLight.Sovereign {
   public class SvrnResult {
      /// <summary>
      /// Svrn이 어떻게 끝났는지의 종류
      /// </summary>
      public enum SvrnResultType { 
         /// <summary>
         /// 성공이든 실패든 정상적으로 끝남.
         /// </summary>
         Finished = 0, 
         /// <summary>
         /// 진행상에 문제(의도된 것)가 있어 중간에 Svrn 세션이 끝남.
         /// </summary>
         ForceExited,
         /// <summary>
         /// 진행상에 문제(에러, 의도되지 않은 것)가 있어 Svrn 세션이 끝남 (디버그용)
         /// </summary>
         Aborted
      }
      public SvrnResultType resultType;
      public bool dummy;
   }
}