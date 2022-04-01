using System;
using System.Linq;
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

      private Vector2 scrollPos { get; set; } = Vector2.zero;
      private GUIStyle style = null;
      private string creatingCompoTypeName => creatingCompoType?.enumNames[creatingCompoType.enumValueIndex] ?? null;

      private SerializedProperty boardRect;
      private SerializedProperty cellSize;
      private SerializedProperty compos;
      private SerializedProperty creatingCompoType;

      private const float pad = 100f;
      private float unit => cellSize.floatValue;
      private readonly Vector2Int yCorrectOne = new Vector2Int(1, -1);
      private readonly Color gridCol = new Color(1f, 1f, 1f, 0.1f);
      private readonly Color highlightCol = new Color(1f, 1f, 1f, 0.1f);
      private readonly Color ghostCol = new Color(0.8f, 0, 0, 1f);
      private readonly Color activeAxisCol = new Color(1, 1, 1, 0.6f);
      private readonly Color passiveAxisCol = new Color(0, 0, 0, 1f);
      private float scrollSafeWidth => EditorGUIUtility.currentViewWidth - 40;

      private Vector2 originPos = Vector2.zero;
      private Vector2Int boardMax => boardRect?.rectIntValue.max ?? Vector2Int.zero;
      private Vector2Int boardMin => boardRect?.rectIntValue.min ?? Vector2Int.zero;
      private Vector2Int boardSize => boardRect?.rectIntValue.size ?? Vector2Int.zero;

      private static CompoInfo?[,] cachedBoardMap = null;
      private bool boardMapDirty = false;

      private PointerTarget pointerTarget = PointerTarget.None;
      private PointerStatus pointerStatus = PointerStatus.None;

      private CompoInfo? _curHotCompoInfo = null;
      private CompoInfo? curHotCompoInfo {
         get => _curHotCompoInfo;
         set {
            curHotCompoProperty = value == null ? null : compos.GetArrayElementAtIndex(value.Value.indexInBoardList);
            _curHotCompoInfo = value;
         }
      }
      private int? curHotCompoIdx => curHotCompoInfo?.indexInBoardList ?? null;
      private string curHotCompoTypeName => curHotCompoInfo?.typeName ?? null;

      private SerializedProperty curHotCompoProperty = null;

      /// <remarks> 위가 y+이고 min이 음수일 수 있는 좌표계 기준 </remarks>
      private Vector2Int? curMouseCell { get; set; } = Vector2Int.zero;

      /// <summary> 직전에 pointer down 이벤트가 있었던 cell 위치 </summary>
      /// <remarks> 위가 y+이고 min이 음수일 수 있는 좌표계 기준 </remarks>
      private Vector2Int? prevDownCell { get; set; } = Vector2Int.zero;

      /// <remarks> 위가 y+이고 min이 음수일 수 있는 좌표계 기준 </remarks>
      private RectInt? ghostBoxRect = null;

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

         handleKeyboardEvent();

         showPointerInfo();

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
         string compoType;
         bool clickPerfomed = false;

         #region SAVE

         prevPointerTarget = pointerTarget;
         prevPointerStatus = pointerStatus;

         #endregion SAVE

         #region Checking current

         if (!curMouseCell.HasValue) {
            pointerTarget = PointerTarget.None;
            pointerStatus = PointerStatus.None;
            goto DO_WORK;
         }

         //마우스가 그리드 내로 들어온 상태라면, 다른 콘트롤에 포커스가 가 있을 필요가 없음
         GUI.FocusControl(null);

         cellCoordFromZero = curMouseCell.Value - boardMin;
         compoType = cachedBoardMap[cellCoordFromZero.x, cellCoordFromZero.y]?.typeName ?? null;

         #region TODO
         if (compoType == null) {
            pointerTarget = PointerTarget.Empty;
         }
         else if (compoType == nameof(PassiveAxis) || compoType == nameof(ActiveAxis)) {
            pointerTarget = PointerTarget.Axis;
         }
         else if (compoType == nameof(Wire)) {
            pointerTarget = PointerTarget.Empty;
         } 
         #endregion

         if (Event.current.isMouse) {
            switch (Event.current.type) {
               case EventType.MouseUp:
                  pointerStatus = PointerStatus.Hover;
                  prevDownCell = null;
                  break;

               case EventType.MouseDown:
                  pointerStatus = PointerStatus.Down;
                  prevDownCell = curMouseCell.Value;
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

#endregion Checking current

DO_WORK:
         if (prevPointerStatus == PointerStatus.Down && pointerStatus == PointerStatus.Hover) {
            clickPerfomed = true;
         }

         if (clickPerfomed) {
            if (pointerTarget == PointerTarget.Axis) {
               curHotCompoInfo = cachedBoardMap[cellCoordFromZero.x, cellCoordFromZero.y];
            }
            else if (pointerTarget == PointerTarget.Empty) {
               curHotCompoInfo = null;
               ghostBoxRect = null;
            }
            return;
         }

         //드래그 하다가 중간에 그리드 밖으로 나감
         if (prevPointerStatus == PointerStatus.Drag && pointerStatus == PointerStatus.None) {
            ghostBoxRect = null;
            return;
         }

         //계속 드래깅 상황
         if (pointerStatus == PointerStatus.Drag && prevDownCell.HasValue) {
            //Compo를 시작점으로 하는 Axis 만들기는 불가능 (어차피 겹쳐서 취소됨)
            var prevDownCellCoordFromZero = prevDownCell.Value - boardMin;
            if (cachedBoardMap[prevDownCellCoordFromZero.x, prevDownCellCoordFromZero.y] == null) {
               var boxMin = Vector2Int.Min(prevDownCell.Value, curMouseCell.Value);
               var boxMax = Vector2Int.Max(prevDownCell.Value, curMouseCell.Value) + Vector2Int.one;
               curHotCompoInfo = null;
               ghostBoxRect = new RectInt(boxMin, boxMax - boxMin);
            }
         }

         //Axis 창조 상황
         if (prevPointerStatus == PointerStatus.Drag && pointerStatus == PointerStatus.Hover && ghostBoxRect.HasValue) {
            var nextIdx = compos.arraySize;
            RectInt newCompoRect = ghostBoxRect.Value;

            if (isValidRectForCompoIdx(newCompoRect, nextIdx)) {
               string creatingTypeName = creatingCompoTypeName;
               object creatingRef = null;
               if (creatingTypeName == nameof(PassiveAxis)) {
                  creatingRef = new PassiveAxis(newCompoRect, new Afforder("Passive"));
               }
               else if (creatingTypeName == nameof(ActiveAxis)) {
                  creatingRef = new ActiveAxis(newCompoRect, new Afforder("Active"));
               }
               else if (creatingTypeName == nameof(Wire)) {

               }
               else {
                  //
               }
               if (creatingRef != null) {
                  compos.InsertArrayElementAtIndex(nextIdx);
                  compos.GetArrayElementAtIndex(nextIdx).managedReferenceValue = creatingRef;
                  curHotCompoInfo = new CompoInfo(nextIdx, creatingTypeName);
                  boardMapDirty = true; 
               }
            }
         }
      }

      private void handleKeyboardEvent() {
         if (!Event.current.isKey || curHotCompoProperty == null || Event.current.type != EventType.KeyDown) {
            return;
         }
         var keyCode = Event.current.keyCode;

         if (keyCode == KeyCode.Delete) {
            compos.DeleteArrayElementAtIndex(curHotCompoIdx.Value);
            curHotCompoInfo = null;
            Event.current.Use();
            boardMapDirty = true;
            //reCalcBoardMap(); // 이후에 cachedBoardMap을 사용하는 코드가 있어서 즉시 업데이트를 해 줘야 함
            return;
         }

         var curCompoRectProperty = curHotCompoProperty.FindPropertyRelative("_rect");
         var curCompoRect = curCompoRectProperty.rectIntValue;
         bool control = Event.current.control;

         switch (keyCode) {
            case KeyCode.RightArrow:
               if (control)
                  curCompoRect.width = curCompoRect.width + 1;
               else
                  curCompoRect.x = curCompoRect.x + 1;
               break;

            case KeyCode.LeftArrow:
               if (control)
                  curCompoRect.width = curCompoRect.width - 1;
               else
                  curCompoRect.x = curCompoRect.x - 1;
               break;

            case KeyCode.UpArrow:
               if (control)
                  curCompoRect.height = curCompoRect.height + 1;
               else
                  curCompoRect.y = curCompoRect.y + 1;
               break;

            case KeyCode.DownArrow:
               if (control)
                  curCompoRect.height = curCompoRect.height - 1;
               else
                  curCompoRect.y = curCompoRect.y - 1;
               break;

            default:
               return;
         }

         if (isValidRectForCompoIdx(curCompoRect, curHotCompoIdx)) {
            curCompoRectProperty.rectIntValue = curCompoRect;
            boardMapDirty = true;
         }
         Event.current.Use();
      }

      private void basicProperties() {
         EditorGUIUtility.wideMode = true;
         EditorGUILayout.BeginHorizontal();
         EditorGUI.BeginChangeCheck();
         EditorGUIUtility.labelWidth = 80;
         EditorGUILayout.PropertyField(boardRect);
         if (EditorGUI.EndChangeCheck()) {
            boardMapDirty = true;
         }
         EditorGUILayout.PropertyField(cellSize);
         EditorGUILayout.EndHorizontal();

         EditorGUILayout.BeginHorizontal();
         EditorGUIUtility.labelWidth = 150;
         EditorGUILayout.PropertyField(creatingCompoType);
         GUILayout.Label(" ", GUILayout.Width(scrollSafeWidth / 4));
         if (GUILayout.Button("Refresh", GUILayout.Width(scrollSafeWidth / 4))) {
            boardMapDirty = true;
         }
         EditorGUILayout.EndHorizontal();
         EditorGUIUtility.labelWidth = 0;
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
            if (curMouseCell.HasValue && yy == curMouseCell.Value.y) {
               horLineRect.height = -unit + 1;
               EditorGUI.DrawRect(horLineRect, highlightCol);
            }
         }
         for (int xx = boardMin.x; xx <= boardMax.x; xx++) {
            var verLineRect = new Rect(originPos + new Vector2(xx, -boardMax.y) * unit, new Vector2(1, boardSize.y * unit));
            EditorGUI.DrawRect(verLineRect, this.gridCol);
            if (curMouseCell.HasValue && xx == curMouseCell.Value.x) {
               verLineRect.x += 1;
               verLineRect.width = unit - 1;
               EditorGUI.DrawRect(verLineRect, highlightCol);
            }
         }

         Vector2Int? rawMouseCell = Vector2Int.FloorToInt((Event.current.mousePosition - originPos) / unit * yCorrectOne);
         // Layout 이벤트에서는 mouseposition이 이상함, repaint에서만 currentMouseCell 계산하도록 함.
         curMouseCell = boardRect.rectIntValue.Contains(rawMouseCell.Value) ? rawMouseCell : null;
      }

      private void drawCompos() {
         if (Event.current.type != EventType.Repaint) {
            return;
         }

         var size = boardSize;
         Color drawColor;
         for (int xx = 0; xx < size.x; xx++) {
            for (int yy = 0; yy < size.y; yy++) {
               CompoInfo? compoInfo = cachedBoardMap[xx, yy];
               if (compoInfo != null) {
                  drawColor = colorPicker(compoInfo.Value);
                  EditorGUI.DrawRect(cellRect2Rect(new RectInt(xx + boardMin.x, yy + boardMin.y, 1, 1)), drawColor);
               }
            }
         }

         Color colorPicker(CompoInfo compoInfo) {
            Color drawColor;
            if (compoInfo.typeName == nameof(PassiveAxis)) {
               drawColor = passiveAxisCol;
            }
            else if (compoInfo.typeName == nameof(ActiveAxis)) {
               drawColor = activeAxisCol;
            }
            else {
               drawColor = Color.clear;
            }
            SerializedProperty hotCompoProperty = compos.GetArrayElementAtIndex(compoInfo.indexInBoardList);
            string afforderCode = hotCompoProperty.FindPropertyRelative("_afforder")?.FindPropertyRelative("_code")?.stringValue ?? null;
            if (afforderCode != null && afforderCode.StartsWith("PRIME")) {
               drawColor = Color.yellow;
            }

            return drawColor;
         }
      }

      private void drawGhostBox() {
         if (Event.current.type != EventType.Repaint) {
            return;
         }
         if (curHotCompoInfo != null) {
            //선택된 compo가 있다면 최우선으로 그리기
            ghostBoxRect = curHotCompoProperty.FindPropertyRelative("_rect").rectIntValue;
         }
         if (!ghostBoxRect.HasValue) {
            return;
         }
         var boxRect = cellRect2Rect(ghostBoxRect.Value);

         EditorGUI.DrawRect(new Rect(boxRect) { height = 3, y = boxRect.y - 1 }, ghostCol);
         EditorGUI.DrawRect(new Rect(boxRect) { height = 3, y = boxRect.yMax - 1 }, ghostCol);
         EditorGUI.DrawRect(new Rect(boxRect) { width = 3, x = boxRect.x - 1 }, ghostCol);
         EditorGUI.DrawRect(new Rect(boxRect) { width = 3, x = boxRect.xMax - 1 }, ghostCol);
      }

      private Rect cellRect2Rect(RectInt cellRect) {
         return new Rect(originPos + (Vector2)cellRect.min * unit * yCorrectOne, (Vector2)cellRect.size * unit * yCorrectOne);
      }

      private void reCalcBoardMap() {
         cachedBoardMap = forceRemakeCachedBoardMap();
         ghostBoxRect = null;
         prevDownCell = null;
      }

      private void validateMouseCell() {
         if (Event.current.type == EventType.Repaint) {
            //GetLastRect는 스크롤뷰의 rect를 반환함
            curMouseCell = GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) ? curMouseCell : null;
         }
      }

      private void showPointerInfo() {
         EditorGUI.LabelField(GUILayoutUtility.GetRect(scrollSafeWidth, EditorGUIUtility.singleLineHeight), (curMouseCell?.ToString() ?? "null //") + $" {pointerTarget}, {pointerStatus}");

         if (boardMapDirty) {
            // boardMap의 정보가 outdated라서 없는 Axis를 참조하려고 할 수 있음.
            return;
         }

         if (style == null) {
            style = new GUIStyle(GUI.skin.box);
         }

         string curHoveringAfCode = null;
         if (curMouseCell.HasValue && cachedBoardMap != null) {
            Vector2Int cellArrayCoord = curMouseCell.Value - boardMin;
            CompoInfo? hoveringCompoInfo;
            if ((hoveringCompoInfo = cachedBoardMap[cellArrayCoord.x, cellArrayCoord.y]) != null) {
               var curHoveringCompo = compos.GetArrayElementAtIndex(hoveringCompoInfo.Value.indexInBoardList);
               curHoveringAfCode = curHoveringCompo.FindPropertyRelative("_afforder").FindPropertyRelative("_code").stringValue;
            }
         }

         if (curHoveringAfCode != null) {
            float minWid, maxWid;

            style.CalcMinMaxWidth(new GUIContent(curHoveringAfCode), out minWid, out maxWid);
            var labelBack = new Rect(Event.current.mousePosition - Vector2.up * EditorGUIUtility.singleLineHeight,
                           new Vector2(maxWid + 10, EditorGUIUtility.singleLineHeight));
            EditorGUI.DrawRect(labelBack, Color.black);
            GUI.Box(labelBack, curHoveringAfCode, style);
         }
      }

      private void editCompo() {
         if (curHotCompoInfo == null) {
            return;
         }

         EditorGUILayout.Space(10);
         EditorGUI.DrawRect(GUILayoutUtility.GetRect(scrollSafeWidth, 3), highlightCol);
         EditorGUILayout.Space(10);

         var compoType = curHotCompoTypeName;
         if (compoType == nameof(PassiveAxis) || compoType == nameof(ActiveAxis)) {
            EditorGUILayout.PropertyField(curHotCompoProperty.FindPropertyRelative("_afforder").FindPropertyRelative("_code"));
         }
         if (compoType == nameof(Wire)) {
            //
         }
      }

      private CompoInfo?[,] forceRemakeCachedBoardMap() {
         var compoIndexMap = new CompoInfo?[boardSize.x, boardSize.y];
         for (int idx = 0; idx < compos.arraySize; idx++) {
            SerializedProperty compo = compos.GetArrayElementAtIndex(idx);
            var cRect = compo.FindPropertyRelative("_rect").rectIntValue;
            //minx, miny가 0인 상태로 만들어주기 (배열)
            cRect.x -= boardMin.x;
            cRect.y -= boardMin.y;

            for (int xx = cRect.min.x; xx < cRect.max.x; xx++) {
               for (int yy = cRect.min.y; yy < cRect.max.y; yy++) {
                  try {
                     if (compoIndexMap[xx, yy] != null) {
                        Debug.LogWarning($"({xx}, {yy})에서 겹침 있는 것 같음...");
                     }
                     compoIndexMap[xx, yy] = new CompoInfo() {
                        indexInBoardList = idx,
                        typeName = compo.managedReferenceFullTypename.Split('.').Last()
                     };
                  }
                  catch (IndexOutOfRangeException) {
                     Debug.LogError($"Board를 벗어난 {nameof(Compo)}가 있음: ({xx}, {yy})");
                  }
               }
            }
         }
         return compoIndexMap;
      }

      private bool isValidRectForCompoIdx(RectInt targetRect, int? compoIdx) {
         if (targetRect.width <= 0 || targetRect.height <= 0) {
            return false;
         }

         RectInt boundingRect = boardRect.rectIntValue;

         if (!boundingRect.Contains(targetRect.min) || !boundingRect.Contains(targetRect.max - Vector2Int.one)) {
            return false;
         }

         var allPointEnumer = targetRect.allPositionsWithin;
         while (allPointEnumer.MoveNext()) {
            var point = allPointEnumer.Current;
            CompoInfo? compoInfo = cachedBoardMap[point.x - boardMin.x, point.y - boardMin.y];
            if (compoInfo != null && compoInfo.Value.indexInBoardList != compoIdx.Value) {
               return false;
            }
         }
         return true;
      }

      private struct CompoInfo {

         public CompoInfo(int _indexInBoardList, string _typeName) {
            indexInBoardList = _indexInBoardList;
            typeName = _typeName;
         }

         public int indexInBoardList;
         public string typeName;
      }

      public void OnEnable() {
         boardRect = serializedObject.FindProperty("_boardRect");
         cellSize = serializedObject.FindProperty("_cellSize");
         compos = serializedObject.FindProperty("_compos");
         creatingCompoType = serializedObject.FindProperty("_creatingCompoType");
         curHotCompoInfo = null;
         style = null;
         reCalcBoardMap();
      }

      public override bool RequiresConstantRepaint() => true;
   }
}