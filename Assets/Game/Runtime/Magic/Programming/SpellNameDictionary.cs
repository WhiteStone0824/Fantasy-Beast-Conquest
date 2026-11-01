using System;
using System.Collections.Generic;
using UnityEngine;
namespace FusionAnyMonster.Spells
{
    [Serializable]public sealed class SpellNameRule {public AttributeElement element;public SpellNodeKind first,second;public string name;}
    [Serializable]public sealed class SpellNameFeature {public SpellNodeKind node;public string word;public int importance;}
    [CreateAssetMenu(menuName="Fusion Any Monster/Spell Names")]
    public sealed class SpellNameDictionary:ScriptableObject
    {
        public int version=1;
        public string[] elements={"火炎","水流","風刃","岩石","天光","冥闇","魔力"};
        public string projectileTemplate="{0}の{1}弾",areaTemplate="{0}の{1}陣";
        public string[] trajectoryWords={"","追尾","螺旋","波動","周回"};
        public int trajectoryImportance=20;
        public SpellNameFeature[] featureRules={new SpellNameFeature{node=SpellNodeKind.Heal,word="癒し",importance=100},new SpellNameFeature{node=SpellNodeKind.SplitProjectile,word="分裂",importance=90},new SpellNameFeature{node=SpellNodeKind.Explosion,word="爆裂",importance=80},new SpellNameFeature{node=SpellNodeKind.LifeSteal,word="吸命",importance=75},new SpellNameFeature{node=SpellNodeKind.ApplyStatus,word="呪縛",importance=60},new SpellNameFeature{node=SpellNodeKind.ApplyBuff,word="祝福",importance=70}};
        public SpellNameRule[] specialRules={new SpellNameRule{element=AttributeElement.Light,first=SpellNodeKind.SpawnProjectile,second=SpellNodeKind.SplitProjectile,name="天光群星"},new SpellNameRule{element=AttributeElement.Dark,first=SpellNodeKind.LifeSteal,second=SpellNodeKind.SplitProjectile,name="冥府の鎖"}};
    }
    public static class SpellNaming
    {
        public static string Generate(SpellProgram p,SpellNameDictionary dictionary=null)
        {
            if(dictionary==null)dictionary=Resources.Load<SpellNameDictionary>("SpellNameDictionary");
            AttributeElement element=AttributeElement.None;SpellParameters projectile=null;bool area=false,explode=false,split=false,heal=false,steal=false,status=false;
            foreach(var n in p.nodes)
            {
                if(projectile==null && (n.kind==SpellNodeKind.SpawnProjectile || n.kind==SpellNodeKind.SpawnArea)){projectile=n.parameters;element=n.parameters.element;area=n.kind==SpellNodeKind.SpawnArea;}
                explode|=n.kind==SpellNodeKind.Explosion;split|=n.kind==SpellNodeKind.SplitProjectile;heal|=n.kind==SpellNodeKind.Heal;steal|=n.kind==SpellNodeKind.LifeSteal;status|=n.kind==SpellNodeKind.ApplyStatus;
            }
            if(dictionary!=null && dictionary.specialRules!=null)foreach(var rule in dictionary.specialRules)
                if(rule.element==element && p.nodes.Exists(n=>n.kind==rule.first) && p.nodes.Exists(n=>n.kind==rule.second))return rule.name;
            if(element==AttributeElement.Dark && steal && split)return "冥府の鎖";
            if(element==AttributeElement.Light && split && projectile!=null && projectile.trajectory==SpellTrajectory.Homing)return "天光群星";
            string[] defaults={"火炎","水流","風刃","岩石","天光","冥闇","魔力"};
            string prefix=dictionary!=null && dictionary.elements!=null && dictionary.elements.Length==7?dictionary.elements[(int)element]:defaults[(int)element];
            string feature=heal?"癒し":split?"分裂":explode?"爆裂":steal?"吸命":status?"呪縛":projectile!=null && projectile.trajectory==SpellTrajectory.Homing?"追尾":projectile!=null && projectile.trajectory==SpellTrajectory.Spiral?"螺旋":projectile!=null && projectile.trajectory==SpellTrajectory.Wave?"波動":projectile!=null && projectile.trajectory==SpellTrajectory.Orbit?"周回":"";
            if(dictionary!=null)
            {
                var features=new Dictionary<string,int>(StringComparer.Ordinal);
                if(dictionary.featureRules!=null)foreach(var rule in dictionary.featureRules)if(!string.IsNullOrEmpty(rule.word) && p.nodes.Exists(n=>n.kind==rule.node))features[rule.word]=rule.importance;
                if(projectile!=null && dictionary.trajectoryWords!=null && dictionary.trajectoryWords.Length==5)
                {string word=dictionary.trajectoryWords[(int)projectile.trajectory];if(!string.IsNullOrEmpty(word))features[word]=dictionary.trajectoryImportance;}
                var ranked=new List<KeyValuePair<string,int>>(features);ranked.Sort((a,b)=>{int weight=b.Value.CompareTo(a.Value);return weight==0?string.CompareOrdinal(a.Key,b.Key):weight;});
                feature="";for(int i=0;i<ranked.Count && i<2;i++)feature+=ranked[i].Key;
            }
            string template=dictionary==null?(area?"{0}の{1}陣":"{0}の{1}弾"):area?dictionary.areaTemplate:dictionary.projectileTemplate;
            return string.Format(template,prefix,feature).Replace("の弾","弾");
        }
    }
}
