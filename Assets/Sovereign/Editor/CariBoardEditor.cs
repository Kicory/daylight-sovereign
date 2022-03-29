using NoFS.DayLight.CariBoard;
using UnityEditor;
using UnityEngine;

namespace NoFS.DayLight.CariBoardEditor {

   [CustomEditor(typeof(Board))]
   public class CariBoardEditor : Editor {
      private enum PointerTarget {
         None,
         Axis,
         Empty
      }
      private enum PointerStatus {
         None,
         Hover,
         Down,
         Drag
      }

      private SerializedProperty boardRect;
      private SerializedProperty cellSize;

      private float unit => cellSize.floatValue;
      private const float pad = 100f;
      private readonly Vector2Int yCorrectOne = new Vector2Int(1, -1);
      private readonly Color gridCol = new Color(1f, 1f, 1f, 0.1f);
      private readonly Color highlightCol = new Color(1f, 1f, 1f, 0.1f);
      private float scrollSafeWidth => EditorGUIUtility.currentViewWidth - 40;

      private Vector2 scrollPos { get; set; } = Vector2.zero;
      private Vector2 originPos { get; set; } = Vector2.zero;
      private Vector2Int boardMax => boardRect?.rectIntValue.max ?? Vector2Int.zero;
      private Vector2Int boardMin => boardRect?.rectIntValue.min ?? Vector2Int.zero;
      private Vector2Int boardSize => boardRect?.rectIntValue.size ?? Vector2Int.zero;

      private Compo[,] compoBoardMap = null;
      private PointerTarget pointerTarget { get; set; } = PointerTarget.None;
      private PointerStatus pointerStatus { get; set; } = PointerStatus.None;
      
      public Vector2Int? currentMouseCell { get; private set; } = Vector2Int.zero;
      public Rect? currentHoveringCell {
         get {
            if (currentMouseCell == null) {
               return null;
            }
            else {
               Vector2 curCell = currentMouseCell.Value;
               return new Rect(originPos + curCell * unit * yCorrectOne, (Vector2)yCorrectOne * unit);
            }
         }
      }

      public override void OnInspectorGUI() {
         serializedObject.Update();

         basicProperties();

         float gridSizeY = boardSize.y * unit + pad;
         float gridSizeX = Mathf.Max(scrollSafeWidth, boardSize.x * unit + pad);

         scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false, GUILayout.Width(scrollSafeWidth + 15), GUILayout.MaxHeight(Mathf.Min(585, gridSizeY) + 15));

         drawBoardGrid(new Vector2(gridSizeX, gridSizeY));

         EditorGUILayout.EndScrollView();
         // 스크롤 뷰 밖에서는 좌표계가 다르므로 grid board에 그리는 코드 쓰지 말 것!

         EditorGUI.LabelField(GUILayoutUtility.GetRect(scrollSafeWidth, EditorGUIUtility.singleLineHeight), currentMouseCell?.ToString() ?? "XXX");

         serializedObject.ApplyModifiedProperties();
      }

      private void basicProperties() {
         EditorGUI.BeginChangeCheck();
         EditorGUILayout.PropertyField(boardRect);
         if (EditorGUI.EndChangeCheck()) {
            (serializedObject.targetObject as Board).getBoardMap();
         }
         EditorGUILayout.PropertyField(cellSize);
      }

      private void drawBoardGrid(Vector2 size) {
         var boardCenter = (boardMax + (Vector2)boardMin) / 2f;

         Rect gridRect = GUILayoutUtility.GetRect(size.x, size.y);
         originPos = gridRect.center - (boardCenter * yCorrectOne * unit);

         //그냥 배경용 박스
         GUI.Box(gridRect, "");
         //오리진 표시
         EditorGUI.DrawRect(new Rect(originPos, (Vector2)yCorrectOne * unit / 3), Color.cyan);

         Vector2Int? rawMouseCell = Vector2Int.FloorToInt((Event.current.mousePosition - originPos) / unit * yCorrectOne);
         currentMouseCell = boardRect.rectIntValue.Contains(rawMouseCell.Value) ? rawMouseCell : null;

         for (int yy = boardMin.y; yy <= boardMax.y; yy++) {
            //인스펙터 좌표계는 아래 = y+ 임...
            var horLineRect = new Rect(originPos + new Vector2(boardMin.x, -yy) * unit, new Vector2(boardSize.x * unit, 1));
            EditorGUI.DrawRect(horLineRect, this.gridCol);
            if (Event.current.type != EventType.Layout && currentMouseCell.HasValue && yy == currentMouseCell.Value.y) {
               horLineRect.height = -unit + 1;
               EditorGUI.DrawRect(horLineRect, highlightCol);
            }
         }
         for (int xx = boardMin.x; xx <= boardMax.x; xx++) {
            var verLineRect = new Rect(originPos + new Vector2(xx, -boardMax.y) * unit, new Vector2(1, boardSize.y * unit));
            EditorGUI.DrawRect(verLineRect, this.gridCol);
            if (Event.current.type != EventType.Layout && currentMouseCell.HasValue && xx == currentMouseCell.Value.x) {
               verLineRect.x += 1;
               verLineRect.width = unit - 1;
               EditorGUI.DrawRect(verLineRect, highlightCol);
            }
         }
      }

      private Rect cellRect2Rect(RectInt cellRect) {
         return new Rect(originPos + (Vector2)cellRect.min * unit * yCorrectOne, (Vector2)cellRect.size * unit * yCorrectOne);
      }
      
      

      public void OnEnable() {
         boardRect = serializedObject.FindProperty("_boardRect");
         cellSize = serializedObject.FindProperty("_cellSize");
      }

      public override bool RequiresConstantRepaint() => true;
   }
}