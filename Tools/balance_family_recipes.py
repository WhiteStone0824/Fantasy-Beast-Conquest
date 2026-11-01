"""Offline authoring utility; the game executes the resulting fixed table, never this optimizer."""
from pathlib import Path
from collections import Counter
import json, re, random, math, heapq

ROOT=Path(__file__).resolve().parents[1]
source=ROOT/'Assets/Game/Runtime/Breeding/FamilyBreedingCatalog.cs'
enums=(ROOT/'Assets/Game/Runtime/Core/GameEnums.cs').read_text(encoding='utf-8-sig')
families=[s.strip() for s in re.search(r'enum SpeciesFamily\s*\{([^}]+)',enums).group(1).split(',')]
names=json.loads('['+re.search(r'string\[\] Names=\{([^}]+)',source.read_text(encoding='utf-8-sig')).group(1)+']')
baseline_path=ROOT/'Tools/FamilyRecipeBaseline.json'
if not baseline_path.exists():
    rows=[re.findall(r'F\.(\w+)',r) for r in re.findall(r'new\[\]\{([^}]+)\}',source.read_text(encoding='utf-8-sig'))]
    baseline_path.write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
rows=json.loads(baseline_path.read_text(encoding='utf-8'))
old={(a,b):families.index(rows[a][b-a]) for a in range(30) for b in range(a,30)}

descriptions=[
 'liquid soft amorphous water small life', 'animal land fur agile body life', 'animal wing air small life',
 'animal scale land armor life', 'animal water land soft life', 'animal water fluid life',
 'animal small wing armor nature life', 'animal armor water life', 'nature growth land life',
 'nature growth decay small soft life', 'human mind body tool life', 'human body large land stone life',
 'object earth tool body', 'object earth stone crystal', 'object metal tool motion mind',
 'life object body mind amorphous', 'magic object mind energy', 'animal scale wing large magic life',
 'animal magic sacred body life', 'human wing small nature magic life', 'magic energy nature fluid',
 'human wing sacred light magic', 'human wing dark magic', 'body death decay magic',
 'death energy amorphous magic', 'object death dark magic', 'dark amorphous energy magic',
 'star air energy magic crystal light', 'dark water death amorphous magic', 'amorphous magic life energy mind'
]
tags=[set(s.split()) for s in descriptions]
def has(t,*s):return all(x in t for x in s)
def anyof(t,*s):return any(x in t for x in s)
def eligible(a,b,r):
    if r in (a,b):return True
    t=tags[a]|tags[b]
    return [
        lambda: has(t,'amorphous') and anyof(t,'water','life','soft') or has(t,'water','soft') or has(t,'fluid','life'),
        lambda: has(t,'animal') and anyof(t,'land','body','fur','earth'),
        lambda: has(t,'wing') and anyof(t,'animal','air','nature'),
        lambda: has(t,'scale') or has(t,'armor') and anyof(t,'animal','life') or has(t,'animal','stone'),
        lambda: has(t,'water') and anyof(t,'land','soft','nature') and anyof(t,'animal','life'),
        lambda: has(t,'water') and anyof(t,'animal','life'),
        lambda: has(t,'wing') and anyof(t,'small','nature','armor') or has(t,'small','armor'),
        lambda: has(t,'armor') and anyof(t,'water','stone','metal') or has(t,'water') and anyof(t,'stone','metal','object') and anyof(t,'animal','life'),
        lambda: has(t,'nature') and anyof(t,'growth','earth','water','magic') or has(t,'life','land','earth'),
        lambda: has(t,'decay') and anyof(t,'growth','nature','life','water') or has(t,'nature','soft'),
        lambda: has(t,'human') and anyof(t,'body','life','animal','tool','mind'),
        lambda: has(t,'large') and anyof(t,'body','earth','metal') or has(t,'human','stone') or has(t,'body','earth') or has(t,'object','magic','stone'),
        lambda: has(t,'object') and anyof(t,'earth','body','tool'),
        lambda: anyof(t,'crystal','stone') and anyof(t,'object','earth','energy','metal') or has(t,'earth','energy'),
        lambda: has(t,'object') and anyof(t,'metal','motion','mind'),
        lambda: has(t,'life','object') or has(t,'body','magic','mind'),
        lambda: has(t,'object','magic') or has(t,'mind','energy','tool'),
        lambda: has(t,'scale') and anyof(t,'wing','large','magic') or has(t,'animal','large','magic') or has(t,'animal','wing') and anyof(t,'large','stone','metal','object') or has(t,'armor','magic') and anyof(t,'animal','life'),
        lambda: has(t,'animal') and anyof(t,'sacred','magic'),
        lambda: has(t,'nature','magic') and anyof(t,'small','human','wing'),
        lambda: has(t,'energy','magic') and anyof(t,'nature','fluid','air','water'),
        lambda: has(t,'sacred','magic') and anyof(t,'human','wing','light') or has(t,'light') and anyof(t,'human','wing','sacred') or has(t,'nature','human','energy','magic'),
        lambda: has(t,'dark','magic') and anyof(t,'human','animal','mind'),
        lambda: has(t,'death') and anyof(t,'body','animal','decay','growth'),
        lambda: has(t,'death') and anyof(t,'energy','air','amorphous'),
        lambda: has(t,'object') and anyof(t,'dark','death','magic'),
        lambda: has(t,'dark') and anyof(t,'energy','amorphous','air'),
        lambda: has(t,'star') and anyof(t,'energy','crystal','air','magic') or has(t,'light','crystal') or has(t,'magic','energy') and anyof(t,'wing','crystal','sacred') or has(t,'magic','crystal'),
        lambda: has(t,'dark') and anyof(t,'water','death','amorphous'),
        lambda: has(t,'light','dark') or has(t,'star','death') or has(t,'amorphous','magic') and anyof(t,'mind','life')
    ][r]()

