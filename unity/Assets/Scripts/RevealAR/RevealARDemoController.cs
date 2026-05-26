using System.Collections;
using RevealAR.Api;
using RevealAR.AR;
using RevealAR.Models;
using UnityEngine;
using UnityEngine.UI;

namespace RevealAR
{
    public class RevealARDemoController : MonoBehaviour
    {
        [Header("RevealAR Services")]
        [SerializeField] private RevealARApiClient apiClient;
        [SerializeField] private RoomCameraCapture cameraCapture;
        [SerializeField] private ARFurniturePlacementController placementController;

        [Header("Demo Data")]
        [SerializeField] private string userId = "demo-user";
        [SerializeField] private string projectName = "Demo Room";
        [SerializeField] private GameObject demoFurniturePrefab;

        [Header("UI")]
        [SerializeField] private InputField questionInput;
        [SerializeField] private InputField texturePromptInput;
        [SerializeField] private Text statusText;

        private string projectId;
        private string roomImageKey;

        private void Awake()
        {
            if (demoFurniturePrefab == null)
            {
                demoFurniturePrefab = CreateDemoFurniturePrefab();
            }

            if (placementController != null)
            {
                placementController.SetFurniturePrefab(demoFurniturePrefab);
            }

            SetStatus("RevealAR ready. Scan a plane, then tap to place furniture.");
        }

        public void CaptureAndUploadRoom()
        {
            if (!EnsureApiReady() || cameraCapture == null)
            {
                SetStatus("Camera capture is not connected.");
                return;
            }

            StartCoroutine(CaptureAndUploadRoutine());
        }

        public void SaveProject()
        {
            if (!EnsureApiReady())
            {
                return;
            }

            if (string.IsNullOrEmpty(roomImageKey))
            {
                SetStatus("Capture and upload a room image first.");
                return;
            }

            var payload = new ProjectPayload
            {
                id = projectId,
                userId = userId,
                name = projectName,
                roomImageKey = roomImageKey,
                furnitureLayout = placementController != null ? placementController.GetFurnitureLayout() : null
            };

            SetStatus("Saving project...");
            StartCoroutine(apiClient.SaveProject(payload, response =>
            {
                projectId = response.id;
                SetStatus("Project saved: " + projectId);
            }, ShowError));
        }

        public void AskRoomQuestion()
        {
            if (!EnsureProjectReady())
            {
                return;
            }

            var question = questionInput != null ? questionInput.text : string.Empty;
            if (string.IsNullOrWhiteSpace(question))
            {
                question = "What color will look good in this room?";
            }

            var payload = new RoomQuestionPayload
            {
                projectId = projectId,
                question = question,
                roomImageKey = roomImageKey,
                detectedObjects = new[] { "wall", "floor", "furniture" }
            };

            SetStatus("Asking AI...");
            StartCoroutine(apiClient.AskRoomQuestion(payload, response =>
            {
                SetStatus(response.answer);
            }, ShowError));
        }

        public void CreateTextureJob()
        {
            if (!EnsureProjectReady())
            {
                return;
            }

            var prompt = texturePromptInput != null ? texturePromptInput.text : string.Empty;
            if (string.IsNullOrWhiteSpace(prompt))
            {
                prompt = "Make the wall a warm wooden texture";
            }

            var payload = new TextureJobPayload
            {
                projectId = projectId,
                prompt = prompt,
                roomImageKey = roomImageKey
            };

            SetStatus("Creating texture job...");
            StartCoroutine(apiClient.CreateTextureJob(payload, response =>
            {
                SetStatus("Texture job queued: " + response.jobId);
            }, ShowError));
        }

        public void RotateSelectedLeft()
        {
            if (placementController != null)
            {
                placementController.RotateSelected(-15f);
            }
        }

        public void RotateSelectedRight()
        {
            if (placementController != null)
            {
                placementController.RotateSelected(15f);
            }
        }

        public void ScaleSelectedUp()
        {
            if (placementController != null)
            {
                placementController.ScaleSelected(1.1f);
            }
        }

        public void ScaleSelectedDown()
        {
            if (placementController != null)
            {
                placementController.ScaleSelected(0.9f);
            }
        }

        public void RemoveSelected()
        {
            if (placementController != null)
            {
                placementController.RemoveSelected();
            }
        }

        private IEnumerator CaptureAndUploadRoutine()
        {
            SetStatus("Capturing room...");

            byte[] imageBytes = null;
            yield return cameraCapture.CaptureJpeg(bytes => imageBytes = bytes);

            if (imageBytes == null || imageBytes.Length == 0)
            {
                SetStatus("Room capture failed.");
                yield break;
            }

            SetStatus("Uploading room image...");
            yield return apiClient.UploadRoomImage(imageBytes, "image/jpeg", response =>
            {
                roomImageKey = response.key;
                SetStatus("Room image uploaded.");
            }, ShowError);
        }

        private bool EnsureApiReady()
        {
            if (apiClient != null)
            {
                return true;
            }

            SetStatus("RevealARApiClient is not connected.");
            return false;
        }

        private bool EnsureProjectReady()
        {
            if (!EnsureApiReady())
            {
                return false;
            }

            if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(roomImageKey))
            {
                SetStatus("Capture/upload and save the project first.");
                return false;
            }

            return true;
        }

        private void ShowError(string error)
        {
            SetStatus("Error: " + error);
        }

        private void SetStatus(string message)
        {
            Debug.Log("[RevealAR] " + message);

            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private static GameObject CreateDemoFurniturePrefab()
        {
            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Demo Table";
            table.transform.localScale = new Vector3(0.6f, 0.08f, 0.45f);

            var renderer = table.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.56f, 0.36f, 0.22f);
            }

            table.SetActive(false);
            return table;
        }
    }
}
