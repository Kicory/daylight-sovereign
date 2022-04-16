using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

namespace NoFS.DayLight.Sovereign {

   [ExecuteAlways]
   [AddComponentMenu("Sovereign/Sovereign Master")]
   public partial class SvrnMaster : MonoBehaviour {

      public enum Channel {
         Base,
         Dense
      };

      /// <summary>
      /// 현재 Sovereign 상태
      /// </summary>
      public enum SvrnStatus { Idle = 0, Running = 1 }

      /// <summary>
      /// 씬에서 항상 하나뿐인 <see cref="SvrnMaster"/>.
      /// </summary>
      private static SvrnMaster inst => GameObject.FindWithTag("SvrnMaster").GetComponent<SvrnMaster>();

      public Vector2 fieldSize => (transform as RectTransform)?.rect.size ?? Vector2.zero;

      public SvrnStatus status { get; private set; } = SvrnStatus.Idle;

      private Dictionary<Channel, SvrnBoard> boards;
      
      private Dictionary<Channel, SvrnCam> cameras;

      private Dictionary<Channel, PostProcessVolume> PPVs;

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
         cleanUpBoards();
         var result = await svrnInst.svrnSequence(master: this);
         cleanUpBoards();
         status = SvrnStatus.Idle;
         return result;
      }

      public MeshManager getMeshManagerInstance(Channel channel) {
         GameObject meshMangerObject = boards[channel].addMeshManagerObject();
         return meshMangerObject.GetComponent<MeshManager>();
      }

      public TSetting getPPSetting<TSetting>(Channel channel) where TSetting : PostProcessEffectSettings {
         return PPVs[channel]?.profile.GetSetting<TSetting>() ?? null;
      }

      private void cleanUpBoards() {
         foreach(var b in boards.Values) {
            b.cleanUpBoard();
         }
      }

      private void Start() {
         boards = new Dictionary<Channel, SvrnBoard>();
         cameras = new Dictionary<Channel, SvrnCam>();
         PPVs = new Dictionary<Channel, PostProcessVolume>();

         foreach (Channel channel in Enum.GetValues(typeof(Channel))) {
            Transform channelHolder = transform.Find($"{channel}");
            if (channelHolder != null) {
               string channelLayerName = $"{channel}Svrn";
#if INDEV
               if (LayerMask.NameToLayer(channelLayerName) == -1) {
                  Debug.LogError($"Sovereign package requires Layer named: {channelLayerName}. Sovereign will not work.");
                  continue;
               } 
#endif
               SvrnBoard board = channelHolder.GetComponentInChildren<SvrnBoard>();
               board.setLayer(channelLayerName);
               this.boards.Add(channel, board);

               SvrnCam cam = channelHolder.GetComponentInChildren<SvrnCam>();
               cam.setLayer(channelLayerName);
               PPVs.Add(channel, cam.postProcessVolume);
               this.cameras.Add(channel, cam);
            }
         }
      }

#if UNITY_EDITOR
      [Button("Toggle Visible")]
      private void toggleHide() {
         foreach (Transform t in transform) {
            t.hideFlags = t.hideFlags != HideFlags.None ? HideFlags.None : HideFlags.HideInHierarchy;
         }
      } 
#endif
   }
}