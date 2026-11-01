using System;
using UnityEngine;

namespace FusionAnyMonster.Spells
{
    /// <summary>Authors GP starter programs by species affinity, family, rank and slot.</summary>
    public static class SpeciesDefaultMagicCatalog
    {
        private enum Form { Bolt, Spread, Area, Orbit, Heal, Guard, Drain, Split, Blast, Status }
        private static readonly Form[] Forms={Form.Bolt,Form.Drain,Form.Spread,Form.Status,Form.Status,Form.Status,
            Form.Status,Form.Guard,Form.Area,Form.Area,Form.Bolt,Form.Area,Form.Orbit,Form.Split,Form.Spread,
            Form.Drain,Form.Orbit,Form.Blast,Form.Heal,Form.Heal,Form.Bolt,Form.Heal,Form.Drain,Form.Status,
            Form.Bolt,Form.Status,Form.Status,Form.Blast,Form.Area,Form.Split};
        private static readonly AttributeElement[] Elements={AttributeElement.Water,AttributeElement.Wind,AttributeElement.Wind,
            AttributeElement.Earth,AttributeElement.Water,AttributeElement.Water,AttributeElement.Wind,AttributeElement.Water,
            AttributeElement.Earth,AttributeElement.Earth,AttributeElement.Fire,AttributeElement.Earth,AttributeElement.Earth,
            AttributeElement.Earth,AttributeElement.Wind,AttributeElement.Dark,AttributeElement.Light,AttributeElement.Fire,
            AttributeElement.Light,AttributeElement.Light,AttributeElement.Wind,AttributeElement.Light,AttributeElement.Dark,
            AttributeElement.Dark,AttributeElement.Dark,AttributeElement.Dark,AttributeElement.Dark,AttributeElement.Light,
            AttributeElement.Water,AttributeElement.None};
        private static readonly DebuffType[] Statuses={DebuffType.Slow,DebuffType.Slow,DebuffType.Paralysis,DebuffType.Poison,
            DebuffType.Freeze,DebuffType.Slow,DebuffType.Poison,DebuffType.Slow,DebuffType.Sleep,DebuffType.Poison,
            DebuffType.Silence,DebuffType.Slow,DebuffType.Slow,DebuffType.Freeze,DebuffType.Paralysis,DebuffType.Curse,
            DebuffType.Silence,DebuffType.Burn,DebuffType.Paralysis,DebuffType.Sleep,DebuffType.Freeze,DebuffType.Silence,
            DebuffType.Curse,DebuffType.Poison,DebuffType.Sleep,DebuffType.Curse,DebuffType.Silence,DebuffType.Paralysis,
            DebuffType.Freeze,DebuffType.Curse};

