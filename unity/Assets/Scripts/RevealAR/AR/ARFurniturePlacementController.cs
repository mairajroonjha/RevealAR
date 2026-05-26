using System.Collections.Generic;
using RevealAR.Models;
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

        private static readonly List<ARRaycastHit> Hits = new List<ARRaycastHit>();
        private readonly List<GameObject> placedObjects = new List<GameObject>();
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

        public List<FurnitureItem> GetFurnitureLayout()
        {
            var items = new List<FurnitureItem>();

            foreach (var placedObject in placedObjects)
            {
                if (placedObject == null)
                {
                    continue;
                }

                items.Add(new FurnitureItem
                {
                    assetId = placedObject.name.Replace("(Clone)", string.Empty).Trim(),
                    positionX = placedObject.transform.position.x,
                    positionY = placedObject.transform.position.y,
                    positionZ = placedObject.transform.position.z,
                    rotationY = placedObject.transform.eulerAngles.y,
                    scale = placedObject.transform.localScale.x
                });
            }

            return items;
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
                placedObjects.Remove(selectedObject);
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
                selectedObject.SetActive(true);
                placedObjects.Add(selectedObject);
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
