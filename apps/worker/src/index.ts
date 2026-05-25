import { HttpError, json, notFound, readJson, requireString } from "./http";
import { assetKey, createId } from "./id";
import type { Env, ProjectPayload, RoomQuestionPayload, TextureJobMessage, TextureJobPayload } from "./types";

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    if (request.method === "OPTIONS") {
      return json({ ok: true });
    }

    try {
      const url = new URL(request.url);
      const path = url.pathname.replace(/\/+$/, "") || "/";

      if (request.method === "GET" && path === "/") {
        return json({
          ok: true,
          service: "revealar-api",
          message: "RevealAR API is running.",
          endpoints: [
            "GET /health",
            "POST /uploads/room-image",
            "POST /projects",
            "GET /projects/:id",
            "POST /ai/room-question",
            "POST /ai/texture-jobs",
            "GET /ai/jobs/:id"
          ]
        });
      }

      if (request.method === "GET" && path === "/health") {
        return json({ ok: true, service: "revealar-api" });
      }

      if (request.method === "POST" && path === "/uploads/room-image") {
        return uploadRoomImage(request, env);
      }

      if (request.method === "POST" && path === "/projects") {
        return saveProject(request, env);
      }

      const projectMatch = path.match(/^\/projects\/([^/]+)$/);
      if (request.method === "GET" && projectMatch) {
        return getProject(projectMatch[1], env);
      }

      if (request.method === "POST" && path === "/ai/room-question") {
        return answerRoomQuestion(request, env);
      }

      if (request.method === "POST" && path === "/ai/texture-jobs") {
        return createTextureJob(request, env);
      }

      const jobMatch = path.match(/^\/ai\/jobs\/([^/]+)$/);
      if (request.method === "GET" && jobMatch) {
        return getJob(jobMatch[1], env);
      }

      return notFound();
    } catch (error) {
      if (error instanceof HttpError) {
        return json({ error: error.message }, { status: error.status });
      }

      console.error(error);
      return json({ error: "Internal server error" }, { status: 500 });
    }
  },

  async queue(batch: MessageBatch<TextureJobMessage>, env: Env): Promise<void> {
    for (const message of batch.messages) {
      await processTextureJob(message.body, env);
      message.ack();
    }
  }
};

async function uploadRoomImage(request: Request, env: Env): Promise<Response> {
  if (!request.body) {
    throw new HttpError(400, "Image body is required");
  }

  const contentType = request.headers.get("content-type") ?? "application/octet-stream";
  const extension = contentType.includes("png") ? "png" : "jpg";
  const imageId = createId("room");
  const key = assetKey("room-images", imageId, extension);

  await env.ROOM_ASSETS.put(key, request.body, {
    httpMetadata: { contentType },
    customMetadata: {
      uploadedAt: new Date().toISOString()
    }
  });

  return json({ imageId, key });
}

async function saveProject(request: Request, env: Env): Promise<Response> {
  const body = await readJson<ProjectPayload>(request);
  const id = body.id ?? createId("project");
  const userId = requireString(body.userId, "userId");
  const name = requireString(body.name, "name");
  const roomImageKey = body.roomImageKey ?? null;
  const furnitureLayout = JSON.stringify(body.furnitureLayout ?? []);

  await env.DB.prepare(
    `INSERT INTO users (id) VALUES (?) ON CONFLICT(id) DO NOTHING`
  ).bind(userId).run();

  await env.DB.prepare(
    `INSERT INTO projects (id, user_id, name, room_image_key, furniture_layout_json, updated_at)
     VALUES (?, ?, ?, ?, ?, CURRENT_TIMESTAMP)
     ON CONFLICT(id) DO UPDATE SET
       name = excluded.name,
       room_image_key = excluded.room_image_key,
       furniture_layout_json = excluded.furniture_layout_json,
       updated_at = CURRENT_TIMESTAMP`
  ).bind(id, userId, name, roomImageKey, furnitureLayout).run();

  return json({ id, userId, name, roomImageKey, furnitureLayout: JSON.parse(furnitureLayout) });
}

async function getProject(projectId: string, env: Env): Promise<Response> {
  const row = await env.DB.prepare(
    `SELECT id, user_id, name, room_image_key, furniture_layout_json, created_at, updated_at
     FROM projects
     WHERE id = ?`
  ).bind(projectId).first<{
    id: string;
    user_id: string;
    name: string;
    room_image_key: string | null;
    furniture_layout_json: string;
    created_at: string;
    updated_at: string;
  }>();

  if (!row) {
    return json({ error: "Project not found" }, { status: 404 });
  }

  return json({
    id: row.id,
    userId: row.user_id,
    name: row.name,
    roomImageKey: row.room_image_key,
    furnitureLayout: JSON.parse(row.furniture_layout_json),
    createdAt: row.created_at,
    updatedAt: row.updated_at
  });
}

