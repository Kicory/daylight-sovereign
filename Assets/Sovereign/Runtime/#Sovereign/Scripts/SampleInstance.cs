using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DelaunatorSharp.Unity;
using DelaunatorSharp.Unity.Extensions;
using NaughtyAttributes;
using UnityEngine;
using DH = NoFS.DayLight.Sovereign.DelaunatorHelper;

namespace NoFS.DayLight.Sovereign.Sample {

   [AddComponentMenu("Sovereign/Sample/Sample Svrn Instance")]
   public class SampleInstance : SvrnInstance {

      public override async UniTask<SvrnResult> svrnSequence(SvrnMaster master) {

         #region 초기 설정

         var cts = getCancelableTokenSource();
         var cts_vertexWaver = getCancelableTokenSource();
         var token = cts.Token;

         var meshHelper = new MeshHelper();
         var mesh = meshHelper.manager.prepareMeshPlaying(this.materials);

         // Delaunator 사용할 거임.
         DH del = new DH(UniformPoissonDiskSampler.SampleRectangle(-Vector2.one * 5, SvrnMaster.fieldSize + Vector2.one * 5, 8).ToPoints());
         // 삼각형 edge 정보 사용할 거임.
         IEnumerable<DH.EdgeInfo> triEdges = del.willUseTriEdge();
         // 삼각형 정보 사용할 거임
         IEnumerable<DH.TriangleInfo> triangles = del.willUseTriangle();
         // Svrn GrapPass 사용할 거임

         #endregion 초기 설정

         var vertList = del.points.ToVectors3();
         mesh.SetVertices(vertList);
         mesh.SetColors(vertList.Select(getVertextCol).ToArray());

         var waver = vertexWave(mesh, cts_vertexWaver.Token);

         var edgeMaker = makeEdge(mesh, 0, triEdges.OrderBy(EdgeSorter(del)), token);
         await UniTask.Delay(1000, cancellationToken: token);
         var triFaceMaker = makeTriFace(mesh, 1, triangles.OrderBy(t => del.delaunator.GetTriangleCircumcenter(t.triIdx).ToVector2().sqrMagnitude), token);

         await UniTask.WhenAll(edgeMaker, triFaceMaker);

         //화면이 검은색으로 꽉 찼고 더 이상 쓸모가 없으므로
         cts_vertexWaver.Cancel();

         await UniTask.Delay(1000, cancellationToken: token);

         cts.Cancel();

         mesh.Clear();

         var svrnResult = new SvrnResult {
            dummy = false
         };
         return svrnResult;
      }

      #region 인스턴스 헬퍼 함수들

      private Color32 getVertextCol(Vector3 v) {
         byte colval = (byte)(byte.MaxValue * (Vector2Int.FloorToInt(v).sqrMagnitude / SvrnMaster.fieldSize.sqrMagnitude));
         //return new Color32((byte)(colval * Random.Range(0, 1.2f)), (byte)(colval * Random.Range(0, 1.2f)), (byte)(colval * Random.Range(0, 1.2f)), byte.MaxValue);
         return new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
      }

      private System.Func<DH.EdgeInfo, float> EdgeSorter(DH delHel) {
         return e => Mathf.Max(delHel.getOriginalPoint(e.P).ToVector2().sqrMagnitude, delHel.getOriginalPoint(e.Q).ToVector2().sqrMagnitude);
      }

      private async UniTask makeEdge(Mesh mesh, int subMeshIdx, IEnumerable<DH.EdgeInfo> edges, CancellationToken token) {
         var indexList = new List<int>();
         float timeSave = 0;
         float interval = 5;
         mesh.GetIndices(indexList, subMeshIdx);
         foreach (var edge in edges) {
            while (timeSave < interval) {
               await UniTask.NextFrame(token);
               timeSave += Time.deltaTime * 1000f;
            }
            timeSave -= interval;

            indexList.Add(edge.Q);
            indexList.Add(edge.P);
            mesh.SetIndices(indexList, MeshTopology.Lines, subMeshIdx);
            //await UniTask.Delay(5, cancellationToken: token);
         }
      }

      private async UniTask makeTriFace(Mesh mesh, int subMeshIdx, IEnumerable<DH.TriangleInfo> tries, CancellationToken token) {
         var indexList = new List<int>();
         float timeSave = 0;
         float interval = 7.5f;
         mesh.GetIndices(indexList, subMeshIdx);
         foreach (var tri in tries) {
            while (timeSave < interval) {
               await UniTask.NextFrame(token);
               timeSave += Time.deltaTime * 1000f;
            }
            timeSave -= interval;

            indexList.Add(tri.idx0);
            indexList.Add(tri.idx1);
            indexList.Add(tri.idx2);
            mesh.SetIndices(indexList, MeshTopology.Triangles, subMeshIdx);
            //await UniTask.Delay(10, cancellationToken: token);
         }
      }

      private async UniTask vertexWave(Mesh mesh, CancellationToken token) {
         var vertList = mesh.vertices;
         Vector3[] newVertList = new Vector3[mesh.vertexCount];
         while (true) {
            for (int idx = 0; idx < vertList.Length; idx++) {
               var vert = vertList[idx];
               newVertList[idx] = vert + new Vector3(Mathf.Cos(vert.sqrMagnitude + Time.time * 2), Mathf.Sin(vert.sqrMagnitude + Time.time * 2), vert.z) * 2;
            }
            mesh.vertices = newVertList;
            await UniTask.NextFrame(cancellationToken: token);
         }
      }

      #endregion 인스턴스 헬퍼 함수들

      [Button("Do Svrn")]
      public void doSeq() {
         SvrnMaster.inst.startSvrn(this).ContinueWith((result) => { Debug.Log(result.dummy); });
      }

      private void OnGUI() {
         if (Application.isPlaying && GUI.Button(new Rect(10, 10 + 80, 140, 30), "Start Sample Svrn")) {
            doSeq();
         }
      }
   }
}