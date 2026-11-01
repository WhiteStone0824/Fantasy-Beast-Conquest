"""Replace generic family keyword matches with concrete, family-correct retry prompts."""
from pathlib import Path
import json, sys

root = Path(__file__).resolve().parents[1]
path = root / "Docs/SpeciesImagePrompts.jsonl"
prompts = [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines()]
repairs = {
    4: ("沼ぬしガエル", "amphibian creature with a squat frog body, broad mouth, moist dark green skin, webbed feet and small swamp reeds", "deep green and luminous cyan"),
    6: ("雷雲の子", "small storm elemental creature made of rolling blue-gray clouds, a clear lightning-shaped crest and bright electric eyes", "deep navy and vivid cyan"),
    7: ("歌う花樹", "single flowering tree guardian with a short wooden trunk body, leafy branch arms and distinct violet blossoms", "dark leaf green and violet"),
    15: ("ミズモチ", "compact translucent mint-green slime creature, round jelly body, two large eyes, tiny simple arms and a clean readable silhouette", "jade green and cream"),
    22: ("水鏡ゼリー", "sleek translucent cyan jelly creature with one rounded glassy body, reflective surface, two eyes and a few flowing water tendrils", "deep cyan and silver"),
    29: ("苔ツノ", "small quadruped forest beast with mossy dark green fur, two short stone horns, four sturdy paws and a long curved tail", "charcoal and bright moss green"),
    30: ("黒斑ヒョウ", "sleek black-spotted leopard monster with four agile legs, a long tail, golden eyes and a strong feline silhouette", "gold and black"),
    67: ("苔ヒキガエル", "small squat toad monster with dark moss-green bumpy skin, broad mouth, four short legs and visible webbed toes", "dark moss green and warm ochre"),
    68: ("水辺イモリ", "single smooth-skinned newt monster with a slender curved tail, four short legs, small external gills and bright eyes", "deep teal and luminous cyan"),
    72: ("月影オオサンショウ", "large salamander monster with a long low body, broad head, four short legs, a long tail and dark blue moonlike markings", "midnight blue and silver"),
    97: ("花粉モス", "one large moth insect with two broad patterned wings, a fuzzy thorax, feathery antennae and golden pollen dust", "burnt orange and golden yellow"),
}
for index in [int(value) for value in sys.argv[1:]]:
    prompt = prompts[index]
    name, anatomy, colors = repairs[index]
    rank = prompt["rank"]
    stage = {
        "F": "tiny simple young form", "E": "small developing form", "D": "young form with one distinctive feature",
        "C": "well-developed form with clear family traits", "B": "large mature form with refined details",
        "A": "majestic advanced form with ornate details", "S": "legendary fully matured form with intricate elegant details",
    }[rank]
    positive = (
        "score_9, score_8_up, score_7_up, score_6_up, score_5_up, score_4_up, source_anime, rating_safe, "
        f"solo, one single {name}, {anatomy}, {rank} rank stage, {stage}, {colors}, "
        "distinctive name-inspired design, cute readable fantasy creature, clear face, cohesive anatomy, "
        "complete full body, single subject centered, front three-quarter view, balanced natural pose, "
        "clean crisp cel shading, strong clear silhouette, simple background, white background"
    )
    prompt["positivePrompt"] = positive
    prompt["seed"] += 10000
path.write_text("".join(json.dumps(item, ensure_ascii=False) + "\n" for item in prompts), encoding="utf-8")
print(json.dumps({"repaired_indices": list(repairs)}, ensure_ascii=False))
