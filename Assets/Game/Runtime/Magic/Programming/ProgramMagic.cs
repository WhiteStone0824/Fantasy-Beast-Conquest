using UnityEngine;
namespace FusionAnyMonster.Spells
{
    public static class ProgramMagic
    {
        public static BuiltMagic Build(MagicGenome genes,SpeciesDefinition species)
        {
            var compiled=SpellCompiler.Compile(genes.programGenes,species);
            SpellInstruction spawn=null;float duration=1,area=.5f;
            float factor=1+Mathf.Clamp(SpeciesPassiveCalculator.Sum(species==null?null:species.passive,PassiveEffectType.DurationBonus),0,1);
            foreach(var n in compiled.instructions)
            {
                bool generates=n.kind==SpellNodeKind.SpawnProjectile || n.kind==SpellNodeKind.SplitProjectile || n.kind==SpellNodeKind.SpawnArea;
                if(generates || n.kind==SpellNodeKind.ApplyStatus || n.kind==SpellNodeKind.ApplyBuff)
                    n.parameters.seconds=Mathf.Min(n.parameters.seconds*factor,generates?compiled.maxLifetime:20);
                if(spawn==null && (n.kind==SpellNodeKind.SpawnProjectile || n.kind==SpellNodeKind.SpawnArea))spawn=n;
                duration=Mathf.Max(duration,n.parameters.seconds);area=Mathf.Max(area,n.parameters.radius);
            }
            var p=spawn==null?new SpellParameters():spawn.parameters;
            var built=new BuiltMagic{program=compiled,displayName=string.IsNullOrWhiteSpace(genes.programGenes.customName)?compiled.name:genes.programGenes.customName,
                element=p.element,trajectory=p.trajectory,power=compiled.basePower,projectileSpeed=p.speed,range=compiled.range,
                cooldown=compiled.cooldown,mpCost=compiled.mpCost,complexity=compiled.complexity,projectileCount=p.count,area=area,
                duration=Mathf.Min(duration,compiled.maxLifetime),targetsAllies=SpellExecutor.IsSupportTarget(compiled.target),
                isArea=spawn!=null && spawn.kind==SpellNodeKind.SpawnArea,centerOnCaster=p.centerOnCaster};
            if(!string.IsNullOrWhiteSpace(genes.customName))built.displayName=genes.customName;
            SpeciesPassiveCalculator.ApplyMagic(species==null?null:species.passive,built);
            compiled.mpCost=built.mpCost;compiled.cooldown=built.cooldown;return built;
        }
    }
}
