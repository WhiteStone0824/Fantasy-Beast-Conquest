"""Build name/rank-specific Pony V6 prompts for every species master."""
from pathlib import Path
import json,re
root=Path(__file__).resolve().parents[1]
families=['slime monster','beast monster','bird monster','reptilian monster','amphibian monster','aquatic monster','insect monster','crustacean monster','plant monster','mushroom monster','demi-human fantasy character','giant monster','animated material golem','crystal mineral beast','clockwork machine creature','artificial lifeform','magical construct','dragon','mythical beast','fairy creature','elemental spirit','angelic creature','demon','undead creature','ghost spirit','cursed animated object','shadow creature','astral creature','abyssal sea monster','chaotic chimera']
ranks=['F','E','D','C','B','A','S'];looks=['tiny simple young form','small developing form','young form with one distinctive feature','well-developed form with clear family traits','large mature form with refined details','majestic advanced form with ornate details','legendary fully matured form, intricate elegant details']
concepts=[(r'スライム|ゼリー|ぷる|もち|泡|雫|しずく','translucent jelly body'),(r'犬|狼|オオカミ|獣|ライガー|猫|虎|豹|狐|キツネ','feline or canine features'),(r'鳥|鷲|ワシ|鷹|フクロウ|梟|鳳|カラス|ツバメ|羽','feathered wings and beaked face'),(r'蛇|ヘビ|トカゲ|竜|龍|ドラゴン|ワニ|サラマンダー','scales, claws, long reptilian tail'),(r'蛙|カエル|イモリ|サンショウウオ|おたま','amphibian skin and broad expressive eyes'),(r'魚|サメ|クジラ|鯨|クラゲ|マンタ|アンコウ|ハゼ|イワシ|タコ|触手','fins, gills and aquatic anatomy'),(r'虫|蜂|バチ|クワガタ|カブト|トンボ|蝶|アゲハ|クモ|蜘蛛|ムカデ|コガネ|蛍','segmented insect body, antennae and delicate wings'),(r'蟹|カニ|ヤドカリ|シャコ|エビ|ロブスター|鋏|ザリガニ','hard shell and articulated pincers'),(r'花|草|樹|木|蔦|苔|葉|アザミ|ラフレシア|マンドレイク','living leaves, stems and botanical silhouette'),(r'茸|キノコ|傘|胞子|シメジ|マッシュ|エリンギ|タケ','mushroom cap and subtle spore details'),(r'人|民|賢者|勇者|剣士|弓手|戦士|騎士|司書|近衛|姫|大公|女王','expressive fantasy humanoid with species-themed clothing'),(r'巨人|巨兵|タイタン|ゴーレム|オーガ|トロル|巨像|大男','powerful broad build and massive hands'),(r'人形|ドール|番兵|像|兵|時計|機械|機巧|歯車|タンク|大砲|オートマタ','crafted body with visible material joints and mechanical details'),(r'水晶|晶|鉱石|石|宝玉|翡翠|瑪瑙|黒曜|琥珀|磁鉄|金属|鋼|鉄','crystalline mineral body with faceted surfaces'),(r'精|精霊|風|火|水|雷|雲|霧|光|影|星|月|夢|天使|セラフ|悪魔|魔神|幽霊|霊|亡霊|死|呪い','ethereal fantasy anatomy and clear magical silhouette')]
colors=['deep blue and ivory','crimson and charcoal','jade green and cream','violet and silver','amber and dark teal','rose pink and white','black and luminous cyan','gold and midnight blue']
ornaments=['','a few modest natural ornaments','a clear crest or evolved feature','distinctive grown details','subtle ornate markings','elegant armor-like natural ornaments','regal crown-like anatomy and restrained luminous accents']
family_traits=[
 'rounded translucent jelly body, expressive eyes and small fluid details', 'four-legged animal body, expressive muzzle, paws and a clear tail', 'feathered wings, hooked beak and visible talons', 'scaled reptilian body, four limbs, claws and a long tail', 'squat amphibian body, broad mouth, moist skin and webbed feet', 'aquatic body with fins, gills and a clear tail silhouette', 'segmented insect body, antennae, six legs and two clear wings', 'hard shell, articulated legs and distinct pincers', 'single botanical creature formed from a trunk, leaves and blossoms', 'mushroom-shaped body, one clear cap and subtle spore details', 'expressive humanoid with clothing and accessories inspired by its species', 'very large broad-bodied creature with massive hands and sturdy feet', 'animated natural material body with visible seams and solid crafted joints', 'faceted gemstone body with readable mineral planes and a distinct face', 'clockwork creature with visible gears, metal limbs and a coherent body', 'artificial living creature with a clear face and crafted organic form', 'magical construct with a clear central body and contained arcane motifs', 'dragon body with horns, scales, wings or a long serpentine tail', 'mythical beast with a distinctive animal body and elegant fantasy traits', 'small fairy creature with delicate wings and a clear face', 'elemental creature formed from one coherent body of its element', 'angelic creature with luminous feathered wings and a clear face', 'demonic creature with horns, claws and a strong single silhouette', 'undead creature with aged anatomy and restrained spectral details', 'ghostly spirit with a clear central form and soft flowing edges', 'cursed animated object with one recognizable object-shaped body', 'shadow creature with a strong dark silhouette and clear bright eyes', 'astral creature with a defined body and restrained star patterns', 'abyssal creature with deep-sea anatomy, fins and luminous eyes', 'fantasy chimera combining compatible animal traits in one body'
]
family_name_features={
 0:[(r'泡|水|雫|しずく|鏡|ゼリー|もち','water bubbles and a glassy reflective surface'),(r'毒|沼|黒|影','dark tinted jelly and a few eerie markings')],
 1:[(r'苔|樹|森|葉','mossy fur and small leaflike horns'),(r'豹|虎|猫|斑','spotted feline coat and a long balancing tail'),(r'狼|犬|狐|熊','recognizable canine or bear-like muzzle and paws')],
 2:[(r'梟|フクロウ|鷲|鷹','broad feathered wings and a hooked beak'),(r'鳳|炎|火','fiery crest feathers and a proud winged stance')],
 3:[(r'竜|龍|蛇|蜥|トカゲ','serpentine tail, layered scales and small horns'),(r'火|炎','warm glowing scales and a flame-shaped crest')],
 4:[(r'蛙|カエル|ヒキガエル|イモリ|サンショウ','broad amphibian face, short sturdy legs and wet skin'),(r'苔|森','mossy patches across its amphibian skin')],
 5:[(r'魚|鯨|サメ|鯱|海','distinct fins, gills and a streamlined aquatic tail'),(r'月|星|光','subtle luminous markings along its fins')],
 6:[(r'蝶|モス|粉','broad patterned wings, fuzzy thorax and feathery antennae'),(r'蜂|針','striped abdomen and small translucent wings')],
 7:[(r'蟹|カニ|鋏|エビ','large shell and clearly separated pincers'),(r'真珠|珊瑚','pearl-like shell accents and branching coral details')],
 8:[(r'花|桜|蓮','distinct blossoms and a leafy stem body'),(r'苔|樹|木','bark texture, moss patches and leafy branch arms')],
 9:[(r'茸|キノコ|胞子','one broad mushroom cap and a few drifting spores')],
 10:[(r'剣|騎士|戦士','species-themed armor and a clear heroic stance'),(r'姫|女王|大公','ornate species-themed clothing and a regal silhouette')],
 11:[(r'岩|巨|兵|像','monumental scale, thick limbs and heavy natural armor')],
 12:[(r'時計|歯車|機巧|機械','clear mechanical joints and exposed gear details')],
 13:[(r'水晶|晶|宝玉|翡翠','faceted crystal plates and a luminous gem core')],
 14:[(r'時計|歯車|機械|戦車','metal armor panels, visible gears and sturdy mechanical limbs')],
 15:[(r'人形|ドール|ぬい|ゴーレム','crafted seams, a clear face and articulated limbs')],
 16:[(r'魔導|魔法|杖|本','arcane symbols and a restrained magical core')],
 17:[(r'竜|龍|翼','layered scales, horns and a powerful dragon silhouette')],
 18:[(r'麒麟|獅子|鷲|幻','rare beast features combined into one coherent body')],
 19:[(r'花|羽|蝶','delicate wings and tiny flowerlike ornaments')],
 20:[(r'雷|雲|風|火|水','body details clearly shaped by its named element')],
 21:[(r'光|聖|天使|翼','luminous wings and elegant sacred markings')],
 22:[(r'角|魔|炎|冥','horns, claws and dark elemental markings')],
 23:[(r'骸|骨|墓|死','aged bones, tattered details and a readable silhouette')],
 24:[(r'霊|幽|魂|亡','translucent flowing edges and a bright central face')],
 25:[(r'呪|仮面|人形','recognizable cursed object details and restrained eerie marks')],
 26:[(r'影|闇|黒','deep shadow body with contrasting luminous eyes')],
 27:[(r'星|月|天|宇宙','small constellations and a restrained astral glow')],
 28:[(r'深|海|鯨|鮫','deep-sea fins, gills and a few luminous spots')],
 29:[(r'混沌|獣|竜|羽','compatible horns, scales or feathers joined into one anatomy')],
}
def describe_species(name,family):
 base=family_traits[family]
 cues=family_name_features.get(family,[])
 detail=next((description for pattern,description in cues if re.search(pattern,name)),None)
 return f"{base}, {detail}" if detail else base
