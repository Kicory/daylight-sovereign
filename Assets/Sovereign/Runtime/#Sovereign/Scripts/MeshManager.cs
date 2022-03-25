using System.Linq;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;

namespace NoFS.DayLight.Sovereign {

   [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
   public class MeshManager : MonoBehaviour {
      private MeshFilter meshHolder { get; set; } = default;

      private MeshRenderer meshRenderer { get; set; } = default;

      public Mesh mesh {
         get => meshHolder.mesh;
         private set => meshHolder.mesh = value;
      }

      public new MeshRenderer renderer {
         get => meshRenderer;
      }

      /// <summary>
      /// 메시 매니저를 사용하는 데에 필요한 material을 공급하고 <see cref="Mesh"/> 인스턴스를 가져온다.
      /// </summary>
      /// <param name="materials">이 <see cref="MeshManager"/>가 사용할 materials. 공급한 material 수만큼 submesh가 생성된다.</param>
      /// <returns>Board 위에 그리는 데에 사용할 수 있는 <see cref="Mesh"/> 인스턴스</returns>
      public Mesh prepareMeshPlaying(params Material[] materials) {
         mesh.subMeshCount = materials.Length;
         renderer.materials = materials;
         return getDynamicMesh();
      }

      private Mesh getDynamicMesh() {
         mesh.MarkDynamic();
         return mesh;
      }

      private void Awake() {
         meshHolder = GetComponent<MeshFilter>();
         meshRenderer = GetComponent<MeshRenderer>();
      }
   }
}