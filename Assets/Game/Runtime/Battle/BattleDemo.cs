using System;
using System.Collections;
using System.Collections.Generic;
using FusionAnyMonster.Save;
using UnityEngine;

namespace FusionAnyMonster.Battle
{
    public enum EnemyArchetype { Normal, Swift, Tank, Ranged, Boss }

    public sealed class BattleDemo : MonoBehaviour
    {
        [SerializeField] private float survivalGoalSeconds = 120f;
        [SerializeField] private float arenaRadius = 9f;
        [SerializeField] private float spawnInterval = 2.2f;
        [SerializeField] private int maxEnemies = 80;
        private readonly List<Combatant> allies = new List<Combatant>();
        private readonly List<Combatant> enemies = new List<Combatant>();
        private float elapsed, spawnTimer;
        private int defeated;
        private bool ended;
        private bool previewMode;
        private bool pvp;
        private float nextPvpSnapshot;
        private double hostDamage,guestDamage;
        private sealed class PvpShotVisual { public SpriteRenderer renderer;public int id;public bool seen;public Vector3 from,to;public float age; }
        private readonly List<PvpShotVisual> pvpProjectiles = new List<PvpShotVisual>();
        private const float PvpSnapshotInterval=.15f;
        private float lastPvpSnapshotTime=-1f;
        public bool IsPvp { get { return pvp; } }
        internal static bool IsLocalPvpSide(bool isHost,bool friendly) { return isHost==friendly; }
        internal bool IsLocalSide(bool friendly)
        { return !pvp?friendly:PvpLanSession.Instance!=null && IsLocalPvpSide(PvpLanSession.Instance.IsHost,friendly); }
        internal bool IntroActive { get; set; }
        public string PvpResultText { get { return result == "VICTORY" ? "勝利" : result == "DEFEAT" ? "敗北" : result == "DISCONNECTED" ? "接続が切断されました" : "引き分け"; } }
        private bool returnToTitleRequested;
        internal bool IsEnded { get { return ended; } }
        private string result = "SURVIVE";
        private GameSaveData save;
        private PersonalityDatabase personalityDatabase;
        internal PersonalityDatabase Personalities { get { return personalityDatabase; } }
        private SpeciesDatabase speciesDatabase;
        private GUIStyle hudStyle, titleStyle;
        private GUIStyle smallHudStyle, spellHudStyle, spellDetailStyle;
        private Vector2 loadoutScrollPosition;
        private bool showMagicLoadout;
        private float battleSpeed = 1f;
        private BattleHud battleHud;
        private Camera battleCamera;
        private int eggsEarned;
        private bool bossSpawned;
        private int bossesDefeated;
        private int bossRushWave = 1;
        private bool bossRushWaveActive = true;
        private readonly List<UnitSaveData> pendingEggs = new List<UnitSaveData>();
        private System.Random equipmentRandom;
        public int EggsEarned { get { return eggsEarned; } }
        public int PendingEggCount { get { return pendingEggs.Count; } }
        public bool ProgressSaveFailed { get; private set; }
        public bool NextStageUnlocked { get; private set; }
        private readonly List<UnitSaveData> capturedUnits=new List<UnitSaveData>();
        private System.Random captureRandom;
        public Combatant CaptureFocus {get;private set;}
        public bool CanCapture {get{return !previewMode && !ended && save!=null && !save.readOnly && save.schemaVersion<=SaveSystem.CurrentSchemaVersion && capturedUnits.Count<CaptureLimit;}}
        public bool FocusForCapture(Combatant target)
        {
            if(!CanCapture || target==null || !target.IsAlive || target.IsFriendly || !target.gameObject.activeInHierarchy || target.WildRecord==null || target.CaptureAttempted || !enemies.Contains(target))return false;
            CaptureFocus=target;return true;
        }
        public void CancelCaptureFocus(){CaptureFocus=null;}
        public IReadOnlyList<UnitSaveData> CapturedUnits {get{return capturedUnits;}}
        public int CaptureLimit {get{return Mathf.Max(1,CaptureRules.Current.maximumCapturesPerBattle);}}
        public CaptureResult TryCapture(Combatant target)
        {
            if(previewMode || ended || target==null || !target.IsAlive || !target.gameObject.activeInHierarchy || target.IsFriendly || !enemies.Contains(target))
                return new CaptureResult{status=CaptureStatus.Unavailable,message="生きている敵を選んでください。"};
            if(captureRandom==null)captureRandom=new System.Random(save==null?0:save.randomSeed ^ save.battlesCompleted);
            GameSaveData committed;
            var outcome=CaptureSystem.TryCapture(save,target.WildRecord,target.HealthRatio,target.Type==EnemyArchetype.Boss,
                target.CaptureAttempted,capturedUnits.Count,captureRandom,CaptureRules.Current,out committed);
            if(outcome.status==CaptureStatus.Failed){target.MarkCaptureAttempted();if(CaptureFocus==target)CaptureFocus=null;}
            if(outcome.status==CaptureStatus.Success)
            {
                save=committed;capturedUnits.Add(outcome.unit);target.MarkCaptured();
                if(CaptureFocus==target || capturedUnits.Count>=CaptureLimit)CaptureFocus=null;
                Feedback(target,CombatFeedbackKind.Status,0,"仲間になった！");StartCoroutine(FadeCaptured(target));
            }
            return outcome;
        }
        private IEnumerator FadeCaptured(Combatant target)
        {
            var visual=target.GetComponent<SpriteRenderer>();float age=0;
            while(target!=null && age<.4f)
            {
                age+=Time.unscaledDeltaTime;
                if(visual!=null){var color=Color.Lerp(Color.white,new Color(.3f,1f,.85f),age/.4f);color.a=1-age/.4f;visual.color=color;}
                yield return null;
            }
            if(target!=null)Destroy(target.gameObject);
        }
        public IReadOnlyList<Combatant> Allies { get { return allies; } }
        public IReadOnlyList<Combatant> Enemies { get { return enemies; } }
        public float ElapsedSeconds { get { return elapsed; } }
        public float GoalSeconds { get { return pvp ? PvpRules.TimeLimitSeconds : survivalGoalSeconds; } }
        public int DefeatedCount { get { return defeated; } }
        public int WaveNumber { get { return BattleStage.BossRush && !pvp ? bossRushWave : 1+Mathf.FloorToInt(elapsed/30f); } }
        public bool IsEndless { get { return !pvp && BattleStage.Endless; } }
        public bool HasEnded { get { return ended; } }
        public bool WasVictory { get { return result=="VICTORY"; } }
        public float BattleSpeed { get { return battleSpeed; } }
        internal bool HasPresentation { get { return battleHud!=null; } }
        public void ChangeSpeed(float value) { if(!ended && !pvp)SetBattleSpeed(Mathf.Clamp(value,1,3)); }
        public void ReturnHome() { if(!ended)End("RETREAT");returnToTitleRequested=true; }
        internal void Feedback(Combatant unit,CombatFeedbackKind kind,float value=0,string label=null) { if(!previewMode && battleHud!=null)battleHud.Emit(unit,kind,value,label); }

        private void Start()
        {
            if (previewMode) return;
            save = SaveSystem.LoadOrCreate();
            personalityDatabase = Resources.Load<PersonalityDatabase>("PersonalityDatabase");
            speciesDatabase = Resources.Load<SpeciesDatabase>("SpeciesDatabase");
            var pvpSession = PvpLanSession.Instance;
            pvp = pvpSession != null && pvpSession.MatchActive;
            CreateArenaVisuals();
            battleHud=gameObject.AddComponent<BattleHud>();battleHud.Initialize(this);
            if (pvp)
            {
                SpawnPvpTeam(pvpSession.HostTeam, true);
                SpawnPvpTeam(pvpSession.GuestTeam, false);
                if (!pvpSession.IsHost) pvpSession.SnapshotReceived += ApplyPvpSnapshot;
                return;
            }
            List<UnitSaveData> formation = save.units.FindAll(u => u != null && u.deployed);
            for (int i = 0; i < formation.Count; i++)
            {
                UnitSaveData record = formation[i];
                Vector2 point = Quaternion.Euler(0f, 0f, i * 360f / formation.Count) * Vector2.up * 1.2f;
                SpeciesDefinition speciesDefinition = ResolveSpecies(record);
                Combatant ally = CreateCombatant(record.displayName, true, point, EnemyArchetype.Normal, new Color(.28f, .83f, 1f));
                ally.Configure(record, speciesDefinition, personalityDatabase);
                Sprite generatedSprite = LoadGeneratedSprite(record.generatedSpritePath);
                if (generatedSprite != null) SetCharacterSprite(ally, generatedSprite);
                else if (speciesDefinition != null) SetCharacterSprite(ally, speciesDefinition.sprite);
                if (speciesDefinition != null) ally.SetCharacterName(speciesDefinition.displayName);
            }
            if(BattleStage.Limited==LimitedStageKind.None && !BattleStage.BossRush && !BattleStage.Endless)survivalGoalSeconds=300f;
            else if(BattleStage.Limited==LimitedStageKind.DailyExperience || BattleStage.Limited==LimitedStageKind.DailySuperExperience)survivalGoalSeconds=60f;
            int initial=BattleStage.Limited==LimitedStageKind.Weekday || BattleStage.Limited==LimitedStageKind.ChallengeBoss?1:BattleStage.Limited==LimitedStageKind.DailyBoss?3:BattleStage.BossRush?1:5;
            for (int i = 0; i < initial; i++) SpawnEnemy();
            battleHud.BeginExpeditionIntro();
        }

        private void SpawnPvpTeam(PvpTeam team, bool friendly)
        {
            if (team == null) return;
            for (int i = 0; i < team.units.Count; i++)
            {
                UnitSaveData record = team.units[i];
                var species = ResolveSpecies(record);
                var unit = CreateCombatant(record.displayName, friendly, new Vector2(friendly ? -3f : 3f, (i - (team.units.Count - 1) * .5f) * 1.1f), EnemyArchetype.Normal, friendly ? Color.cyan : Color.red);
                unit.Configure(record, species, personalityDatabase);
                if (species != null) { SetCharacterSprite(unit, species.sprite); unit.SetCharacterName(species.displayName); }
                if (!PvpLanSession.Instance.IsHost) unit.enabled = false;
            }
        }

