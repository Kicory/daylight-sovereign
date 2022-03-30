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
      private SerializedProperty compos;

      private float unit => cellSize.floatValue;
      private const float pad = 100f;
      private readonly Vector2Int yCorrectOne = new Vector2Int(1, -1);
      private readonly Color gridCol = new Color(1f, 1f, 1f, 0.1f);
      private readonly Color highlightCol = new Color(1f, 1f, 1f, 0.1f);
      private readonly Color ghostCol = new Color(0, 1, 1, 0.3f);
      private float scrollSafeWidth => EditorGUIUtility.currentViewWidth - 40;

      private Vector2 scrollPos { get; set; } = Vector2.zero;
      private Vector2 originPos { get; set; } = Vector2.zero;
      private Vector2Int boardMax => boardRect?.rectIntValue.max ?? Vector2Int.zero;
      private Vector2Int boardMin => boardRect?.rectIntValue.min ?? Vector2Int.zero;
      private Vector2Int boardSize => boardRect?.rectIntValue.size ?? Vector2Int.zero;

      private CompoInfo[,] cachedBoardMap = null;
      private bool boardMapDirty = false;
      private PointerTarget pointerTarget { get; set; } = PointerTarget.None;
      private PointerStatus pointerStatus { get; set; } = PointerStatus.None;
      private int? currentHotCompo { get; set; } = null;
      private Rect? currentHoveringRect {
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
      
      /// <remarks> 위가 y+이고 min이 음수일 수 있는 좌표계 기준 </remarks>
      private Vector2Int? currentMouseCell { get; set; } = Vector2Int.zero;
      /// <summary> 직전에 pointer down 이벤트가 있었던 cell 위치 </summary>
      /// <remarks> 위가 y+이고 min이 음수일 수 있는 좌표계 기준 </remarks>
      private Vector2Int? prevDownCell { get; set; } = Vector2Int.zero;
      /// <remarks> 위가 y+이고 min이 음수일 수 있는 좌표계 기준 </remarks>
      private RectInt? ghostBoxCells = null;

      public override void OnInspectorGUI() {
         serializedObject.Update();

         basicProperties();

         float gridSizeX = Mathf.Max(scrollSafeWidth, boardSize.x * unit + pad);
         float gridSizeY = boardSize.y * unit + pad;
         scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false, GUILayout.Width(scrollSafeWidth + 15), GUILayout.MaxHeight(Mathf.Min(585, gridSizeY) + 15));

         drawBoardGrid(new Vector2(gridSizeX, gridSizeY));

         drawCompos();

         drawGhostBox();

         EditorGUILayout.EndScrollView();
         // 스크롤 뷰 밖에서는 좌표계가 다르므로 grid board에 그리는 코드 쓰지 말 것!

         validateMouseCell();

         handlePointerEvent();

         EditorGUI.LabelField(GUILayoutUtility.GetRect(scrollSafeWidth, EditorGUIUtility.singleLineHeight), currentMouseCell?.ToString() ?? "XXX");
         EditorGUI.LabelField(GUILayoutUtility.GetRect(scrollSafeWidth, EditorGUIUtility.singleLineHeight), $"{pointerTarget}, {pointerStatus}");

         editCompo();

         serializedObject.ApplyModifiedProperties();

         if (boardMapDirty) {
            reCalcBoardMap();
            boardMapDirty = false;
         }
      }

      private void handlePointerEvent() {

         PointerTarget prevPointerTarget;
         PointerStatus prevPointerStatus;
         Vector2Int cellCoordFromZero = Vector2Int.zero;
         System.Type compoType;
         bool clickPerfomed = false;

STORE:
         prevPointerTarget = pointerTarget;
         prevPointerStatus = pointerStatus;
CHECK_CURRENT:
         if (!currentMouseCell.HasValue) {
            pointerTarget = PointerTarget.None;
            pointerStatus = PointerStatus.None;
            goto DO_WORK;
         }
         
         cellCoordFromZero = currentMouseCell.Value - boardMin;

         if (Event.current.isMouse) {
            switch (Event.current.type) {
               case EventType.MouseUp:
                  pointerStatus = PointerStatus.Hover;
                  prevDownCell = null;
                  break;
               case EventType.MouseDown:
                  pointerStatus = PointerStatus.Down;
                  prevDownCell = currentMouseCell.Value;
                  break;
               case EventType.MouseDrag:
                  if (prevPointerStatus == PointerStatus.Down || prevPointerStatus == PointerStatus.Drag) {
                     pointerStatus = PointerStatus.Drag;
                  }
                  else {
                     //바깥에서 누른 채로 들어왔을 경우
                     pointerStatus = PointerStatus.Hover;
                  }
                  break;
               case EventType.ContextClick:
               default:
                  break;
            }
         }
         else if (pointerStatus == PointerStatus.None) {
            //보드 안에 들어와 있고 (currentMouseCell이 null이 아님), 아무런 마우스 이벤트도 일어나지 않았다면 Hover
            pointerStatus = PointerStatus.Hover;
         }

         compoType = cachedBoardMap[cellCoordFromZero.x, cellCoordFromZero.y]?.type ?? null;

         if (compoType == null) {
            pointerTarget = PointerTarget.Empty;
         }
         else if (compoType == typeof(Axis)) {
            pointerTarget = PointerTarget.Axis;
         }
         else if (compoType == typeof(Wire)) {
            pointerTarget = PointerTarget.Empty;
         }

DO_WORK:
         if (prevPointerStatus == PointerStatus.Down && pointerStatus == PointerStatus.Hover) {
            clickPerfomed = true;
         }

         if (clickPerfomed) {
            if (pointerTarget == PointerTarget.Axis) {
               currentHotCompo = cachedBoardMap[cellCoordFromZero.x, cellCoordFromZero.y].indexInBoardList;
            }
            else if (pointerTarget == PointerTarget.Empty) {
               currentHotCompo = null;
               ghostBoxCells = null;
            }
         }
         
         //드래그 하다가 중간에 그리드 밖으로 나감
         if (prevPointerStatus == PointerStatus.Drag && pointerStatus == PointerStatus.None) {
            ghostBoxCells = null;
         }

         //계속 드래깅 상황
         if (pointerStatus == PointerStatus.Drag && prevDownCell.HasValue) {
            //Compo를 시작점으로 하는 Axis 만들기는 불가능 (어차피 겹쳐서 취소됨)
            if (cachedBoardMap[prevDownCell.Value.x, prevDownCell.Value.y] == null) {
               var boxMin = Vector2Int.Min(prevDownCell.Value, currentMouseCell.Value);
               var boxMax = Vector2Int.Max(prevDownCell.Value, currentMouseCell.Value) + Vector2Int.one;
               currentHotCompo = null;
               ghostBoxCells = new RectInt(boxMin, boxMax - boxMin);
            }
         }

         if (prevPointerStatus == PointerStatus.Drag && pointerStatus == PointerStatus.Hover) {
            //Axis 만들기
         }

         return;
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

      private void drawCompos() {
         if (Event.current.type != EventType.Repaint) {
            return;
         }
      }

      private void drawGhostBox() {
         if (Event.current.type != EventType.Repaint) {
            return;
         }
         if (currentHotCompo.HasValue) {
            //선택된 compo가 있다면 최우선으로 그리기
            ghostBoxCells = compos.GetArrayElementAtIndex(currentHotCompo.Value).FindPropertyRelative("_rect").rectIntValue;
         }
         if (!ghostBoxCells.HasValue) {
            return;
         }
         var boxRect = cellRect2Rect(ghostBoxCells.Value);

         EditorGUI.DrawRect(new Rect(boxRect) { height = 3, y = boxRect.y - 1 }, ghostCol);
         EditorGUI.DrawRect(new Rect(boxRect) { height = 3, y = boxRect.yMax - 1}, ghostCol);
         EditorGUI.DrawRect(new Rect(boxRect) { width = 3, x = boxRect.x - 1}, ghostCol);
         EditorGUI.DrawRect(new Rect(boxRect) { width = 3, x = boxRect.xMax - 1}, ghostCol);
      }

      private Rect cellRect2Rect(RectInt cellRect) {
         return new Rect(originPos + (Vector2)cellRect.min * unit * yCorrectOne, (Vector2)cellRect.size * unit * yCorrectOne);
      }

      private void reCalcBoardMap() {
         cachedBoardMap = (serializedObject.targetObject as Board).forceRemakeCachedBoardMap();
         ghostBoxCells = null;
      }

      private void validateMouseCell() {
         if (Event.current.type == EventType.Repaint) {
            //GetLastRect는 스크롤뷰의 rect를 반환함
            currentMouseCell = GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) ? currentMouseCell : null;
         }
      }

      private void editCompo() {
         if (currentHotCompo == null) {
            return;
         }
         
         var hot = currentHotCompo.Value;
         if (compos.arraySize <= hot || hot < 0) {
            Debug.LogWarning($"{hot}-th {nameof(Compo)}는 없음.");
            return;
         }
         EditorGUILayout.Space(10);
         EditorGUI.DrawRect(GUILayoutUtility.GetRect(scrollSafeWidth, 3), Color.white);
         EditorGUILayout.Space(10);
         SerializedProperty hotCompoProperty = compos.GetArrayElementAtIndex(hot);

         var compoType = hotCompoProperty.managedReferenceFullTypename.Split(' ')[1];
         if (compoType == typeof(Axis).FullName) {
            EditorGUILayout.PropertyField(hotCompoProperty.FindPropertyRelative("_afforder").FindPropertyRelative("_code"));
         }
         if (compoType == typeof(Wire).FullName) {
            //
         }
         EditorGUILayout.PropertyField(compos);
      }

      public void OnEnable() {
         boardRect = serializedObject.FindProperty("_boardRect");
         cellSize = serializedObject.FindProperty("_cellSize");
         compos = serializedObject.FindProperty("_compos");
         currentHotCompo = 0;
         reCalcBoardMap();
      }

      public override bool RequiresConstantRepaint() => true;
   }
}