using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using RTF = UnityEngine.RectTransform;

namespace NoFS.DayLight.Sovereign {

   [ExecuteAlways]
   [AddComponentMenu("Sovereign/Sovereign Board")]
   public class SvrnBoard : MonoBehaviour {

      [SerializeField]
      private GameObject meshManagerPrefab;

      public CinemachineVirtualCamera svrnVC { get; private set; }

      public RTF rtf => transform as RTF;

      private HashSet<GameObject> boardComponents = new HashSet<GameObject>();

      public GameObject addMeshManagerObject() {
         GameObject meshManagerObject = Instantiate(meshManagerPrefab, rtf, false);
         meshManagerObject.layer = gameObject.layer;
         this.boardComponents.Add(meshManagerObject);
         return meshManagerObject;
      }

      public void cleanUpBoard() {
         if (boardComponents.Count == 0) {
            return;
         }
         foreach (GameObject obj in boardComponents) {
            Destroy(obj);
         }
         boardComponents.Clear();
      }

      private void Awake() {
         svrnVC = GetComponentInChildren<CinemachineVirtualCamera>();
      }

      public void setLayer(string layerName) {
         gameObject.layer = LayerMask.NameToLayer(layerName);
         foreach (Transform chgo in rtf) {
            chgo.gameObject.layer = LayerMask.NameToLayer(layerName);
         }
      }
   }
}