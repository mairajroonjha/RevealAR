# RevealAR Architecture

## Runtime Split

```txt
Unity mobile app
├── Real-time AR tracking
├── Plane detection
├── Furniture placement
├── Camera capture
└── Texture rendering

Cloudflare Worker API
├── Auth and request validation
├── Upload orchestration
├── Project persistence
├── AI job creation
└── AI result polling

Cloudflare storage/services
├── R2 for room images, generated textures, and 3D assets
├── D1 for users, projects, prompts, and job history
├── KV for temporary job state
└── Queues for async AI work

External AI/GPU services
├── YOLOv8n or mobile object detector
├── Gemini Flash for multimodal room Q&A
└── Stable Diffusion + ControlNet for texture generation
```

## Important Rule

Heavy AI models should not run inside the Worker. Workers coordinate AI calls, store results, and expose clean APIs to Unity. GPU-heavy tasks should run on a dedicated inference provider.

## First Prototype Flow

```txt
User scans room
↓
Unity detects floor/wall planes
↓
User captures room image
↓
Unity uploads image to Worker
↓
Worker stores image in R2
↓
User saves project metadata to D1
↓
User asks question or requests texture
↓
Worker creates AI call/job
↓
Unity polls result and updates AR scene
```
