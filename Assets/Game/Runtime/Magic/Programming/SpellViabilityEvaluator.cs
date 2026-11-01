using System;
using UnityEngine;
namespace FusionAnyMonster.Spells
{
    public sealed class SpellViabilityResult
    {
        public SpellViabilityStatus status;public SpellSemanticReport semantics;public float mpCost,maxMp;public string warning;
        public bool Acceptable {get{return status==SpellViabilityStatus.Viable || status==SpellViabilityStatus.Situational;}}
    }
    public static class SpellViabilityEvaluator
    {
        public static SpellViabilityResult Evaluate(MagicGenome genome,SpeciesDefinition species,UnitInstance unit,int slot,PersonalityDatabase personalities=null)
        {
            var result=new SpellViabilityResult();
            try
            {
                var magic=MagicFactory.Build(genome,species);result.semantics=SpellSemanticAnalyzer.Analyze(magic.program.phenotype);
                result.status=result.semantics.status;result.mpCost=magic.mpCost;
                if(!result.semantics.Acceptable)return result;
                if(unit==null || species==null){result.warning="対象個体の最大MPを評価できません";return result;}
                result.maxMp=UnitStatsCalculator.Calculate(species,unit,null,personalities).mp;
                if(slot<RankRules.MagicSlots(species.rank) && result.mpCost>result.maxMp){result.status=SpellViabilityStatus.Unaffordable;result.warning="最大MPを超えています";}
                else if(slot>=RankRules.MagicSlots(species.rank) && result.mpCost>result.maxMp)result.warning="未解放枠: 現在の最大MPを超えています";
                if(result.status!=SpellViabilityStatus.Unaffordable && magic.program.range<=0 && magic.program.target!=SpellTarget.Self){result.status=SpellViabilityStatus.Situational;result.warning="射程ゼロは接触位置でのみ対象を選択します";}
            }
            catch(Exception e){result.status=SpellViabilityStatus.InvalidStructure;result.warning=e.Message;}
            return result;
        }
    }
}
