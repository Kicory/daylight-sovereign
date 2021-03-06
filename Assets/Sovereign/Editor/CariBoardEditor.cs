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
      private const string AfforderPath = "_afforder";
      private const string AfTypePath = "_afType";
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

      /// <remarks> ?????? y+?????? min??? ????????? ??? ?????? ????????? ?????? </remarks>
      private Vector2Int? curMouseCell { get; set; } = null;

      /// <summary> ????????? pointer down ???????????? ????????? cell ?????? </summary>
      /// <remarks> ?????? y+?????? min??? ????????? ??? ?????? ????????? ?????? </remarks>
      private Vector2Int? prevDownCell { get; set; } = null;

      /// <remarks> ?????? y+?????? min??? ????????? ??? ?????? ????????? ?????? </remarks>
      private RectInt? ghostBoxRect = null;

      public override void OnInspectorGUI() {
         serializedObject.Update();
         EditorGUILayout.HelpBox("???????????? Axis ?????? / ??????????????? Axis ?????? / Del ?????? ????????? Axis ?????? / " +
            "????????? ?????? ????????? Axis ?????? / Ctrl + ????????? ?????? ????????? Axis ?????? ??????", MessageType.Info);

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
         // ????????? ??? ???????????? ???????????? ???????????? grid board??? ????????? ?????? ?????? ??? ???!

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

         //???????????? ????????? ?????? ????????? ????????????, ?????? ???????????? ???????????? ??? ?????? ????????? ??????
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
                     //???????????? ?????? ?????? ???????????? ??????
                     pointerStatus = PointerStatus.Hover;
                  }
                  break;

               case EventType.ContextClick:
               default:
                  break;
            }
         }
         else if (pointerStatus == PointerStatus.None) {
            //?????? ?????? ????????? ?????? (currentMouseCell??? null??? ??????), ????????? ????????? ???????????? ???????????? ???????????? Hover
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

         //????????? ????????? ????????? ????????? ????????? ??????
         if (prevPointerStatus == PointerStatus.Drag && pointerStatus == PointerStatus.None) {
            ghostBoxRect = null;
            return;
         }

         //?????? ????????? ??????
         if (pointerStatus == PointerStatus.Drag && prevDownCell.HasValue) {
            //Compo??? ??????????????? ?????? Axis ???????????? ????????? (????????? ????????? ?????????)
            var prevDownCellCoordFromZero = prevDownCell.Value - boardMin;
            if (cachedBoardMap[prevDownCellCoordFromZero.x, prevDownCellCoordFromZero.y] == null) {
               var boxMin = Vector2Int.Min(prevDownCell.Value, curMouseCell.Value);
               var boxMax = Vector2Int.Max(prevDownCell.Value, curMouseCell.Value) + Vector2Int.one;
               curHotCompoInfo = null;
               ghostBoxRect = new RectInt(boxMin, boxMax - boxMin);
            }
         }

         //Axis ?????? ??????
         if (prevPointerStatus == PointerStatus.Drag && pointerStatus == PointerStatus.Hover && ghostBoxRect.HasValue) {
            var nextIdx = compos.arraySize;
            RectInt newCompoRect = ghostBoxRect.Value;

            if (isValidRectForCompoIdx(newCompoRect, nextIdx)) {
               string creatingTypeName = creatingCompoTypeName;
               object creatingRef = null;
               if (creatingTypeName == nameof(PassiveAxis)) {
                  creatingRef = new PassiveAxis(newCompoRect, null);
               }
               else if (creatingTypeName == nameof(ActiveAxis)) {
                  creatingRef = new ActiveAxis(newCompoRect, null);
               }
               else if (creatingTypeName == nameof(Wire)) {
                  creatingRef = new Wire(newCompoRect);
               }
               else {
                  //
               }
               if (creatingRef != null) {
                  compos.InsertArrayElementAtIndex(nextIdx);
                  compos.GetArrayElementAtIndex(nextIdx).managedReferenceValue = creatingRef;
                  curHotCompoInfo = new CompoInfo(nextIdx, creatingTypeName);
                  boardMapDirty = Dirtyness.Full;
               }
            }
            else if (prevPrevDownCell.HasValue && prevPrevDownCell.Value == curMouseCell.Value) {
               //??? ???????????? ???????????? ????????? ????????????, ?????? ????????? ????????? ???.
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
            //reCalcBoardMap(); // ????????? cachedBoardMap??? ???????????? ????????? ????????? ?????? ??????????????? ??? ?????? ???
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

         //?????? ????????? ??????
         GUI.Box(gridRect, "");
         EditorGUI.DrawRect(gridRect, new Color(0.3f, 0.3f, 0.3f, 1));

         if (Event.current.type != EventType.Repaint) {
            //??? ???????????? ????????? Layout ?????? ????????? ????????? ?????? (???????????? ?????? ???)
            return;
         }
         //????????? ??? ?????????
         originPos = new Vector2(gridRect.xMin + (size.x / 2), gridRect.center.y) - (boardCenter * yCorrectOne * unit);

         for (int yy = boardMin.y; yy <= boardMax.y; yy++) {
            //???????????? ???????????? ?????? = y+ ???...
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
         // Layout ?????????????????? mouseposition??? ?????????, repaint????????? currentMouseCell ??????????????? ???.
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
            //????????? compo??? ????????? ??????????????? ?????????
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
            //GetLastRect??? ??????????????? rect??? ?????????
            curMouseCell = GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition) ? curMouseCell : null;
         }
      }

      private void showPointerInfo() {
         EditorGUI.LabelField(GUILayoutUtility.GetRect(scrollSafeWidth, EditorGUIUtility.singleLineHeight), (curMouseCell?.ToString() ?? "null //") + $" {pointerTarget}, {pointerStatus}");

         if (boardMapDirty != Dirtyness.None) {
            // boardMap??? ????????? outdated?????? ?????? Axis??? ??????????????? ??? ??? ??????.
            return;
         }

         if (style == null) {
            style = new GUIStyle(GUI.skin.box);
            style.normal.textColor = Color.white;
         }

         string curHoveringAfCode;
         int curHoveringIndex;
         if (curMouseCell.HasValue && cachedBoardMap != null) {
            Vector2Int cellArrayCoord = curMouseCell.Value - boardMin;
            CompoInfo? hoveringCompoInfo;
            if ((hoveringCompoInfo = cachedBoardMap[cellArrayCoord.x, cellArrayCoord.y]) != null) {
               curHoveringIndex = hoveringCompoInfo.Value.indexInBoardList;
               var curHoveringCompoProperty = this.compos.GetArrayElementAtIndex(curHoveringIndex);
               curHoveringAfCode = curHoveringCompoProperty.FindPropertyRelative(AfforderPath)?.FindPropertyRelative("_code")?.stringValue ?? null;

               string toShow = $"{curHoveringIndex}" + (curHoveringAfCode == null ? "" : $": {curHoveringAfCode}");
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
         SerializedProperty afType = curHotCompoProperty.FindPropertyRelative(AfTypePath);
         int oldAfType = afType.enumValueIndex;
         Afforder.Type newAfType;

         EditorGUI.BeginChangeCheck();
         EditorGUILayout.PropertyField(afType);
         if (EditorGUI.EndChangeCheck()) {
            newAfType = (Afforder.Type)Enum.GetValues(typeof(Afforder.Type)).GetValue(afType.enumValueIndex);
            switch (newAfType) {
               case Afforder.Type.NA:
                  // ???????????? ?????? ??????????????? ?????????
                  afType.enumValueIndex = oldAfType;
                  Debug.LogWarning($"{nameof(newAfType)}??? ?????? ??? ??????");
                  break;
               case Afforder.Type.Empty:
                  curHotCompoProperty.FindPropertyRelative(AfforderPath).managedReferenceValue = null;
                  break;
               default:
                  System.Reflection.ConstructorInfo constructorInfo = newAfType.type().GetConstructor(new Type[] { typeof(string) });
                  curHotCompoProperty.FindPropertyRelative(AfforderPath).managedReferenceValue = constructorInfo.Invoke(new object[] { "NEW" });
                  break;
            }
         }

         if (curHotCompoProperty.FindPropertyRelative(AfforderPath).managedReferenceValue != null) {
            SerializedProperty _property = curHotCompoProperty.FindPropertyRelative(AfforderPath);
            var e = _property.GetEnumerator();
            while (e.MoveNext()) {
               SerializedProperty cur = e.Current as SerializedProperty;
               EditorGUILayout.PropertyField(cur);
            }
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

            //minx, miny??? 0??? ????????? ??????????????? (??????)
            cRect.x -= boardMin.x;
            cRect.y -= boardMin.y;

            for (int xx = cRect.min.x; xx < cRect.max.x; xx++) {
               for (int yy = cRect.min.y; yy < cRect.max.y; yy++) {
                  try {
                     if (newBoardMap[xx, yy] != null) {
                        Debug.LogWarning($"({xx}, {yy})?????? ?????? ?????? ??? ??????...");
                     }
                     newBoardMap[xx, yy] = new CompoInfo() {
                        indexInBoardList = idx,
                        compoRef = compoRef
                     };
                  }
                  catch (IndexOutOfRangeException) {
                     Debug.LogError($"Board??? ????????? {nameof(Compo)}??? ??????: ({xx}, {yy})");
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
         private static readonly Color activeAxisCol = new Color(1, 1, 1, 1f);
         private static readonly Color passiveAxisCol = new Color(0.2f, 0.2f, 0.2f, 1f);
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
               ActiveAxis => activeAxisCol,
               PassiveAxis => passiveAxisCol,
               Wire => wireCol,
               _ => Color.white,
            };

            string afforderCode = compo?.FindPropertyRelative(AfforderPath)?.FindPropertyRelative("_code")?.stringValue ?? null;
            if (afforderCode != null && afforderCode.StartsWith("PRIME")) {
               tintCol = Color.cyan;
            }

            return tintCol;
         }

         /// <summary>
         /// ?????? ???????????? ????????? ??????
         /// </summary>
         /// <returns>????????? ????????? null ??????</returns>
         private Texture2D getTexture() {
            var compoRef = compo.managedReferenceValue;

            Texture2D tex;
            if (compoRef is Axis axis) {
               tex = axis.afType switch {
                  Afforder.Type.Empty or
                  Afforder.Type.SquareButton or 
                  Afforder.Type.VerticalSwitch => AssetDatabase.LoadAssetAtPath<Texture2D>($"{PathPrefix}/Editor/Graphics/Afforders/{axis.afType}.png"),
                  _ => null,
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
               // Compo??? ?????? ???????????? ??? ???????????? ????????? ??? ??? ?????????
               return;
            }

            Texture2D tex;
            if ((tex = getTexture()) != null) {
               //???????????? ?????? ??????
               Color tintCol = getTintColor();
               GUI.DrawTexture(getGridRect(boardEditor), tex, drawScaleMode(), true, 0, tintCol, 0, 0);
               if (drawBorderBox()) {
                  boardEditor.drawGhostBox(compoRect, tintCol, Mathf.Max(2, boardEditor.unit / 10));
               }
            }
            else {
               //?????? ?????? (???????????? ??????)
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