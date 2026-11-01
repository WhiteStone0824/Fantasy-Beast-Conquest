"""Stage API workflows in the OS temp directory for MCP ComfyUI batch runs."""
from pathlib import Path
import json,sys,tempfile
root=Path(__file__).resolve().parents[1]
if len(sys.argv)==3:
 start,count=map(int,sys.argv[1:3]);indices=range(start,start+count)
else:
 indices=[int(x) for x in sys.argv[1:]]
prompts=[json.loads(x) for x in (root/'Docs/SpeciesImagePrompts.jsonl').read_text(encoding='utf-8').splitlines()]
template=json.loads((root/'Assets/Game/Art/workflow_api.json').read_text(encoding='utf-8'))
folder=Path(tempfile.gettempdir())/'FusionAnyMonsterSpeciesWorkflows';folder.mkdir(exist_ok=True)
out=[]
for index in indices:
 p=prompts[index]
 w=json.loads(json.dumps(template));w['3']['inputs'].update(seed=p['seed'],steps=30,cfg=5.0,sampler_name='euler_ancestral',scheduler='normal');w['6']['inputs']['text']=p['positivePrompt'];w['7']['inputs']['text']=p['negativePrompt'];w['12']['inputs']['filename_prefix']='FusionAnyMonster/Generated/'+p['speciesId']
 path=folder/(p['speciesId']+'.json');path.write_text(json.dumps(w,ensure_ascii=False),encoding='utf-8')
 out.append({'index':index,'speciesId':p['speciesId'],'workflow':str(path)})
print(json.dumps(out,ensure_ascii=True,separators=(',',':')))
