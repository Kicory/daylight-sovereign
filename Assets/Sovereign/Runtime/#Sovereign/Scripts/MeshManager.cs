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

      public Mesh prepareMeshPlaying(Material[] materials) {
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