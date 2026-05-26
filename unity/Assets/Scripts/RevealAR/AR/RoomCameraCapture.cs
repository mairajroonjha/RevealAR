using System;
using System.Collections;
using UnityEngine;

namespace RevealAR.AR
{
    public class RoomCameraCapture : MonoBehaviour
    {
        [SerializeField] private Camera arCamera;
        [SerializeField] private int captureWidth = 720;
        [SerializeField] private int captureHeight = 1280;

        public IEnumerator CaptureJpeg(Action<byte[]> onCaptured)
        {
            yield return new WaitForEndOfFrame();

            if (arCamera == null)
            {
                arCamera = Camera.main;
            }

            if (arCamera == null)
            {
                Debug.LogError("RoomCameraCapture needs an AR camera.");
                yield break;
            }

            var renderTexture = new RenderTexture(captureWidth, captureHeight, 24);
            var previousTarget = arCamera.targetTexture;
            var previousActive = RenderTexture.active;

            arCamera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            arCamera.Render();

            var image = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
            image.Apply();

            arCamera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Destroy(renderTexture);

            onCaptured?.Invoke(image.EncodeToJPG(85));
            Destroy(image);
        }
    }
}
