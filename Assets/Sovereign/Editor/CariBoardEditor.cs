using UnityEngine;
using UnityEditor;
using NoFS.DayLight.Sovereign.Cari;

namespace NoFS.DayLight.SovereignEditor {
   [CustomEditor(typeof(CariBoard))]
   public class CariBoardEditor : Editor {

      private string inputCode { get; set; }
      public string boardCode { get; private set; }

      public override void OnInspectorGUI() {
         serializedObject.Update();

         EditorGUILayout.LabelField("Input Board Source...");
         var newInputCode = EditorGUILayout.TextArea(inputCode, GUILayout.MinHeight(200));
         if (newInputCode != inputCode) {
            inputCode = newInputCode;
            decodeBoard(inputCode);
            boardCode = encodeBoard();
         }
         EditorGUILayout.Space(5);
         EditorGUILayout.LabelField("Encoded Board Source...");
         EditorGUILayout.TextArea(boardCode, GUILayout.MaxHeight(100));

         Rect lineRect = new Rect(GUILayoutUtility.GetLastRect());
         lineRect.yMin = lineRect.yMax + 10;
         lineRect.yMax = lineRect.yMin + 5;

         EditorGUI.DrawRect(lineRect, Color.white);

         EditorGUILayout.Space();

         serializedObject.ApplyModifiedProperties();
      }

      private string encodeBoard() {
         //Encoding Logic
         return inputCode;
      }
      private void decodeBoard(string boardCode) {
         //Decoding Logic
      }
   } 
}