        public static MagicGenome Copy(SpeciesDefinition species,int slot)
        {
            if(species==null)throw new ArgumentNullException(nameof(species));
            if(slot<0 || slot>=SpellSlotStorage.SlotCount)throw new ArgumentOutOfRangeException(nameof(slot));
            var defaults=species.defaultMagicGenomes;
            if(defaults!=null && slot<defaults.Length && defaults[slot]!=null && defaults[slot].HasProgram)return defaults[slot].Clone();
            return Create(species,slot);
        }
        public static MagicGenome Create(SpeciesDefinition species,int slot)
        {
            if(species==null)throw new ArgumentNullException(nameof(species));
            if(slot<0 || slot>=SpellSlotStorage.SlotCount)throw new ArgumentOutOfRangeException(nameof(slot));
            int family=species.family==SpeciesFamily.Truth?(int)SpeciesFamily.Chaos:(int)species.family,rank=(int)species.rank;
            if(family<0 || family>=Forms.Length || rank<0 || rank>6)throw new ArgumentException("Unknown species family or rank");
            bool variant=(species.speciesId??"").EndsWith("_B",StringComparison.Ordinal);
            int index=(family*7+rank)*2+(variant?1:0);
            Form form=slot==0?Forms[family]:slot==1?Form.Status:slot==2?(family%2==0?Form.Heal:Form.Guard):slot==3?Form.Split:Form.Blast;
            if(slot==0 && species.speciesId=="species_F08_R1_A")form=Form.Heal;
            var element=Affinity(species,Elements[family]);
            bool support=form==Form.Heal || form==Form.Guard;
            bool area=form==Form.Area || support;
            var target=support?SpellTarget.LowestHealthAlly:SpellTarget.NearestEnemy;
            var trajectory=form==Form.Orbit?SpellTrajectory.Orbit:family==2 || family==4 || family==26?SpellTrajectory.Wave:
                family==5 || family==20 || family==24?SpellTrajectory.Homing:family==27?SpellTrajectory.Spiral:
                variant?SpellTrajectory.Wave:SpellTrajectory.Straight;
            int count=form==Form.Spread?Mathf.Min(4,2+rank/3):1;
            var genes=SpellProgramBuilder.Projectile(element,6+rank*2.5f+index*.002f+slot,count,trajectory,
                form==Form.Heal?SpellNodeKind.Heal:form==Form.Guard?SpellNodeKind.ApplyBuff:SpellNodeKind.Damage,target,area);
            var p=genes.chromosomeA;p.baseCooldown=5+slot*.4f+(variant?.35f:0);p.range=form==Form.Orbit?2.5f:5.5f+rank*.25f;
            var trigger=p.nodes[0];trigger.parameters.seconds=p.baseCooldown;
            var spawn=p.nodes[1];spawn.parameters.speed=5.5f+rank*.35f;spawn.parameters.homing=.35f+rank*.06f;
            spawn.parameters.seconds=area?1.1f:Mathf.Min(6,p.range/spawn.parameters.speed+.8f);
            if(area)spawn.parameters.radius=support?2f:1.1f+rank*.12f;
            var effect=p.nodes[2];
            if(form==Form.Guard)
            {
                effect.parameters.buff=slot==0?BuffType.DefenseBoost:variant?BuffType.Haste:BuffType.AttackBoost;
                effect.parameters.multiplier=.12f+rank*.015f;effect.parameters.seconds=4+rank*.2f;
                p.target=spawn.parameters.target=SpellTarget.NearestAlly;
            }
            else if(form==Form.Drain)Append(p,effect,SpellNodeKind.LifeSteal,element,.2f);
            else if(form==Form.Split)
            {
                var split=Append(p,effect,SpellNodeKind.SplitProjectile,element,.55f);
                split.parameters.count=2;split.parameters.target=SpellTarget.NextEnemy;split.parameters.seconds=1.4f;
                split.parameters.speed=spawn.parameters.speed;split.parameters.trajectory=SpellTrajectory.Homing;
                var hit=SpellProgramBuilder.Add(p,SpellNodeKind.ApplyStatus,SpellContext.Hit);SetupStatus(hit,element,Statuses[family],rank);
                split.edges.Add(new SpellEdge(SpellPort.OnHit,hit.id));
            }
            else if(form==Form.Blast)
            {
                var blast=Append(p,effect,SpellNodeKind.Explosion,element,.4f);blast.parameters.radius=1+rank*.12f;
                if(rank>=3){var status=Append(p,blast,SpellNodeKind.ApplyStatus,element,.1f);SetupStatus(status,element,Statuses[family],rank);}
            }
            else if(form==Form.Status || form==Form.Area || form==Form.Bolt && family==0)
            {
                var status=Append(p,effect,SpellNodeKind.ApplyStatus,element,.1f);SetupStatus(status,element,Statuses[family],rank);
                if(family==11 || family==12){status.kind=SpellNodeKind.Knockback;status.parameters.multiplier=.5f;}
            }
            genes.chromosomeB=p.Clone();
            return SpellSlotStorage.Prepare(new MagicGenome{programGenes=genes},index*5+slot);
        }
        private static SpellNode Append(SpellProgram p,SpellNode previous,SpellNodeKind kind,AttributeElement element,float multiplier)
        {var n=SpellProgramBuilder.Add(p,kind,SpellContext.Hit);n.parameters.element=element;n.parameters.multiplier=multiplier;previous.edges.Add(new SpellEdge(SpellPort.Next,n.id));return n;}
        private static void SetupStatus(SpellNode n,AttributeElement element,DebuffType status,int rank)
        {n.parameters.element=element;n.parameters.status=status;n.parameters.seconds=1.5f+rank*.2f;n.parameters.probability=.35f+rank*.05f;n.parameters.multiplier=.1f;}
        private static AttributeElement Affinity(SpeciesDefinition species,AttributeElement fallback)
        {
            if(species.passive!=null && species.passive.effects!=null)
                foreach(var effect in species.passive.effects)
                    if(effect!=null && (effect.type==PassiveEffectType.ElementDamage || effect.type==PassiveEffectType.ElementMpReduction || effect.type==PassiveEffectType.ElementResistance))return effect.element;
            return fallback;
        }
    }
}
