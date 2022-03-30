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

      private Compo[,] cachedBoardMap = null;
      private bool boardMapDirty = false;
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

         float gridSizeX = Mathf.Max(scrollSafeWidth, boardSize.x * unit + pad);
         float gridSizeY = boardSize.y * unit + pad;
         scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false, GUILayout.Width(scrollSafeWidth + 15), GUILayout.MaxHeight(Mathf.Min(585, gridSizeY) + 15));

         drawBoardGrid(new Vector2(gridSizeX, gridSizeY));

         EditorGUILayout.EndScrollView();
         // 스크롤 뷰 밖에서는 좌표계가 다르므로 grid board에 그리는 코드 쓰지 말 것!

         validateMouseCell();

         handlePointerEvent();

         EditorGUI.LabelField(GUILayoutUtility.GetRect(scrollSafeWidth, EditorGUIUtility.singleLineHeight), currentMouseCell?.ToString() ?? "XXX");
         EditorGUI.LabelField(GUILayoutUtility.GetRect(scrollSafeWidth, EditorGUIUtility.singleLineHeight), $"{pointerTarget}, {pointerStatus}");

         serializedObject.ApplyModifiedProperties();

         if (boardMapDirty) {
            reCalcBoardMap();
            boardMapDirty = false;
         }
      }

      private void handlePointerEvent() {

         PointerTarget prevPointerTarget;
         PointerStatus prevPointerStatus;
         Vector2Int cell;
///
STORE:
         prevPointerTarget = pointerTarget;
         prevPointerStatus = pointerStatus;
///
CHECK_CURRENT:
         if (!currentMouseCell.HasValue) {
            pointerTarget = PointerTarget.None;
            pointerStatus = PointerStatus.None;
            goto DO_WORK;
         }
         
         cell = currentMouseCell.Value - boardMin;

         if (Event.current.isMouse) {
            switch (Event.current.type) {
               case EventType.MouseUp:
                  pointerStatus = PointerStatus.Hover;
                  break;
               case EventType.MouseDown:
                  pointerStatus = PointerStatus.Down;
                  break;
               case EventType.MouseDrag:
                  if (prevPointerStatus == PointerStatus.Down || prevPointerStatus == PointerStatus.Drag) {
                     pointerStatus = PointerStatus.Drag;
                  }
                  else {
                     pointerStatus = PointerStatus.Hover;
                  }
                  break;
               case EventType.ContextClick:
               default:
                  //pointerStatus = PointerStatus.Hover;
                  break;
            }
         }

         switch (cachedBoardMap[cell.x, cell.y]) {
            case Axis axis:
               pointerTarget = PointerTarget.Axis;
               break;
            default:
               pointerTarget = PointerTarget.Empty;
               break;
         }

///
DO_WORK:
         return;
///
      }

      private void basicProperties() {
         EditorGUI.BeginChangeCheck();
         EditorGUILayout.PropertyField(boardRect);
         if (EditorGUI.EndChangeCheck()) {
            boardMapDirty = true;
         }
         EditorGUILayout.PropertyField(cellSize);
      }

      private void drawBoardGrid(Vector2 size) {

         var boardCenter = (boardMax + (Vector2)boardMin) / 2f;

         Rect gridRect = GUILayoutUtility.GetRect(size.x, size.y);
         originPos = gridRect.center - (boardCenter * yCorrectOne * unit);

         //그냥 배경용 박스
         GUI.Box(gridRect, "");
         if (Event.current.type != EventType.Repaint) {
            //이 이후로는 어차피 Layout 등이 관여할 필요가 없음 (그리기만 하면 됨)
            return;
         }
         //오리진 표시
         EditorGUI.DrawRect(new Rect(originPos, (Vector2)yCorrectOne * unit / 3), Color.cyan);

         for (int yy = boardMin.y; yy <= boardMax.y; yy++) {
            //인스펙터 좌표계는 아래 = y+ 임...
            var horLineRect = new Rect(originPos + new Vector2(boardMin.x, -yy) * unit, new Vector2(boardSize.x * unit, 1));
            EditorGUI.DrawRect(horLineRect, this.gridCol);
            if (currentMouseCell.HasValue && yy == currentMouseCell.Value.y) {
               horLineRect.height = -unit + 1;
               EditorGUI.DrawRect(horLineRect, highlightCol);
            }
         }
         for (int xx = boardMin.x; xx <= boardMax.x; xx++) {
            var verLineRect = new Rect(originPos + new Vector2(xx, -boardMax.y) * unit, new Vector2(1, boardSize.y * unit));
            EditorGUI.DrawRect(verLineRect, this.gridCol);
            if (currentMouseCell.HasValue && xx == currentMouseCell.Value.x) {
               verLineRect.x += 1;
               verLineRect.width = unit - 1;
               EditorGUI.DrawRect(verLineRect, highlightCol);
            }
         }

         Vector2Int? rawMouseCell = Vector2Int.FloorToInt((Event.current.mousePosition - originPos) / unit * yCorrectOne);
         // Layout 이벤트에서는 mouseposition이 이상함, repaint에서만 currentMouseCell 계산하도록 함.
         currentMouseCell = boardRect.rectIntValue.Contains(rawMouseCell.Value) ? rawMouseCell : null;
      }

      private Rect cellRect2Rect(RectInt cellRect) {
         return new Rect(originPos + (Vector2)cellRect.min * unit * yCorrectOne, (Vector2)cellRect.size * unit * yCorrectOne);
      }

      private void reCalcBoardMap() => cachedBoardMap = (serializedObject.targetObject as Board).forceRemakeCachedBoardMap();

      private void validateMouseCell() {
         if (Event.current.type == EventType.Repaint) {
            //GetLastRect는 스크롤뷰의 rect를 반환함
            currentMouseCell = GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) ? currentMouseCell : null;
         }
      }

      public void OnEnable() {
         boardRect = serializedObject.FindProperty("_boardRect");
         cellSize = serializedObject.FindProperty("_cellSize");
         reCalcBoardMap();
      }

      public override bool RequiresConstantRepaint() => true;
   }
}