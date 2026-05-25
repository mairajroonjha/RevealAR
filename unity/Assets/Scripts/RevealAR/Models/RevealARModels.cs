using System;
using System.Collections.Generic;

namespace RevealAR.Models
{
    [Serializable]
    public class ProjectPayload
    {
        public string id;
        public string userId;
        public string name;
        public string roomImageKey;
        public List<FurnitureItem> furnitureLayout = new List<FurnitureItem>();
    }

    [Serializable]
    public class FurnitureItem
    {
        public string assetId;
        public float positionX;
        public float positionY;
        public float positionZ;
        public float rotationY;
        public float scale = 1f;
    }

    [Serializable]
    public class UploadRoomImageResponse
    {
        public string imageId;
        public string key;
    }

    [Serializable]
    public class ProjectResponse
    {
        public string id;
        public string userId;
        public string name;
        public string roomImageKey;
        public List<FurnitureItem> furnitureLayout;
    }

    [Serializable]
    public class RoomQuestionPayload
    {
        public string projectId;
        public string question;
        public string roomImageKey;
        public string[] detectedObjects;
    }

    [Serializable]
    public class RoomQuestionResponse
    {
        public string promptId;
        public string answer;
    }

    [Serializable]
    public class TextureJobPayload
    {
        public string projectId;
        public string prompt;
        public string roomImageKey;
        public string wallMaskKey;
    }

    [Serializable]
    public class TextureJobResponse
    {
        public string jobId;
        public string status;
    }
}
