using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;

namespace RevealAR.AR
{
    public class WallTextureApplier : MonoBehaviour
    {
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private Material wallMaterialTemplate;

        public void ApplyTextureToNearestWall(Vector3 userPosition, Texture2D texture)
        {
            var wall = FindNearestVerticalPlane(userPosition);
            if (wall == null)
            {
                Debug.LogWarning("No wall plane found yet.");
                return;
            }

            var renderer = wall.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = wall.gameObject.AddComponent<MeshRenderer>();
            }

            var material = new Material(wallMaterialTemplate);
            material.mainTexture = texture;
            renderer.material = material;
        }

        public IEnumerator ApplyTextureFromUrl(Vector3 userPosition, string textureUrl)
        {
            using var request = UnityWebRequestTexture.GetTexture(textureUrl);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error);
                yield break;
            }

            ApplyTextureToNearestWall(userPosition, DownloadHandlerTexture.GetContent(request));
        }

        private ARPlane FindNearestVerticalPlane(Vector3 userPosition)
        {
            ARPlane nearest = null;
            var nearestDistance = float.MaxValue;

            foreach (var plane in planeManager.trackables)
            {
                if (!IsVertical(plane))
                {
                    continue;
                }

                var distance = Vector3.Distance(userPosition, plane.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = plane;
                }
            }

            return nearest;
        }

        private static bool IsVertical(ARPlane plane)
        {
            var upDot = Mathf.Abs(Vector3.Dot(plane.transform.up, Vector3.up));
            return upDot < 0.35f;
        }
    }
}