out=[]
for p in sorted((root/'Assets/Game/Data/Species').glob('*.asset')):
 s=p.read_text(encoding='utf-8');m=re.search(r'^  speciesId: (.*)$',s,re.M)
 if not m:continue
 ident=m.group(1);name=json.loads(re.search(r'^  displayName: (.*)$',s,re.M).group(1));family=int(re.search(r'^  family: (\d+)$',s,re.M).group(1));rank=int(re.search(r'^  rank: (\d+)$',s,re.M).group(1))
 concept=describe_species(name,family)
 color=colors[(family*3+rank+int(ident.endswith('_B')))%len(colors)]
 appearance=', '.join(x for x in (f"solo, one single {name}", families[family], concept, f"{ranks[rank]} rank stage", looks[rank], ornaments[rank], color, "distinctive name-inspired design", "cute readable fantasy creature", "cohesive anatomy", "complete full body") if x)
 pose='single subject centered, isolated full-body character, front three-quarter view, balanced standing pose, calm confident expression, clear face and visible limbs'
 positive=f"score_9, score_8_up, score_7_up, score_6_up, score_5_up, score_4_up, source_anime, rating_safe, {appearance}, {pose}, simple background, white background"
 negative='score_4, score_5, score_6, source_pony, source_furry, 3d, realistic'
 out.append({'speciesId':ident,'name':name,'family':family,'rank':ranks[rank],'positivePrompt':positive,'negativePrompt':negative,'checkpoint':'ponyDiffusionV6XL_v6StartWithThisOne.safetensors','layerDiffuse':'SDXL, Conv Injection','clipSkip':2,'width':1024,'height':1024,'steps':30,'cfg':5.0,'sampler':'euler_ancestral','scheduler':'normal','seed':262900+len(out)})
assert len(out)==420
path=root/'Docs/SpeciesImagePrompts.jsonl'
old={x['speciesId']:x for x in (json.loads(line) for line in path.read_text(encoding='utf-8').splitlines())} if path.exists() else {}
out=[old.get(x['speciesId'],x) if i<100 else x for i,x in enumerate(out)]
path.write_text(''.join(json.dumps(x,ensure_ascii=False)+'\n' for x in out),encoding='utf-8')
print(f'Created {len(out)} prompts: {path}')