        [Serializable] private sealed class PvpPose { public float x, y, hp, mp; public bool alive; }
        [Serializable] private sealed class PvpShot { public int id;public float x, y, size, r, g, b, a; }
        [Serializable] private sealed class PvpSnapshot { public float elapsed; public bool ended; public string result; public PvpPose[] host, guest; public PvpShot[] shots; }
        private static PvpPose[] CaptureTeam(List<Combatant> team)
        {
            var poses = new PvpPose[team.Count];
            for (int i = 0; i < team.Count; i++)
            {
                var u = team[i];
                poses[i] = u == null ? new PvpPose() : new PvpPose { x=u.transform.position.x, y=u.transform.position.y, hp=u.CurrentHp, mp=u.CurrentMp, alive=u.IsAlive };
            }
            return poses;
        }
        private string CreatePvpSnapshot()
        {
            var shots = new List<PvpShot>();
            foreach (var projectile in FindObjectsByType<Spells.SpellProjectile>(FindObjectsSortMode.None))
            {
                if (projectile == null || !projectile.gameObject.activeInHierarchy) continue;
                var renderer = projectile.GetComponent<SpriteRenderer>(); if (renderer == null || !renderer.enabled) continue;
                var c=renderer.color; var p=projectile.transform.position;
                shots.Add(new PvpShot { id=projectile.NetworkVisualId,x=p.x, y=p.y, size=projectile.transform.lossyScale.x, r=c.r, g=c.g, b=c.b, a=c.a });
                if(shots.Count>=256)break;
            }
            return JsonUtility.ToJson(new PvpSnapshot { elapsed=elapsed, ended=ended, result=result, host=CaptureTeam(allies), guest=CaptureTeam(enemies), shots=shots.ToArray() });
        }
        private void ApplyPvpSnapshot(string json)
        {
            if (!pvp || PvpLanSession.Instance == null || PvpLanSession.Instance.IsHost) return;
            try
            {
                var snapshot = JsonUtility.FromJson<PvpSnapshot>(json);
                if (snapshot == null || snapshot.host == null || snapshot.guest == null || snapshot.host.Length != allies.Count || snapshot.guest.Length != enemies.Count || snapshot.shots == null || snapshot.shots.Length > 256) return;
                if(snapshot.elapsed<lastPvpSnapshotTime || ended && !snapshot.ended)return;
                lastPvpSnapshotTime=snapshot.elapsed;
                elapsed = Mathf.Max(0, snapshot.elapsed);
                for (int i=0;i<allies.Count;i++) ApplyPvpPose(allies[i],snapshot.host[i]);
                for (int i=0;i<enemies.Count;i++) ApplyPvpPose(enemies[i],snapshot.guest[i]);
                foreach(var visual in pvpProjectiles)visual.seen=false;
                for (int i=0;i<snapshot.shots.Length;i++)
                {
                    var shot=snapshot.shots[i];if(shot==null)continue;
                    PvpShotVisual visual=null;foreach(var candidate in pvpProjectiles)if(candidate.renderer.gameObject.activeSelf && candidate.id==shot.id){visual=candidate;break;}
                    if(visual==null){foreach(var candidate in pvpProjectiles)if(!candidate.renderer.gameObject.activeSelf && !candidate.seen){visual=candidate;break;}}
                    if(visual==null){var go=new GameObject("PvP magic visual");go.transform.SetParent(transform,false);var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=CircleSprite;renderer.sortingOrder=260;visual=new PvpShotVisual{renderer=renderer};pvpProjectiles.Add(visual);}
                    Vector3 position=new Vector3(shot.x,shot.y,0);bool fresh=!visual.renderer.gameObject.activeSelf || visual.id!=shot.id;
                    visual.id=shot.id;visual.seen=true;visual.from=fresh?position:visual.renderer.transform.position;visual.to=position;visual.age=0;
                    visual.renderer.gameObject.SetActive(true);if(fresh)visual.renderer.transform.position=position;
                    visual.renderer.transform.localScale=Vector3.one*Mathf.Clamp(shot.size*3.125f,.1f,4f);visual.renderer.color=new Color(shot.r,shot.g,shot.b,shot.a);
                }
                foreach(var visual in pvpProjectiles)if(!visual.seen)visual.renderer.gameObject.SetActive(false);
                if (snapshot.ended && !ended) { ended=true; result=snapshot.result=="VICTORY"?"DEFEAT":snapshot.result=="DEFEAT"?"VICTORY":"DRAW"; }
            }
            catch (Exception exception) { Debug.LogWarning("Invalid PvP state: " + exception.Message); }
        }
        private static void ApplyPvpPose(Combatant unit,PvpPose pose) { if(unit != null && pose != null) unit.ApplyPvpPose(new Vector2(pose.x,pose.y),pose.hp,pose.mp,pose.alive); }
        private void SmoothPvpVisuals(float delta)
        {
            foreach(var unit in allies)if(unit!=null)unit.SmoothPvpPose(delta,PvpSnapshotInterval);
            foreach(var unit in enemies)if(unit!=null)unit.SmoothPvpPose(delta,PvpSnapshotInterval);
            foreach(var shot in pvpProjectiles)if(shot.renderer!=null && shot.renderer.gameObject.activeSelf)
            {shot.age+=delta;shot.renderer.transform.position=Vector3.Lerp(shot.from,shot.to,Mathf.Clamp01(shot.age/PvpSnapshotInterval));}
        }
        public void AbortPvp() { if (pvp && !ended) { ended=true; result="DISCONNECTED"; } }

        private Combatant CreateCombatant(string label, bool friendly, Vector2 position, EnemyArchetype type, Color color)
        {
            GameObject go = new GameObject(label);
            go.transform.position = position;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = CircleSprite;
            renderer.color = color;
            Rigidbody2D body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.bodyType = RigidbodyType2D.Kinematic;
            CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
            collider.radius = type == EnemyArchetype.Boss ? .8f : friendly ? .42f : .32f;
            Combatant unit = go.AddComponent<Combatant>().Initialize(this, label, friendly, type);
            if (previewMode)
            {
                go.transform.SetParent(transform,true);
                go.layer=2;
                unit.PreparePreview();
            }
            (friendly ? allies : enemies).Add(unit);
            unit.ClampToView();
            if(battleHud!=null)battleHud.Register(unit);
            return unit;
        }
        internal void InitializePreview() { previewMode=true; enabled=false; }
        internal Combatant CreatePreviewUnit(string label,bool friendly,Vector2 position)
        {
            return CreateCombatant(label,friendly,position,EnemyArchetype.Normal,Color.white);
        }
        internal static void ApplyPreviewSprite(Combatant unit,Sprite sprite) { SetCharacterSprite(unit,sprite); }

        private static Sprite circleSprite;
        private static Sprite CircleSprite
        {
            get
            {
                if (circleSprite == null)
                {
                    const int size = 32;
                    var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                    var pixels = new Color[size * size];
                    for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                    {
                        float dx = (x + .5f - size * .5f) / (size * .5f), dy = (y + .5f - size * .5f) / (size * .5f);
                        pixels[y * size + x] = dx * dx + dy * dy <= 1f ? Color.white : Color.clear;
                    }
                    texture.SetPixels(pixels); texture.Apply();
                    circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f);
                }
                return circleSprite;
            }
        }

        private void Update()
        {
            if (previewMode) return;
            if (returnToTitleRequested)
            {
                // Finish the old GUI frame before loading the title scene.
                enabled = false;
                if (pvp && PvpLanSession.Instance != null) PvpLanSession.Instance.Close();
                UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
                return;
            }
            if (IntroActive) return;
            if (pvp)
            {
                var session = PvpLanSession.Instance;
                if (session == null) return;
                if (!session.IsHost) { SmoothPvpVisuals(Time.unscaledDeltaTime);return; }
                if (!ended && Time.timeScale > 0)
                {
                    elapsed += Time.deltaTime;
                    int left = AliveAllyCount(), right = AliveEnemyCount();
                    if (left == 0 || right == 0 || elapsed >= PvpRules.TimeLimitSeconds) End(PvpRules.Resolve(left,right,hostDamage,guestDamage));
                }
                if (Time.unscaledTime >= nextPvpSnapshot) { nextPvpSnapshot = Time.unscaledTime + PvpSnapshotInterval; session.SendSnapshot(CreatePvpSnapshot()); }
                return;
            }
            if (ended || Time.timeScale<=0) return;
            elapsed += Time.deltaTime; spawnTimer += Time.deltaTime;
            if(BattleStage.Limited==LimitedStageKind.Weekday || BattleStage.Limited==LimitedStageKind.DailyBoss || BattleStage.Limited==LimitedStageKind.ChallengeBoss)
            {
                if(AliveAllyCount()==0)End("DEFEAT");
                else if(AliveEnemyCount()==0)End(bossSpawned?"VICTORY":"DEFEAT");
                else if(BattleStage.HideWave && elapsed>=survivalGoalSeconds)End("DEFEAT");
                return;
            }
            float interval = Mathf.Max(.6f, Mathf.Max(BattleStage.MinimumSpawnInterval,spawnInterval) - elapsed * .006f);
            if (BattleStage.BossRush)
            {
                if (AliveAllyCount() == 0) { End("DEFEAT"); return; }
                if (bossRushWaveActive && AliveEnemyCount() == 0)
                {
                    if (!BattleStage.Endless && bossRushWave >= 5) { End("VICTORY"); return; }
                    bossRushWaveActive = false; spawnTimer = 0f;
                }
                if (!bossRushWaveActive && spawnTimer >= 2f)
                {
                    bossRushWave++; bossRushWaveActive = true; spawnTimer = 0f;
                    for (int i = 0; i < Mathf.Min(bossRushWave, maxEnemies); i++) SpawnEnemy();
                }
            }
            else if (spawnTimer >= interval && AliveEnemyCount() < maxEnemies) { spawnTimer = 0f; SpawnEnemy(); }
            if (AliveAllyCount() == 0) End("DEFEAT");
            else if (!BattleStage.Endless && !BattleStage.BossRush && elapsed >= survivalGoalSeconds) End("VICTORY");
        }

