using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FusionAnyMonster.UI;

namespace FusionAnyMonster.Battle
{
    public enum CombatFeedbackKind { Damage, Heal, Mana, LevelUp, Status }

    /// <summary>Scene-owned battle UI. Floating text and world markers are reused and bounded.</summary>
    public sealed class BattleHud : MonoBehaviour
    {
        public const int MaximumFloatingLabels=64;
        private BattleDemo battle;
        private RectTransform root, floatingRoot, markerRoot, partyRoot, bossRoot, modal, spellRoot;
        private TMP_FontAsset font;
        private TMP_Text summary,bossName,banner,modalTitle,spellText,pauseLabel;
        private RectTransform goalFill,bossFill;
        private CanvasGroup bannerGroup;
        private Button pauseButton;
        private Button modalPrimary,modalReturn;
        private readonly List<Marker> markers=new List<Marker>();
        private readonly Queue<Marker> markerPool=new Queue<Marker>();
        private readonly List<PartyCard> party=new List<PartyCard>();
        private readonly List<FloatingLabel> labels=new List<FloatingLabel>();
        private sealed class OcclusionPanel { public RectTransform rect;public CanvasGroup group; }
        private readonly List<OcclusionPanel> occlusionPanels=new List<OcclusionPanel>();
        private readonly List<Rect> characterScreenBounds=new List<Rect>(96);
        private readonly Vector3[] uiCorners=new Vector3[4];
        private const float OccludedAlpha=.35f;
        private readonly StringBuilder textBuffer=new StringBuilder(1024);
        private float nextRefresh,bannerRemaining;
        private int lastWave,sequence;
        private bool paused,resultShown;
        private RectTransform introRoot,introCard;
        private CanvasGroup introGroup;
        private float introElapsed;
        private const float IntroDuration=2.4f;
        private static readonly Color Ink=GameUiTheme.Text,Teal=GameUiTheme.Accent,Gold=GameUiTheme.Gold;
        public bool IsPaused { get { return paused; } }
        public void CaptureModeChanged(bool open){if(pauseLabel!=null)pauseLabel.text=open?"再開":"停止";}
        public int ActiveFloatingLabels { get { int count=0;for(int i=0;i<labels.Count;i++)if(labels[i].active)count++;return count; } }

        private sealed class FloatingLabel
        {
            public RectTransform rect;public TMP_Text text;public Combatant target;public Vector3 position;public Color color;
            public float age,amount;public bool active;public CombatFeedbackKind kind;public string caption;
        }
        private sealed class Marker
        {
            public Combatant unit;public RectTransform rect,hp,mp;public TMP_Text name,status;
        }
        private sealed class PartyCard
        {
            public Combatant unit;public TMP_Text name,hpText,mpText,status;public RectTransform hp,mp;public Image portrait,background;
        }

