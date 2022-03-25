using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

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
      [InfoBox("Svrn 전용 레이어 설정 (필수!!)", EInfoBoxType.Error), HorizontalLine, SerializeField, Layer, OnValueChanged("updateLayer")]
      private string svrnLayer;
      [InfoBox("Svrn 전용 렌더 텍스쳐 설졍 (필수!!)", EInfoBoxType.Error), SerializeField, OnValueChanged("updateRenderTexture")]
      private RenderTexture svrnTargetTexture;

      [ShowNativeProperty]
      // ExecuteAlways 라도 인스펙터에서 한 번 안 보이면 Awake가 실행이 안 되므로 null인 경우가 있다.
      private RenderTexture svrnTargetTextureShower => camera?.camera?.targetTexture ?? null;
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
         try {
            if (UnityEditor.PrefabUtility.IsPartOfImmutablePrefab(gameObject) == false) {
               gameObject.layer = LayerMask.NameToLayer(svrnLayer);
               camera.setLayer(svrnLayer);
               board.setLayer(svrnLayer);
               UnityEditor.PrefabUtility.ApplyPrefabInstance(this.gameObject, UnityEditor.InteractionMode.AutomatedAction);
            }
         }
         catch (System.ArgumentException) { }
      }
      public void updateRenderTexture() {
         try {
            if (UnityEditor.PrefabUtility.IsPartOfImmutablePrefab(gameObject) == false) {
               camera.camera.targetTexture = svrnTargetTexture;
               GetComponentInChildren<RawImage>().texture = svrnTargetTexture;
               UnityEditor.PrefabUtility.ApplyPrefabInstance(this.gameObject, UnityEditor.InteractionMode.AutomatedAction);
            }
         }
         catch (System.ArgumentException) { }
      }
#endif
   }
}