using System;
using System.Collections.Generic;
using System.Linq;
using NoFS.DayLight.CariBoard;
using UnityEditor;
using UnityEngine;

namespace NoFS.DayLight.CariBoardEditor {

   [CustomEditor(typeof(Board))]
   public class CariBoardEditor : Editor {

      private enum PointerTarget {
         None,
         Compo,
         Empty
      }

      private enum PointerStatus {
         None,
         Hover,
         Down,
         Drag
      }

      [Flags]
      private enum Dirtyness { None = 0, Grid = 1 << 0, Visual = 1 << 1, Full = Grid | Visual }

      private Vector2 scrollPos { get; set; } = Vector2.zero;
      private GUIStyle style = null;
      private string creatingCompoTypeName => creatingCompoType?.enumNames[creatingCompoType.enumValueIndex] ?? null;

      private SerializedProperty boardRect;
      private SerializedProperty cellSize;
      private SerializedProperty compos;
      private SerializedProperty creatingCompoType;

      private const float pad = 100f;
#if PACKAGE_INDEV
      private const string PathPrefix = "Assets/Sovereign";
#else
      private const string PathPrefix = "Packages/com.nofs.daylight.sovereign";
#endif

      public float unit => Mathf.Round(cellSize.floatValue / 2) * 2;
      private readonly Vector2Int yCorrectOne = new Vector2Int(1, -1);
      private readonly Color gridCol = new Color(1f, 1f, 1f, 0.1f);
      private readonly Color highlightCol = new Color(1f, 1f, 1f, 0.1f);
      private readonly Color ghostCol = new Color(0.8f, 0.1f, 0.1f, 1f);
      private float scrollSafeWidth => EditorGUIUtility.currentViewWidth - 40;

      private Vector2 originPos = Vector2.zero;
      private Vector2Int boardMax => boardRect?.rectIntValue.max ?? Vector2Int.zero;
      private Vector2Int boardMin => boardRect?.rectIntValue.min ?? Vector2Int.zero;
      private Vector2Int boardSize => boardRect?.rectIntValue.size ?? Vector2Int.zero;

      private static CompoInfo?[,] cachedBoardMap = null;
      private static List<DrawInfo> cachedDrawList = null;
      private static Dirtyness boardMapDirty = Dirtyness.None;

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
      //private string curHotCompoTypeName => curHotCompoInfo?.typeName ?? null;

      private SerializedProperty curHotCompoProperty = null;

      /// <remarks> 위가 y+이고 min이 음수일 수 있는 좌표계 기준 </remarks>
      private Vector2Int? curMouseCell { get; set; } = null;

      /// <summary> 직전에 pointer down 이벤트가 있었던 cell 위치 </summary>
      /// <remarks> 위가 y+이고 min이 음수일 수 있는 좌표계 기준 </remarks>
      private Vector2Int? prevDownCell { get; set; } = null;

      /// <remarks> 위가 y+이고 min이 음수일 수 있는 좌표계 기준 </remarks>
      private RectInt? ghostBoxRect = null;

      public override void OnInspectorGUI() {
         serializedObject.Update();
         EditorGUILayout.HelpBox("클릭하여 Axis 선택 / 드래그하여 Axis 생성 / Del 키로 선택된 Axis 제거 / " +
            "화살표 키로 선택된 Axis 이동 / Ctrl + 화살표 키로 선택된 Axis 크기 변경", MessageType.Info);

         basicProperties();

         float gridSizeX = Mathf.Max(scrollSafeWidth, boardSize.x * unit + pad);
         float gridSizeY = boardSize.y * unit + pad;
         scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false, GUILayout.Width(scrollSafeWidth + 15), GUILayout.MaxHeight(Mathf.Min(585, gridSizeY) + 15));

         drawBoardGrid(new Vector2(gridSizeX, gridSizeY));

         drawCompos();

