using TMPro;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FusionAnyMonster.Battle
{
    /// <summary>Tactical capture selection. Combat pauses while the player inspects an enemy.</summary>
    public sealed class BattleCaptureUI : MonoBehaviour
    {
        private BattleDemo battle;private BattleHud hud;private RectTransform panel;
        private TMP_Text title,details,message,quota;private Image portrait;private Button attempt,weaken;
        private Combatant selected;private LineRenderer selectionRing;private Material ringMaterial;
        public bool IsOpen {get;private set;}
        public Combatant Selected {get{return selected;}}

        public void Initialize(BattleDemo owner,BattleHud presentation,RectTransform root,TMP_FontAsset font)
        {
            battle=owner;hud=presentation;
            panel=Rect("Capture Panel",root,new Vector2(.70f,.225f),new Vector2(.985f,.79f));
            var background=panel.gameObject.AddComponent<Image>();background.color=new Color(.02f,.055f,.085f,1);background.raycastTarget=true;
            title=Text("Capture heading","敵を捕獲する",new Vector2(.05f,.85f),new Vector2(.95f,.98f),24,font);
            portrait=Rect("Capture portrait",panel,new Vector2(.28f,.60f),new Vector2(.72f,.84f)).gameObject.AddComponent<Image>();portrait.preserveAspect=true;portrait.raycastTarget=false;
            details=Text("Capture target details","",new Vector2(.05f,.35f),new Vector2(.95f,.55f),18,font);
            message=Text("Capture message","",new Vector2(.05f,.25f),new Vector2(.95f,.35f),16,font);
            quota=Text("Capture quota","",new Vector2(.05f,.02f),new Vector2(.95f,.10f),14,font);
            attempt=Button("捕獲する",new Vector2(.05f,.11f),new Vector2(.63f,.21f),font,AttemptCapture);
            Button("再開",new Vector2(.67f,.11f),new Vector2(.95f,.21f),font,Close);
            weaken=Button("手加減して再開",new Vector2(.05f,.215f),new Vector2(.95f,.285f),font,()=>
            {
                if(selected!=null && battle.CaptureFocus==selected){battle.CancelCaptureFocus();message.text="手加減を解除しました。";Refresh();return;}
                if(battle.FocusForCapture(selected))Close();else message.text="この敵は捕獲対象にできません。";
            });
            // Keep explanatory text above the extra action row.
            message.rectTransform.anchorMin=new Vector2(.05f,.29f);message.rectTransform.anchorMax=new Vector2(.95f,.37f);
            details.rectTransform.anchorMin=new Vector2(.05f,.38f);details.rectTransform.anchorMax=new Vector2(.95f,.57f);
            panel.gameObject.SetActive(false);
            var marker=new GameObject("Capture target ring");marker.transform.SetParent(transform,false);
            selectionRing=marker.AddComponent<LineRenderer>();selectionRing.useWorldSpace=false;selectionRing.loop=true;selectionRing.positionCount=40;
            ringMaterial=new Material(Shader.Find("Sprites/Default"));selectionRing.sharedMaterial=ringMaterial;
            selectionRing.startColor=selectionRing.endColor=new Color(1,.8f,.3f);selectionRing.widthMultiplier=.045f;selectionRing.sortingOrder=500;
            for(int i=0;i<40;i++){float angle=i*Mathf.PI*2/40;selectionRing.SetPosition(i,new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0));}
            selectionRing.gameObject.SetActive(false);
        }
        public void Toggle(){if(IsOpen)Close();else Open();}
        public void Open()
        {
            if(battle==null || battle.HasEnded || hud.IsPaused)return;
            IsOpen=true;Time.timeScale=0;panel.gameObject.SetActive(true);hud.CaptureModeChanged(true);
            message.text="敵を選択してください（戦闘停止中）";Select(battle.CaptureFocus);
        }
        public void Close()
        {
            if(!IsOpen)return;
            IsOpen=false;selected=null;
            if(panel!=null)panel.gameObject.SetActive(false);
            if(selectionRing!=null)selectionRing.gameObject.SetActive(false);
            Time.timeScale=battle!=null && !battle.HasEnded?battle.BattleSpeed:1;
            if(hud!=null)hud.CaptureModeChanged(false);
        }
        private void Update()
        {
            if(!IsOpen){ShowRing(battle==null?null:battle.CaptureFocus);return;}
            if(battle==null || battle.HasEnded){Close();return;}
            if(Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame){Close();return;}
            if(selected!=null && (!selected.IsAlive || !selected.gameObject.activeInHierarchy))Select(null);
            var mouse=Mouse.current;
            if(mouse==null || !mouse.leftButton.wasPressedThisFrame || Camera.main==null)return;
            if(EventSystem.current!=null && EventSystem.current.IsPointerOverGameObject())return;
            Vector2 pointer=mouse.position.ReadValue();Combatant closest=null;float distance=48*48;
            foreach(var enemy in battle.Enemies)
            {
                if(enemy==null || !enemy.IsAlive || !enemy.gameObject.activeInHierarchy || enemy.WildRecord==null)continue;
                Vector3 screen=Camera.main.WorldToScreenPoint(enemy.transform.position);
                if(screen.z<=0 || screen.x<0 || screen.x>Screen.width || screen.y<0 || screen.y>Screen.height)continue;
                float sq=((Vector2)screen-pointer).sqrMagnitude;if(sq<distance){distance=sq;closest=enemy;}
            }
            Select(closest);
        }
        public void Select(Combatant target)
        {
            selected=target!=null && !target.IsFriendly && target.IsAlive && battle.Enemies.Contains(target)?target:null;
            message.text=selected==null?"敵を選択してください（戦闘停止中）":selected.CaptureAttempted?"この敵には再挑戦できません。":selected.HealthRatio>CaptureRules.Current.healthThreshold?"「手加減」でHPを1残せます。":"捕獲の準備ができています。";
            Refresh();
        }
        private void ShowRing(Combatant target)
        {
            if(selectionRing==null)return;
            bool show=target!=null && target.IsAlive && target.gameObject.activeInHierarchy;
            selectionRing.gameObject.SetActive(show);if(!show)return;
            selectionRing.transform.position=target.transform.position+Vector3.forward*-.1f;
            selectionRing.transform.localScale=Vector3.one*Mathf.Max(.5f,target.transform.localScale.x*.6f);
        }
        private void Refresh()
        {
            quota.text="今回の捕獲 "+battle.CapturedUnits.Count+" / "+battle.CaptureLimit+"体";
            portrait.enabled=selected!=null;attempt.interactable=false;weaken.interactable=false;ShowRing(selected);
            weaken.GetComponentInChildren<TMP_Text>().text="手加減して再開";
            if(selected==null){title.text="敵を捕獲する";details.text="敵を選択してください\nHP "+Mathf.Ceil(CaptureRules.Current.healthThreshold*100)+"%以下で挑戦できます";return;}
            var visual=selected.GetComponent<SpriteRenderer>();portrait.sprite=visual==null?null:visual.sprite;
            var record=selected.WildRecord;title.text=selected.CharacterName;
            float chance=CaptureRules.Current.Chance(selected.HealthRatio,selected.Type==EnemyArchetype.Boss);
            string state=record==null?"捕獲できない敵です":selected.CaptureAttempted?"この敵は挑戦済みです":chance<=0?"もっと弱らせてください":"捕獲成功率 "+Mathf.Ceil(chance*100)+"%";
            details.text=(record==null?"": "Lv."+record.level+"　ランク "+record.rank+"\n")+"HP "+Mathf.Ceil(selected.CurrentHp)+" / "+Mathf.Ceil(selected.MaximumHp)+"\n"+state;
            attempt.interactable=record!=null && chance>0 && !selected.CaptureAttempted && battle.CanCapture;
            weaken.interactable=record!=null && !selected.CaptureAttempted && battle.CanCapture;
            if(battle.CaptureFocus==selected)weaken.interactable=true;
            weaken.GetComponentInChildren<TMP_Text>().text=battle.CaptureFocus==selected?"手加減を解除":"手加減して再開";
        }
        private void AttemptCapture()
        {
            var outcome=battle.TryCapture(selected);message.text=outcome.message;
            if(outcome.status==CaptureStatus.Success)selected=null;
            Refresh();
        }
        private void OnDisable(){Close();}
        private void OnDestroy(){if(ringMaterial!=null)Destroy(ringMaterial);}
        private static RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max)
        {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rect=go.GetComponent<RectTransform>();rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;}
        private TMP_Text Text(string name,string value,Vector2 min,Vector2 max,float size,TMP_FontAsset font)
        {var text=Rect(name,panel,min,max).gameObject.AddComponent<TextMeshProUGUI>();text.text=value;text.font=font;text.fontSize=size;text.enableAutoSizing=true;text.fontSizeMin=10;text.fontSizeMax=size;text.lineSpacing=20;text.alignment=TextAlignmentOptions.Center;text.color=new Color(.92f,.97f,1);text.raycastTarget=false;return text;}
        private Button Button(string label,Vector2 min,Vector2 max,TMP_FontAsset font,UnityEngine.Events.UnityAction action)
        {var rect=Rect("Button "+label,panel,min,max);var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.08f,.28f,.31f);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(action);var parent=panel;panel=rect;Text("Label",label,Vector2.zero,Vector2.one,17,font);panel=parent;return button;}
    }
}
