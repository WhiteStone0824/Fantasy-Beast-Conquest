using System.Collections.Generic;
using FusionAnyMonster.Battle;
using UnityEngine;
namespace FusionAnyMonster.Spells
{
    public sealed class CastSession
    {
        public CompiledSpell spell;public Combatant caster;public int projectilesGenerated,eventsReserved,pending,projectilesAlive;
        public float deadline;internal uint random;internal bool active;
        public bool TryProjectile(int generation)
        {if(generation<1 || generation>spell.maxGeneration || projectilesGenerated>=spell.maxProjectiles)return false;projectilesGenerated++;return true;}
        public bool TryEvent(){if(eventsReserved>=spell.maxEvents)return false;eventsReserved++;return true;}
        public float Random(){random^=random<<13;random^=random>>17;random^=random<<5;return (random&0xffffff)/16777216f;}
    }
    public struct SpellEventContext
    {
        public Combatant target,previous,previous2,previous3;
        public Vector2 position;
        public float multiplier,lastDamage;
        public AttributeElement element;
        public int generation,source;
    }
    public sealed class SpellExecutor:MonoBehaviour
    {
        private struct PendingEvent {public CastSession session;public int node;public SpellEventContext context;public float due;public bool areaTick;public float areaUntil;}
        private readonly PendingEvent[] pending=new PendingEvent[2048];private int count;
        private readonly List<CastSession> sessions=new List<CastSession>(128);
        private readonly Queue<SpellProjectile> pool=new Queue<SpellProjectile>(128);
        private readonly List<SpellProjectile> projectiles=new List<SpellProjectile>(256);
        private BattleDemo battle;private int castIndex;
        public int PendingEvents {get{return count;}}
        internal bool Running {get{return battle!=null && !battle.HasEnded;}}
        public static Combatant TargetForCast(BattleDemo battle,Combatant caster,CompiledSpell spell)
        {
            if(spell.target==SpellTarget.Self)return caster;
            bool support=IsSupportTarget(spell.target);var list=caster.IsFriendly==support?battle.Allies:battle.Enemies;
            Combatant best=null;float score=float.MaxValue;
            for(int i=0;i<list.Count;i++)
            {
                var t=list[i];if(t==null || !t.IsAlive || !t.gameObject.activeInHierarchy)continue;
                float distance=((Vector2)t.transform.position-(Vector2)caster.transform.position).sqrMagnitude;if(distance>spell.range*spell.range)continue;
                float value=spell.target==SpellTarget.LowestHealthAlly || spell.target==SpellTarget.WeakestEnemy?t.HealthRatio*1000+distance*.001f:distance;
                if(value<score){score=value;best=t;}
            }
            return best;
        }
        public static CastSession Cast(BattleDemo battle,Combatant caster,Combatant target,CompiledSpell spell,int seed=0)
        {
            if(battle==null || battle.HasEnded || !battle.gameObject.activeInHierarchy || caster==null || !caster.IsAlive || !caster.gameObject.activeInHierarchy || spell==null)return null;
            var runtime=battle.GetComponent<SpellExecutor>();if(runtime==null){runtime=battle.gameObject.AddComponent<SpellExecutor>();runtime.battle=battle;}
            if(!runtime.isActiveAndEnabled)return null;
            return runtime.Begin(caster,target,spell,seed);
        }
        private CastSession Begin(Combatant caster,Combatant target,CompiledSpell spell,int seed)
        {
            CastSession session=null;foreach(var candidate in sessions)if(!candidate.active){session=candidate;break;}
            if(session==null){if(sessions.Count>=128)return null;session=new CastSession();sessions.Add(session);}
            session.active=true;session.spell=spell;session.caster=caster;session.pending=session.projectilesAlive=session.eventsReserved=session.projectilesGenerated=0;
            session.deadline=Time.time+60;session.random=(uint)(seed==0?++castIndex:seed);if(session.random==0)session.random=1;
            var context=new SpellEventContext{target=target,position=caster.transform.position,multiplier=1,source=-1};
            if(!Schedule(session,spell.root,context,0)){session.active=false;session.spell=null;session.caster=null;return null;}
            return session;
        }
        private bool Schedule(CastSession s,int node,SpellEventContext context,float delay,bool areaTick=false,float until=0)
        {
            if(node<0 || count>=pending.Length || !s.active || !s.TryEvent())return false;
            pending[count++]=new PendingEvent{session=s,node=node,context=context,due=Time.time+Mathf.Max(0,delay),areaTick=areaTick,areaUntil=until};s.pending++;return true;
        }
        private void Emit(CastSession s,int node,SpellPort port,SpellEventContext context,float delay=0)
        {if(node>=0)Schedule(s,s.spell.instructions[node].outputs[(int)port],context,delay);}
        private void Update()
        {
            if(Time.timeScale<=0)return;
            int processed=0;
            for(int i=0;i<count && processed<512;)
            {
                var e=pending[i];var s=e.session;
                if(!s.active || s.caster==null || !s.caster.IsAlive || !s.caster.gameObject.activeInHierarchy || battle==null || battle.HasEnded || Time.time>s.deadline)
                {pending[i]=pending[--count];pending[count]=default(PendingEvent);s.pending--;continue;}
                if(e.due>Time.time){i++;continue;}
                // FIFO among ready events; this keeps ordered effects such as Damage -> LifeSteal deterministic.
                for(int j=i+1;j<count;j++)pending[j-1]=pending[j];pending[--count]=default(PendingEvent);s.pending--;processed++;
                Execute(e);
            }
            foreach(var s in sessions)if(s.active && s.pending==0 && s.projectilesAlive==0){s.active=false;s.caster=null;s.spell=null;}
        }
        private void Execute(PendingEvent e)
        {
            var s=e.session;var n=s.spell.instructions[e.node];var p=n.parameters;var c=e.context;
            switch(n.kind)
            {
                case SpellNodeKind.PeriodicTrigger:case SpellNodeKind.EnemyNearTrigger:case SpellNodeKind.HPThresholdTrigger:break;
                case SpellNodeKind.Delay:Emit(s,e.node,SpellPort.Next,c,p.seconds);return;
                case SpellNodeKind.RandomBranch:Emit(s,e.node,s.Random()<p.probability?SpellPort.True:SpellPort.False,c);return;
                case SpellNodeKind.Conditional:
                    bool condition=p.condition==SpellCondition.CasterLowHealth?s.caster.HealthRatio<=p.threshold:p.condition==SpellCondition.TargetLowHealth?c.target!=null && c.target.HealthRatio<=p.threshold:p.condition==SpellCondition.TargetAlive?c.target!=null && c.target.IsAlive:EnemyNear(s,c.position,p.radius);
                    Emit(s,e.node,condition?SpellPort.True:SpellPort.False,c);return;
                case SpellNodeKind.Sequence:Emit(s,e.node,SpellPort.Next,c);Emit(s,e.node,SpellPort.True,c);Emit(s,e.node,SpellPort.False,c);return;
                case SpellNodeKind.SpawnProjectile:case SpellNodeKind.SplitProjectile:
                    Spawn(s,e.node,c);break;
                case SpellNodeKind.SpawnArea:
                    if(!e.areaTick)
                    {
                        var target=p.centerOnCaster || p.target==SpellTarget.Self?s.caster:Select(s,p.target,c.position,c.target);
                        if(target!=null)c.position=target.transform.position;
                        MagicImpactVisual.Play(c.position,BattleDemo.ElementColor(p.element),p.radius,Mathf.Min(p.seconds,s.spell.maxLifetime),transform);
                        e.areaUntil=Time.time+Mathf.Min(p.seconds,s.spell.maxLifetime);c.element=p.element;
                        c.multiplier*=p.multiplier;
                    }
                    Area(s,e.node,c,p.radius,false);
                    if(Time.time+.5f<e.areaUntil)Schedule(s,e.node,c,.5f,true,e.areaUntil);
                    else Emit(s,e.node,SpellPort.OnExpire,c,Mathf.Max(0,e.areaUntil-Time.time));
                    if(e.areaTick)return;
                    break;
                case SpellNodeKind.Damage:
                    if(Hostile(s,c.target))
                    {
                        bool alive=c.target.IsAlive;
                        c.lastDamage=battle.DealDamage(s.caster,c.target,(s.spell.basePower+s.caster.MagicAttack)*p.multiplier*c.multiplier*s.caster.OutgoingDamageMultiplier,p.element,true);
                        if(alive && !c.target.IsAlive){Emit(s,e.node,SpellPort.OnKill,c);if(c.source>=0)Emit(s,c.source,SpellPort.OnKill,c);}
                    }
                    break;
                case SpellNodeKind.Heal:
                    if(Friendly(s,c.target))c.target.Heal((s.spell.basePower+s.caster.MagicAttack)*p.multiplier*c.multiplier*s.caster.HealingDoneMultiplier);break;
                case SpellNodeKind.ApplyBuff:
                    if(Friendly(s,c.target) && System.Enum.IsDefined(typeof(BuffType),p.buff))c.target.ApplyBuff(p.buff,Mathf.Clamp(p.multiplier,.05f,.5f),p.seconds);break;
                case SpellNodeKind.ApplyStatus:
                    if(Hostile(s,c.target))c.target.ApplyDebuffFrom(p.status,p.seconds,0,p.probability,p.element,s.caster);break;
                case SpellNodeKind.Explosion:
                    if(s.Random()<p.probability)
                    {
                        MagicImpactVisual.Play(c.position,BattleDemo.ElementColor(p.element),p.radius,.6f,transform);
                        var list=Targets(s,false);for(int i=0;i<list.Count;i++)if(Hostile(s,list[i]) && ((Vector2)list[i].transform.position-c.position).sqrMagnitude<=p.radius*p.radius)
                        {
                            float dealt=battle.DealDamage(s.caster,list[i],(s.spell.basePower+s.caster.MagicAttack)*p.multiplier*c.multiplier*s.caster.OutgoingDamageMultiplier,p.element,true);
                            if(!list[i].IsAlive){var killed=c;killed.target=list[i];killed.position=list[i].transform.position;killed.lastDamage=dealt;Emit(s,e.node,SpellPort.OnKill,killed);if(c.source>=0)Emit(s,c.source,SpellPort.OnKill,killed);}
                        }
                    }
                    break;
                case SpellNodeKind.Knockback:if(Hostile(s,c.target) && s.Random()<p.probability)c.target.KnockbackFrom(s.caster,p.multiplier);break;
                case SpellNodeKind.LifeSteal:if(s.Random()<p.probability)s.caster.Heal(Mathf.Max(0,c.lastDamage)*Mathf.Clamp01(p.multiplier));break;
                case SpellNodeKind.ManaSteal:if(Hostile(s,c.target) && s.Random()<p.probability)s.caster.RestoreMana(c.target.DrainMana(s.spell.basePower*p.multiplier));break;
            }
            Emit(s,e.node,SpellPort.Next,c);
        }
        private void Area(CastSession s,int node,SpellEventContext c,float radius,bool unused)
        {
            var p=s.spell.instructions[node].parameters;bool support=IsSupportTarget(p.target);
            var list=Targets(s,support);
            for(int i=0;i<list.Count;i++)if(list[i]!=null && list[i].IsAlive && list[i].gameObject.activeInHierarchy && ((Vector2)list[i].transform.position-c.position).sqrMagnitude<=radius*radius)
            {var hit=c;hit.target=list[i];hit.position=list[i].transform.position;hit.source=node;hit.lastDamage=0;Emit(s,node,SpellPort.OnHit,hit);}
        }
        internal static bool IsSupportTarget(SpellTarget t){return t==SpellTarget.NearestAlly || t==SpellTarget.LowestHealthAlly || t==SpellTarget.Self;}
        private bool EnemyNear(CastSession s,Vector2 position,float radius)
        {var list=Targets(s,false);for(int i=0;i<list.Count;i++)if(list[i]!=null && list[i].IsAlive && list[i].gameObject.activeInHierarchy && ((Vector2)list[i].transform.position-position).sqrMagnitude<=radius*radius)return true;return false;}
        internal IReadOnlyList<Combatant> Targets(CastSession s,bool support){return s.caster.IsFriendly==support?battle.Allies:battle.Enemies;}
        private static bool Hostile(CastSession s,Combatant t){return t!=null && t.IsAlive && t.gameObject.activeInHierarchy && t.IsFriendly!=s.caster.IsFriendly;}
        private static bool Friendly(CastSession s,Combatant t){return t!=null && t.IsAlive && t.gameObject.activeInHierarchy && t.IsFriendly==s.caster.IsFriendly;}
        internal Combatant Select(CastSession s,SpellTarget mode,Vector2 position,Combatant previous,bool excludePrevious=false,Combatant exclude2=null,Combatant exclude3=null)
        {
            if(mode==SpellTarget.Self)return s.caster;
            var list=Targets(s,IsSupportTarget(mode));Combatant best=null;float score=float.MaxValue;
            for(int i=0;i<list.Count;i++)
            {
                var t=list[i];if(t==null || !t.IsAlive || !t.gameObject.activeInHierarchy || t==exclude2 || t==exclude3 || (mode==SpellTarget.NextEnemy || excludePrevious) && t==previous)continue;
                float distance=((Vector2)t.transform.position-position).sqrMagnitude;if(distance>s.spell.range*s.spell.range)continue;
                float rank=mode==SpellTarget.WeakestEnemy || mode==SpellTarget.LowestHealthAlly?t.HealthRatio*1000+distance*.001f:distance;
                if(rank<score){score=rank;best=t;}
            }
            return best;
        }
        private void Spawn(CastSession s,int node,SpellEventContext c)
        {
            var n=s.spell.instructions[node];var p=n.parameters;int generation=c.generation+1;
            if(n.kind==SpellNodeKind.SplitProjectile && s.Random()>=p.probability)return;
            c.multiplier*=p.multiplier;
            if(n.kind==SpellNodeKind.SpawnProjectile && c.generation==0)c.position=s.caster.transform.position;
            for(int i=0;i<p.count;i++)
            {
                if(!s.TryProjectile(generation))break;
                bool split=n.kind==SpellNodeKind.SplitProjectile;
                Combatant target=Select(s,split && !IsSupportTarget(p.target)?SpellTarget.NextEnemy:p.target,c.position,c.target,split || c.generation>0,c.previous2,c.previous3);
                // Keep the AI/preview's selected first recipient when this node uses the same targeting rule.
                if(!split && c.generation==0 && p.target==s.spell.target &&
                    (IsSupportTarget(p.target)?Friendly(s,c.target):Hostile(s,c.target)) &&
                    ((Vector2)c.target.transform.position-c.position).sqrMagnitude<=s.spell.range*s.spell.range)
                    target=c.target;
                float offset=p.count<=1?p.angle:(p.angle>=359?i*360f/p.count:-p.angle*.5f+i*p.angle/(p.count-1));
                SpellProjectile projectile=null;
                while(pool.Count>0 && projectile==null)projectile=pool.Dequeue();
                if(projectile==null){if(projectiles.Count>=256)break;var go=new GameObject("Pooled program projectile");go.transform.SetParent(transform,false);projectile=go.AddComponent<SpellProjectile>();projectiles.Add(projectile);}
                projectile.gameObject.layer=s.caster.gameObject.layer;
                projectile.Launch(this,s,node,c,target,generation,offset);s.projectilesAlive++;
            }
        }
        internal void Impact(SpellProjectile projectile,Combatant target,bool expired)
        {
            var s=projectile.session;var c=projectile.context;c.target=target;c.position=projectile.transform.position;c.generation=projectile.generation;c.source=projectile.node;c.lastDamage=0;
            if(!expired){c.previous3=c.previous2;c.previous2=c.previous;c.previous=target;}
            Emit(s,projectile.node,expired?SpellPort.OnExpire:SpellPort.OnHit,c);
        }
        internal void Recycle(SpellProjectile p){p.session.projectilesAlive--;p.gameObject.SetActive(false);p.session=null;pool.Enqueue(p);}
        internal void ProjectileDestroyed(SpellProjectile projectile,CastSession session)
        {
            if(session!=null)session.projectilesAlive=Mathf.Max(0,session.projectilesAlive-1);
            projectiles.Remove(projectile);
        }
        private void OnDisable()
        {
            for(int i=0;i<count;i++)pending[i]=default(PendingEvent);count=0;
            foreach(var s in sessions){s.active=false;s.pending=s.projectilesAlive=0;s.caster=null;s.spell=null;}
            pool.Clear();foreach(var p in projectiles)if(p!=null){p.gameObject.SetActive(false);p.session=null;pool.Enqueue(p);}
        }
    }
    public sealed class SpellProjectile:MonoBehaviour
    {
        private static int nextNetworkVisualId;
        internal int NetworkVisualId { get; private set; }
        internal CastSession session;internal SpellEventContext context;internal int node,generation;
        private SpellExecutor runtime;private Combatant target;private Vector2 direction,origin,basePosition;private float age,fade;private bool ending;
        private SpriteRenderer visual;private static Sprite sprite;private static Material unlitMaterial;
        public void Launch(SpellExecutor owner,CastSession cast,int instruction,SpellEventContext source,Combatant victim,int level,float angle)
        {
            NetworkVisualId=unchecked(++nextNetworkVisualId);
            runtime=owner;session=cast;node=instruction;context=source;target=victim;generation=level;age=fade=0;ending=false;
            var p=session.spell.instructions[node].parameters;
            if(visual==null){visual=gameObject.AddComponent<SpriteRenderer>();if(sprite==null){var tex=new Texture2D(16,16,TextureFormat.RGBA32,false);var pixels=new Color[256];for(int y=0;y<16;y++)for(int x=0;x<16;x++)pixels[y*16+x]=new Vector2(x-7.5f,y-7.5f).sqrMagnitude<52?Color.white:Color.clear;tex.SetPixels(pixels);tex.Apply(false,true);sprite=Sprite.Create(tex,new Rect(0,0,16,16),Vector2.one*.5f,16);}visual.sprite=sprite; if(unlitMaterial==null){var shader=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");if(shader!=null)unlitMaterial=new Material(shader);}if(unlitMaterial!=null)visual.sharedMaterial=unlitMaterial;}
            origin=basePosition=context.position;transform.position=origin;transform.localScale=Vector3.one*Mathf.Clamp(p.radius*2,.1f,1);
            direction=target==null?Vector2.right:((Vector2)target.transform.position-origin).normalized;if(direction.sqrMagnitude<.01f)direction=Vector2.right;
            direction=SpellGeometry.Rotate(direction,angle);
            visual.color=BattleDemo.ElementColor(p.element);
            visual.sortingOrder=260;gameObject.SetActive(true);
        }
        private void Update()
        {
            if(Time.timeScale<=0)return;
            if(session==null){gameObject.SetActive(false);return;}
            if(ending)
            {
                fade+=Time.deltaTime;Color c=visual.color;c.a=Mathf.Max(0,1-fade/.3f);visual.color=c;
                if(fade>=.3f)runtime.Recycle(this);return;
            }
            if(!runtime.Running){End(false);return;}
            if(!session.active || session.caster==null || !session.caster.IsAlive || !session.caster.gameObject.activeInHierarchy || Time.time>session.deadline){End(false);return;}
            var p=session.spell.instructions[node].parameters;
            float remaining=Mathf.Min(p.seconds,session.spell.maxLifetime)-age;
            if(remaining<=0){End(true);return;}
            float delta=Mathf.Min(Time.deltaTime,remaining);age+=delta;
            Vector2 start=transform.position;
            float range=session.spell.range;
            if((start-origin).sqrMagnitude>range*range+.0001f){End(true);return;}
            if(p.trajectory==SpellTrajectory.Homing && target!=null && target.IsAlive)direction=Vector2.MoveTowards(direction,((Vector2)target.transform.position-start).normalized,p.homing*7*delta).normalized;
            basePosition+=direction*p.speed*delta;Vector2 end=basePosition;
            if(p.trajectory==SpellTrajectory.Wave)end+=new Vector2(-direction.y,direction.x)*Mathf.Sin(age*8)*.4f;
            else if(p.trajectory==SpellTrajectory.Spiral)end=origin+SpellGeometry.Rotate(direction,age*180)*p.speed*age;
            else if(p.trajectory==SpellTrajectory.Orbit)end=(Vector2)session.caster.transform.position+SpellGeometry.Rotate(direction,age*150)*Mathf.Clamp(p.speed*.2f,.5f,3);
            bool reachedRange=(end-origin).sqrMagnitude>range*range;
            if(reachedRange)
            {
                Vector2 motion=end-start,offset=start-origin;float a=motion.sqrMagnitude,b=Vector2.Dot(offset,motion);
                float discriminant=Mathf.Max(0,b*b-a*(offset.sqrMagnitude-range*range));
                float clippedFraction=a>.0000001f?Mathf.Clamp01((-b+Mathf.Sqrt(discriminant))/a):0;
                end=start+motion*clippedFraction;
            }
            var list=runtime.Targets(session,SpellExecutor.IsSupportTarget(p.target));Combatant hit=null;float fraction=float.MaxValue;
            for(int i=0;i<list.Count;i++)
            {
                var t=list[i];if(t==null || !t.IsAlive || !t.gameObject.activeInHierarchy || t==context.previous || t==context.previous2 || t==context.previous3)continue;
                // An outgoing support projectile must not collide with its caster at the muzzle.
                if(t==session.caster && t!=target)continue;
                float f;if(SpellGeometry.TrySegmentHit(start,end,t.transform.position,p.radius,out f) && f<fraction){hit=t;fraction=f;}
            }
            if(hit!=null){transform.position=Vector2.Lerp(start,end,fraction);runtime.Impact(this,hit,false);End(false);return;}
            transform.position=end;
            if(age>=Mathf.Min(p.seconds,session.spell.maxLifetime) || reachedRange)End(true);
        }
        private void End(bool expired){if(ending)return;ending=true;if(expired && session.active && session.caster!=null && session.caster.IsAlive)runtime.Impact(this,null,true);}
        private void OnDestroy(){if(runtime!=null)runtime.ProjectileDestroyed(this,session);session=null;}
    }
}