locks={(a,a):a for a in range(30)}
for a,b,r in [('Spirit','Material','Arcane'),('Beast','Machine','ArtificialLife'),('Plant','DemiHuman','Fairy'),
              ('Beast','Reptile','Dragon'),('Ghost','Material','CursedObject'),('Angel','Demon','Chaos'),
              ('Astral','Abyssal','Chaos'),('Bird','Beast','Beast'),('Slime','Plant','Fungi'),
              ('Mineral','Spirit','Arcane'),('Beast','Dragon','Dragon')]:
    a,b=sorted((families.index(a),families.index(b)));locks[a,b]=families.index(r)

pairs=[(a,b) for a in range(30) for b in range(a+1,30) if (a,b) not in locks]
def cost(a,b,r):
    if not eligible(a,b,r):return 10000
    # Retaining a parent's body plan is plausible, as are compatible traits from both parents.
    overlap_a=len(tags[a]&tags[r])/len(tags[r]);overlap_b=len(tags[b]&tags[r])/len(tags[r])
    score=20-10*(overlap_a+overlap_b)-5*min(overlap_a,overlap_b)
    if r in (a,b):score-=2
    if r==old[a,b]:score-=2
    return max(0,round(score*10))
costs={p:[cost(*p,r) for r in range(30)] for p in pairs}

# Min-cost flow: each result needs 14 or 15 off-diagonal recipes, plus its own diagonal.
N=1+len(pairs)+30+1;start=0;end=N-1;graph=[[] for _ in range(N)]
def edge(a,b,cap,cost):
    graph[a].append([b,cap,cost,len(graph[b])]);graph[b].append([a,0,-cost,len(graph[a])-1])
locked=Counter(r for (a,b),r in locks.items() if a!=b)
for i,p in enumerate(pairs):
    edge(start,1+i,1,0)
    for r,c in enumerate(costs[p]):
        if c<10000:edge(1+i,1+len(pairs)+r,1,c)
for r in range(30):
    edge(1+len(pairs)+r,end,14-locked[r],0);edge(1+len(pairs)+r,end,1,100000)
potential=[0]*N
for flow in range(len(pairs)):
    dist=[10**12]*N;dist[start]=0;previous=[None]*N;queue=[(0,start)]
    while queue:
        d,v=heapq.heappop(queue)
        if d!=dist[v]:continue
        for i,e in enumerate(graph[v]):
            w,cap,c,_=e
            nd=d+c+potential[v]-potential[w]
            if cap and nd<dist[w]:dist[w]=nd;previous[w]=(v,i);heapq.heappush(queue,(nd,w))
    if previous[end] is None:raise RuntimeError('Plausible candidates cannot satisfy balanced quotas')
    for v in range(N):
        if dist[v]<10**12:potential[v]+=dist[v]
    v=end
    while v!=start:
        parent,i=previous[v];e=graph[parent][i];e[1]-=1;graph[v][e[3]][1]+=1;v=parent
assignment=dict(locks)
for i,p in enumerate(pairs):
    chosen=[e[0]-1-len(pairs) for e in graph[1+i] if 1+len(pairs)<=e[0]<end and e[1]==0]
    assert len(chosen)==1;assignment[p]=chosen[0]
assert set(Counter(assignment.values()).values())<={15,16}

# Swap equally weighted recipes to diversify each parental row without changing global quotas.
row_counts=[[0]*30 for _ in range(30)]
for (a,b),r in assignment.items():
    row_counts[a][r]+=1
    if b!=a:row_counts[b][r]+=1