async function answerRoomQuestion(request: Request, env: Env): Promise<Response> {
  const body = await readJson<RoomQuestionPayload>(request);
  const projectId = requireString(body.projectId, "projectId");
  const question = requireString(body.question, "question");
  const roomImageKey = requireString(body.roomImageKey, "roomImageKey");

  const answer = await getRoomDesignAnswer({
    question,
    roomImageKey,
    detectedObjects: body.detectedObjects ?? []
  }, env);

  const promptId = createId("prompt");
  await env.DB.prepare(
    `INSERT INTO prompts (id, project_id, prompt_type, prompt_text, response_text)
     VALUES (?, ?, 'room_question', ?, ?)`
  ).bind(promptId, projectId, question, answer).run();

  return json({ promptId, answer });
}

async function createTextureJob(request: Request, env: Env): Promise<Response> {
  const body = await readJson<TextureJobPayload>(request);
  const jobId = createId("job");
  const projectId = requireString(body.projectId, "projectId");
  const prompt = requireString(body.prompt, "prompt");
  const roomImageKey = requireString(body.roomImageKey, "roomImageKey");

  const input = {
    projectId,
    prompt,
    roomImageKey,
    wallMaskKey: body.wallMaskKey
  };

  await env.DB.prepare(
    `INSERT INTO ai_jobs (id, project_id, job_type, status, input_json)
     VALUES (?, ?, 'texture_generation', 'queued', ?)`
  ).bind(jobId, projectId, JSON.stringify(input)).run();

  await env.JOB_CACHE.put(jobId, JSON.stringify({ status: "queued" }), { expirationTtl: 60 * 60 });
  await env.AI_JOBS.send({ jobId, ...input });

  return json({ jobId, status: "queued" }, { status: 202 });
}

async function getJob(jobId: string, env: Env): Promise<Response> {
  const row = await env.DB.prepare(
    `SELECT id, project_id, job_type, status, input_json, output_json, error_text, created_at, updated_at
     FROM ai_jobs
     WHERE id = ?`
  ).bind(jobId).first();

  if (!row) {
    return json({ error: "Job not found" }, { status: 404 });
  }

  return json(row);
}

async function processTextureJob(job: TextureJobMessage, env: Env): Promise<void> {
  await updateJob(env, job.jobId, "running");

  try {
    const output = await generateTexture(job, env);

    await env.DB.prepare(
      `UPDATE ai_jobs
       SET status = 'completed', output_json = ?, updated_at = CURRENT_TIMESTAMP
       WHERE id = ?`
    ).bind(JSON.stringify(output), job.jobId).run();

    await env.JOB_CACHE.put(job.jobId, JSON.stringify({ status: "completed", output }), {
      expirationTtl: 60 * 60
    });
  } catch (error) {
    const message = error instanceof Error ? error.message : "Texture generation failed";
    await env.DB.prepare(
      `UPDATE ai_jobs
       SET status = 'failed', error_text = ?, updated_at = CURRENT_TIMESTAMP
       WHERE id = ?`
    ).bind(message, job.jobId).run();
  }
}

async function updateJob(env: Env, jobId: string, status: string): Promise<void> {
  await env.DB.prepare(
    `UPDATE ai_jobs SET status = ?, updated_at = CURRENT_TIMESTAMP WHERE id = ?`
  ).bind(status, jobId).run();
  await env.JOB_CACHE.put(jobId, JSON.stringify({ status }), { expirationTtl: 60 * 60 });
}

async function getRoomDesignAnswer(
  input: { question: string; roomImageKey: string; detectedObjects: string[] },
  env: Env
): Promise<string> {
  if (!env.GEMINI_API_KEY) {
    return `AI provider is not configured yet. Question received: "${input.question}".`;
  }

  // Next step: call Gemini Flash through AI Gateway with the R2 room image.
  return `Design recommendation placeholder for: "${input.question}".`;
}

async function generateTexture(job: TextureJobMessage, env: Env): Promise<{ textureKey: string; textureUrl?: string }> {
  if (!env.GPU_SERVICE_URL) {
    const textureKey = assetKey("generated-textures", job.jobId, "txt");
    await env.ROOM_ASSETS.put(textureKey, `Texture placeholder for prompt: ${job.prompt}`, {
      httpMetadata: { contentType: "text/plain; charset=utf-8" }
    });
    return { textureKey };
  }

  const response = await fetch(`${env.GPU_SERVICE_URL}/textures`, {
    method: "POST",
    headers: {
      "content-type": "application/json",
      ...(env.GPU_SERVICE_TOKEN ? { authorization: `Bearer ${env.GPU_SERVICE_TOKEN}` } : {})
    },
    body: JSON.stringify(job)
  });

  if (!response.ok) {
    throw new Error(`GPU service failed with ${response.status}`);
  }

  return await response.json<{ textureKey: string; textureUrl?: string }>();
}
