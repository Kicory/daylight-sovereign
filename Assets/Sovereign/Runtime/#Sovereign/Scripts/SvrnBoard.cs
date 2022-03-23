using UnityEngine;
using RTF = UnityEngine.RectTransform;

namespace NoFS.DayLight.Sovereign {
   public class SvrnBoard : MonoBehaviour {
      public RTF rtf => transform as RTF;
   }
}