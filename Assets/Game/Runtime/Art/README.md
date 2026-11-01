# ComfyUI Art Integration

The game calls the local ComfyUI HTTP API directly at runtime. It never switches to another image service. A failed request is recorded and the existing placeholder artwork remains usable.

## Setup

1. Start the local ComfyUI at `http://127.0.0.1:8188` (the default endpoint). The bundled `Assets/Game/Art/workflow_api.json` uses Pony Diffusion V6 XL (`ponyDiffusionV6XL_v6StartWithThisOne.safetensors`) with LayerDiffuse SDXL Conv Injection at 1024×1024. Future ComfyUI image generations should use 1024×1024 unless a request specifies another size. It uses the separate `LayeredDiffusionDecode` -> `InvertMask` -> `JoinImageWithAlpha` path for transparent PNG output. Keep the checkpoint, LayerDiffuse node pack, and Conv Injection weights installed.
2. The bundled API workflow is ready to use. If you replace it, export the graph in **API format** to `Assets/Game/Art/workflow_api.json` and update the node IDs in the settings asset.
3. Run **Fusion Any Monster > Setup > Create Master Data and UI Demo**. This creates `Assets/Game/Resources/ComfyUISettings.asset`. You can also create another `ComfyUISettings` asset from **Assets > Create > Fusion Any Monster > ComfyUI Settings**.
4. Configure the endpoint and workflow JSON path. Set the positive/negative prompt node IDs and input names, and the seed node ID/input to match the exported graph. Defaults assume the common `CLIPTextEncode` nodes `6` and `7`, and `KSampler` node `3`.
5. The uGUI bootstraps `ComfyUIGenerationService` and loads `Resources/ComfyUISettings`. On the Magic screen, select a unit and choose **GENERATE SELECTED UNIT SPRITE**. Code may also call `Generate(speciesId, positivePrompt, negativePrompt, seed)`. Subscribe to `GenerationCompleted` and `GenerationFailed` to customize response UI.

The runtime client submits to `/prompt`, checks `/history/{prompt_id}`, and downloads the first available image through `/view`. Generated PNGs are written under `Assets/Game/Sprites/Generated` by default and imported as sprites in the Unity Editor. Batch species art uses `<speciesId>.png` plus `<speciesId>.generation.json` sidecars; the completed catalog contains one 1024×1024 RGBA sprite for each of the 420 species. Prompt JSONL and generation history JSON are written beneath `Application.persistentDataPath/FusionAnyMonster/ComfyUI`.

## Notes

- This integration expects the standard local ComfyUI API endpoints and an API-format workflow object. It does not support partner API workflows or Comfy Cloud.
- Use only locally installed models/nodes and keep the workflow API graph compatible with the running ComfyUI version.
- Sprite import succeeds in the Unity Editor. A standalone player can write the PNG, but cannot create a new imported Unity asset at runtime.
- The default model should be selected in the workflow itself; this client does not silently choose a model.
- On an 8 GB GPU, prefer image workloads and moderate resolutions. See the machine-specific generation skill for model-aware workflow advice.
- Prompt/image history includes species ID, timestamps, prompts, seed, Comfy prompt ID, result status, output path, and error details.
