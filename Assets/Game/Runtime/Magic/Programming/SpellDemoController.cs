using FusionAnyMonster.Battle;
using FusionAnyMonster.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
namespace FusionAnyMonster.Spells
{
    public sealed class SpellDemoController:MonoBehaviour
    {
        private BattleDemo arena;private TMP_Text title,summary;private TMP_FontAsset font;private SpellGenotype current;
        public Combatant Caster {get;private set;}
        private void Start()
        {
            var cameraObject=new GameObject("Spell demo camera");var camera=cameraObject.AddComponent<Camera>();cameraObject.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=5.5f;camera.transform.position=new Vector3(0,0,-10);camera.backgroundColor=new Color(.025f,.045f,.08f);
            font=Resources.Load<TMP_FontAsset>("Fonts & Materials/KH-Dot-Kodenmachou-12-Ki SDF");
            var canvasObject=new GameObject("Spell demo UI",typeof(RectTransform));var canvas=canvasObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvasObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;canvasObject.AddComponent<GraphicRaycaster>();
            if(Object.FindAnyObjectByType<EventSystem>()==null)new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            title=Text(canvas.transform,"GP固有魔法の実行デモ",new Vector2(.03f,.89f),new Vector2(.97f,.99f),26);
            summary=Text(canvas.transform,"",new Vector2(.03f,.73f),new Vector2(.97f,.88f),16);
            string[] labels={"追尾・爆発","分裂・毒","連鎖・麻痺"};
            for(int i=0;i<3;i++){int index=i;var rect=Rect(canvas.transform,new Vector2(.03f+i*.24f,.035f),new Vector2(.25f+i*.24f,.12f));var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.08f,.34f,.42f);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(()=>SetGenes(SpellExamples.Create(index)));Text(rect,labels[i],Vector2.zero,Vector2.one,18).alignment=TextAlignmentOptions.Center;}
            Text(canvas.transform,"仲間が自動発動します。的のHPは検証用に高く設定しています。",new Vector2(.03f,.14f),new Vector2(.97f,.20f),15);
            SetGenes(SpellExamples.Create(0));
        }
        public void SetGenes(SpellGenotype genes)
        {
            string report;SpellGenetics.Express(genes,out report);current=genes.Clone();
            if(arena!=null){arena.gameObject.SetActive(false);Destroy(arena.gameObject);}
            var root=new GameObject("Program spell demonstration");arena=root.AddComponent<BattleDemo>();arena.InitializePreview();root.layer=0;
            var genome=SpellSlotStorage.Prepare(new MagicGenome{programGenes=current},0);
            var data=new UnitSaveData{id="spell-demo",displayName="魔法を使う仲間",rank="S",personality="Calm",baseStats=new float[]{1000,1000,10,20,10,10,2,1,10,50},magicGenomeJson=new[]{JsonUtility.ToJson(genome)}};
            Caster=arena.CreatePreviewUnit(data.displayName,true,new Vector2(-3,0));Caster.gameObject.layer=0;Caster.Configure(data,null,null);Caster.EnableAutonomousDemo();
            var db=Resources.Load<SpeciesDatabase>("SpeciesDatabase");Sprite sprite=db==null?null:db.GetById("demo_00").sprite;BattleDemo.ApplyPreviewSprite(Caster,sprite);
            for(int i=0;i<7;i++){var target=arena.CreatePreviewUnit("的 "+(i+1),false,new Vector2(1.2f+i%3*1.2f,-1.6f+i/3*1.5f));target.gameObject.layer=0;BattleDemo.ApplyPreviewSprite(target,sprite);}
            var built=MagicFactory.Build(genome);if(title!=null)title.text=built.displayName;
            if(summary!=null)summary.text="威力 "+Mathf.Ceil(built.power)+"　MP "+Mathf.Ceil(built.mpCost)+"　再使用 "+Mathf.Ceil(built.cooldown)+"秒\n"+SpellPlayerDescription.Effects(built.program,5)+"\n"+report;
        }
        private TMP_Text Text(Transform parent,string value,Vector2 min,Vector2 max,int size)
        {var rect=Rect(parent,min,max);var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSize=size;text.text=value;text.color=Color.white;text.raycastTarget=false;return text;}
        private static RectTransform Rect(Transform parent,Vector2 min,Vector2 max)
        {var go=new GameObject("Demo control",typeof(RectTransform));var rect=(RectTransform)go.transform;rect.SetParent(parent,false);rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;}
    }
}