         //Draw origin
         EditorGUI.DrawRect(new Rect(originPos, (Vector2)yCorrectOne * unit / 3), Color.red);

         drawSelectionGhostBox(ghostCol);

         EditorGUILayout.EndScrollView();
         // 스크롤 뷰 밖에서는 좌표계가 다르므로 grid board에 그리는 코드 쓰지 말 것!

         validateMouseCell();

         handlePointerEvent();

         handleKeyboardEvent();

         showPointerInfo();

         editCompo();

         serializedObject.ApplyModifiedProperties();

         if (boardMapDirty != Dirtyness.None && Event.current.type == EventType.Repaint) {
            forceRemakeBoardCache(boardMapDirty);
            boardMapDirty = Dirtyness.None;
         }
      }

      private void handlePointerEvent() {
         PointerTarget prevPointerTarget;
         PointerStatus prevPointerStatus;
         Vector2Int cellCoordFromZero = Vector2Int.zero;
         object compoRef;
         bool clickPerfomed = false;
         Vector2Int? prevPrevDownCell = null;

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
         compoRef = cachedBoardMap?[cellCoordFromZero.x, cellCoordFromZero.y]?.compoRef ?? null;

         pointerTarget = compoRef switch {
            Compo => PointerTarget.Compo,
            _ => PointerTarget.Empty
         };

         if (Event.current.isMouse) {
            switch (Event.current.type) {
               case EventType.MouseUp:
                  pointerStatus = PointerStatus.Hover;
                  prevPrevDownCell = prevDownCell;
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
            if (pointerTarget == PointerTarget.Compo) {
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
               CompoType creatingType = (CompoType)creatingCompoType.enumValueIndex;
               object creatingRef = null;
               switch (creatingType) {
                  case CompoType.Wire:
                     creatingRef = new Wire(newCompoRect);
                     break;
                  case CompoType.Axis:
                     creatingRef = new Axis(newCompoRect, false);
                     break;
                  case CompoType.Afforder:
                     creatingRef = new Afforder(newCompoRect, false, false);
                     break;
                  case CompoType.Container:
                     creatingRef = new Container(newCompoRect, false, false);
                     break;
                  case CompoType.Forwarder:
                     creatingRef = new Forwarder(newCompoRect, false);
                     break;
                  case CompoType.None:
                     //
                     break;
               }
               if (creatingRef != null) {
                  compos.InsertArrayElementAtIndex(nextIdx);
                  compos.GetArrayElementAtIndex(nextIdx).managedReferenceValue = creatingRef;
                  curHotCompoInfo = new CompoInfo(nextIdx, creatingType.ToString());
                  boardMapDirty = Dirtyness.Full;
               }
            }
            else if (prevPrevDownCell.HasValue && prevPrevDownCell.Value == curMouseCell.Value) {
               //셀 안에서만 마우스가 드래그 되었다면, 그냥 클릭한 것으로 침.
               prevPrevDownCell = null;
               prevPointerStatus = PointerStatus.Down;
               goto DO_WORK;
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
            boardMapDirty = Dirtyness.Full;
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
            boardMapDirty = Dirtyness.Full;
         }
         Event.current.Use();
      }

      private void basicProperties() {
         EditorGUIUtility.wideMode = true;
         EditorGUILayout.BeginHorizontal();
         EditorGUIUtility.labelWidth = 80;
         EditorGUI.BeginChangeCheck();
         EditorGUILayout.PropertyField(boardRect);
         if (EditorGUI.EndChangeCheck()) {
            boardMapDirty = Dirtyness.Full;
         }

         EditorGUI.BeginChangeCheck();
         EditorGUILayout.PropertyField(cellSize);
         if (EditorGUI.EndChangeCheck()) {
            boardMapDirty = Dirtyness.Full;
         }
         EditorGUILayout.EndHorizontal();

         EditorGUILayout.BeginHorizontal();
         EditorGUIUtility.labelWidth = 150;
         EditorGUILayout.PropertyField(creatingCompoType);
         GUILayout.Label(" ", GUILayout.Width(scrollSafeWidth / 4));
         if (GUILayout.Button("Refresh", GUILayout.Width(scrollSafeWidth / 4))) {
            boardMapDirty = Dirtyness.Full;
            (target as Board).invalidateBoardMap();
         }
         EditorGUILayout.EndHorizontal();
         EditorGUIUtility.labelWidth = 0;
      }

      private void drawBoardGrid(Vector2 size) {
         var boardCenter = (boardMax + (Vector2)boardMin) / 2f;

         Rect gridRect = GUILayoutUtility.GetRect(size.x, size.y);

         //그냥 배경용 박스
         GUI.Box(gridRect, "");
         EditorGUI.DrawRect(gridRect, new Color(0.3f, 0.3f, 0.3f, 1));

         if (Event.current.type != EventType.Repaint) {
            //이 이후로는 어차피 Layout 등이 관여할 필요가 없음 (그리기만 하면 됨)
            return;
         }
         //오리진 값 구하기
         originPos = new Vector2(gridRect.xMin + (size.x / 2), gridRect.center.y) - (boardCenter * yCorrectOne * unit);

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
         if (Event.current.type != EventType.Repaint || cachedDrawList == null) {
            return;
         }

         foreach (DrawInfo draw in cachedDrawList) {
            draw.draw(this);
         }
      }

      private void drawSelectionGhostBox(Color col) {
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

         drawGhostBox(ghostBoxRect.Value, col);
      }

      private void drawGhostBox(RectInt cellRect, Color col, float wid = 3) {
         var gridRect = cellRect2GridRect(cellRect);

         EditorGUI.DrawRect(new Rect(gridRect) { height = wid, y = gridRect.y, x = gridRect.x + wid, width = gridRect.width - (wid * 2) }, col);
         EditorGUI.DrawRect(new Rect(gridRect) { height = wid, y = gridRect.yMax - wid, width = gridRect.width - wid }, col);
         EditorGUI.DrawRect(new Rect(gridRect) { width = wid, x = gridRect.x, height = gridRect.height - wid}, col);
         EditorGUI.DrawRect(new Rect(gridRect) { width = wid, x = gridRect.xMax - wid }, col);
      }

      private Rect cellRect2GridRect(RectInt cellRect) {
         return new Rect(originPos + new Vector2(cellRect.xMin, cellRect.yMax) * unit * yCorrectOne, (Vector2)cellRect.size * unit);
      }

      private void validateMouseCell() {
         if (Event.current.type == EventType.Repaint) {
            //GetLastRect는 스크롤뷰의 rect를 반환함
            curMouseCell = GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) ? curMouseCell : null;
         }
      }

      private void showPointerInfo() {
         EditorGUI.LabelField(GUILayoutUtility.GetRect(scrollSafeWidth, EditorGUIUtility.singleLineHeight), (curMouseCell?.ToString() ?? "null //") + $" {pointerTarget}, {pointerStatus}");

         if (boardMapDirty != Dirtyness.None) {
            // boardMap의 정보가 outdated라서 없는 Axis를 참조하려고 할 수 있음.
            return;
         }

         if (style == null) {
            style = new GUIStyle(GUI.skin.box);
            style.normal.textColor = Color.white;
         }

         string curHoverCompoId;
         int curHoveringIndex;
         if (curMouseCell.HasValue && cachedBoardMap != null) {
            Vector2Int cellArrayCoord = curMouseCell.Value - boardMin;
            CompoInfo? hoveringCompoInfo;
            if ((hoveringCompoInfo = cachedBoardMap[cellArrayCoord.x, cellArrayCoord.y]) != null) {
               curHoveringIndex = hoveringCompoInfo.Value.indexInBoardList;
               var curHoveringCompoProperty = this.compos.GetArrayElementAtIndex(curHoveringIndex);
               curHoverCompoId = curHoveringCompoProperty?.FindPropertyRelative("_id")?.stringValue ?? null;

               string toShow = $"{curHoveringIndex}" + (string.IsNullOrEmpty(curHoverCompoId) ? "" : $": {curHoverCompoId}");
               this.style.CalcMinMaxWidth(new GUIContent(toShow), out _, out float maxWid);
               var labelBack = new Rect(Event.current.mousePosition - Vector2.up * EditorGUIUtility.singleLineHeight,
                              new Vector2(maxWid + 10, EditorGUIUtility.singleLineHeight));
               EditorGUI.DrawRect(labelBack, Color.black);
               GUI.Box(labelBack, toShow, style);
            }
         }
      }

      private void editCompo() {
         EditorGUILayout.Space(10, true);
         EditorGUI.DrawRect(GUILayoutUtility.GetRect(scrollSafeWidth, 3), highlightCol);
         EditorGUILayout.Space(10, true);

         if (curHotCompoInfo != null) {
            object compoRef = curHotCompoInfo.Value.compoRef;
            switch (compoRef) {
               case Axis:
                  editAxis();
                  break;
               default:
                  break;
            }
         }
      }

      private void editAxis() {
         object mv = curHotCompoProperty.managedReferenceValue;
         EditorGUILayout.LabelField(mv.GetType().Name, EditorStyles.boldLabel);
         if (mv is Compo) {
            EditorGUILayout.PropertyField(curHotCompoProperty.FindPropertyRelative("_id"));
            //EditorGUILayout.PropertyField(curHotCompoProperty.FindPropertyRelative("_rect"));
         }
         if (mv is Axis) {
            EditorGUILayout.PropertyField(curHotCompoProperty.FindPropertyRelative("_blocking"));
         }
         if (mv is Actor) {
            EditorGUILayout.PropertyField(curHotCompoProperty.FindPropertyRelative("_locked"));
         }
      }

      private void forceRemakeBoardCache(Dirtyness dirtyness) {
         var newBoardMap = new CompoInfo?[boardSize.x, boardSize.y];
         var newDrawList = new List<DrawInfo>();

         for (int idx = 0; idx < compos.arraySize; idx++) {
            SerializedProperty compo = compos.GetArrayElementAtIndex(idx);
            var cRect = compo.FindPropertyRelative("_rect").rectIntValue;
            object compoRef = compo.managedReferenceValue;
            newDrawList.Add(new DrawInfo(cRect, compo));
            if (!dirtyness.HasFlag(Dirtyness.Grid)) {
               continue;
            }

            //minx, miny가 0인 상태로 만들어주기 (배열)
            cRect.x -= boardMin.x;
            cRect.y -= boardMin.y;

            for (int xx = cRect.min.x; xx < cRect.max.x; xx++) {
               for (int yy = cRect.min.y; yy < cRect.max.y; yy++) {
                  try {
                     if (newBoardMap[xx, yy] != null) {
                        Debug.LogWarning($"({xx}, {yy})에서 겹침 있는 것 같음...");
                     }
                     newBoardMap[xx, yy] = new CompoInfo() {
                        indexInBoardList = idx,
                        compoRef = compoRef
                     };
                  }
                  catch (IndexOutOfRangeException) {
                     Debug.LogError($"Board를 벗어난 {nameof(Compo)}가 있음: ({xx}, {yy})");
                  }
               }
            }
         }
         cachedBoardMap = newBoardMap;
         cachedDrawList = newDrawList;
         ghostBoxRect = null;
         prevDownCell = null;
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
         public int indexInBoardList;
         public object compoRef;

         public CompoInfo(int _indexInBoardList, object _compoRef) {
            indexInBoardList = _indexInBoardList;
            compoRef = _compoRef;
         }
      }

      private struct DrawInfo {
         private static readonly Color freeAxisCol = new Color(1, 1, 1, 1f);
         private static readonly Color blockingAxisCol = new Color(0.2f, 0.2f, 0.2f, 1f);
         private static readonly Color wireCol = new Color(0.7f, 0.7f, 1f, 1);

         public RectInt compoRect;
         public SerializedProperty compo;

         public DrawInfo(RectInt compoRect, SerializedProperty compo) {
            this.compoRect = compoRect;
            this.compo = compo;
         }

         private Rect getGridRect(CariBoardEditor board) {
            return board.cellRect2GridRect(compoRect);
         }

         private Color getTintColor() {
            var compoRef = compo.managedReferenceValue;

            Color tintCol = compoRef switch {
               Axis axis => axis.blocking ? blockingAxisCol : freeAxisCol,
               Wire => wireCol,
               _ => Color.white,
            };

            return tintCol;
         }

         /// <summary>
         /// 그릴 텍스쳐가 있는지 찾기
         /// </summary>
         /// <returns>텍스쳐 없으면 null 리턴</returns>
         private Texture2D getTexture() {
            var compoRef = compo.managedReferenceValue;

            Texture2D tex;
            if (compoRef is Axis axis) {
               tex = axis switch {
                  Afforder => AssetDatabase.LoadAssetAtPath<Texture2D>($"{PathPrefix}/Editor/Graphics/Axis/Afforder.png"),
                  Container => AssetDatabase.LoadAssetAtPath<Texture2D>($"{PathPrefix}/Editor/Graphics/Axis/Container.png"),
                  Forwarder => AssetDatabase.LoadAssetAtPath<Texture2D>($"{PathPrefix}/Editor/Graphics/Axis/Forwarder.png"),
                  _ => AssetDatabase.LoadAssetAtPath<Texture2D>($"{PathPrefix}/Editor/Graphics/Axis/Empty.png")
               };
            }
            else if (compoRef is Wire) {
               string wireVariant;
               if (compoRect.width > compoRect.height) {
                  wireVariant = "Hor";
               }
               else if (compoRect.width < compoRect.height) {
                  wireVariant = "Ver";
               }
               else {
                  wireVariant = "Dot";
               }
               tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{PathPrefix}/Editor/Graphics/Wire/{wireVariant}.png");
            }
            else {
               tex = null;
            }
            return tex;
         }

         private bool drawBorderBox() => compo.managedReferenceValue switch {
            Wire => false,
            _ => true
         };

         private ScaleMode drawScaleMode() => compo.managedReferenceValue switch {
            Wire => ScaleMode.StretchToFill,
            _ => ScaleMode.ScaleToFit
         };

         public void draw(CariBoardEditor boardEditor) {
            if (boardMapDirty != Dirtyness.None) {
               // Compo가 이미 삭제되어 개 짜증나는 에러가 뜰 수 있서요
               return;
            }

            Texture2D tex;
            if ((tex = getTexture()) != null) {
               //텍스쳐가 있는 경우
               Color tintCol = getTintColor();
               GUI.DrawTexture(getGridRect(boardEditor), tex, drawScaleMode(), true, 0, tintCol, 0, 0);
               if (drawBorderBox()) {
                  boardEditor.drawGhostBox(compoRect, tintCol, Mathf.Max(2, boardEditor.unit / 10));
               }
            }
            else {
               //없는 경우 (색깔로만 그림)
               EditorGUI.DrawRect(getGridRect(boardEditor), getTintColor());
            }
         }
      }

      public void OnEnable() {
         boardRect = serializedObject.FindProperty("_boardRect");
         cellSize = serializedObject.FindProperty("_cellSize");
         compos = serializedObject.FindProperty("_compos");
         creatingCompoType = serializedObject.FindProperty("_creatingCompoType");
         curHotCompoInfo = null;
         style = null;
         cachedBoardMap = null;
         cachedDrawList = null;
         boardMapDirty = Dirtyness.Full;
      }

      public override bool RequiresConstantRepaint() => true;
   }
}