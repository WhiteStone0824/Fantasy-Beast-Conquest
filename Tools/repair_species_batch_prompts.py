"""Build focused retry prompts for visual QA failures in a species batch."""
from pathlib import Path
import json, sys

root = Path(__file__).resolve().parents[1]
path = root / "Docs/SpeciesImagePrompts.jsonl"
prompts = [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines()]
designs = {
    117: ("海底城の守り手", "one crab guardian with a broad deep-sea hard shell, two clear pincers, small expressive eyes, sturdy articulated legs and an amber-and-teal crown-like shell crest", "S", "gold and teal"),
    123: ("蔦巻きローパー", "one compact vine monster with a single green central body, two curling leafy vine arms, rootlike feet, a clear friendly face and several small violet blossoms", "D", "emerald green and violet"),
    130: ("世界樹の若枝", "one mature world-tree creature with a central trunk body and visible face, two branch arms, rootlike legs and one broad leafy crown", "S", "deep leaf green and gold"),
    141: ("腐海のヌメリタケ", "one large brown slimy mushroom monster with a single broad spotted cap, short stout stem body, two small eyes and short root feet", "A", "dark brown and crimson"),
    148: ("角笛の戦士", "(solo:1.5), (1boy:1.3), full body, standing anime game character, one blue-and-gold armored horn warrior holding one brass war horn, expressive face with detailed open eyes looking at viewer, visible limbs and a confident pose, vivid full color cel shading", "D", "deep blue, ivory and gold"),
    151: ("紅蓮の舞い手", "(solo:1.5), (1girl:1.3), full body, standing anime game character, one graceful red dancer in a crimson-and-gold kimono, expressive face with detailed open eyes looking at viewer, visible limbs and a balanced dance pose, vivid full color cel shading", "C", "vivid crimson, charcoal and gold"),
    169: ("星喰い巨人", "one colossal broad-bodied humanoid titan with a clear face, massive arms and legs, dark stone armor and small glowing star marks", "A", "midnight blue and gold"),
    173: ("藁束ドール", "one small cute straw-stuffed doll creature with a complete humanoid body, visible bound straw bundles, stitched face, two arms and two legs", "F", "warm straw gold and dark brown"),
    178: ("青銅の番兵", "one full-body bronze mechanical sentinel with a single helmeted head, plated torso, two arms, two legs and a few visible gear joints", "C", "bronze and midnight blue"),
    191: ("紅玉コウモリ", "one small ruby-bat creature with a single gemlike body, one pair of broad bat wings, tiny feet, pointed ears and a clear face", "D", "ruby red and dark teal"),
    190: ("翡翠カメ", "cute cartoon tortoise animal, four-legged low quadruped, large emerald crystal shell on its back, short sturdy reptile legs, small visible turtle head, short neck and tiny tail", "D", "emerald green and jade"),
    234: ("禁書の番犬", "one cute dark-teal fantasy dog monster, clearly recognizable canine head with two ears, two eyes and a short snout, four sturdy visible legs, one curved tail, small gold spellbook-shaped collar", "C", "deep teal and antique gold"),
    273: ("蜜集めの羽虫", "one chibi honey-collecting fairy insect with a yellow-and-black striped body, two translucent wings, clear eyes and tiny pollen basket", "D", "warm yellow and leaf green"),
    275: ("月灯りウィスプ", "one small floating teal ghost wisp with a compact rounded body, two clear glowing eyes and a simple smiling face, a soft silver moon aura around its single form", "C", "luminous teal and silver"),
    279: ("七色の羽根姫", "one centered full-body fairy princess with a clear face, one pair of large vivid rainbow-feather wings and an elegant colorful dress", "A", "vivid rainbow colors and gold"),
    299: ("黎明の騎士天使", "one centered full-body angel knight with a single helmeted head, clear face, white-and-gold armor, two arms, two legs and one pair of feathered wings", "D", "white, dawn gold and deep blue"),
    333: ("冥界の将軍", "one imposing undead general with a single skull-like face, red-black armor, one helmet, two arms, two legs and a clear full-body silhouette", "A", "crimson and charcoal"),
    337: ("窓辺のゆうれい", "one small floating friendly ghost child with a clear face, two glowing eyes, a simple trailing sheetlike body and a complete centered silhouette", "F", "pale blue and white with navy outline"),
    338: ("迷子の霊", "one small lost spirit with a single rounded translucent body, a clear worried face and two tiny floating hands", "E", "mint green and pale blue"),
    339: ("すすり泣き", "one tiny teardrop-shaped ghost creature with a visible face, closed crying eyes and two clear tear marks", "E", "soft blue and silver"),
    341: ("古井戸の影", "one dark shadow ghost with a single drooping ragged cloak, a clear face with two luminous cyan eyes, a visible floating lower body and strong contrast", "D", "charcoal and luminous cyan"),
    342: ("鈴鳴り幽霊", "one small bell ghost with a single round body, clear friendly face, one brass bell integrated into its head and a soft floating tail", "C", "warm gold and pale blue"),
    343: ("夜汽車の亡霊", "one ghost train conductor with a clear face, a single dark uniform, one lantern and a long translucent lower body", "C", "midnight blue and silver"),
    345: ("白無垢の花嫁霊", "one full-body ghost bride in a flowing white wedding kimono, one clear face, a simple veil and a single soft spectral tail", "B", "ivory white and pale cyan"),
    347: ("千年の怨霊", "one ancient wrathful ghost with a single skull-like face, long tattered midnight-blue robes, two visible arms, bright cyan eyes and a strong centered silhouette", "A", "midnight blue and bright cyan"),
    372: ("月蝕の獣", "one quadruped shadow wolf beast with a clear head, four sturdy legs, a long tail and a bright crescent moon mark on its chest", "B", "charcoal and silver"),
    376: ("常闇の主", "(solo:1.5), one imposing full-body shadow king, single horn-crowned head, bright cyan eyes, dark-violet armor, two arms and two legs, centered large subject", "S", "black, deep violet and luminous cyan"),
}
stages = {"F":"tiny young form", "E":"small developing form", "D":"young form with one distinctive feature", "C":"well-developed form", "B":"large mature form", "A":"majestic advanced form", "S":"legendary fully matured form"}
for index in [int(value) for value in sys.argv[1:]]:
    name, design, rank, colors = designs[index]
    item = prompts[index]
    item["positivePrompt"] = (
        "score_9, score_8_up, score_7_up, score_6_up, score_5_up, score_4_up, source_anime, rating_safe, "
        f"solo, one {name}, {design}, {rank} rank stage, {stages[rank]}, {colors}, "
        "distinctive clear silhouette, cute readable fantasy character, clean cel shading, crisp outline, "
        "complete full body, single subject centered, front three-quarter view, simple background, white background"
    )
    item["seed"] += 10000
path.write_text("".join(json.dumps(item, ensure_ascii=False) + "\n" for item in prompts), encoding="utf-8")
print(json.dumps({"retry_indices": [int(value) for value in sys.argv[1:]]}))
