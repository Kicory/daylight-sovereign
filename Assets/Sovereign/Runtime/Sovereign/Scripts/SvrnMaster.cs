using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

namespace NoFS.DayLight.Sovereign {

   [ExecuteAlways]
   [AddComponentMenu("Sovereign/Sovereign Master")]
   public partial class SvrnMaster : MonoBehaviour {

      /// <summary>
      /// 현재 Sovereign 상태
      /// </summary>
      public enum SvrnStatus { Idle = 0, Running = 10 }

      /// <summary>
      /// 씬에서 항상 하나뿐인 <see cref="SvrnMaster"/>.
      /// </summary>
      private static SvrnMaster inst => GameObject.FindWithTag("SvrnMaster").GetComponent<SvrnMaster>();

      public Vector2 fieldSize => (transform as RectTransform)?.rect.size ?? Vector2.zero;

      public SvrnStatus status { get; private set; } = SvrnStatus.Idle;

      public SvrnBoard board { get; private set; }

      public new SvrnCam camera { get; private set; }

      public PostProcessVolume PPV => camera.postProcessVolume;

      public static async UniTask<SvrnResult> startSvrn(SvrnInstance svrnInst) {
         return await inst.startSvrnInternal(svrnInst);
      }

      private async UniTask<SvrnResult> startSvrnInternal(SvrnInstance svrnInst) {
         if (status != SvrnStatus.Idle) {
#if INDEV
            Debug.LogWarning($"{svrnInst.gameObject.name}가 기존 SVRN 이 진행되는 동안 새로운 SVRN을 시작하려 함.");
#endif
            return new SvrnResult() {
               resultType = SvrnResult.SvrnResultType.Aborted
            };
         }
         status = SvrnStatus.Running;
         board.cleanUpBoard();
         var result = await svrnInst.svrnSequence(master: this);
         board.cleanUpBoard();
         status = SvrnStatus.Idle;
         return result;
      }

      public MeshManager getMeshManagerInstance() {
         GameObject meshMangerObject = board.addMeshManagerObject();
         return meshMangerObject.GetComponent<MeshManager>();
      }

      public TSetting getPPSetting<TSetting>() where TSetting : PostProcessEffectSettings {
         return PPV?.profile.GetSetting<TSetting>() ?? null;
      }

      private void Awake() {
         board = GetComponentInChildren<SvrnBoard>();
         camera = GetComponentInChildren<SvrnCam>();
      }
   }

#if UNITY_EDITOR

   public partial class SvrnMaster {

      [InfoBox("Svrn 전용 레이어 설정 (필수!!)", EInfoBoxType.Error), HorizontalLine, SerializeField, Layer, OnValueChanged("updateLayer")]
      private string svrnLayer;

      [InfoBox("Svrn 전용 렌더 텍스쳐 설졍 (필수!!)", EInfoBoxType.Error), SerializeField, OnValueChanged("updateRenderTexture")]
      private RenderTexture svrnTargetTexture;

      [ShowNativeProperty]
#pragma warning disable UNT0007 // Null coalescing on Unity objects
#pragma warning disable UNT0008 // Null propagation on Unity objects
      private RenderTexture svrnTargetTextureShower => camera?.camera?.targetTexture ?? null;
#pragma warning restore UNT0008 // Null propagation on Unity objects
#pragma warning restore UNT0007 // Null coalescing on Unity objects

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
   }

#endif
}