def penalty(c):return c*c*3+max(0,c-3)**2*80
rng=random.Random(20261028)
for iteration in range(1500000):
    p,q=rng.sample(pairs,2);x,y=assignment[p],assignment[q]
    if x==y or costs[p][y]>=10000 or costs[q][x]>=10000:continue
    changes=Counter()
    for row in p:changes[row,x]-=1;changes[row,y]+=1
    for row in q:changes[row,y]-=1;changes[row,x]+=1
    delta=(costs[p][y]+costs[q][x]-costs[p][x]-costs[q][y])
    delta+=sum(penalty(row_counts[a][r]+d)-penalty(row_counts[a][r]) for (a,r),d in changes.items())
    for row in set(p+q):
        before=sum(c>0 for c in row_counts[row])
        after=before+sum((row_counts[row][r]+d>0)-(row_counts[row][r]>0) for (a,r),d in changes.items() if a==row)
        delta+=40*(max(0,16-after)**2-max(0,16-before)**2)
    temperature=max(.15,18*(1-iteration/1500000)**3)
    if delta<=0 or rng.random()<math.exp(-delta/temperature):
        assignment[p],assignment[q]=y,x
        for (a,r),d in changes.items():row_counts[a][r]+=d

counts=Counter(assignment.values());assert set(counts.values())<={15,16}
assert all(assignment[p]==r for p,r in locks.items())
new_rows=['            new[]{'+','.join('F.'+families[assignment[a,b]] for b in range(a,30))+'},' for a in range(30)]
text=source.read_text(encoding='utf-8-sig');a=text.index('        private static readonly F[][] Results=');b=text.index('        public static F Resolve',a)
text=text[:a]+'        private static readonly F[][] Results={\n'+'\n'.join(new_rows)+'\n        };\n'+text[b:]
source.write_text(text,encoding='utf-8')

report={'seed':20261028,'recipes':465,'changes':sum(old[p]!=r for p,r in assignment.items()),
        'countsBefore':{names[r]:Counter(old.values())[r] for r in range(30)},
        'countsAfter':{names[r]:counts[r] for r in range(30)},
        'rowDiversity':{names[a]:{'distinctResults':sum(c>0 for c in row_counts[a]),'largestRepetition':max(row_counts[a])} for a in range(30)}}
(ROOT/'Docs/Verification/FamilyRecipeBalance.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
lines=['# 系統配合表の重複・偏り調整','',
       '組み合わせごとの結果は固定し、全465レシピで各系統が結果になる回数を15～16通りに揃えました。465÷30＝15.5のため、整数の表で可能な最小の差です。',
       '', '同系統30通りと既存の代表レシピを維持し、姿・生息環境・素材・生命・霊性の関連性を候補条件にしています。その範囲内で全体の回数と、親の片方を固定した行の重複を抑えました。各レシピの考え方は[全一覧](BreedingFamilyTable.md)と[レシピCSV](BreedingFamilyRecipes.csv)に記載しています。',
       '', 'ランタイムは保存済みの表を参照します。オフラインの割り当てツールはゲーム中には実行しません。親の順序、ランク、個体値、魔法遺伝の規則は維持します。',
       '', '| 結果系統 | 変更前 | 変更後 | 465通り中の割合 | 固定した親の結果種類数 | 同じ結果の最大回数 |',
       '|---|---:|---:|---:|---:|---:|']
for r,name in enumerate(names):
    info=report['rowDiversity'][name]
    lines.append(f'|{name}|{report["countsBefore"][name]}|{counts[r]}|{counts[r]/465*100:.2f}%|{info["distinctResults"]}|{info["largestRepetition"]}|')
lines += ['', '「割合」は全465組を均等に選ぶ場合です。親A・Bを独立に30系統から均等に選ぶ900通りの数え方でも、各結果は29～31通り（約3.22～3.44%）です。実際のプレイでは所持している親の系統構成によって偏りが変わります。',
          '', f'変更したレシピは{report["changes"]}通りです。固定した親1系統について、結果は{min(i["distinctResults"] for i in report["rowDiversity"].values())}～{max(i["distinctResults"] for i in report["rowDiversity"].values())}種類、同じ結果は最大{max(i["largestRepetition"] for i in report["rowDiversity"].values())}通りです。',
          '', '[30×30早見表](BreedingFamilyMatrix.csv)／[集計JSON](Verification/FamilyRecipeBalance.json)',
          '', '調整用ツールは`Tools/balance_family_recipes.py`です。標準ライブラリだけで動き、意味的な候補条件・出現数制約・重複ペナルティから固定シードで表を生成します。生成後はUnityの`Fusion Any Monster > Breeding > Apply Family Combination Table`でアセットと一覧を更新してください。']
(ROOT/'Docs/BreedingFamilyBalance.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
print(json.dumps(report,ensure_ascii=False,indent=2))
