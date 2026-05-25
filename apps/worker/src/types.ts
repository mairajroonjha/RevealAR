export interface Env {
  DB: D1Database;
  ROOM_ASSETS: R2Bucket;
  JOB_CACHE: KVNamespace;
  AI_JOBS: Queue<TextureJobMessage>;
  AI_GATEWAY_BASE_URL: string;
  GEMINI_API_KEY?: string;
  GPU_SERVICE_URL?: string;
  GPU_SERVICE_TOKEN?: string;
}

export interface TextureJobMessage {
  jobId: string;
  projectId: string;
  prompt: string;
  roomImageKey: string;
  wallMaskKey?: string;
}

export interface ProjectPayload {
  id?: string;
  userId: string;
  name: string;
  roomImageKey?: string;
  furnitureLayout?: unknown[];
}

export interface RoomQuestionPayload {
  projectId: string;
  question: string;
  roomImageKey: string;
  detectedObjects?: string[];
}

export interface TextureJobPayload {
  projectId: string;
  prompt: string;
  roomImageKey: string;
  wallMaskKey?: string;
}
