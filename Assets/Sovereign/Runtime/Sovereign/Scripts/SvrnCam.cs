using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace NoFS.DayLight.Sovereign {

   [ExecuteAlways]
   [RequireComponent(typeof(Camera), typeof(PostProcessVolume), typeof(PostProcessLayer))]
   [AddComponentMenu("Sovereign/Sovereign Camera")]
   public class SvrnCam : MonoBehaviour {
      public new Camera camera { get; private set; } = default;
      public PostProcessLayer postProcessLayer { get; private set; } = default;
      public PostProcessVolume postProcessVolume { get; private set; } = default;

      private void Awake() {
         camera = GetComponent<Camera>();
         postProcessLayer = GetComponent<PostProcessLayer>();
         postProcessVolume = GetComponent<PostProcessVolume>();
      }

#if UNITY_EDITOR

      public void setLayer(string mask) {
         gameObject.layer = LayerMask.NameToLayer(mask);
         camera.cullingMask = LayerMask.GetMask(mask);
         postProcessLayer.volumeLayer = LayerMask.GetMask(mask);
      }

#endif
   }
}