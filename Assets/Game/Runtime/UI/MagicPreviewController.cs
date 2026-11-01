using System.Collections.Generic;
using FusionAnyMonster.Battle;
using FusionAnyMonster.Save;
using UnityEngine;
using UnityEngine.UI;

namespace FusionAnyMonster.UI
{
    public sealed class MagicPreviewController : MonoBehaviour
    {
        private GameObject world;
        private BattleDemo simulation;
        private Camera previewCamera;
        private RenderTexture output;
        private RawImage view;
        private UnitSaveData unit;
        private SpeciesDefinition species;
        private PersonalityDatabase personalities;
        private MagicGenome genome;
        private BuiltMagic magic;
        private Sprite portrait, targetSprite, groundSprite;
        private Combatant caster;
        private readonly List<Combatant> targets = new List<Combatant>();
        private readonly List<float> targetOffsets = new List<float>();
        private readonly List<LineRenderer> healthBars = new List<LineRenderer>();
        private static Material healthBarMaterial;
        private float nextReplay, elapsed;
        public RenderTexture OutputTexture { get { return output; } }
        public int PlaybackCount { get; private set; }

        public void Initialize(UnitSaveData source, SpeciesDefinition definition, PersonalityDatabase personalityData, MagicGenome selected, Sprite sprite, RawImage targetView)
        {
            Dispose();
            unit=JsonUtility.FromJson<UnitSaveData>(JsonUtility.ToJson(source));
            species=definition; personalities=personalityData; genome=selected.Clone(); portrait=sprite; view=targetView;
            magic=MagicFactory.Build(genome,definition);
            output=new RenderTexture(960,540,24,RenderTextureFormat.ARGB32);
            output.name="Individual magic preview"; output.filterMode=FilterMode.Bilinear; output.Create();
            view.texture=output;
            world=new GameObject("Magic Preview World");
            world.transform.position=new Vector3(10000,10000,0);
            world.layer=2;
            var cameraObject=new GameObject("Magic Preview Camera");
            cameraObject.transform.SetParent(world.transform,false);
            cameraObject.transform.localPosition=new Vector3(0,0,-10);
            previewCamera=cameraObject.AddComponent<Camera>();
            previewCamera.orthographic=true;
            previewCamera.orthographicSize=Mathf.Clamp(Mathf.Max(3.5f,magic.range*.45f,magic.area*1.2f),3.5f,24f);
            previewCamera.aspect=output.width/(float)output.height;
            previewCamera.clearFlags=CameraClearFlags.SolidColor;
            previewCamera.backgroundColor=new Color(.018f,.032f,.055f);
            previewCamera.cullingMask=1<<2;
            previewCamera.targetTexture=output;
            previewCamera.allowHDR=false;previewCamera.useOcclusionCulling=false;
            targetSprite=CreateSprite(true);groundSprite=CreateSprite(false);
            var ground=new GameObject("Practice ground");
            ground.transform.SetParent(world.transform,false);ground.layer=2;
            var renderer=ground.AddComponent<SpriteRenderer>();renderer.sprite=groundSprite;renderer.sortingOrder=-100;
            float height=previewCamera.orthographicSize*2;
            ground.transform.localScale=new Vector3(height*previewCamera.aspect/8f,height/8f,1);
            Replay();
        }

        public void Replay()
        {
            if(world==null)return;
            if(simulation!=null)
            {
                simulation.StopAllCoroutines();
                simulation.gameObject.SetActive(false);
                Destroy(simulation.gameObject);
            }
            targets.Clear();targetOffsets.Clear();healthBars.Clear();elapsed=0;
            var arena=new GameObject("Magic Preview Simulation");
            arena.transform.SetParent(world.transform,false);arena.layer=2;
            simulation=arena.AddComponent<BattleDemo>();simulation.InitializePreview();
            Vector2 origin=(Vector2)world.transform.position;
            caster=simulation.CreatePreviewUnit(unit.displayName,true,origin);
            caster.Configure(unit,species,personalities);
            BattleDemo.ApplyPreviewSprite(caster,portrait);
            float distance=magic.isArea && magic.centerOnCaster?Mathf.Max(.05f,magic.area*.65f):Mathf.Clamp(magic.range*.6f,.05f,3f);
            Vector2 destination=origin+Vector2.right*distance;
            for(int i=0;i<7;i++)
            {
                Vector2 point;
                if(i==0)point=destination;
                else if(magic.isArea)
                    point=destination+Spells.SpellGeometry.Rotate(Vector2.right,i*360f/6f)*Mathf.Max(.2f,magic.area*.6f);
                else point=origin+Spells.SpellGeometry.Rotate(Vector2.right,i*360f/7f)*distance;
                Combatant target=simulation.CreatePreviewUnit(magic.targetsAllies?"練習用の味方":"練習用の的",magic.targetsAllies,point);
                if(magic.targetsAllies){target.Configure(unit,species,personalities);target.SetPreviewHealthRatio(.3f);}
                BattleDemo.ApplyPreviewSprite(target,magic.targetsAllies?portrait:targetSprite);
                targets.Add(target);targetOffsets.Add(0f);
                if(magic.targetsAllies)healthBars.Add(CreateHealthBar(target));
            }
            Spells.SpellExecutor.Cast(simulation,caster,targets[0],magic.program);
            nextReplay=Time.unscaledTime+Mathf.Clamp(magic.duration+magic.projectileCount*.075f+1.2f,3f,25f);
            PlaybackCount++;
        }

