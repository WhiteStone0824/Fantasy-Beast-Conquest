"""Audit the generated species PNGs against their prompt records and sidecars."""
from pathlib import Path
import json, struct, sys

root = Path(__file__).resolve().parents[1]
prompts = [json.loads(line) for line in (root / "Docs/SpeciesImagePrompts.jsonl").read_text(encoding="utf-8").splitlines()]
start = int(sys.argv[1]) if len(sys.argv) > 1 else 0
count = int(sys.argv[2]) if len(sys.argv) > 2 else len(prompts)
failures = []
for index in range(start, min(start + count, len(prompts))):
    item = prompts[index]
    image = root / "Assets/Game/Sprites/Generated" / f"{item['speciesId']}.png"
    sidecar = image.with_suffix(".generation.json")
    if not image.is_file() or not sidecar.is_file():
        failures.append({"index": index, "speciesId": item["speciesId"], "error": "missing image or metadata"})
        continue
    try:
        metadata = json.loads(sidecar.read_text(encoding="utf-8"))
        raw = image.read_bytes()
        if raw[:8] != b"\x89PNG\r\n\x1a\n" or raw[12:16] != b"IHDR":
            raise ValueError("invalid PNG header")
        width, height, bit_depth, color_type = struct.unpack(">IIBB", raw[16:26])
        expected = {
            "speciesId": item["speciesId"], "seed": item["seed"],
            "positivePrompt": item["positivePrompt"], "negativePrompt": item["negativePrompt"],
            "checkpoint": item["checkpoint"], "steps": item["steps"], "cfg": item["cfg"],
            "clipSkip": item["clipSkip"], "width": item["width"], "height": item["height"],
        }
        mismatches = [key for key, value in expected.items() if metadata.get(key) != value]
        if mismatches:
            raise ValueError("metadata mismatch: " + ", ".join(mismatches))
        if (width, height, bit_depth, color_type) != (1024, 1024, 8, 6):
            raise ValueError(f"unexpected PNG format: {width}x{height}, bit depth {bit_depth}, color type {color_type}")
    except Exception as error:
        failures.append({"index": index, "speciesId": item["speciesId"], "error": str(error)})
print(json.dumps({"checked": min(count, len(prompts) - start), "passed": min(count, len(prompts) - start) - len(failures), "failures": failures}, ensure_ascii=False))
