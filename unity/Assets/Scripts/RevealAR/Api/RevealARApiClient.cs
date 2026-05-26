using System;
using System.Collections;
using System.Text;
using RevealAR.Models;
using UnityEngine;
using UnityEngine.Networking;

namespace RevealAR.Api
{
    public class RevealARApiClient : MonoBehaviour
    {
        [SerializeField] private string baseUrl = "https://revealar-api.mirajroonjha.workers.dev";

        public IEnumerator UploadRoomImage(byte[] imageBytes, string contentType, Action<UploadRoomImageResponse> onSuccess, Action<string> onError)
        {
            using var request = new UnityWebRequest($"{baseUrl}/uploads/room-image", UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(imageBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("content-type", contentType);

            yield return request.SendWebRequest();
            HandleJsonResponse(request, onSuccess, onError);
        }

        public IEnumerator SaveProject(ProjectPayload payload, Action<ProjectResponse> onSuccess, Action<string> onError)
        {
            yield return PostJson("/projects", payload, onSuccess, onError);
        }

        public IEnumerator AskRoomQuestion(RoomQuestionPayload payload, Action<RoomQuestionResponse> onSuccess, Action<string> onError)
        {
            yield return PostJson("/ai/room-question", payload, onSuccess, onError);
        }

        public IEnumerator CreateTextureJob(TextureJobPayload payload, Action<TextureJobResponse> onSuccess, Action<string> onError)
        {
            yield return PostJson("/ai/texture-jobs", payload, onSuccess, onError);
        }

        private IEnumerator PostJson<TRequest, TResponse>(string path, TRequest payload, Action<TResponse> onSuccess, Action<string> onError)
        {
            var json = JsonUtility.ToJson(payload);
            var body = Encoding.UTF8.GetBytes(json);

            using var request = new UnityWebRequest($"{baseUrl}{path}", UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("content-type", "application/json");

            yield return request.SendWebRequest();
            HandleJsonResponse(request, onSuccess, onError);
        }

        private static void HandleJsonResponse<T>(UnityWebRequest request, Action<T> onSuccess, Action<string> onError)
        {
            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(request.error);
                return;
            }

            if (request.responseCode < 200 || request.responseCode >= 300)
            {
                onError?.Invoke(request.downloadHandler.text);
                return;
            }

            onSuccess?.Invoke(JsonUtility.FromJson<T>(request.downloadHandler.text));
        }
    }
}
