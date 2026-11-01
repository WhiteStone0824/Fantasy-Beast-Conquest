"""Increase single-subject clarity and contrast for selected image QA retries."""
from pathlib import Path
import json, sys

root = Path(__file__).resolve().parents[1]
path = root / "Docs/SpeciesImagePrompts.jsonl"
prompts = [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines()]
indices = [int(value) for value in sys.argv[1:]]
for index in indices:
    prompt = prompts[index]
    extra = "strictly one character, isolated single subject, one silhouette, one face, no character sheet, no lineup, no companions"
    if index in {6, 7, 29, 72}:
        extra += ", vivid saturated colors, strong dark outline, high contrast details, clear readable silhouette"
    prompt["positivePrompt"] = prompt["positivePrompt"].replace(
        ", single subject centered,", f", {extra}, single subject centered,", 1
    )
    prompt["seed"] += 10000
path.write_text("".join(json.dumps(item, ensure_ascii=False) + "\n" for item in prompts), encoding="utf-8")
print(json.dumps({"retried": indices}, ensure_ascii=False))
