using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace RevealAR.AR
{
    public class ARFurniturePlacementController : MonoBehaviour
    {
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private Camera arCamera;
        [SerializeField] private GameObject selectedFurniturePrefab;

        private static readonly List<ARRaycastHit> Hits = new();
        private GameObject selectedObject;

        private void Update()
        {
            if (Input.touchCount == 0)
            {
                return;
            }

            var touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Began || IsPointerOverUi(touch.fingerId))
            {
                return;
            }

            if (raycastManager.Raycast(touch.position, Hits, TrackableType.PlaneWithinPolygon))
            {
                PlaceOrMoveSelected(Hits[0].pose);
            }
        }

        public void SetFurniturePrefab(GameObject prefab)
        {
            selectedFurniturePrefab = prefab;
            selectedObject = null;
        }

        public void RotateSelected(float degrees)
        {
            if (selectedObject != null)
            {
                selectedObject.transform.Rotate(Vector3.up, degrees, Space.World);
            }
        }

        public void ScaleSelected(float scaleMultiplier)
        {
            if (selectedObject != null)
            {
                selectedObject.transform.localScale *= scaleMultiplier;
            }
        }

        public void RemoveSelected()
        {
            if (selectedObject != null)
            {
                Destroy(selectedObject);
                selectedObject = null;
            }
        }

        private void PlaceOrMoveSelected(Pose pose)
        {
            if (selectedObject == null)
            {
                if (selectedFurniturePrefab == null)
                {
                    Debug.LogWarning("No furniture prefab selected.");
                    return;
                }

                selectedObject = Instantiate(selectedFurniturePrefab, pose.position, pose.rotation);
                return;
            }

            selectedObject.transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        private static bool IsPointerOverUi(int fingerId)
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(fingerId);
        }
    }
}
