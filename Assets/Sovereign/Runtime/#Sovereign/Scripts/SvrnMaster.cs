using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace NoFS.DayLight.Sovereign {

   [ExecuteAlways]
   public class SvrnMaster : MonoBehaviour {

      /// <summary>
      /// 씬에서 항상 하나뿐인 <see cref="SvrnMaster"/>.
      /// </summary>
      public static SvrnMaster inst => GameObject.FindWithTag("SvrnMaster").GetComponent<SvrnMaster>();

      public static Vector2 fieldSize => (inst.transform as RectTransform)?.rect.size ?? Vector2.zero;

      public PostProcessVolume PPV => camera.postProcessVolume;

      public SvrnBoard board { get; private set; }
      new public SvrnCam camera { get; private set; }

#if UNITY_EDITOR
      [InfoBox("Svrn 전용 레이어 설정 (필수!!)"), HorizontalLine, SerializeField, Layer, OnValueChanged("updateLayer")]
      private string svrnLayer;
#endif

      public MeshManager getMeshManagerInstance() {
         GameObject meshMangerObject = board.addMeshManagerObject();
         return meshMangerObject.GetComponent<MeshManager>();
      }

      public TSetting getPPSetting<TSetting>() where TSetting : PostProcessEffectSettings {
         return PPV?.profile.GetSetting<TSetting>() ?? null;
      }

      public async UniTask<SvrnResult> startSvrn(SvrnInstance inst) {
         var result = await inst.svrnSequence(master: this);
         board.cleanUpBoard();
         return result;
      }

      private void Awake() {
         board = GetComponentInChildren<SvrnBoard>();
         camera = GetComponentInChildren<SvrnCam>();
      }

#if UNITY_EDITOR
      public void updateLayer() {
         gameObject.layer = LayerMask.NameToLayer(svrnLayer);
         camera.setLayer(svrnLayer);
         board.setLayer(svrnLayer);
      }
#endif
   }
}