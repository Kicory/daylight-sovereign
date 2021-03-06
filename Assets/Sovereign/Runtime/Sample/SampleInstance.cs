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
   [AddComponentMenu("Sovereign/Sample/Sovereign Sample Instance")]
   public class SampleInstance : SvrnInstance {

      [SerializeField, InfoBox("메시를 그릴 때 사용할 메티리얼, 갯수에 따라 subMeshCount도 정해진다")]
      protected Material[] materials;

      public override async UniTask<SvrnResult> svrnSequence(SvrnMaster master) {

         #region 초기 설정

         var cts = getCancelableTokenSource();
         var cts_vertexWaver = getCancelableTokenSource();
         var token = cts.Token;

         var meshHelper = new MeshHelper(master, SvrnMaster.Channel.Base);
         var mesh = meshHelper.manager.prepareMeshPlaying(materials);

         // Delaunator 사용할 거임
         DH del = new DH(DH.getCoveringPoints(new Rect(Vector2.zero, master.fieldSize), 12));
         // 삼각형 edge 정보 사용할 거임
         IEnumerable<DH.EdgeInfo> triEdges = del.willUseTriEdge();
         // 삼각형 정보 사용할 거임
         IEnumerable<DH.TriangleInfo> triangles = del.willUseTriangle();
         // Svrn GrapPass 사용할 거임

         #endregion 초기 설정

         var vertList = del.points.ToVectors3();
         mesh.SetVertices(vertList);
         mesh.SetColors(vertList.Select(v => getVertextCol(master, v)).ToArray());

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
            resultType = SvrnResult.SvrnResultType.Finished,
            dummy = false
         };
         return svrnResult;
      }

      #region 인스턴스 헬퍼 함수들

      private Color32 getVertextCol(SvrnMaster master, Vector3 v) {
         byte colval = (byte)(byte.MaxValue * (Vector2Int.FloorToInt(v).sqrMagnitude / master.fieldSize.sqrMagnitude));
         //return new Color32((byte)(colval * Random.Range(0, 1.2f)), (byte)(colval * Random.Range(0, 1.2f)), (byte)(colval * Random.Range(0, 1.2f)), byte.MaxValue);
         return new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
      }

      private System.Func<DH.EdgeInfo, float> EdgeSorter(DH delHel) {
         return e => Mathf.Max(delHel.getOriginalPoint(e.P).ToVector2().sqrMagnitude, delHel.getOriginalPoint(e.Q).ToVector2().sqrMagnitude);
      }

      private async UniTask makeEdge(Mesh mesh, int subMeshIdx, IEnumerable<DH.EdgeInfo> edges, CancellationToken token) {
         var indexList = new List<int>();
         double overWaited = 0;
         mesh.GetIndices(indexList, subMeshIdx);
         foreach (var edge in edges) {
            
            overWaited = await waitNextFramesIfNecessary(0.004, overWaited, token);

            indexList.Add(edge.Q);
            indexList.Add(edge.P);
            mesh.SetIndices(indexList, MeshTopology.Lines, subMeshIdx);
         }
      }

      private async UniTask makeTriFace(Mesh mesh, int subMeshIdx, IEnumerable<DH.TriangleInfo> tries, CancellationToken token) {
         var indexList = new List<int>();
         double overWaited = 0;
         mesh.GetIndices(indexList, subMeshIdx);
         foreach (var tri in tries) {

            overWaited = await waitNextFramesIfNecessary(0.007, overWaited, token);

            indexList.Add(tri.idx0);
            indexList.Add(tri.idx1);
            indexList.Add(tri.idx2);
            mesh.SetIndices(indexList, MeshTopology.Triangles, subMeshIdx);
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
            await UniTask.NextFrame(token);
         }
      }

      #endregion 인스턴스 헬퍼 함수들

      [Button("Do Svrn")]
      public void doSeq() {
         SvrnMaster.startSvrn(this).ContinueWith((result) => { Debug.Log(result.dummy); });
      }

      private void OnGUI() {
         if (Application.isPlaying && GUI.Button(new Rect(10, 10 + 80, 140, 30), "Start Sample Svrn")) {
            doSeq();
         }
      }
   }
}