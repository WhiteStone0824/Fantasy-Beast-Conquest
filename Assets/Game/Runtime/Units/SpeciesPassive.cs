using System;
using System.Text;
using UnityEngine;

namespace FusionAnyMonster
{
    public enum PassiveEffectType { ElementResistance, ElementDamage, ElementMpReduction, StatBonus, DebuffResistance, HealingDone, HealingReceived, HpRegeneration, LifeSteal, LowHpDamage, LowHpProtection, CooldownReduction, DurationBonus, OnHitDebuff, AllStatsBonus, OnHitInstantDeath, ReviveAlliesOnDeath }
    [Serializable] public sealed class PassiveEffect
    {
        public PassiveEffectType type;
        public AttributeElement element;
        public DebuffType debuff;
        public int statIndex;
        public float amount;
    }
    [Serializable] public sealed class SpeciesPassive
    {
        public string id,displayName;
        public PassiveEffect[] effects=new PassiveEffect[0];
        public string Description
        {
            get
            {
                var text=new StringBuilder();
                if(effects!=null)for(int i=0;i<effects.Length;i++){if(effects[i]==null)continue;if(text.Length>0)text.Append('\n');text.Append("・").Append(SpeciesPassiveCalculator.Describe(effects[i]));}
                return text.ToString();
            }
        }
    }
    public static class SpeciesPassiveCalculator
    {
        public static float Sum(SpeciesPassive passive,PassiveEffectType type,int selector=-1)
        {
            float result=0;if(passive==null || passive.effects==null)return 0;
            foreach(PassiveEffect effect in passive.effects)
            {
                if(effect==null || effect.type!=type || float.IsNaN(effect.amount) || float.IsInfinity(effect.amount))continue;
                int key=type==PassiveEffectType.StatBonus?effect.statIndex:type==PassiveEffectType.DebuffResistance?(int)effect.debuff:(int)effect.element;
                if(selector<0 || key==selector)result+=Mathf.Max(0,effect.amount);
            }
            return result;
        }
        public static float ElementReduction(SpeciesDefinition species,AttributeElement element)
        { return species==null?0:Mathf.Clamp01(species.GetResistance(element).damageReduction+Sum(species.passive,PassiveEffectType.ElementResistance,(int)element)); }
        public static float DebuffReduction(SpeciesDefinition species,DebuffType type)
        { return species==null?0:Mathf.Clamp01(species.GetResistance(type).applicationResistance+Sum(species.passive,PassiveEffectType.DebuffResistance,(int)type)); }
        public static void ApplyStats(SpeciesPassive passive,UnitStats stats)
        { if(stats!=null)for(int i=0;i<10;i++)stats.Set(i,stats.Get(i)*(1+Mathf.Clamp(Sum(passive,PassiveEffectType.StatBonus,i)+Sum(passive,PassiveEffectType.AllStatsBonus),0,1))); }
        public static float DamageMultiplier(SpeciesPassive passive,AttributeElement element,float healthRatio)
        { return 1+Sum(passive,PassiveEffectType.ElementDamage,(int)element)+(healthRatio<=.35f?Sum(passive,PassiveEffectType.LowHpDamage):0); }
        public static void ApplyMagic(SpeciesPassive passive,BuiltMagic magic)
        {
            if(magic==null)return;
            magic.mpCost*=1-Mathf.Clamp(Sum(passive,PassiveEffectType.ElementMpReduction,(int)magic.element),0,.8f);
            magic.cooldown=Mathf.Clamp(magic.cooldown*(1-Mathf.Clamp(Sum(passive,PassiveEffectType.CooldownReduction),0,.5f)),.1f,60);
        }
        private static readonly string[] Elements={"火","水","風","土","光","闇","無"};
        private static readonly string[] Debuffs={"毒","火傷","凍結","麻痺","睡眠","鈍足","沈黙","呪い"};
        private static readonly string[] Stats={"最大HP","最大MP","物理攻撃力","魔法攻撃力","物理防御力","魔法防御力","移動速度","攻撃速度","索敵範囲","MP回復速度"};
        public static string Describe(PassiveEffect effect)
        {
            string percent=Math.Ceiling(Math.Round((double)effect.amount*100,4)).ToString("0")+"%";
            string element=Elements[Mathf.Clamp((int)effect.element,0,6)],debuff=Debuffs[Mathf.Clamp((int)effect.debuff,0,7)];
            switch(effect.type)
            {
                case PassiveEffectType.ElementResistance:return element+"属性耐性 +"+percent;
                case PassiveEffectType.ElementDamage:return element+"属性の与ダメージ +"+percent;
                case PassiveEffectType.ElementMpReduction:return element+"属性魔法の消費MP -"+percent;
                case PassiveEffectType.StatBonus:return Stats[Mathf.Clamp(effect.statIndex,0,9)]+" +"+percent;
                case PassiveEffectType.DebuffResistance:return debuff+"への抵抗 +"+percent;
                case PassiveEffectType.HealingDone:return "回復魔法の回復量 +"+percent;
                case PassiveEffectType.HealingReceived:return "受けるHP回復量 +"+percent;
                case PassiveEffectType.HpRegeneration:return "毎秒、最大HPの"+percent+"を回復";
                case PassiveEffectType.LifeSteal:return "直撃で与えたダメージの"+percent+"をHP吸収";
                case PassiveEffectType.LowHpDamage:return "HP35%以下で与ダメージ +"+percent;
                case PassiveEffectType.LowHpProtection:return "HP35%以下で被ダメージ -"+percent;
                case PassiveEffectType.CooldownReduction:return "魔法のクールダウン -"+percent;
                case PassiveEffectType.DurationBonus:return "魔法の持続時間 +"+percent;
                case PassiveEffectType.AllStatsBonus:return "すべての能力値 +"+percent;
                case PassiveEffectType.OnHitInstantDeath:return "攻撃命中時、"+percent+"で即死";
                case PassiveEffectType.ReviveAlliesOnDeath:return "戦闘不能になった時、1度だけ自身と倒れた味方全員をHP・MP全回復で復活";
                case PassiveEffectType.OnHitDebuff:return "攻撃命中時、"+percent+"で"+debuff+"を"+(effect.debuff==DebuffType.Poison?"10":effect.debuff==DebuffType.Burn?"4":"2")+"秒付与（判定は毎秒1回）";
                default:return "不明な効果";
            }
        }
    }
}
