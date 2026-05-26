# RevealAR

RevealAR is an AI-powered mobile AR interior design assistant. The app scans a real room, detects surfaces and objects, lets users place 3D furniture, generates wall textures from text prompts, and answers design questions about the room.

## Repository Layout

```txt
apps/worker       Cloudflare Worker API
unity/Assets      Unity scripts for AR and backend integration
docs              Architecture and implementation notes
```

## What Is Built First

This starter includes:

```txt
Cloudflare Worker API
D1 database schema
R2 upload endpoint for room images
Project save/load endpoints
AI room question endpoint placeholder
Queued texture-generation job endpoint
Unity API client
Unity AR furniture placement controller
Unity room camera capture helper
Unity wall texture applier
```

## Backend Setup

From `apps/worker`:

```bash
npm install
npm run dev
```

Create Cloudflare resources before deploying:

```bash
wrangler r2 bucket create revealar-room-assets
wrangler d1 create revealar-db
wrangler kv namespace create JOB_CACHE
wrangler queues create revealar-ai-jobs
```

Then update `apps/worker/wrangler.jsonc` with the generated D1 and KV IDs.

Apply the database schema:

```bash
npm run db:migrate:local
```

## Backend Endpoints

```txt
GET  /health
POST /uploads/room-image
POST /projects
GET  /projects/:id
POST /ai/room-question
POST /ai/texture-jobs
GET  /ai/jobs/:id
```

## Unity Setup

Create a Unity mobile project and install:

```txt
AR Foundation
ARCore XR Plugin
ARKit XR Plugin
XR Plugin Management
```

Copy or keep the scripts under:

```txt
unity/Assets/Scripts/RevealAR
```

Add these components to your AR scene:

```txt
AR Session
XR Origin / AR Session Origin
AR Camera
AR Plane Manager
AR Raycast Manager
RevealARApiClient
ARFurniturePlacementController
RoomCameraCapture
WallTextureApplier
RevealARDemoController
```

Create a simple Canvas with buttons and wire them to `RevealARDemoController`:

```txt
Capture Room        -> CaptureAndUploadRoom
Save Project        -> SaveProject
Ask AI              -> AskRoomQuestion
Generate Texture    -> CreateTextureJob
Rotate Left         -> RotateSelectedLeft
Rotate Right        -> RotateSelectedRight
Scale Up            -> ScaleSelectedUp
Scale Down          -> ScaleSelectedDown
Remove              -> RemoveSelected
```

Optional UI fields:

```txt
Question Input      -> questionInput
Texture Prompt      -> texturePromptInput
Status Text         -> statusText
```

For mobile backend testing, set the API base URL in `RevealARApiClient` to:

```txt
https://revealar-api.mirajroonjha.workers.dev
```

For local backend testing in the Unity editor, you can temporarily use:

```txt
http://127.0.0.1:8787
```

For mobile-device testing against a local dev server, use your machine LAN IP or a tunnel URL because `127.0.0.1` points to the phone itself.

## Next Build Steps

1. Create the Unity scene and wire the serialized fields.
2. Add one test furniture prefab.
3. Connect the capture button to `RoomCameraCapture` and `RevealARApiClient.UploadRoomImage`.
4. Connect save/load project buttons.
5. Replace AI placeholders with Gemini Flash and GPU texture-generation service calls.
6. Add object detection, preferably on-device first for lower latency.
