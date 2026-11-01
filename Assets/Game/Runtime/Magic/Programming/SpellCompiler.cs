using System;
using System.Collections.Generic;
using UnityEngine;
namespace FusionAnyMonster.Spells
{
    public sealed class SpellInstruction
    {
        public SpellNodeKind kind;public SpellContext input;public SpellParameters parameters;
        public int[] outputs=new int[7];
    }
    public sealed class CompiledSpell
    {
        public SpellInstruction[] instructions;
        public SpellProgram phenotype;
        public int root,maxProjectiles,maxGeneration,maxEvents;
        public float maxLifetime,basePower,range,mpCost,cooldown,complexity,hpThreshold;
        public SpellTarget target;
        public string name,expressionReport;
    }
    public static class SpellCompiler
    {
        public const float ManaCostMultiplier = 1.5f;
        public const float StandardCooldownMultiplier = .8f;
        public const float HealingCooldownMultiplier = 1.2f;
        public static CompiledSpell Compile(SpellGenotype genes,SpeciesDefinition species=null,SpellSettings settings=null)
        {
            settings=settings??SpellSettings.Current;string report;
            SpellProgram phenotype=SpellGenetics.Express(genes,out report,settings);string error;
            if(!SpellValidator.Validate(phenotype,out error,settings))throw new InvalidOperationException(error);
            var compiled=new CompiledSpell{phenotype=phenotype,instructions=new SpellInstruction[phenotype.nodes.Count],maxProjectiles=Mathf.Clamp(settings.maxProjectiles,1,256),maxGeneration=Mathf.Clamp(settings.maxGeneration,0,8),maxEvents=Mathf.Clamp(settings.maxEvents,1,1024),maxLifetime=Mathf.Clamp(settings.maxLifetime,.1f,20),basePower=phenotype.basePower,range=phenotype.range,target=phenotype.target,expressionReport=report};
            var map=new Dictionary<int,int>();for(int i=0;i<phenotype.nodes.Count;i++)map.Add(phenotype.nodes[i].id,i);
            float estimate=1,complexity=phenotype.nodes.Count*.4f;bool containsHealing=false;
            for(int i=0;i<phenotype.nodes.Count;i++)
            {
                var node=phenotype.nodes[i];var p=JsonUtility.FromJson<SpellParameters>(JsonUtility.ToJson(node.parameters));
                if(node.kind==SpellNodeKind.Heal)containsHealing=true;
                var instruction=new SpellInstruction{kind=node.kind,input=node.input,parameters=p};for(int j=0;j<7;j++)instruction.outputs[j]=-1;
                foreach(var e in node.edges)instruction.outputs[(int)e.port]=map[e.child];compiled.instructions[i]=instruction;
                if(node.kind==SpellNodeKind.SpawnProjectile || node.kind==SpellNodeKind.SplitProjectile){estimate=Mathf.Min(compiled.maxProjectiles,estimate*p.count);complexity+=p.count*.28f+p.homing*.7f+phenotype.basePower*Mathf.Max(0,p.multiplier-1)*.025f;}
                if(node.kind==SpellNodeKind.Explosion || node.kind==SpellNodeKind.SpawnArea)complexity+=p.radius*.6f;
                if(node.kind==SpellNodeKind.ApplyStatus)complexity+=p.probability*p.seconds*.15f;
                if(node.kind==SpellNodeKind.Damage || node.kind==SpellNodeKind.Heal || node.kind==SpellNodeKind.Explosion || node.kind==SpellNodeKind.ApplyStatus)complexity+=phenotype.basePower*p.multiplier*.02f;
            }
            complexity+=Mathf.Min(compiled.maxEvents,estimate*phenotype.nodes.Count)*.045f;
            compiled.root=map[phenotype.root];compiled.hpThreshold=compiled.instructions[compiled.root].parameters.threshold;
            compiled.complexity=complexity;compiled.mpCost=Mathf.Clamp((complexity*.8f+phenotype.basePower*.025f)*2f,1,250)*ManaCostMultiplier;
            var trigger=phenotype.Find(phenotype.root);
            float interval=trigger.kind==SpellNodeKind.PeriodicTrigger?Mathf.Max(phenotype.baseCooldown,trigger.parameters.seconds):phenotype.baseCooldown;
            compiled.cooldown=Mathf.Clamp(interval*(1+complexity*.06f)*(containsHealing?HealingCooldownMultiplier:StandardCooldownMultiplier),.1f,60);
            compiled.name=SpellNaming.Generate(phenotype);
            return compiled;
        }
    }
}
