using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using DelaunatorSharp;
using DelaunatorSharp.Unity;
using DelaunatorSharp.Unity.Extensions;

namespace NoFS.DayLight.Sovereign {
   public class DelaunatorHelper {
      #region Data structures

      public struct EdgeInfo {
         public int edgeIdx;
         public int Q;
         public int P;

         public EdgeInfo(int edgeIdx, int vertIdxQ, int vertIdxP) {
            this.edgeIdx = edgeIdx;
            this.Q = vertIdxQ;
            this.P = vertIdxP;
         }
      }

      public struct TriangleInfo {
         public int triIdx;
         public int idx0;
         public int idx1;
         public int idx2;

         public TriangleInfo(int triIdx, IEnumerable<IPoint> points, System.Func<IPoint, int> idxGetter) {
            this.triIdx = triIdx;
#if INDEV
            Debug.Assert(points.Count() == 3);
#endif
            this.idx0 = idxGetter(points.ElementAt(0));
            this.idx1 = idxGetter(points.ElementAt(1));
            this.idx2 = idxGetter(points.ElementAt(2));
         }
      }

      #endregion Data structures

      /// <summary>
      /// 생성된 <see cref="Delaunator"/> 객체
      /// </summary>
      public readonly Delaunator delaunator;

      public IPoint[] points => delaunator.Points;

      /// <summary>
      /// 삼각형의 면이 주어진 <see cref="Rect"/>, <paramref name="toCover"/>을 완전히 덮는 것이 보장된 <see cref="IPoint"/>s를 리턴함
      /// </summary>
      /// <remarks>
      /// <code>
      /// ... new DelaunatorHelper(getCoveringPoints(new Rect(Vector2.zero, master.fieldSize), 8));
      /// </code>
      /// </remarks>
      public static IPoint[] getCoveringPoints(Rect toCover, float minDistance) {
         Vector2 padding = Vector2.one * minDistance;
         return UniformPoissonDiskSampler.SampleRectangle(toCover.min - padding, toCover.max + padding, minDistance).ToPoints();
      }
      
      /// <summary>
      /// Delaunator 판 하나를 만들 <see cref="DelaunatorHelper"/> 객체 생성 및 초기화
      /// </summary>
      /// <param name="points">사용할 포인트들 (초기값)</param>
      public DelaunatorHelper(IPoint[] points) {
         delaunator = getDelaunator(points);
      }

      #region Point Hashing / Initialize

      private Dictionary<IPoint, int> originalPointsHash;

      private IReadOnlyList<IPoint> originalPointList;

      private Delaunator getDelaunator(IPoint[] points) {
         var del = new Delaunator(points);
         makePointHash(del.Points);
         originalPointList = del.Points;
         return del;
      }

      private void makePointHash(IEnumerable<IPoint> points) {
         originalPointsHash = originalPointsHash ?? new Dictionary<IPoint, int>();
         originalPointsHash.Clear();

         int idx = 0;
         foreach (var point in points) {
            originalPointsHash.Add(point, idx++);
         }
      }

      private int getPointIdx(IPoint point) {
         return originalPointsHash[point];
      }

      /// <summary>
      /// <see cref="getDelaunator(IPoint[])"/> 이후에만 사용 가능.
      /// </summary>
      /// <returns><see cref="getDelaunator(IPoint[])"/>이후 <see cref="Delaunator"/>에서 사용되는
      /// <paramref name="idx"/>번째 <see cref="IPoint"/>의 <b>최초</b> 값</returns>
      public IPoint getOriginalPoint(int idx) {
         if (originalPointList == null) {
#if INDEV
            Debug.LogError($"Points list is not initialized.");
#endif
            return default;
         }
         if (idx < 0 || idx > originalPointList.Count) {
#if INDEV
            Debug.LogError($"Invalid Point Index: {idx}");
#endif
            return default;
         }
         return originalPointList[idx];
      }

      #endregion Point Hashing / Initialize

      /// <summary>
      /// 삼각형 edge 정보를 사용하려면 이 함수를 사용
      /// </summary>
      public IEnumerable<EdgeInfo> willUseTriEdge() {
         return delaunator.GetEdges().Select(e => new EdgeInfo(e.Index, getPointIdx(e.Q), getPointIdx(e.P)));
      }

      /// <summary>
      /// 삼각형 Face 정보를 사용하려면 이 함수를 사용
      /// </summary>
      public IEnumerable<TriangleInfo> willUseTriangle() {
         var triangles = delaunator.GetTriangles();
         return delaunator.GetTriangles().Select(t => new TriangleInfo(t.Index, t.Points, getPointIdx));
      }
   }
}