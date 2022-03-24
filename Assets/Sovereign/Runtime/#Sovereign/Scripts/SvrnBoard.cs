using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using RTF = UnityEngine.RectTransform;

namespace NoFS.DayLight.Sovereign {
   [ExecuteAlways]
   public class SvrnBoard : MonoBehaviour {

      [SerializeField]
      private GameObject meshManagerPrefab;

      public CinemachineVirtualCamera svrnVC { get; private set; }

      public RTF rtf => transform as RTF;

      private HashSet<GameObject> boardComponents = new HashSet<GameObject>();

      public GameObject addMeshManagerObject() {
         GameObject meshManagerObject = Instantiate(meshManagerPrefab, rtf, false);
         this.boardComponents.Add(meshManagerObject);
         return meshManagerObject;
      }

      public void cleanUpBoard() {
         foreach (GameObject obj in boardComponents) {
            Destroy(obj);
         }
         boardComponents.Clear();
      }

      private void Awake() {
         svrnVC = GetComponentInChildren<CinemachineVirtualCamera>();
      }

#if UNITY_EDITOR
      public void setLayer(string svrnLayer) {
         gameObject.layer = LayerMask.NameToLayer(svrnLayer);
         meshManagerPrefab.layer = LayerMask.NameToLayer(svrnLayer);
         foreach(Transform chgo in rtf) {
            chgo.gameObject.layer = LayerMask.NameToLayer(svrnLayer);
         }
      } 
#endif
   }
}