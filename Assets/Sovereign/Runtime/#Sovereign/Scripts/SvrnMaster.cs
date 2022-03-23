using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace NoFS.DayLight.Sovereign {

   public class SvrnMaster : MonoBehaviour {

      /// <summary>
      /// 씬에서 항상 하나뿐인 <see cref="SvrnMaster"/>.
      /// </summary>
      public static SvrnMaster inst => GameObject.FindWithTag("SvrnMaster").GetComponent<SvrnMaster>();

      public static Vector2 fieldSize => (inst.transform as RectTransform)?.rect.size ?? Vector2.zero;

      [SerializeField]
      public SvrnBoard board;
      [SerializeField]
      private GameObject meshManagerPrefab;
      [SerializeField]
      public PostProcessVolume postProcessing;

      private HashSet<GameObject> meshDrawers = new HashSet<GameObject>();

      public MeshManager getMeshManagerInstance() {
         GameObject obj = Instantiate(meshManagerPrefab, board.rtf, false);
         this.meshDrawers.Add(obj);
         return obj.GetComponent<MeshManager>();
      }

      public TSetting getPPSetting<TSetting>() where TSetting : PostProcessEffectSettings {
         return postProcessing?.profile.GetSetting<TSetting>() ?? null;
      }

      public async UniTask<SvrnResult> startSvrn(SvrnInstance inst) {
         var result = await inst.svrnSequence(master: this);
         cleanUpBoard();
         return result;
      }

      private void cleanUpBoard() {
         foreach (GameObject obj in meshDrawers) {
            Destroy(obj);
         }
         meshDrawers.Clear();
      }
   }
}