        private void SpawnEnemy()
        {
            float roll = UnityEngine.Random.value;
            bool limitedBoss=BattleStage.Limited==LimitedStageKind.Weekday || BattleStage.Limited==LimitedStageKind.DailyBoss || BattleStage.Limited==LimitedStageKind.ChallengeBoss;
            EnemyArchetype type = limitedBoss || BattleStage.BossRush || BattleStage.Limited==LimitedStageKind.None && elapsed > 65f && (!bossSpawned || BattleStage.Endless && roll < .035f) ? EnemyArchetype.Boss : roll < .18f ? EnemyArchetype.Swift : roll < .34f ? EnemyArchetype.Tank : roll < .47f ? EnemyArchetype.Ranged : EnemyArchetype.Normal;
            SpeciesDefinition species = BattleStage.Limited==LimitedStageKind.Weekday ? LimitedStageRules.ChooseSpecies(speciesDatabase,BattleStage.LimitedFamily,(int)SpeciesRank.B) : BattleStage.Limited==LimitedStageKind.DailyBoss || BattleStage.Limited==LimitedStageKind.ChallengeBoss ? BattleStage.ChooseEnemySpecies(speciesDatabase,(int)SpeciesRank.S) : BattleStage.ChooseEnemySpecies(speciesDatabase,BattleStage.Rank(elapsed,BattleStage.BossRush ? bossRushWave - 1 : bossesDefeated));
            if(species==null) { Debug.LogWarning("No eligible enemy species are available for the current stage rank.");return; }
            if (type == EnemyArchetype.Boss) bossSpawned = true;
            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude < .01f) direction = Vector2.up;
            Vector2 position = direction * UnityEngine.Random.Range(arenaRadius * .75f, arenaRadius);
            int stageProgress = BattleStage.BossRush ? bossRushWave - 1 : bossesDefeated;
            int level = BattleStage.Limited==LimitedStageKind.ChallengeBoss?1000:BattleStage.Limited==LimitedStageKind.DailyBoss?99:BattleStage.Limited==LimitedStageKind.Weekday?75:BattleStage.Level(elapsed, stageProgress);
            float scale = 1f + (level - 1) * .08f;
            Color color = type == EnemyArchetype.Swift ? new Color(1f, .75f, .22f) : type == EnemyArchetype.Tank ? new Color(.75f, .35f, .24f) : type == EnemyArchetype.Ranged ? new Color(.75f, .45f, 1f) : type == EnemyArchetype.Boss ? new Color(1f, .18f, .31f) : new Color(.92f, .38f, .42f);
            Combatant enemy = CreateCombatant(type.ToString(), false, position, type, color);
            enemy.ScaleDifficulty(scale);
            enemy.ScaleHealth(type==EnemyArchetype.Boss?BattleStage.BossHealthMultiplier:BattleStage.NormalEnemyHealthMultiplier);
            if (species != null)
            {
                enemy.ApplySpeciesCombatTraits(species);
                SetCharacterSprite(enemy, species.sprite);
                enemy.SetCharacterName(species.displayName);
                int seed=UnityEngine.Random.Range(0,int.MaxValue);
                enemy.ConfigureWildMagic(WildUnitFactory.Create(species,level,seed),species);
                enemy.ConfigureWildAttack(species);
                if(level>99)enemy.ConfigureChallengeLevel(level);
            }
        }

        private SpeciesDefinition ResolveSpecies(UnitSaveData record)
        {
            if (speciesDatabase == null || record == null) return null;
            SpeciesDefinition species = speciesDatabase.GetById(record.speciesId);
            if (species != null) return species;
            for (int i = 0; i < speciesDatabase.species.Length; i++)
            {
                SpeciesDefinition candidate = speciesDatabase.species[i];
                if (candidate != null && string.Equals(candidate.displayName, record.displayName, StringComparison.OrdinalIgnoreCase)) return candidate;
            }
            SpeciesFamily family; SpeciesRank rank;
            if (Enum.TryParse(record.family, out family) && Enum.TryParse(record.rank, out rank))
            {
                SpeciesDefinition[] candidates = speciesDatabase.FindCandidates(family, rank);
                if (candidates.Length > 0) return candidates[0];
            }
            return null;
        }

        private static void SetCharacterSprite(Combatant character, Sprite sprite)
        {
            if (character == null || sprite == null) return;
            SpriteRenderer renderer = character.GetComponent<SpriteRenderer>();
            renderer.sprite = FullCharacterSprite(sprite);
            renderer.color = Color.white;
            renderer.sortingOrder = 1;
            FitSpriteToCombatant(renderer, character.IsLocalView ? 1.05f : character.Type == EnemyArchetype.Boss ? 1.3f : .78f);
            character.ClampToView();
        }

        internal Vector2 ClampToView(Vector2 desired, SpriteRenderer renderer)
        {
            if(previewMode)return desired;
            if(battleCamera==null)battleCamera=Camera.main;
            if(battleCamera==null || !battleCamera.orthographic)return desired;
            float halfHeight=battleCamera.orthographicSize,halfWidth=halfHeight*battleCamera.aspect;
            Vector2 center=battleCamera.transform.position;
            Vector2 extents=renderer!=null && renderer.sprite!=null?(Vector2)renderer.bounds.extents:Vector2.zero;
            Vector2 offset=renderer!=null?(Vector2)(renderer.bounds.center-renderer.transform.position):Vector2.zero;
            const float margin=.12f;
            float xLimit=Mathf.Max(0,halfWidth-extents.x-margin),yLimit=Mathf.Max(0,halfHeight-extents.y-margin);
            return new Vector2(Mathf.Clamp(desired.x+offset.x,center.x-xLimit,center.x+xLimit)-offset.x,
                Mathf.Clamp(desired.y+offset.y,center.y-yLimit,center.y+yLimit)-offset.y);
        }

        private void CreateArenaVisuals()
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.backgroundColor = new Color(.035f, .055f, .09f);
                if (!pvp) BattleStageBackground.Create(camera, BattleStage.Difficulty);
            }
            Material lineMaterial = new Material(Shader.Find("Sprites/Default"));
            for (int ring = 0; ring < 4; ring++)
            {
                float radius = arenaRadius * (ring + 1) / 4f;
                GameObject ringObject = new GameObject("Arena Ring " + (ring + 1));
                LineRenderer line = ringObject.AddComponent<LineRenderer>();
                line.useWorldSpace = false; line.loop = true; line.positionCount = 96; line.widthMultiplier = ring == 3 ? .035f : .018f;
                line.material = lineMaterial; line.sortingOrder = -10;
                Color tint = ring == 3 ? new Color(.22f, .55f, .75f, .62f) : new Color(.2f, .34f, .48f, .25f);
                line.startColor = tint; line.endColor = tint;
                for (int point = 0; point < line.positionCount; point++)
                {
                    float angle = point * Mathf.PI * 2f / line.positionCount;
                    line.SetPosition(point, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, .15f));
                }
            }
            for (int spoke = 0; spoke < 12; spoke++)
            {
                float angle = spoke * Mathf.PI * 2f / 12f;
                GameObject spokeObject = new GameObject("Arena Sector " + (spoke + 1));
                LineRenderer line = spokeObject.AddComponent<LineRenderer>();
                line.useWorldSpace = false; line.positionCount = 2; line.widthMultiplier = .012f; line.material = lineMaterial; line.sortingOrder = -11;
                line.startColor = new Color(.15f, .28f, .4f, .2f); line.endColor = line.startColor;
                line.SetPosition(0, Vector3.zero); line.SetPosition(1, new Vector3(Mathf.Cos(angle) * arenaRadius, Mathf.Sin(angle) * arenaRadius, .16f));
            }
        }

        internal Combatant FindNearestEnemy(Vector2 position, float range)
        {
            Combatant best = null; float bestSq = range * range;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Combatant enemy = enemies[i];
                if (enemy == null) { enemies.RemoveAt(i); continue; }
                if (!enemy.IsAlive || !enemy.gameObject.activeInHierarchy) continue;
                float sq = ((Vector2)enemy.transform.position - position).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = enemy; }
            }
            return best;
        }

        internal Combatant FindPreferredEnemy(Vector2 position, float range, float lowHpPriority)
        {
            Combatant best = null;
            float bestScore = float.MaxValue;
            float safeRange = Mathf.Max(.1f, range);
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Combatant enemy = enemies[i];
                if (enemy == null) { enemies.RemoveAt(i); continue; }
                if (!enemy.IsAlive || !enemy.gameObject.activeInHierarchy) continue;
                float distance = Vector2.Distance(position, enemy.transform.position);
                if (distance > safeRange) continue;
                float score = distance / safeRange - (1f - enemy.HealthRatio) * Mathf.Clamp01(lowHpPriority) * .65f;
                if (score < bestScore) { best = enemy; bestScore = score; }
            }
            return best;
        }

        internal Combatant FindPvpTarget(Vector2 position, float range, float lowHpPriority, bool friendly)
        {
            List<Combatant> targets = friendly ? enemies : allies;
            Combatant best = null; float bestScore = float.MaxValue; float safeRange = Mathf.Max(.1f, range);
            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive) continue;
                float distance = Vector2.Distance(position, target.transform.position);
                if (distance > safeRange) continue;
                float score = distance / safeRange - (1f - target.HealthRatio) * Mathf.Clamp01(lowHpPriority) * .65f;
                if (score < bestScore) { best = target; bestScore = score; }
            }
            return best;
        }

        internal Combatant FindNearestAlly(Vector2 position)
        {
            Combatant best = null; float bestSq = float.MaxValue;
            for (int i = 0; i < allies.Count; i++)
            {
                Combatant ally = allies[i]; if (ally == null || !ally.IsAlive) continue;
                float sq = ((Vector2)ally.transform.position - position).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = ally; }
            }
            return best;
        }

        internal Vector2 Separation(Combatant self, bool friendly)
        {
            Vector2 force = Vector2.zero; List<Combatant> group = friendly ? allies : enemies;
            for (int i = 0; i < group.Count; i++)
            {
                Combatant other = group[i]; if (other == null || other == self || !other.IsAlive) continue;
                Vector2 delta = (Vector2)self.transform.position - (Vector2)other.transform.position; float sq = delta.sqrMagnitude;
                if (sq > .001f && sq < 1.1f) force += delta.normalized / Mathf.Max(.15f, sq);
            }
            return force;
        }







        internal void OnDeath(Combatant unit)
        {
            if (previewMode) return;
            if (unit.IsFriendly) return;
            defeated++;
            if (pvp) return;
            if (unit.Type == EnemyArchetype.Boss) bossesDefeated++;
            if(equipmentRandom==null)equipmentRandom=new System.Random(Guid.NewGuid().GetHashCode());
            if(unit.Type==EnemyArchetype.Boss || equipmentRandom.NextDouble()<.10)
            {
                var candidate=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(save));
                candidate.readOnly=save.readOnly;candidate.readOnlyReason=save.readOnlyReason;
                if(candidate.equipmentInventory==null)candidate.equipmentInventory=new List<EquipmentItem>();
                int level=unit.UnitData==null?1:unit.UnitData.level;
                var item=EquipmentCatalog.Roll(equipmentRandom.Next(),level,unit.Type==EnemyArchetype.Boss);
                candidate.equipmentInventory.Add(item);
                if(candidate.autoDiscardOldEquipment)EquipmentCatalog.DiscardOldestExcess(candidate);
                if(SaveSystem.Save(candidate,out string lootError)) { save=candidate;Feedback(unit,CombatFeedbackKind.Status,0,"装備を獲得！"); }
                else Debug.LogError("Could not save equipment: "+lootError);
            }
            if (unit.WildRecord != null && LimitedStageRules.DropsEgg(equipmentRandom,unit.Type == EnemyArchetype.Boss))
            {
                GameSaveData committed;
                string eggError;
                if (EggSystem.TryAward(save, unit.WildRecord, out committed, out eggError))
                {
                    save = committed; eggsEarned++;
                    Feedback(unit, CombatFeedbackKind.Status, 0, "卵を獲得！");
                }
                else
                {
                    pendingEggs.Add(unit.WildRecord);
                    Debug.LogError("Could not save egg: " + eggError);
                }
            }
            long earnedExperience=BattleStage.StageEnemyExperience(unit.UnitData==null?1:unit.UnitData.level,unit.Type==EnemyArchetype.Boss,BattleStage.Limited);
            if (ExperienceSystem.Distribute(allies.ConvertAll(a => a == null ? null : a.UnitData).ToArray(), earnedExperience) > 0)
                for (int i = 0; i < allies.Count; i++) if (allies[i] != null) allies[i].RefreshAfterLevelUp();
            Destroy(unit.gameObject, .35f);
        }
        internal void ShowMagicImpact(Vector2 position, BuiltMagic magic, float radiusOverride = -1f)
        {
            if (magic == null) return;
            float radius = radiusOverride > 0f ? radiusOverride : Mathf.Max(.6f, magic.area);
            MagicImpactVisual.Play(position, ElementColor(magic.element), radius, .72f,previewMode?transform:null);
        }

        internal static Color ElementColor(AttributeElement element)
        {
            switch (element)
            {
                case AttributeElement.Fire: return new Color(1f, .31f, .12f);
                case AttributeElement.Water: return new Color(.18f, .58f, 1f);
                case AttributeElement.Wind: return new Color(.34f, 1f, .67f);
                case AttributeElement.Earth: return new Color(.82f, .62f, .28f);
                case AttributeElement.Light: return new Color(1f, .9f, .38f);
                case AttributeElement.Dark: return new Color(.72f, .35f, 1f);
                default: return new Color(.82f, .9f, 1f);
            }
        }





        internal float DealDamage(Combatant source,Combatant target,float amount,AttributeElement element,bool magical)
        {
            if(target==null || (source!=null && source.IsFriendly==target.IsFriendly))return 0;
            float enemyFactor=DamageFactor(source,target);
            float dealt=target.TakeDamage(amount*(source==null?1:source.SpeciesDamageMultiplier(element))*enemyFactor,element,magical);
            RecordPvpDamage(source,target,dealt);
            if(source!=null && source.IsAlive)source.OnDamageDealt(target,dealt,element);
            return dealt;
        }
        internal void RecordPvpDamage(Combatant source,Combatant target,float dealt)
        {
            if(!pvp || source==null || target==null || source.IsFriendly==target.IsFriendly || dealt<=0 || float.IsNaN(dealt) || float.IsInfinity(dealt))return;
            if(source.IsFriendly)hostDamage+=dealt;else guestDamage+=dealt;
        }
        internal void ReviveAllies(Combatant source)
        {
            if(source==null)return;
            var team=source.IsFriendly?allies:enemies;
            for(int i=0;i<team.Count;i++)if(team[i]!=null && team[i].ReviveFully())
            { if(battleHud!=null)battleHud.RegisterRevived(team[i]);Feedback(team[i],CombatFeedbackKind.Status,0,"完全復活"); }
        }
        internal void NotifyRevived(Combatant unit) { if(battleHud!=null)battleHud.RegisterRevived(unit); }
        internal float DamageFactor(Combatant source,Combatant target)
        { return !previewMode && !pvp && source!=null && target!=null && !source.IsFriendly && target.IsFriendly?BattleStage.EnemyDamageMultiplier:1f; }



        private int AliveAllyCount() { int count = 0; for (int i = 0; i < allies.Count; i++) if (allies[i] != null && allies[i].IsAlive) count++; return count; }
        private int AliveEnemyCount() { int count = 0; for (int i = enemies.Count - 1; i >= 0; i--) { if (enemies[i] == null) enemies.RemoveAt(i); else if (enemies[i].IsAlive) count++; } return count; }
        private void End(string text)
        {
            CaptureFocus=null;
            ended = true; result = text; Time.timeScale = 1f;
            if (pvp) return;
            for (int i = 0; i < allies.Count; i++)
            {
                Combatant ally = allies[i]; if (ally == null || ally.UnitData == null) continue;
                UnitSaveData saved = save.units.Find(u => u != null && u.id == ally.UnitData.id);
                if (saved != null) { saved.level = Mathf.Clamp(ally.UnitData.level, 1, 99); saved.experience = ally.UnitData.experience; }
            }
            if (text == "VICTORY") save.battlesCompleted++;
            for (int i = pendingEggs.Count - 1; i >= 0; i--)
            {
                GameSaveData committed; string eggError;
                if (EggSystem.TryAward(save, pendingEggs[i], out committed, out eggError))
                { save = committed; eggsEarned++; pendingEggs.RemoveAt(i); }
                else Debug.LogError("Could not save egg: " + eggError);
            }
            bool unlocked = false; // Stages unlock from player rank, not a victory flag.
            ProgressSaveFailed = !SaveSystem.Save(save, out string saveError);
            NextStageUnlocked = unlocked && !ProgressSaveFailed;
            if (ProgressSaveFailed) Debug.LogError("Could not save battle progression: " + saveError);
        }

        private void OnGUI()
        {
            if (battleHud!=null || previewMode || returnToTitleRequested || !isActiveAndEnabled) return;
            if (hudStyle == null)
            {
                hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
                titleStyle = new GUIStyle(hudStyle) { fontSize = 46, alignment = TextAnchor.MiddleCenter };
                smallHudStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = new Color(.78f, .86f, .96f) } };
                spellHudStyle = new GUIStyle(smallHudStyle) { fontSize = 13, alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Clip };
                spellDetailStyle = new GUIStyle(spellHudStyle) { fontSize = 11, alignment = TextAnchor.MiddleLeft, wordWrap = true };
            }
            GUI.color = new Color(.025f, .04f, .075f, .94f); GUI.Box(new Rect(14, 14, 286, 148), GUIContent.none); GUI.color = Color.white;
            GUI.color = new Color(.13f, .78f, .93f); GUI.DrawTexture(new Rect(14, 14, 4, 148), Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(new Rect(28, 22, 250, 25), "魔物たちの自動戦闘", hudStyle);
            GUI.Label(new Rect(28, 52, 250, 24), "生存時間　" + FormatTime(elapsed) + " ／ " + FormatTime(survivalGoalSeconds), smallHudStyle);
            GUI.Label(new Rect(28, 75, 250, 22), "味方　" + AliveAllyCount() + "　　 撃破　" + defeated, smallHudStyle);
            GUI.Label(new Rect(28, 97, 250, 22), "敵　" + AliveEnemyCount() + (BattleStage.HideWave?"":"　　 ウェーブ　" + (1 + Mathf.FloorToInt(elapsed / 30f))), smallHudStyle);
            GUI.Label(new Rect(28, 126, 88, 24), "戦闘速度", spellHudStyle);
            if (GUI.Button(new Rect(120, 124, 45, 26), "1倍")) SetBattleSpeed(1f);
            if (GUI.Button(new Rect(168, 124, 45, 26), "2倍")) SetBattleSpeed(2f);
            if (GUI.Button(new Rect(216, 124, 58, 26), "3倍")) SetBattleSpeed(3f);
            int guiDepth = GUI.depth;
            GUI.depth = -20;
            Rect magicToggle = new Rect(Screen.width - 180f, 14f, 166f, 32f);
            if (GUI.Button(magicToggle, showMagicLoadout ? "固有魔法を閉じる" : "味方の固有魔法を開く")) showMagicLoadout = !showMagicLoadout;
            if (showMagicLoadout) DrawMagicLoadoutHud();
            if (ended)
            {
                GUI.color = new Color(.02f, .03f, .05f, .82f); GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none); GUI.color = Color.white;
                GUI.Label(new Rect(0, Screen.height / 2f - 80, Screen.width, 100), (result == "VICTORY" ? "勝利" : "敗北") + "\n撃破数：" + defeated, titleStyle);
                if (GUI.Button(new Rect(Screen.width / 2f - 110, Screen.height / 2f + 30, 220, 48), "管理画面へ戻る")) returnToTitleRequested = true;
            }
            GUI.depth = guiDepth;
        }

        private void DrawMagicLoadoutHud()
        {
            float width = Mathf.Min(410f, Screen.width * .46f);
            float height = Mathf.Min(430f, Screen.height * .76f);
            float x = Screen.width - width - 14f;
            const float top = 54f;
            float contentHeight = 4f;
            const float spellRowHeight = 56f;
            for (int i = 0; i < allies.Count; i++)
            {
                Combatant ally = allies[i];
                if (ally == null) continue;
                int rows = Mathf.Max(1, Mathf.CeilToInt(ally.Magics.Count / 2f));
                contentHeight += 26f + Mathf.Max(1, ally.Magics.Count) * spellRowHeight + 5f;
            }
            GUI.color = new Color(.025f, .04f, .075f, .98f);
            GUI.Box(new Rect(x, top, width, height), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(x + 12f, top + 6f, width - 24f, 24f), "味方の固有魔法", hudStyle);
            Rect view = new Rect(x + 7f, top + 34f, width - 14f, height - 42f);
            loadoutScrollPosition = GUI.BeginScrollView(view, loadoutScrollPosition, new Rect(0, 0, view.width - 16f, contentHeight));
            float y = 2f;
            float chipWidth = view.width - 30f;
            for (int i = 0; i < allies.Count; i++)
            {
                Combatant ally = allies[i];
                if (ally == null) continue;
                GUI.color = ally.IsAlive ? new Color(.35f, .9f, 1f) : new Color(.62f, .65f, .72f);
                GUI.Label(new Rect(7f, y, view.width - 22f, 20f), ally.CharacterName + "　・　魔法 " + ally.Magics.Count + "個" + (ally.IsAlive ? "" : "　戦闘不能"), smallHudStyle);
                GUI.color = Color.white;
                y += 20f;
                if (ally.Magics.Count == 0)
                {
                    GUI.Label(new Rect(7f, y, view.width - 22f, 20f), "通常攻撃のみ", spellHudStyle);
                    y += spellRowHeight + 4f;
                    continue;
                }
                for (int spell = 0; spell < ally.Magics.Count; spell++)
                {
                    BuiltMagic magic = ally.Magics[spell];
                    Rect chip = new Rect(4f, y + spell * spellRowHeight, chipWidth, spellRowHeight - 3f);
                    Color element = ElementColor(magic.element);
                    GUI.color = new Color(element.r * .20f, element.g * .20f, element.b * .20f, .98f);
                    GUI.DrawTexture(chip, Texture2D.whiteTexture);
                    GUI.color = element;
                    string details = ally.MagicCooldownRemaining(spell)>0f?"待機 あと"+Mathf.CeilToInt(ally.MagicCooldownRemaining(spell))+"秒":"使用可能";
                    string header = "固有魔法 " + (spell + 1) + "　" + ElementShortName(magic.element) + "属性";
                    GUI.Label(new Rect(chip.x + 6f, chip.y + 2f, chip.width - 12f, 17f), new GUIContent(header, details), spellHudStyle);
                    GUI.Label(new Rect(chip.x + 6f, chip.y + 19f, chip.width - 12f, 29f), details, spellDetailStyle);
                    float ready = 1f - ally.MagicCooldownRatio(spell);
                    GUI.color = new Color(element.r, element.g, element.b, .9f);
                    GUI.DrawTexture(new Rect(chip.x, chip.yMax - 2f, chip.width * ready, 2f), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                y += Mathf.Max(1, ally.Magics.Count) * spellRowHeight + 5f;
            }
            GUI.EndScrollView();
            if (!string.IsNullOrEmpty(GUI.tooltip))
            {
                Vector2 mouse = Event.current.mousePosition;
                GUI.Box(new Rect(Mathf.Clamp(mouse.x + 12f, 4f, Screen.width - 250f), Mathf.Clamp(mouse.y + 12f, 4f, Screen.height - 52f), 240f, 40f), GUI.tooltip);
            }
        }
        private static string ElementShortName(AttributeElement value)
        {
            switch (value) { case AttributeElement.Fire: return "火"; case AttributeElement.Water: return "水"; case AttributeElement.Wind: return "風"; case AttributeElement.Earth: return "土"; case AttributeElement.Light: return "光"; case AttributeElement.Dark: return "闇"; default: return "無"; }
        }



        private void SetBattleSpeed(float value) { battleSpeed = value; Time.timeScale = battleSpeed; }
        private void OnDestroy() { if (pvp && PvpLanSession.Instance != null) PvpLanSession.Instance.SnapshotReceived -= ApplyPvpSnapshot; if (!previewMode) Time.timeScale = 1f; }
        private static string FormatTime(float seconds) { int value = Mathf.FloorToInt(seconds); return (value / 60).ToString("00") + ":" + (value % 60).ToString("00"); }
        private static readonly Dictionary<string, Sprite> generatedSpriteCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<Texture2D, Sprite> fullCharacterSpriteCache = new Dictionary<Texture2D, Sprite>();
        private static Sprite FullCharacterSprite(Sprite source)
        {
            if (source == null || source.texture == null) return source;
            Texture2D texture = source.texture;
            Rect rect = source.rect;
            if (rect.x == 0 && rect.y == 0 && rect.width == texture.width && rect.height == texture.height) return source;
            if (fullCharacterSpriteCache.TryGetValue(texture, out Sprite cached) && cached != null) return cached;
            Sprite full = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            full.name = source.name + " (full character)";
            fullCharacterSpriteCache[texture] = full;
            return full;
        }
        private static Sprite LoadGeneratedSprite(string path)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return null;
            Sprite sprite; if (generatedSpriteCache.TryGetValue(path, out sprite)) return sprite;
            try
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(System.IO.File.ReadAllBytes(path))) { UnityEngine.Object.Destroy(texture); return null; }
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
                generatedSpriteCache[path] = sprite; return sprite;
            }
            catch (Exception exception) { Debug.LogWarning("Could not load generated species sprite: " + exception.Message); return null; }
        }
        private static void FitSpriteToCombatant(SpriteRenderer renderer, float maximumWorldSize)
        {
            if (renderer == null || renderer.sprite == null) return;
            Vector2 bounds = renderer.sprite.bounds.size;
            float largestDimension = Mathf.Max(bounds.x, bounds.y);
            if (largestDimension > 0f)
                renderer.transform.localScale = Vector3.one * (maximumWorldSize / largestDimension);
        }
    }

    public sealed class Combatant : MonoBehaviour
    {
        private Vector2 desiredMoveDirection;
        private BattleDemo battle; private Rigidbody2D body; private float hp, maxHp, speed, range, cooldown, attackInterval, damage, magicAttack, physicalDefense, magicDefense; private bool dead;
        private const float ChargeStartDistance = 2.5f, ChargeContactDistance = .55f, ChargeSpeed = 10f, ChargeDuration = .4f;
        private Combatant chargeTarget;
        private float chargeRemaining, chargeDamage;
        private Vector2 chargeDirection, retreatDirection;
        private float retreatRemaining, retreatTime;
        private bool chargeAllowedFromDepletion;
        private bool magicExecutionBlocked;
        private bool revivePassiveUsed;
        private float pvpControlImmuneUntil;
        private PersonalityDefinition personality;
        private UnitStats speciesBaseStats;
        private readonly List<BuiltMagic> magics = new List<BuiltMagic>();
        private readonly CombatantBuffState buffs=new CombatantBuffState();
        private SpeciesPassive speciesPassive;
        private float passiveRegenerationTime,nextPassiveStatusTime;
        public string PassiveName { get { return speciesPassive==null?"":speciesPassive.displayName; } }
        internal float HealingDoneMultiplier { get { return 1+SpeciesPassiveCalculator.Sum(speciesPassive,PassiveEffectType.HealingDone); } }
        internal float SpeciesDamageMultiplier(AttributeElement element) { return SpeciesPassiveCalculator.DamageMultiplier(speciesPassive,element,IsAlive?HealthRatio:1); }
        internal void OnDamageDealt(Combatant target,float dealt,AttributeElement element)
        {
            if(dealt<=0 || speciesPassive==null || speciesPassive.effects==null)return;
            float steal=SpeciesPassiveCalculator.Sum(speciesPassive,PassiveEffectType.LifeSteal);if(steal>0)Heal(dealt*steal);
            for(int i=0;i<speciesPassive.effects.Length;i++)
            {
                PassiveEffect effect=speciesPassive.effects[i];if(effect==null || target==null || !target.IsAlive)continue;
                if(effect.type==PassiveEffectType.OnHitDebuff && Time.time>=nextPassiveStatusTime)
                {
                    nextPassiveStatusTime=Time.time+1;
                    target.ApplyDebuffFrom(effect.debuff,2,0,effect.amount,element,this);
                }
                else if(effect.type==PassiveEffectType.OnHitInstantDeath && StatusApplication.Roll(effect.amount))target.ApplyInstantDeathFrom(this);
            }
        }
        private LineRenderer buffAura;
        private static Material buffAuraMaterial;
        private readonly List<float> magicCooldowns = new List<float>();
        private readonly List<int> magicCastCounts = new List<int>();
        private bool previewMode;
        private float previewFlashTime;
        private Color previewFlashColor;
        private int lastDisplayedLevel;
        private string characterName;
        private readonly float[] damageOverTime = new float[8];
        private readonly float[] damageOverTimeTime = new float[8];
        private readonly Combatant[] damageOverTimeSources = new Combatant[8];
        private readonly AttributeElement[] damageOverTimeElements = new AttributeElement[8];
        private float slowTime, freezeTime, paralysisTime, sleepTime, silenceTime, curseTime, slowMultiplier = 1f, mana, maxMana, manaRegen;
        private static GUIStyle friendlyNameStyle, enemyNameStyle;
        private readonly float[] elementReduction = new float[7];
        private readonly float[] elementAilmentResistance = new float[7];
        private readonly float[] debuffResistance = new float[8];
        public UnitInstance UnitData { get; private set; }
        public bool IsFriendly { get; private set; }
        internal bool IsLocalView { get { return battle==null?IsFriendly:battle.IsLocalSide(IsFriendly); } }
        public bool IsAlive { get { return !dead; } }
        public float HealthRatio { get { return maxHp <= 0f ? 0f : Mathf.Clamp01(hp / maxHp); } }
        public float CurrentHp { get { return hp; } }
        public float MaximumHp { get { return maxHp; } }
        public float CurrentMp { get { return mana; } }
        public float MaximumMp { get { return maxMana; } }
        public bool RecentlyDamaged { get { return Time.unscaledTime-lastDamageDisplayTime<2f; } }
        private float lastDamageDisplayTime=float.NegativeInfinity;
        private int cachedStatusFlags=-1;
        private DebuffVisual debuffVisual;
        internal int ActiveDebuffMask
        {
            get
            {
                if(dead || battle==null || battle.IsEnded)return 0;
                int mask=0;
                if(damageOverTimeTime[0]>0)mask|=1;if(damageOverTimeTime[1]>0)mask|=2;
                if(freezeTime>0)mask|=4;if(paralysisTime>0)mask|=8;if(sleepTime>0)mask|=16;
                if(slowTime>0)mask|=32;if(silenceTime>0)mask|=64;if(curseTime>0)mask|=128;
                return mask;
            }
        }
        private string cachedStatusSummary="";
        public string StatusSummary
        {
            get
            {
                int flags=0;
                if(damageOverTimeTime[0]>0)flags|=1;if(damageOverTimeTime[1]>0)flags|=2;
                if(freezeTime>0)flags|=4;if(paralysisTime>0)flags|=8;if(sleepTime>0)flags|=16;if(slowTime>0)flags|=32;if(silenceTime>0)flags|=64;if(curseTime>0)flags|=128;
                if(buffs.AttackMultiplier>1)flags|=256;if(buffs.DamageMultiplier<1)flags|=512;if(buffs.SpeedMultiplier>1)flags|=1024;
                if(flags!=cachedStatusFlags)
                {
                    cachedStatusFlags=flags;var text=new System.Text.StringBuilder();
                    for(int i=0;i<StatusNames.Length;i++)if((flags&(1<<i))!=0){if(text.Length>0)text.Append(" ");text.Append(StatusNames[i]);}
                    cachedStatusSummary=text.ToString();
                }
                return cachedStatusSummary;
            }
        }
        private static readonly string[] StatusNames={"毒","火傷","凍結","麻痺","睡眠","鈍足","沈黙","呪い","攻撃↑","防御↑","加速"};
        internal bool CanMoveInPreview { get { return IsAlive && freezeTime<=0f && sleepTime<=0f; } }
        private float MovementDebuffMultiplier { get { return slowMultiplier*(paralysisTime>0f?.7f:1f); } }
        internal float PreviewMovementScale { get { return MovementDebuffMultiplier*buffs.SpeedMultiplier; } }
        internal void PreparePreview()
        {
            previewMode=true;
            if (!IsFriendly) {hp=maxHp=100000f;maxMana=mana=1000f;}
        }
        internal void SetPreviewHealthRatio(float ratio) { if(previewMode && !dead)hp=maxHp*Mathf.Clamp01(ratio); }
        internal float MagicAttack { get { return magicAttack; } }
        internal float OutgoingDamageMultiplier { get { return buffs.AttackMultiplier*(curseTime > 0f ? .5f : 1f); } }
        public EnemyArchetype Type { get; private set; }
        public UnitSaveData WildRecord {get;private set;}
        public bool CaptureAttempted {get;private set;}
        internal void SetWildRecord(UnitSaveData record){WildRecord=record;}
        internal void ConfigureWildMagic(UnitSaveData record,SpeciesDefinition species)
        {
            if(IsFriendly)throw new InvalidOperationException("Wild magic can only be assigned to enemies.");
            SetWildRecord(record);
            UnitData=new UnitInstance{id=record.id,speciesId=record.speciesId,level=record.level,personality=ParsePersonality(record.personality),iv=new IndividualValues()};
            for(int i=0;i<IndividualValues.StatCount;i++)UnitData.iv.Set(i,record.individualValues[i]);
            ConfigurePersonality(battle == null ? null : battle.Personalities);
            // Preserve archetype HP, attack, movement, defenses and mana already initialized for this enemy.
            ConfigureMagicSlots(record,species);
            for(int i=0;i<magicCooldowns.Count;i++)magicCooldowns[i]=2f+i*.3f+UnityEngine.Random.Range(0,.4f);
        }
        internal void MarkCaptureAttempted(){CaptureAttempted=true;}
        internal void MarkCaptured()
        {
            CaptureAttempted=true;dead=true;desiredMoveDirection=Vector2.zero;enabled=false;
            var collider=GetComponent<Collider2D>();if(collider!=null)collider.enabled=false;
        }
        public IReadOnlyList<BuiltMagic> Magics { get { return magics; } }
        public string CharacterName { get { return string.IsNullOrEmpty(characterName) ? name : characterName; } }
        public int MagicCastCount(int index) { return index < 0 || index >= magicCastCounts.Count ? 0 : magicCastCounts[index]; }
        public float MagicCooldownRemaining(int index) { return index < 0 || index >= magicCooldowns.Count ? 0f : Mathf.Max(0f, magicCooldowns[index]); }
        public float MagicCooldownRatio(int index) { return index < 0 || index >= magics.Count || magics[index].cooldown <= 0f ? 0f : Mathf.Clamp01(magicCooldowns[index] / magics[index].cooldown); }
        internal void SetCharacterName(string value) { characterName = value; }
        private bool hasPvpPose;
        private Vector3 pvpPoseFrom,pvpPoseTo;
        private float pvpPoseAge;
        internal void ApplyPvpPose(Vector2 position, float health, float mp, bool alive)
        {
            bool wasDead=dead;
            Vector2 clamped=battle==null?position:battle.ClampToView(position,GetComponent<SpriteRenderer>());
            pvpPoseTo=new Vector3(clamped.x,clamped.y,transform.position.z);
            pvpPoseFrom=hasPvpPose?transform.position:pvpPoseTo;pvpPoseAge=0;
            if(!hasPvpPose){transform.position=pvpPoseTo;hasPvpPose=true;}
            hp = Mathf.Clamp(health, 0, maxHp); mana = Mathf.Clamp(mp, 0, maxMana); dead = !alive;
            var renderer = GetComponent<SpriteRenderer>(); if (renderer != null) renderer.color = dead ? Color.gray : Color.white;
            if(wasDead && alive && battle!=null)battle.NotifyRevived(this);
        }
        internal void SmoothPvpPose(float delta,float duration)
        {
            if(!hasPvpPose)return;
            pvpPoseAge+=Mathf.Max(0,delta);
            transform.position=Vector3.Lerp(pvpPoseFrom,pvpPoseTo,Mathf.Clamp01(pvpPoseAge/Mathf.Max(.001f,duration)));
        }
        internal void EnableAutonomousDemo(){previewMode=false;}
        internal Combatant Initialize(BattleDemo owner, string label, bool friendly, EnemyArchetype type)
        {
            battle = owner; IsFriendly = friendly; Type = type; name = label; body = GetComponent<Rigidbody2D>();
            maxHp = (friendly ? 100f : type == EnemyArchetype.Tank ? 115f : type == EnemyArchetype.Boss ? 1300f : type == EnemyArchetype.Swift ? 30f : 42f)*UnitStatsCalculator.HealthMultiplier;
            speed = friendly ? 3.3f : type == EnemyArchetype.Swift ? 3f : type == EnemyArchetype.Tank ? .85f : type == EnemyArchetype.Boss ? 1.2f : type == EnemyArchetype.Ranged ? 1.05f : 1.45f;
            range = friendly ? 5.2f : type == EnemyArchetype.Ranged ? 4.5f : type == EnemyArchetype.Boss ? 1.4f : 1.05f;
            attackInterval = friendly ? 1f : type == EnemyArchetype.Boss ? .9f : 1.5f; damage = friendly ? 18f : type == EnemyArchetype.Boss ? 18f : type == EnemyArchetype.Tank ? 13f : 8f; hp = maxHp;
            if (!friendly) { maxMana=20f; mana=maxMana; manaRegen=.2f; }
            return this;
        }
        internal void ApplySpeciesCombatTraits(SpeciesDefinition species)
        {
            if (species == null) return;
            SpeciesPassive previous=speciesPassive;
            CopySpeciesResistances(species);
            if (species.baseStats == null) return;
            UnitStats traits=species.baseStats.Copy();SpeciesPassiveCalculator.ApplyStats(speciesPassive,traits);
            physicalDefense = Mathf.Max(0f,traits.physicalDefense);magicDefense = Mathf.Max(0f,traits.magicDefense);
            maxMana = Mathf.Max(0f,traits.mp); mana=maxMana;manaRegen = Mathf.Max(0f,traits.mpRegen)*.2f;
            float hpFactor=PassiveStatRatio(previous,0);maxHp*=hpFactor;hp*=hpFactor;
            damage*=PassiveStatRatio(previous,2);magicAttack*=PassiveStatRatio(previous,3);speed*=PassiveStatRatio(previous,6);attackInterval/=PassiveStatRatio(previous,7);range*=PassiveStatRatio(previous,8);
        }
        internal void ConfigureWildAttack(SpeciesDefinition species)
        {
            if (IsFriendly || species == null || UnitData == null) return;
            UnitStats stats = UnitStatsCalculator.Calculate(species, UnitData);
            magicAttack = Mathf.Max(1f, stats.magicAttack);
        }
        internal void ConfigureChallengeLevel(int level)
        {
            if(IsFriendly || UnitData==null || level<=99)return;
            // Wild records and recruited units remain capped at 99; battle-only stats extrapolate the same growth curve.
            float ratio=(.5f+level/100f)/(.5f+99f/100f);
            magicAttack*=ratio;physicalDefense*=ratio;magicDefense*=ratio;UnitData.level=level;
        }
        private float PassiveStatRatio(SpeciesPassive previous,int index) { return (1+SpeciesPassiveCalculator.Sum(speciesPassive,PassiveEffectType.StatBonus,index))/(1+SpeciesPassiveCalculator.Sum(previous,PassiveEffectType.StatBonus,index)); }
        private void CopySpeciesResistances(SpeciesDefinition species)
        {
            speciesPassive=species.passive;
            for (int i=0;i<7;i++)
            {
                ResistanceValue resistance=species.GetResistance((AttributeElement)i);
                elementReduction[i]=SpeciesPassiveCalculator.ElementReduction(species,(AttributeElement)i); elementAilmentResistance[i]=resistance.applicationResistance;
            }
            for(int i=0;i<8;i++) debuffResistance[i]=SpeciesPassiveCalculator.DebuffReduction(species,(DebuffType)i);
        }
        internal void Configure(UnitSaveData data, SpeciesDefinition speciesDefinition, PersonalityDatabase personalityDatabase)
        {
            speciesPassive=null;passiveRegenerationTime=0;nextPassiveStatusTime=0;
            UnitData = new UnitInstance { id = data.id, speciesId = data.speciesId, level = Mathf.Clamp(data.level, 1, 99), experience = data.experience, personality = ParsePersonality(data.personality), equipment = data.equipment, iv = new IndividualValues(), magicGenomes = new List<MagicGenome>() };
            lastDisplayedLevel=UnitData.level;
            for (int i = 0; i < 10; i++) UnitData.iv.Set(i, data.individualValues[i]);
            for (int i = 0; i < 7; i++) { elementReduction[i] = data.attributeDamageReduction[i]; elementAilmentResistance[i] = data.attributeAilmentResistance[i]; }
            for (int i = 0; i < 8; i++) debuffResistance[i] = data.debuffResistance[i];
            if (speciesDefinition != null) CopySpeciesResistances(speciesDefinition);
            ConfigurePersonality(personalityDatabase);
            speciesBaseStats = speciesDefinition != null && speciesDefinition.baseStats != null ? speciesDefinition.baseStats.Copy() : StatsFromArray(data.baseStats);
            RecalculateDerivedStats(false);
            ConfigureMagicSlots(data,speciesDefinition);
        }
        private void ConfigurePersonality(PersonalityDatabase personalityDatabase)
        {
            int boost = UnitData.personality == PersonalityType.Brave ? 2 : UnitData.personality == PersonalityType.Timid ? 6 : UnitData.personality == PersonalityType.Calm ? 3 : UnitData.personality == PersonalityType.Cautious ? 4 : UnitData.personality == PersonalityType.Aggressive ? 7 : 0;
            int reduce = UnitData.personality == PersonalityType.Brave ? 4 : UnitData.personality == PersonalityType.Timid ? 2 : UnitData.personality == PersonalityType.Calm ? 6 : UnitData.personality == PersonalityType.Cautious ? 7 : UnitData.personality == PersonalityType.Aggressive ? 4 : 7;
            personality = personalityDatabase == null ? null : personalityDatabase.Get(UnitData.personality);
            if (personality == null) personality = new PersonalityDefinition { type = UnitData.personality, boostedStatIndex = boost, reducedStatIndex = reduce, boostMultiplier = 1.1f, reductionMultiplier = .9f, aggression = UnitData.personality == PersonalityType.Aggressive || UnitData.personality == PersonalityType.Brave ? .9f : .45f, caution = UnitData.personality == PersonalityType.Timid || UnitData.personality == PersonalityType.Cautious ? .85f : .3f, lowHpPriority = UnitData.personality == PersonalityType.Calm ? .95f : .5f, chargeCooldownMultiplier=PersonalityDefinition.DefaultChargeCooldown(UnitData.personality), chargeRetreatDistance=PersonalityDefinition.DefaultRetreatDistance(UnitData.personality) };
        }
        private void ConfigureMagicSlots(UnitSaveData data,SpeciesDefinition speciesDefinition)
        {
            magics.Clear(); magicCooldowns.Clear(); magicCastCounts.Clear();
            UnitData.magicGenomes.Clear();
            SpeciesRank parsedRank;
            if (speciesDefinition != null) parsedRank = speciesDefinition.rank;
            else if (!Enum.TryParse(data.rank, true, out parsedRank) || !Enum.IsDefined(typeof(SpeciesRank), parsedRank)) parsedRank = SpeciesRank.F;
            int magicSlotLimit = RankRules.MagicSlots(parsedRank);
            if (data.magicGenomeJson != null)
            {
                for (int i = 0; i < data.magicGenomeJson.Length && i < magicSlotLimit; i++)
                {
                    string genomeJson = data.magicGenomeJson[i];
                    MagicGenome genome;
                    bool valid=MagicGenome.TryParseJson(genomeJson,out genome);
                    if(!valid)Debug.LogWarning("Skipped invalid magic genome for " + data.displayName);
                    UnitData.magicGenomes.Add(valid?genome.Clone():null);
                    BuiltMagic built = valid?MagicFactory.Build(genome,speciesDefinition):new BuiltMagic{available=false,displayName="使用できない魔法",cooldown=1};
                    magics.Add(built);
                    magicCooldowns.Add(UnityEngine.Random.Range(.05f, .55f));
                    magicCastCounts.Add(0);
                }
            }
            UnitData.magicGeneSlots=Spells.SpellSlotStorage.ForBattle(UnitData,data.magicSlotGenomeJson,data.magicGenomeJson);
        }
        private static PersonalityType ParsePersonality(string value) { PersonalityType p; return Enum.TryParse(value, out p) ? p : PersonalityType.Brave; }
        private static UnitStats StatsFromArray(float[] values)
        {
            var stats = new UnitStats();
            if (values != null) for (int i = 0; i < 10 && i < values.Length; i++) stats.Set(i, values[i]);
            return stats;
        }
        private void RecalculateDerivedStats(bool preserveRatios)
        {
            float healthRatio = preserveRatios && maxHp > 0f ? hp / maxHp : 1f;
            float manaRatio = preserveRatios && maxMana > 0f ? mana / maxMana : 1f;
            UnitStats stats = UnitStatsCalculator.Calculate(speciesBaseStats, UnitData, null, personality);
            SpeciesPassiveCalculator.ApplyStats(speciesPassive,stats);
            maxHp = Mathf.Max(1f, stats.hp);
            maxMana = Mathf.Max(0f, stats.mp);
            speed = Mathf.Max(.5f, stats.moveSpeed);
            attackInterval = 1f / Mathf.Max(.1f, stats.attackSpeed);
            damage = Mathf.Max(5f, stats.physicalAttack);
            magicAttack = Mathf.Max(0f, stats.magicAttack);
            physicalDefense = Mathf.Max(0f, stats.physicalDefense);
            magicDefense = Mathf.Max(0f, stats.magicDefense);
            range = Mathf.Clamp(stats.searchRange, 3f, 12f);
            manaRegen = Mathf.Max(.02f, stats.mpRegen);
            hp = dead ? 0f : preserveRatios ? Mathf.Clamp(maxHp * healthRatio, 1f, maxHp) : maxHp;
            mana = preserveRatios ? Mathf.Clamp(maxMana * manaRatio, 0f, maxMana) : maxMana;
        }
        internal void RefreshAfterLevelUp() { RecalculateDerivedStats(true);if(UnitData.level>lastDisplayedLevel)battle.Feedback(this,CombatFeedbackKind.LevelUp,0,"Lv."+UnitData.level);lastDisplayedLevel=UnitData.level; }
        internal void ScaleDifficulty(float scale) { maxHp *= scale; hp = maxHp; damage *= Mathf.Sqrt(scale); }
        internal void ScaleHealth(float scale) { maxHp *= scale; hp = maxHp; }
        private void Update()
        {
            desiredMoveDirection=Vector2.zero;
            if (dead || battle==null || battle.IsEnded || battle.IntroActive || Time.timeScale<=0) return; float recovery=Time.deltaTime*(paralysisTime>0f?.5f:1f)*(slowTime>0f?.5f:1f);cooldown -= recovery; Vector2 position = transform.position;
            bool wasDepleted=mana<=0f;
            mana=Mathf.Min(maxMana,mana+manaRegen*Time.deltaTime*(slowTime>0f?.5f:1f));
            float manaAfterRegeneration=mana;
            chargeAllowedFromDepletion=false;
            for (int i = 0; i < magicCooldowns.Count; i++) magicCooldowns[i] = Mathf.Max(0f, magicCooldowns[i] - recovery);
            SpriteRenderer bodyRenderer = GetComponent<SpriteRenderer>();
            if (bodyRenderer != null) bodyRenderer.sortingOrder = Mathf.Clamp(100 - Mathf.RoundToInt(transform.position.y * 10f), -50, 250);
            float regeneration=SpeciesPassiveCalculator.Sum(speciesPassive,PassiveEffectType.HpRegeneration);
            if(regeneration>0){passiveRegenerationTime+=Time.deltaTime;if(passiveRegenerationTime>=1){Heal(maxHp*regeneration*passiveRegenerationTime);passiveRegenerationTime=0;}}
            buffs.Tick(Time.deltaTime);
            UpdateSupportVisual();
            for (int i = 0; i < damageOverTimeTime.Length; i++)
            {
                if (damageOverTimeTime[i] <= 0f) continue;
                float tick = Mathf.Min(damageOverTimeTime[i], Time.deltaTime);
                damageOverTimeTime[i] -= tick;
                Combatant damageSource=damageOverTimeSources[i];
                float dealt=TakeDamageCore(damageOverTime[i] * tick, damageOverTimeElements[i], true, true);
                if(battle!=null)battle.RecordPvpDamage(damageSource,this,dealt);
            }
            freezeTime = Mathf.Max(0f, freezeTime - Time.deltaTime);
            paralysisTime = Mathf.Max(0f, paralysisTime - Time.deltaTime);
            sleepTime = Mathf.Max(0f, sleepTime - Time.deltaTime);
            silenceTime = Mathf.Max(0f, silenceTime - Time.deltaTime);
            curseTime = Mathf.Max(0f, curseTime - Time.deltaTime);
            if (slowTime > 0f) { slowTime -= Time.deltaTime; if (slowTime <= 0f) slowMultiplier = 1f; }
            if (previewMode)
            {
                previewFlashTime=Mathf.Max(0,previewFlashTime-Time.deltaTime);
                if(bodyRenderer!=null)
                {
                    bodyRenderer.sortingOrder=5;
                    bodyRenderer.color=previewFlashTime>0?previewFlashColor:freezeTime>0?new Color(.35f,.8f,1f):paralysisTime>0?Color.yellow:damageOverTimeTime[(int)DebuffType.Poison]>0?new Color(.45f,1f,.45f):damageOverTimeTime[(int)DebuffType.Burn]>0?new Color(1f,.45f,.2f):slowTime>0?new Color(.7f,.65f,1f):Color.white;
                }
                return;
            }
            if (dead || freezeTime > 0f || sleepTime > 0f) return;
            bool castThisFrame=CastPrograms();
            // If the frame began empty, this frame's passive regeneration alone must not
            // erase the opportunity to charge. A spell that spent MP takes priority.
            chargeAllowedFromDepletion=(wasDepleted && Mathf.Approximately(mana,manaAfterRegeneration)) || (!castThisFrame && AllAvailableMagicCoolingDown());
            bool canCharge = mana <= 0f || chargeAllowedFromDepletion;
            if (chargeRemaining > 0f || retreatRemaining > 0f) return;
            if(ApproachProgramSupport())return;
            if (IsFriendly || battle.IsPvp)
            {
                float lowHpPriority = personality == null ? .5f : personality.lowHpPriority;
                Combatant target = battle.IsPvp ? battle.FindPvpTarget(position, Mathf.Max(range, 24f), lowHpPriority, IsFriendly) : battle.FindPreferredEnemy(position, range, lowHpPriority);
                // When nothing is in sight, approach the nearest opponent rather than
                // standing still forever outside the spell's targeting range.
                if (target == null && !battle.IsPvp) target = battle.FindNearestEnemy(position, 1000f);
                if (target == null) return;
                Vector2 delta = (Vector2)target.transform.position - position;
                float caution = personality == null ? 0f : personality.caution;
                float aggression = personality == null ? .5f : personality.aggression;
                float preferredRange = range;
                for (int offset = 0; offset < magics.Count; offset++)
                {
                    int index = offset;
                    BuiltMagic candidate = magics[index];
                    if(!candidate.available)continue;
                    if(candidate.targetsAllies)continue;
                    if (magicCooldowns[index] > 0f || candidate.mpCost > mana || silenceTime > 0f) continue;
                    preferredRange = Mathf.Min(range,candidate.range);
                    var program = candidate.program;
                    if (program != null && program.instructions[program.root].kind == Spells.SpellNodeKind.EnemyNearTrigger)
                        preferredRange = Mathf.Min(preferredRange, program.instructions[program.root].parameters.radius);
                    break;
                }
                float desiredDistance = preferredRange * Mathf.Lerp(.52f, .92f, Mathf.Max(aggression, 1f - caution));
                if (delta.magnitude > desiredDistance) Move((delta.normalized + battle.Separation(this, IsFriendly) * .32f).normalized);
                else if (delta.magnitude < desiredDistance * .48f && caution > .55f) Move((-delta.normalized + battle.Separation(this, IsFriendly) * .32f).normalized);
                if (canCharge && cooldown <= 0f && delta.magnitude > ChargeStartDistance)
                {
                    Move((delta.normalized + battle.Separation(this, IsFriendly) * .32f).normalized);
                }
                else if (canCharge && cooldown <= 0f && BasicAttack(target,damage))
                {
                    cooldown = ChargeCooldown();
                }
            }
            else
            {
                Combatant target = battle.FindNearestAlly(position); if (target == null) return;
                Vector2 delta = (Vector2)target.transform.position - position; bool ranged = Type == EnemyArchetype.Ranged; float desired = ranged ? range * .82f : range;
                if (delta.magnitude > desired) Move((delta.normalized + battle.Separation(this, false) * .2f).normalized);
                if (canCharge && cooldown <= 0f && delta.magnitude > ChargeStartDistance) Move((delta.normalized + battle.Separation(this, false) * .2f).normalized);
                else if (canCharge && cooldown <= 0f && BasicAttack(target,damage)) cooldown = ChargeCooldown();
            }
        }
        private bool BasicAttack(Combatant target,float amount)
        {
            if((mana>0f && !chargeAllowedFromDepletion) || target==null || !target.IsAlive || target.IsFriendly==IsFriendly || chargeRemaining>0f || retreatRemaining>0f ||
               ((Vector2)target.transform.position-(Vector2)transform.position).sqrMagnitude>ChargeStartDistance*ChargeStartDistance)return false;
            chargeTarget=target; chargeRemaining=ChargeDuration; chargeDamage=amount*.5f; desiredMoveDirection=Vector2.zero;
            Vector2 direction=(Vector2)target.transform.position-(Vector2)transform.position;
            chargeDirection=direction.sqrMagnitude>.0001f ? direction.normalized : (IsFriendly ? Vector2.right : Vector2.left);
            return true;
        }
        private float ChargeCooldown()
        {
            float multiplier = personality == null ? 1f : Mathf.Clamp(personality.chargeCooldownMultiplier,.1f,5f);
            return Mathf.Max(1f,Mathf.Max(.7f,attackInterval)*multiplier/Mathf.Max(.1f,buffs.SpeedMultiplier));
        }
        private void BeginChargeRetreat(Combatant target)
        {
            Vector2 away=target==null ? -chargeDirection : body.position-(Vector2)target.transform.position;
            retreatDirection=away.sqrMagnitude>.0001f ? away.normalized : -chargeDirection;
            retreatRemaining=personality==null ? 1.3f : Mathf.Clamp(personality.chargeRetreatDistance,.1f,3f);
            retreatTime=1.5f; chargeTarget=null; chargeRemaining=0f;
        }
        private bool AllAvailableMagicCoolingDown()
        {
            if(silenceTime>0f || magicExecutionBlocked)return true;
            for(int i=0;i<magics.Count;i++)
            {
                var magic=magics[i];
                if(!magic.available || magic.program==null || magicCooldowns[i]>0f || magic.mpCost>mana || !ProgramTriggerReady(magic.program))continue;
                var target=Spells.SpellExecutor.TargetForCast(battle,this,magic.program);
                if(target!=null && (!magic.targetsAllies || target.NeedsSupport(magic)))return false;
            }
            return true;
        }
        private bool CastPrograms()
        {
            magicExecutionBlocked=false;
            if(silenceTime>0 || Time.timeScale<=0)return false;
            bool cast=false;
            for(int i=0;i<magics.Count;i++)
            {
                BuiltMagic magic=magics[i];var program=magic.program;
                if(!magic.available)continue;
                if(program==null || magicCooldowns[i]>0 || mana<magic.mpCost)continue;
                if(!ProgramTriggerReady(program))continue;
                Combatant target=Spells.SpellExecutor.TargetForCast(battle,this,program);
                if(target==null || magic.targetsAllies && !target.NeedsSupport(magic))continue;
                if(Spells.SpellExecutor.Cast(battle,this,target,program)==null){magicExecutionBlocked=true;continue;}
                mana-=magic.mpCost;magicCooldowns[i]=magic.cooldown;magicCastCounts[i]++;cast=true;
            }
            return cast;
        }
        private bool ProgramTriggerReady(Spells.CompiledSpell program)
        {
            var root=program.instructions[program.root];
            if(root.kind==Spells.SpellNodeKind.HPThresholdTrigger)return HealthRatio<=root.parameters.threshold;
            if(root.kind!=Spells.SpellNodeKind.EnemyNearTrigger)return true;
            var enemies=IsFriendly?battle.Enemies:battle.Allies;float radius=Mathf.Min(range,root.parameters.radius);
            for(int i=0;i<enemies.Count;i++){var enemy=enemies[i];if(enemy!=null && enemy.IsAlive && enemy.gameObject.activeInHierarchy && ((Vector2)enemy.transform.position-(Vector2)transform.position).sqrMagnitude<=radius*radius)return true;}
            return false;
        }
        private bool ApproachProgramSupport()
        {
            if(silenceTime>0 || Time.timeScale<=0)return false;
            for(int index=0;index<magics.Count;index++)
            {
                var magic=magics[index];
                if(!magic.available)continue;
                if(magic.program==null || magic.program.target==Spells.SpellTarget.Self || !magic.targetsAllies || magicCooldowns[index]>0 || mana<magic.mpCost || !ProgramTriggerReady(magic.program))continue;
                Combatant best=null;float score=float.MaxValue;
                var team=IsFriendly?battle.Allies:battle.Enemies;
                for(int allyIndex=0;allyIndex<team.Count;allyIndex++)
                {
                    var ally=team[allyIndex];
                    if(ally==null || ally==this || !ally.IsAlive || !ally.NeedsSupport(magic))continue;
                    float distance=((Vector2)ally.transform.position-(Vector2)transform.position).sqrMagnitude;
                    float supportSearch=Mathf.Max(range,magic.range*1.5f);
                    if(distance>supportSearch*supportSearch || distance<=magic.range*magic.range)continue;
                    float value=ally.HealthRatio*1000+distance*.001f;if(value<score){score=value;best=ally;}
                }
                if(best!=null){Move(((Vector2)best.transform.position-(Vector2)transform.position).normalized);return true;}
            }
            return false;
        }




        private void Move(Vector2 direction) { desiredMoveDirection=direction.sqrMagnitude>.001f?direction.normalized:Vector2.zero; }
        internal void ClampToView()
        {
            if(body==null || battle==null)return;
            body.position=battle.ClampToView(body.position,GetComponent<SpriteRenderer>());
        }
        private void MoveBounded(Vector2 position)
        { body.MovePosition(battle.ClampToView(position,GetComponent<SpriteRenderer>())); }
        private void FixedUpdate()
        {
            if(battle!=null && battle.IntroActive)return;
            if(body!=null && battle!=null)ClampToView();
            if(body==null || dead || battle==null || battle.IsEnded || freezeTime>0 || sleepTime>0)
            { chargeTarget=null; chargeRemaining=0f; return; }
            if(Time.timeScale<=0)return;
            if(retreatRemaining>0f)
            {
                float step=Mathf.Min(retreatRemaining,Mathf.Clamp(speed,.5f,6f)*MovementDebuffMultiplier*buffs.SpeedMultiplier*Time.fixedDeltaTime);
                if(step>0)MoveBounded(body.position+retreatDirection*step);
                retreatRemaining=Mathf.Max(0,retreatRemaining-step);retreatTime-=Time.fixedDeltaTime;
                if(retreatTime<=0)retreatRemaining=0;
                return;
            }
            if(chargeRemaining>0f)
            {
                Combatant target=chargeTarget;
                if(target==null || !target.IsAlive || !target.gameObject.activeInHierarchy){BeginChargeRetreat(target);return;}
                Vector2 delta=(Vector2)target.transform.position-body.position;
                float distance=delta.magnitude;
                if(distance>.0001f)chargeDirection=delta/distance;
                float contact=ChargeContactDistance+(target.Type==EnemyArchetype.Boss?.25f:0f);
                float chargeFactor=MovementDebuffMultiplier;
                float step=ChargeSpeed*Time.fixedDeltaTime*chargeFactor;
                if(distance>contact && distance>.0001f)MoveBounded(body.position+delta/distance*Mathf.Min(step,distance-contact));
                chargeRemaining-=Time.fixedDeltaTime*chargeFactor;
                if(distance<=contact+step)
                {
                    battle.DealDamage(this,target,chargeDamage*OutgoingDamageMultiplier,AttributeElement.None,false);
                    BeginChargeRetreat(target);
                }
                else if(chargeRemaining<=0f)BeginChargeRetreat(target);
                return;
            }
            if(desiredMoveDirection.sqrMagnitude>.001f)MoveBounded(body.position+desiredMoveDirection*speed*MovementDebuffMultiplier*buffs.SpeedMultiplier*Time.fixedDeltaTime);
        }
        private void OnDisable(){desiredMoveDirection=Vector2.zero;chargeTarget=null;chargeRemaining=retreatRemaining=0f;}
        internal void Heal(float amount) { if(dead)return;float before=hp;float healing=Mathf.Max(0,amount)*(1+SpeciesPassiveCalculator.Sum(speciesPassive,PassiveEffectType.HealingReceived));if(battle!=null && battle.IsPvp)healing=PvpRules.LimitHealing(healing,maxHp);hp = Mathf.Min(maxHp, hp + healing); if(hp>before){previewFlashColor=Color.green;previewFlashTime=.5f;battle.Feedback(this,CombatFeedbackKind.Heal,hp-before);} }
        internal void ApplyBuff(BuffType effect,float strength,float duration)
        {
            if(dead)return;
            bool changed=buffs.Needs(effect,strength,duration);buffs.Apply(effect,strength,duration);UpdateSupportVisual();
            if(changed)battle.Feedback(this,CombatFeedbackKind.Status,0,effect==BuffType.AttackBoost?"攻撃↑":effect==BuffType.DefenseBoost?"防御↑":"加速");
        }
        internal bool NeedsSupport(BuiltMagic magic)
        {
            if(dead || magic.program==null)return false;
            foreach(var node in magic.program.instructions)
                if(node.kind==Spells.SpellNodeKind.Heal && HealthRatio<.98f ||
                    node.kind==Spells.SpellNodeKind.ApplyBuff && buffs.Needs(node.parameters.buff,node.parameters.multiplier,node.parameters.seconds))return true;
            return false;
        }

        private void UpdateSupportVisual()
        {
            if(buffAura==null && (buffs.Active || previewFlashTime>0))
            {
                var auraObject=new GameObject("Support aura");auraObject.transform.SetParent(transform,false);auraObject.layer=gameObject.layer;
                buffAura=auraObject.AddComponent<LineRenderer>();buffAura.useWorldSpace=false;buffAura.loop=true;buffAura.positionCount=32;buffAura.widthMultiplier=.035f;buffAura.sortingOrder=80;
                if(buffAuraMaterial==null)buffAuraMaterial=new Material(Shader.Find("Sprites/Default"));
                buffAura.sharedMaterial=buffAuraMaterial;
                for(int i=0;i<32;i++){float angle=i*Mathf.PI*2/32;buffAura.SetPosition(i,new Vector3(Mathf.Cos(angle)*.72f,Mathf.Sin(angle)*.72f,0));}
            }
            if(buffAura!=null)
            {
                buffAura.enabled=buffs.Active || previewFlashTime>0;
                buffAura.startColor=buffAura.endColor=previewFlashTime>0?previewFlashColor:buffs.Tint;
            }
            if(!previewMode)previewFlashTime=Mathf.Max(0,previewFlashTime-Time.deltaTime);
        }
        internal float DrainMana(float amount) { float drained = Mathf.Min(mana, Mathf.Max(0f, amount)); mana -= drained; return drained; }
        internal void RestoreMana(float amount) { float before=mana;mana = Mathf.Min(maxMana, mana + Mathf.Max(0f, amount)); if(mana>before){battle.Feedback(this,CombatFeedbackKind.Mana,mana-before);if(previewMode){previewFlashColor=Color.cyan;previewFlashTime=.35f;}} }
        internal void KnockbackFrom(Combatant source, float distance)
        {
            if (source == null || dead) return;
            Vector2 direction = (Vector2)transform.position - (Vector2)source.transform.position;
            if (direction.sqrMagnitude > .001f) MoveBounded(body.position + direction.normalized * Mathf.Clamp(distance, 0f, 2f));
        }
        internal float TakeDamage(float amount, AttributeElement element = AttributeElement.None, bool magical = false)
        { return TakeDamageCore(amount,element,magical,false); }
        private float TakeDamageCore(float amount, AttributeElement element, bool magical, bool ignoreMitigation)
        {
            if (dead) return 0f;
            int index = Mathf.Clamp((int)element, 0, elementReduction.Length - 1);
            float defense = magical ? magicDefense : physicalDefense;
            float protection=HealthRatio<=.35f?Mathf.Clamp01(SpeciesPassiveCalculator.Sum(speciesPassive,PassiveEffectType.LowHpProtection)):0;
            bool melting=!ignoreMitigation && freezeTime>0f && element==AttributeElement.Fire;
            float afterDefense = ignoreMitigation ? amount : ResistanceCalculator.ApplyDefense(amount*(melting?1.25f:1f), defense)*buffs.DamageMultiplier*(1-protection);
            float damageAmount=Mathf.Max(0f, afterDefense * (ignoreMitigation ? 1f : 1f - Mathf.Clamp01(elementReduction[index])));
            float dealt = Mathf.Min(hp,damageAmount);
            if(battle!=null && battle.CaptureFocus==this)dealt=Mathf.Min(dealt,Mathf.Max(0,hp-1));
            if (dealt > 0f) {if(!ignoreMitigation){bool released=sleepTime>0f || melting;sleepTime=0f;if(melting)freezeTime=0f;if(released && battle!=null && battle.IsPvp)pvpControlImmuneUntil=Time.time+Mathf.Max(freezeTime,sleepTime)+PvpRules.ControlImmunitySeconds;}lastDamageDisplayTime=Time.unscaledTime;battle.Feedback(this,CombatFeedbackKind.Damage,dealt);}
            hp -= dealt;
            if (hp <= 0f) Die();
            return dealt;
        }
        internal bool ApplyInstantDeath()
        {
            if(dead || battle==null || battle.CaptureFocus==this)return false;
            hp=0f;battle.Feedback(this,CombatFeedbackKind.Status,0,"即死");Die();return true;
        }
        internal bool ApplyInstantDeathFrom(Combatant source)
        {
            float before=hp;
            if(!ApplyInstantDeath())return false;
            if(battle!=null)battle.RecordPvpDamage(source,this,before);
            return true;
        }
        private void Die()
        {
            if(dead)return;dead=true;if(debuffVisual!=null)debuffVisual.Clear();if(buffAura!=null)buffAura.enabled=false;
            var renderer=GetComponent<SpriteRenderer>();if(renderer!=null)renderer.color=Color.gray;
            if(!revivePassiveUsed && SpeciesPassiveCalculator.Sum(speciesPassive,PassiveEffectType.ReviveAlliesOnDeath)>0)
            { revivePassiveUsed=true;if(battle!=null)battle.ReviveAllies(this); }
            if(dead && battle!=null)battle.OnDeath(this);
        }
        internal bool ReviveFully()
        {
            if(!dead || !gameObject.activeInHierarchy)return false;
            dead=false;hp=maxHp;mana=maxMana;desiredMoveDirection=Vector2.zero;chargeRemaining=retreatRemaining=0;chargeTarget=null;
            pvpControlImmuneUntil=0f;
            slowTime=freezeTime=paralysisTime=sleepTime=silenceTime=curseTime=0;slowMultiplier=1f;
            Array.Clear(damageOverTime,0,damageOverTime.Length);Array.Clear(damageOverTimeTime,0,damageOverTimeTime.Length);Array.Clear(damageOverTimeSources,0,damageOverTimeSources.Length);
            buffs.Clear();cachedStatusFlags=-1;if(debuffVisual!=null)debuffVisual.Clear();
            var renderer=GetComponent<SpriteRenderer>();if(renderer!=null)renderer.color=Color.white;
            return true;
        }
        internal bool ApplyDebuff(DebuffType type, float duration, float dps, float chance, AttributeElement element)
        { return ApplyDebuffFrom(type,duration,dps,chance,element,null); }
        internal bool ApplyDebuffFrom(DebuffType type, float duration, float dps, float chance, AttributeElement element, Combatant source)
        {
            if (dead) return false;
            bool pvpControl=battle!=null && battle.IsPvp && PvpRules.IsHardControl(type);
            if(pvpControl && Time.time<pvpControlImmuneUntil)return false;
            int debuffIndex = Mathf.Clamp((int)type, 0, debuffResistance.Length - 1);
            int elementIndex = Mathf.Clamp((int)element, 0, elementAilmentResistance.Length - 1);
            float applicationChance = StatusApplication.Chance(chance, elementAilmentResistance[elementIndex], debuffResistance[debuffIndex]);
            if (!StatusApplication.Roll(applicationChance)) return false;
            float safeDuration = Mathf.Clamp(duration, .1f, 20f);
            if(pvpControl){safeDuration=Mathf.Min(safeDuration,PvpRules.MaximumControlSeconds);pvpControlImmuneUntil=Time.time+safeDuration+PvpRules.ControlImmunitySeconds;}
            if (type == DebuffType.Poison || type == DebuffType.Burn)
            {
                damageOverTime[debuffIndex] = maxHp * (type == DebuffType.Poison ? .02f : .05f);
                damageOverTimeElements[debuffIndex] = element;
                damageOverTimeSources[debuffIndex] = source;
                damageOverTimeTime[debuffIndex] = type == DebuffType.Poison ? 10f : 4f;
            }
            else if (type == DebuffType.Freeze) freezeTime = Mathf.Max(freezeTime, safeDuration);
            else if (type == DebuffType.Slow) { slowTime = Mathf.Max(slowTime, safeDuration); slowMultiplier = Mathf.Min(slowMultiplier, .5f); }
            else if (type == DebuffType.Paralysis) paralysisTime = Mathf.Max(paralysisTime, safeDuration);
            else if (type == DebuffType.Sleep) sleepTime = Mathf.Max(sleepTime, safeDuration);
            else if (type == DebuffType.Silence) silenceTime = Mathf.Max(silenceTime, safeDuration);
            else if (type == DebuffType.Curse) curseTime = Mathf.Max(curseTime, safeDuration);
            if(debuffVisual==null)
            {
                var effect=new GameObject("Debuff effects");effect.transform.SetParent(transform,false);
                debuffVisual=effect.AddComponent<DebuffVisual>();debuffVisual.Initialize(this);
            }
            else debuffVisual.Wake();
            battle.Feedback(this,CombatFeedbackKind.Status,0,StatusNames[debuffIndex]);
            return true;
        }
        private void OnGUI()
        {
            if (battle.HasPresentation || previewMode || dead || Camera.main == null) return; Vector3 screen = Camera.main.WorldToScreenPoint(transform.position); if (screen.z < 0) return;
            if(battle.IsPvp && !IsLocalView)return;
            bool ownSide=IsLocalView;
            float width = ownSide ? 44f : Type == EnemyArchetype.Boss ? 70f : 30f; Rect rect = new Rect(screen.x - width / 2f, Screen.height - screen.y - 25f, width, 5f);
            if (friendlyNameStyle == null) friendlyNameStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = new Color(.7f, .95f, 1f) } };
            if (enemyNameStyle == null) enemyNameStyle = new GUIStyle(GUI.skin.label) { fontSize = 9, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(.9f, .85f, .83f) } };
            GUI.Label(new Rect(rect.x - 24f, rect.y - 16f, rect.width + 48f, 14f), CharacterName, ownSide ? friendlyNameStyle : enemyNameStyle);
            GUI.color = new Color(0, 0, 0, .8f); GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = ownSide ? new Color(.2f, .9f, 1f) : new Color(1f, .25f, .3f); GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(hp / maxHp), rect.height), Texture2D.whiteTexture); GUI.color = Color.white;
        }
    }



    public sealed class MagicImpactVisual : MonoBehaviour
    {
        private static readonly Queue<MagicImpactVisual> pool = new Queue<MagicImpactVisual>();
        private static Material ringMaterial;
        private LineRenderer line;
        private float age, duration, radius;
        private Color color;

        public static void Play(Vector2 position, Color tint, float size, float seconds, Transform parent = null)
        {
            MagicImpactVisual visual = TakeReusableVisual();
            if (visual == null) visual = Create();
            if (visual == null) return;
            visual.transform.SetParent(parent,true); visual.gameObject.layer=parent==null?0:parent.gameObject.layer;
            if (visual.Configure(position, tint, size, seconds)) return;

            if (visual != null) Destroy(visual.gameObject);
            visual = Create();
            if (visual != null) {visual.transform.SetParent(parent,true);visual.gameObject.layer=parent==null?0:parent.gameObject.layer;}
            if (visual != null && !visual.Configure(position, tint, size, seconds)) Destroy(visual.gameObject);
        }

        private static MagicImpactVisual TakeReusableVisual()
        {
            while (pool.Count > 0)
            {
                MagicImpactVisual candidate = pool.Dequeue();
                if (candidate != null) return candidate;
            }
            return null;
        }

        private static MagicImpactVisual Create()
        {
            GameObject go = new GameObject("Pooled Magic Impact");
            return go.AddComponent<MagicImpactVisual>();
        }

        private void Awake()
        {
            EnsureLineRenderer();
            if (gameObject != null) gameObject.SetActive(false);
        }

        private bool EnsureLineRenderer()
        {
            if (this == null || gameObject == null) return false;
            if (line == null) line = GetComponent<LineRenderer>();
            if (line == null) line = gameObject.AddComponent<LineRenderer>();
            if (line == null) return false;

            line.useWorldSpace = false; line.loop = true; line.positionCount = 40; line.sortingOrder = 76;
            if (ringMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null) ringMaterial = new Material(shader);
            }
            if (ringMaterial != null) line.material = ringMaterial;
            for (int i = 0; i < line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f));
            }
            return true;
        }

        private bool Configure(Vector2 position, Color tint, float size, float seconds)
        {
            if (this == null || gameObject == null) return false;
            if (line == null && !EnsureLineRenderer()) return false;
            age = 0f; duration = Mathf.Max(.08f, seconds); radius = Mathf.Clamp(size, .25f, 8f); color = tint;
            transform.position = new Vector3(position.x, position.y, -.1f);
            transform.localScale = Vector3.one * radius * .3f;
            line.widthMultiplier = Mathf.Clamp(radius * .07f, .035f, .18f);
            line.startColor = new Color(color.r, color.g, color.b, .95f); line.endColor = line.startColor; line.enabled = true;
            gameObject.SetActive(true);
            return true;
        }

        private void Update()
        {
            age += Time.deltaTime;
            float progress = Mathf.Clamp01(age / duration);
            transform.localScale = Vector3.one * radius * Mathf.Lerp(.3f, 1f, progress);
            float alpha = 1f - progress;
            line.startColor = new Color(color.r, color.g, color.b, alpha);
            line.endColor = line.startColor;
            line.widthMultiplier = Mathf.Lerp(.09f, .015f, progress);
            if (progress < 1f) return;
            line.enabled = false;
            gameObject.SetActive(false);
            if (pool.Count < 32) pool.Enqueue(this); else Destroy(gameObject);
        }
    }
}