        public void Initialize(BattleDemo owner)
        {
            if(FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>()==null)
            {
                var events=new GameObject("Battle EventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                events.transform.SetParent(transform,false);
            }
            battle=owner;font=Resources.Load<TMP_FontAsset>("Fonts & Materials/KH-Dot-Kodenmachou-12-Ki SDF");
            if(font==null)font=TMP_Settings.defaultFontAsset;
            root=CreateRect("Battle HUD",transform,Vector2.zero,Vector2.one);
            var canvas=root.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=70;
            var scaler=root.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            root.gameObject.AddComponent<GraphicRaycaster>();
            markerRoot=CreateRect("World Health Bars",root,Vector2.zero,Vector2.one);
            floatingRoot=CreateRect("Combat Numbers",root,Vector2.zero,Vector2.one);
            for(int i=0;i<MaximumFloatingLabels;i++)
            {
                TMP_Text text=Text("Floating combat text",floatingRoot,"",22,Ink,Vector2.zero,Vector2.zero);
                text.rectTransform.anchorMin=text.rectTransform.anchorMax=new Vector2(.5f,.5f);text.rectTransform.sizeDelta=new Vector2(180,36);text.fontStyle=FontStyles.Bold;
                text.gameObject.SetActive(false);labels.Add(new FloatingLabel {rect=text.rectTransform,text=text});
            }
            var info=Panel("Battle Overview",root,new Vector2(.015f,.81f),new Vector2(.285f,.985f));
            TrackOcclusion(info);
            summary=Text("Battle status",info,"",17,Ink,new Vector2(.04f,.22f),new Vector2(.96f,.95f),TextAlignmentOptions.MidlineLeft);
            goalFill=Bar("Survival progress",info,new Vector2(.05f,.09f),new Vector2(.95f,.16f),Teal);
            var controls=Panel("Battle Controls",root,new Vector2(.66f,.90f),new Vector2(.985f,.985f));
            TrackOcclusion(controls);
            for(int i=0;i<3;i++){int speed=i+1;var speedButton=Button(controls,speed+"倍",new Vector2(i*.145f,.13f),new Vector2(i*.145f+.135f,.87f),()=>{if(!IsPaused)battle.ChangeSpeed(speed);});speedButton.interactable=!battle.IsPvp;}
            Button(controls,"魔法",new Vector2(.46f,.13f),new Vector2(.70f,.87f),()=>spellRoot.gameObject.SetActive(!spellRoot.gameObject.activeSelf));
            pauseButton=Button(controls,"停止",new Vector2(.73f,.13f),new Vector2(.99f,.87f),TogglePause);pauseButton.interactable=!battle.IsPvp;pauseLabel=pauseButton.GetComponentInChildren<TMP_Text>();
            bossRoot=Panel("Boss Health",root,new Vector2(.31f,.84f),new Vector2(.64f,.975f));
            TrackOcclusion(bossRoot);
            bossName=Text("Boss name",bossRoot,"",18,Gold,new Vector2(.04f,.45f),new Vector2(.96f,.94f));
            bossFill=Bar("Boss HP",bossRoot,new Vector2(.05f,.15f),new Vector2(.95f,.35f),new Color(1,.25f,.36f));bossRoot.gameObject.SetActive(false);
            var bannerRoot=CreateRect("Wave Announcement",root,new Vector2(.30f,.70f),new Vector2(.70f,.79f));
            bannerGroup=bannerRoot.gameObject.AddComponent<CanvasGroup>();bannerGroup.blocksRaycasts=false;
            banner=Text("Wave text",bannerRoot,"",25,Gold,Vector2.zero,Vector2.one);
            partyRoot=CreateRect("Party Status",root,new Vector2(.015f,.015f),new Vector2(.985f,.185f));
            BuildSpellPanel();
            modal=Panel("Battle Modal",root,Vector2.zero,Vector2.one,new Color(.01f,.02f,.04f,.88f));modal.GetComponent<Image>().raycastTarget=true;
            modalTitle=Text("Modal title",modal,"",32,Gold,new Vector2(.16f,.47f),new Vector2(.84f,.69f));
            modalPrimary=Button(modal,"再開",new Vector2(.19f,.30f),new Vector2(.48f,.40f),()=>{if(resultShown)battle.ReturnHome();else TogglePause();});
            modalReturn=Button(modal,"ホームに戻る",new Vector2(.52f,.30f),new Vector2(.81f,.40f),()=>battle.ReturnHome());
            modal.gameObject.SetActive(false);
        }

        private void BuildSpellPanel()
        {
            spellRoot=Panel("Battle Magic Details",root,new Vector2(.64f,.21f),new Vector2(.985f,.88f));
            TrackOcclusion(spellRoot);
            Text("Magic heading",spellRoot,"味方の固有魔法",18,Gold,new Vector2(.04f,.89f),new Vector2(.96f,.99f));
            var viewport=CreateRect("Magic scroll viewport",spellRoot,new Vector2(.04f,.035f),new Vector2(.96f,.88f));viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            spellText=Text("Magic status list",viewport,"",15,Ink,new Vector2(0,1),Vector2.one,TextAlignmentOptions.TopLeft);
            spellText.rectTransform.pivot=new Vector2(.5f,1);spellText.rectTransform.sizeDelta=new Vector2(0,500);spellText.overflowMode=TextOverflowModes.Overflow;
            spellText.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            scroll.content=spellText.rectTransform;spellRoot.gameObject.SetActive(false);
        }

        public void Register(Combatant unit)
        {
            if(root==null || unit==null)return;
            Marker marker=markerPool.Count>0?markerPool.Dequeue():CreateMarker();marker.unit=unit;marker.rect.gameObject.SetActive(true);markers.Add(marker);
            bool ownSide=battle.IsLocalSide(unit.IsFriendly);
            if(ownSide && party.Count<5)
            {
                int index=party.Count;var card=Panel("Party Card "+index,partyRoot,new Vector2(index*.20f,0),new Vector2(index*.20f+.193f,1));
                TrackOcclusion(card);
                var entry=new PartyCard {unit=unit,background=card.GetComponent<Image>()};
                entry.portrait=CreateRect("Portrait",card,new Vector2(.015f,.23f),new Vector2(.23f,.92f)).gameObject.AddComponent<Image>();entry.portrait.preserveAspect=true;entry.portrait.raycastTarget=false;
                entry.name=Text("Name",card,"",14,Ink,new Vector2(.24f,.73f),new Vector2(.98f,.98f),TextAlignmentOptions.MidlineLeft);
                entry.hp=Bar("Health",card,new Vector2(.25f,.49f),new Vector2(.96f,.68f),Teal);entry.mp=Bar("Mana",card,new Vector2(.25f,.25f),new Vector2(.96f,.43f),new Color(.36f,.55f,1));
                entry.hpText=Text("HP",card,"",12,Ink,new Vector2(.25f,.49f),new Vector2(.96f,.68f));entry.mpText=Text("MP",card,"",12,Ink,new Vector2(.25f,.25f),new Vector2(.96f,.43f));
                entry.status=Text("Status",card,"",11,Gold,new Vector2(.03f,.01f),new Vector2(.97f,.22f));party.Add(entry);
            }
        }
        public void RegisterRevived(Combatant unit)
        {
            if(root==null || unit==null)return;
            for(int i=0;i<markers.Count;i++)if(markers[i].unit==unit){markers[i].rect.gameObject.SetActive(true);return;}
            Marker marker=markerPool.Count>0?markerPool.Dequeue():CreateMarker();marker.unit=unit;marker.rect.gameObject.SetActive(true);markers.Add(marker);
        }
        public void BeginExpeditionIntro()
        {
            if(battle==null || battle.IsPvp || root==null)return;
            battle.IntroActive=true;introElapsed=0;
            introRoot=Panel("Expedition Intro",root,Vector2.zero,Vector2.one,new Color(.01f,.025f,.045f,.90f));
            introRoot.GetComponent<Image>().raycastTarget=true;
            introGroup=introRoot.gameObject.AddComponent<CanvasGroup>();
            introCard=Panel("Expedition Intro Card",introRoot,new Vector2(.20f,.25f),new Vector2(.80f,.75f),new Color(.025f,.065f,.095f,.98f));
            Panel("Upper gold rule",introCard,new Vector2(.05f,.91f),new Vector2(.95f,.92f),Gold);
            Panel("Lower gold rule",introCard,new Vector2(.05f,.08f),new Vector2(.95f,.09f),Gold);
            Text("Intro caption",introCard,"仲間とともに、遠征へ",17,Teal,new Vector2(.06f,.79f),new Vector2(.94f,.90f));
            Text("Intro stage",introCard,BattleStage.Name,32,Gold,new Vector2(.08f,.60f),new Vector2(.92f,.79f));
            int count=Mathf.Min(5,battle.Allies.Count);
            for(int i=0;i<count;i++)
            {
                var unit=battle.Allies[i];if(unit==null)continue;
                float width=.15f,gap=.015f,start=(1f-(count*width+(count-1)*gap))*.5f,left=start+i*(width+gap);
                var portrait=CreateRect("Intro ally "+i,introCard,new Vector2(left,.20f),new Vector2(left+width,.58f)).gameObject.AddComponent<Image>();
                var renderer=unit.GetComponent<SpriteRenderer>();portrait.sprite=renderer==null?null:renderer.sprite;portrait.preserveAspect=true;portrait.raycastTarget=false;
            }
            Text("Intro start",introCard,"出撃",19,Ink,new Vector2(.08f,.09f),new Vector2(.92f,.20f));
        }
        private void UpdateExpeditionIntro()
        {
            if(introRoot==null)return;
            introElapsed+=Time.unscaledDeltaTime;
            float fadeIn=Mathf.Clamp01(introElapsed/.3f),fadeOut=Mathf.Clamp01((IntroDuration-introElapsed)/.4f);
            introGroup.alpha=Mathf.Min(fadeIn,fadeOut);
            introCard.anchoredPosition=new Vector2(0,Mathf.Lerp(-28f,0f,Mathf.SmoothStep(0,1,fadeIn)));
            if(introElapsed<IntroDuration)return;
            battle.IntroActive=false;Destroy(introRoot.gameObject);introRoot=null;
        }
        private Marker CreateMarker()
        {
            var rect=CreateRect("Unit marker",markerRoot,Vector2.zero,Vector2.zero);rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(110,49);
            return new Marker {rect=rect,name=Text("Name",rect,"",12,Ink,new Vector2(0,.65f),Vector2.one),hp=Bar("HP",rect,new Vector2(.1f,.46f),new Vector2(.9f,.58f),Teal),mp=Bar("MP",rect,new Vector2(.1f,.30f),new Vector2(.9f,.38f),new Color(.35f,.55f,1)),status=Text("Status",rect,"",11,Gold,Vector2.zero,new Vector2(1,.28f))};
        }

        public void Emit(Combatant unit,CombatFeedbackKind kind,float value=0,string caption=null)
        {
            if(root==null || unit==null || (caption==null && value<=0))return;
            for(int i=0;i<labels.Count;i++)
            {
                FloatingLabel existing=labels[i];
                if(existing.active && existing.target==unit && existing.kind==kind && existing.caption==caption && existing.age<.16f)
                {existing.amount+=value;existing.text.text=FeedbackText(existing);return;}
            }
            FloatingLabel item=null;
            for(int i=0;i<labels.Count;i++)if(!labels[i].active){item=labels[i];break;}
            if(item==null)return;
            item.active=true;item.age=0;item.target=unit;item.kind=kind;item.amount=value;item.caption=caption;
            float horizontal=kind==CombatFeedbackKind.Damage?1.6f:kind==CombatFeedbackKind.Heal || kind==CombatFeedbackKind.Mana?-1.6f:0;
            float vertical=kind==CombatFeedbackKind.Status || kind==CombatFeedbackKind.LevelUp?1.6f:kind==CombatFeedbackKind.Mana?-.2f:.1f;
            item.position=unit.transform.position+Vector3.up*vertical+Vector3.right*(horizontal+((sequence++%3)-1)*.1f);
            item.color=kind==CombatFeedbackKind.Heal?new Color(.35f,1,.5f):kind==CombatFeedbackKind.Mana?new Color(.35f,.75f,1):kind==CombatFeedbackKind.LevelUp?Gold:kind==CombatFeedbackKind.Status?new Color(.85f,.7f,1):unit.IsFriendly?new Color(1,.32f,.35f):Ink;
            item.text.color=item.color;item.text.text=FeedbackText(item);item.rect.gameObject.SetActive(true);
        }
        private static string FeedbackText(FloatingLabel item) { return item.caption??((item.kind==CombatFeedbackKind.Damage?"-":"+")+Number(item.amount)+(item.kind==CombatFeedbackKind.Mana?" MP":"")); }
        public void TogglePause()
        {
            if(battle==null || battle.HasEnded)return;
            paused=!paused;Time.timeScale=paused?0:battle.BattleSpeed;
            modal.gameObject.SetActive(paused);modalTitle.text="一時停止中";pauseLabel.text=paused?"再開":"停止";
            modalReturn.gameObject.SetActive(true);
            modalPrimary.GetComponentInChildren<TMP_Text>().text="再開";
            var primaryRect=(RectTransform)modalPrimary.transform;primaryRect.anchorMin=new Vector2(.19f,.30f);primaryRect.anchorMax=new Vector2(.48f,.40f);
        }
        private void LateUpdate()
        {
            if(battle==null || root==null)return;
            UpdateExpeditionIntro();
            Camera camera=Camera.main;
            UpdateOccludedPanels(camera,Time.unscaledDeltaTime);
            for(int i=markers.Count-1;i>=0;i--)
            {
                Marker marker=markers[i];Combatant unit=marker.unit;
                if(unit==null || !unit.IsAlive){marker.rect.gameObject.SetActive(false);markers.RemoveAt(i);marker.unit=null;markerPool.Enqueue(marker);continue;}
                bool ownSide=battle.IsLocalSide(unit.IsFriendly);
                bool visible=(battle.IsPvp?ownSide:unit.IsFriendly || unit.RecentlyDamaged || unit.Type==EnemyArchetype.Boss) && Position(marker.rect,unit.transform.position+Vector3.up*.85f,camera);
                marker.rect.gameObject.SetActive(visible);if(!visible)continue;
                Fill(marker.hp,unit.HealthRatio);Fill(marker.mp,unit.MaximumMp<=0?0:unit.CurrentMp/unit.MaximumMp);marker.mp.parent.gameObject.SetActive(ownSide);
                marker.name.text=ownSide?unit.CharacterName:"";marker.status.text=unit.StatusSummary;
            }
            float delta=IsPaused?0:Time.unscaledDeltaTime;
            for(int i=0;i<labels.Count;i++)
            {
                FloatingLabel item=labels[i];if(!item.active)continue;item.age+=delta;
                if(item.age>=1.05f){item.active=false;item.target=null;item.rect.gameObject.SetActive(false);continue;}
                bool visible=Position(item.rect,item.position+Vector3.up*item.age*.65f,camera);item.rect.gameObject.SetActive(visible);
                Color color=item.color;color.a=1-Mathf.InverseLerp(.55f,1.05f,item.age);item.text.color=color;
            }
            bannerRemaining=Mathf.Max(0,bannerRemaining-delta);bannerGroup.alpha=Mathf.Clamp01(bannerRemaining);
            if(Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.12f;Refresh();}
        }
        private void TrackOcclusion(RectTransform rect)
        {
            var group=rect.gameObject.AddComponent<CanvasGroup>();
            occlusionPanels.Add(new OcclusionPanel{rect=rect,group=group});
        }
        private void AddCharacterBounds(IReadOnlyList<Combatant> units,Camera camera)
        {
            for(int i=0;i<units.Count;i++)
            {
                var unit=units[i];if(unit==null || !unit.gameObject.activeInHierarchy)continue;
                var sprite=unit.GetComponent<SpriteRenderer>();if(sprite==null || !sprite.enabled || sprite.sprite==null)continue;
                Bounds bounds=sprite.bounds;Vector3 a=camera.WorldToScreenPoint(bounds.min),b=camera.WorldToScreenPoint(bounds.max);
                if(a.z<=0 && b.z<=0)continue;
                characterScreenBounds.Add(UnityEngine.Rect.MinMaxRect(Mathf.Min(a.x,b.x),Mathf.Min(a.y,b.y),Mathf.Max(a.x,b.x),Mathf.Max(a.y,b.y)));
            }
        }
        private void UpdateOccludedPanels(Camera camera,float delta)
        {
            characterScreenBounds.Clear();
            if(camera!=null){AddCharacterBounds(battle.Allies,camera);AddCharacterBounds(battle.Enemies,camera);}
            foreach(var panel in occlusionPanels)
            {
                if(panel.rect==null || !panel.rect.gameObject.activeInHierarchy)continue;
                panel.rect.GetWorldCorners(uiCorners);
                Vector2 a=RectTransformUtility.WorldToScreenPoint(null,uiCorners[0]),b=RectTransformUtility.WorldToScreenPoint(null,uiCorners[2]);
                UnityEngine.Rect area=UnityEngine.Rect.MinMaxRect(Mathf.Min(a.x,b.x),Mathf.Min(a.y,b.y),Mathf.Max(a.x,b.x),Mathf.Max(a.y,b.y));
                bool overlaps=false;
                for(int i=0;i<characterScreenBounds.Count;i++)if(area.Overlaps(characterScreenBounds[i])){overlaps=true;break;}
                panel.group.alpha=Mathf.MoveTowards(panel.group.alpha,overlaps?OccludedAlpha:1f,Mathf.Max(0,delta)*6f);
            }
        }
        private void Refresh()
        {
            int alive=0;for(int i=0;i<battle.Allies.Count;i++)if(battle.Allies[i]!=null && battle.Allies[i].IsAlive)alive++;
            summary.text=battle.IsPvp ? "通信対戦　青：ホスト / 赤：参加者\n経過時間 "+Clock(battle.ElapsedSeconds)+" / "+Clock(battle.GoalSeconds) : BattleStage.Name+"\n生存時間 "+Clock(battle.ElapsedSeconds)+(battle.IsEndless || BattleStage.BossRush ? "" : " / "+Clock(battle.GoalSeconds))+(BattleStage.HideWave?"\n撃破 "+battle.DefeatedCount:"\nウェーブ "+battle.WaveNumber+"　撃破 "+battle.DefeatedCount)+"\n味方 "+alive+"　戦闘速度 "+Number(battle.BattleSpeed)+"倍";
            Fill(goalFill,battle.IsEndless ? 0 : !battle.IsPvp && BattleStage.BossRush ? (battle.HasEnded && battle.WasVictory ? 1f : (battle.WaveNumber-1)/5f) : battle.GoalSeconds<=0?1:battle.ElapsedSeconds/battle.GoalSeconds);
            if(!battle.IsPvp && !BattleStage.HideWave && !battle.IntroActive && lastWave!=battle.WaveNumber){lastWave=battle.WaveNumber;banner.text="ウェーブ "+lastWave+" 開始";bannerRemaining=2.6f;}
            Combatant boss=null;for(int i=0;i<battle.Enemies.Count;i++){Combatant unit=battle.Enemies[i];if(unit!=null && unit.IsAlive && unit.Type==EnemyArchetype.Boss){boss=unit;break;}}
            bossRoot.gameObject.SetActive(boss!=null);if(boss!=null){bossName.text=boss.CharacterName+(boss.UnitData==null?"":" Lv."+boss.UnitData.level)+"　"+Number(boss.CurrentHp)+" / "+Number(boss.MaximumHp);Fill(bossFill,boss.HealthRatio);}
            for(int i=0;i<party.Count;i++)
            {
                PartyCard card=party[i];Combatant unit=card.unit;if(unit==null)continue;
                card.name.text=unit.CharacterName+" Lv."+(unit.UnitData==null?1:unit.UnitData.level);
                card.hpText.text="HP "+Number(unit.CurrentHp)+" / "+Number(unit.MaximumHp);card.mpText.text="MP "+Number(unit.CurrentMp)+" / "+Number(unit.MaximumMp);
                Fill(card.hp,unit.HealthRatio);Fill(card.mp,unit.MaximumMp<=0?0:unit.CurrentMp/unit.MaximumMp);
                card.status.text=unit.IsAlive?(string.IsNullOrEmpty(unit.StatusSummary)?(string.IsNullOrEmpty(unit.PassiveName)?"自動戦闘中":unit.PassiveName):unit.StatusSummary):"戦闘不能";
                card.background.color=!unit.IsAlive?new Color(.12f,.1f,.14f,.95f):unit.HealthRatio<.25f?new Color(.34f,.07f,.09f,.95f):new Color(.025f,.06f,.10f,.96f);
                var renderer=unit.GetComponent<SpriteRenderer>();if(renderer!=null)card.portrait.sprite=renderer.sprite;
            }
            if(spellRoot.gameObject.activeSelf)RefreshSpells();
            if(battle.HasEnded && !resultShown)
            {
                resultShown=true;paused=false;Time.timeScale=1;modal.gameObject.SetActive(true);pauseButton.interactable=false;
                modalTitle.text=battle.IsPvp ? battle.PvpResultText + "\n対戦時間 " + Clock(battle.ElapsedSeconds) : (battle.WasVictory?"遠征成功":"遠征終了")+"\n生存時間 "+Clock(battle.ElapsedSeconds)+"　撃破 "+battle.DefeatedCount+"\n獲得した卵 "+battle.EggsEarned+"個"+(battle.NextStageUnlocked?"\n次の難易度が解放されました！":"")+(battle.ProgressSaveFailed?"\n戦闘結果を保存できませんでした":"");
                modalReturn.gameObject.SetActive(false);
                modalPrimary.GetComponentInChildren<TMP_Text>().text="ホームに戻る";
                var primaryRect=(RectTransform)modalPrimary.transform;primaryRect.anchorMin=new Vector2(.35f,.30f);primaryRect.anchorMax=new Vector2(.65f,.40f);
            }
        }
        private void RefreshSpells()
        {
            textBuffer.Length=0;
            var shownTeam=battle.IsPvp && PvpLanSession.Instance!=null && !PvpLanSession.Instance.IsHost ? battle.Enemies : battle.Allies;
            for(int i=0;i<shownTeam.Count;i++)
            {
                Combatant unit=shownTeam[i];if(unit==null)continue;textBuffer.Append(unit.CharacterName).AppendLine();
                for(int j=0;j<unit.Magics.Count;j++)
                {
                    BuiltMagic magic=unit.Magics[j];textBuffer.Append("固有魔法 ").Append(j+1).Append("　").Append(string.IsNullOrEmpty(magic.displayName)?ElementName(magic.element):magic.displayName).AppendLine();
                    float remaining=unit.MagicCooldownRemaining(j);
                    textBuffer.Append("威力 ").Append(Number(magic.power)).Append("　MP ").Append(Number(magic.mpCost)).Append("　").Append(!magic.available?"使用不可":!unit.IsAlive?"戦闘不能":unit.CurrentMp<magic.mpCost?"MP不足":remaining>0?"準備中":"使用可能");
                    if(remaining>0)textBuffer.Append("　待機 あと").Append(Mathf.CeilToInt(remaining)).Append("秒");
                    textBuffer.AppendLine();
                }
                textBuffer.AppendLine();
            }
            spellText.text=textBuffer.ToString();
        }
        private bool Position(RectTransform rect,Vector3 world,Camera camera)
        {
            if(camera==null)return false;Vector3 screen=camera.WorldToScreenPoint(world);if(screen.z<0)return false;
            Vector2 local;RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,null,out local);
            Vector2 half=rect.rect.size*.5f;
            local.x=Mathf.Clamp(local.x,root.rect.xMin+half.x,root.rect.xMax-half.x);local.y=Mathf.Clamp(local.y,root.rect.yMin+half.y,root.rect.yMax-half.y);rect.anchoredPosition=local;
            return screen.x>=0 && screen.x<=Screen.width && screen.y>=0 && screen.y<=Screen.height;
        }
        private void OnDisable() { if(battle!=null)battle.IntroActive=false;if(paused){paused=false;Time.timeScale=1;} }
        private static string Number(float value) { return Mathf.Ceil(value).ToString("0"); }
        private static string Clock(float value) { int seconds=Mathf.Max(0,Mathf.FloorToInt(value));return (seconds/60).ToString("00")+":"+(seconds%60).ToString("00"); }
        private static string ElementName(AttributeElement value) { return value==AttributeElement.Fire?"火":value==AttributeElement.Water?"水":value==AttributeElement.Wind?"風":value==AttributeElement.Earth?"土":value==AttributeElement.Light?"光":value==AttributeElement.Dark?"闇":"無"; }
        private static void Fill(RectTransform rect,float ratio) { rect.anchorMax=new Vector2(Mathf.Clamp01(ratio),1); }
        private static RectTransform CreateRect(string name,Transform parent,Vector2 min,Vector2 max)
        {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rect=go.GetComponent<RectTransform>();rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;}
        private static RectTransform Panel(string name,Transform parent,Vector2 min,Vector2 max,Color? color=null)
        {var rect=CreateRect(name,parent,min,max);var image=rect.gameObject.AddComponent<Image>();image.color=color??GameUiTheme.Surface;image.raycastTarget=false;if(color==null)GameUiTheme.ApplySurface(image);return rect;}
        private TMP_Text Text(string name,Transform parent,string value,float size,Color color,Vector2 min,Vector2 max,TextAlignmentOptions alignment=TextAlignmentOptions.Center)
        {
            var rect=CreateRect(name,parent,min,max);var text=rect.gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.text=value;text.fontSize=size;text.enableAutoSizing=true;text.fontSizeMin=10;text.fontSizeMax=size;text.color=color;text.alignment=alignment;text.lineSpacing=20;text.overflowMode=TextOverflowModes.Ellipsis;text.raycastTarget=false;
            var outline=rect.gameObject.AddComponent<Shadow>();outline.effectColor=new Color(0,0,0,.8f);outline.effectDistance=new Vector2(1,-1);return text;
        }
        private RectTransform Bar(string name,Transform parent,Vector2 min,Vector2 max,Color color)
        {var bar=Panel(name,parent,min,max,new Color(.01f,.02f,.04f));return Panel("Fill",bar,Vector2.zero,Vector2.one,color);}
        private Button Button(Transform parent,string caption,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action)
        {var rect=Panel("Button "+caption,parent,min,max,GameUiTheme.Button);var image=rect.GetComponent<Image>();GameUiTheme.ApplySurface(image);image.raycastTarget=true;var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(action);var colors=button.colors;colors.highlightedColor=new Color(1f,.93f,.75f);colors.pressedColor=new Color(.7f,.85f,.88f);colors.selectedColor=new Color(1f,.85f,.63f);colors.fadeDuration=.12f;button.colors=colors;Text("Label",rect,caption,15,Ink,Vector2.zero,Vector2.one);return button;}
    }
}