        private void Update()
        {
            if(world==null)return;
            if(Time.unscaledTime>=nextReplay){Replay();return;}
            elapsed+=Time.deltaTime;
            for(int i=0;i<targets.Count;i++)
            {
                Combatant target=targets[i];
                float amplitude=magic.trajectory==Spells.SpellTrajectory.Homing?.65f:.2f;
                float offset=Mathf.Sin(elapsed*1.3f+i*.8f)*amplitude;
                if(target!=null && target.CanMoveInPreview)
                    target.transform.position+=Vector3.up*(offset-targetOffsets[i])*target.PreviewMovementScale;
                targetOffsets[i]=offset;
                if(magic.targetsAllies && target!=null && healthBars[i]!=null)
                {
                    healthBars[i].transform.position=target.transform.position+Vector3.up*.55f;
                    healthBars[i].SetPosition(1,new Vector3(Mathf.Lerp(-.35f,.35f,target.HealthRatio),0,0));
                }
            }
        }

        private LineRenderer CreateHealthBar(Combatant target)
        {
            var go=new GameObject("Preview ally health");go.transform.SetParent(simulation.transform,false);go.layer=2;
            var line=go.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=2;line.widthMultiplier=.07f;line.sortingOrder=90;
            if(healthBarMaterial==null)healthBarMaterial=new Material(Shader.Find("Sprites/Default"));
            line.sharedMaterial=healthBarMaterial;line.startColor=line.endColor=new Color(.3f,1f,.5f);
            go.transform.position=target.transform.position+Vector3.up*.55f;
            line.SetPosition(0,new Vector3(-.35f,0,0));line.SetPosition(1,new Vector3(Mathf.Lerp(-.35f,.35f,target.HealthRatio),0,0));
            return line;
        }

        private static Sprite CreateSprite(bool target)
        {
            const int size=128;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            texture.filterMode=FilterMode.Bilinear;
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=(x+.5f-size*.5f)/(size*.5f),dy=(y+.5f-size*.5f)/(size*.5f);
                float radius=Mathf.Sqrt(dx*dx+dy*dy);
                if(target)
                    pixels[y*size+x]=radius>.88f?Color.clear:radius<.18f?new Color(.85f,.23f,.18f):radius<.40f?new Color(.94f,.82f,.53f):radius<.64f?new Color(.63f,.24f,.17f):new Color(.75f,.55f,.31f);
                else pixels[y*size+x]=(x%16==0||y%16==0)?new Color(.08f,.17f,.22f):new Color(.025f,.065f,.09f);
            }
            texture.SetPixels(pixels);texture.Apply(false,true);
            return Sprite.Create(texture,new Rect(0,0,size,size),Vector2.one*.5f,target?128:16);
        }

        private void OnDisable(){Dispose();}
        private void OnDestroy(){Dispose();}
        private void Dispose()
        {
            if(simulation!=null)simulation.StopAllCoroutines();
            if(world!=null){world.SetActive(false);Destroy(world);world=null;}
            simulation=null;previewCamera=null;caster=null;targets.Clear();targetOffsets.Clear();healthBars.Clear();
            if(view!=null)view.texture=null;
            if(output!=null){output.Release();Destroy(output);output=null;}
            DestroySprite(targetSprite);DestroySprite(groundSprite);targetSprite=null;groundSprite=null;
        }
        private static void DestroySprite(Sprite sprite)
        {
            if(sprite==null)return;
            Texture2D texture=sprite.texture;Destroy(sprite);if(texture!=null)Destroy(texture);
        }
    }
}
