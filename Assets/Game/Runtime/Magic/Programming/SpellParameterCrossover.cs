using System;
using System.Collections.Generic;
using UnityEngine;

namespace FusionAnyMonster.Spells
{
    /// <summary>Recombines node contents across parental gametes while preserving each tree's wiring.</summary>
    public static class SpellParameterCrossover
    {
        public static SpellProgram Cross(SpellProgram basis, SpellProgram donor, System.Random random,
            SpellSettings settings=null, List<string> history=null, string sourceLabel="")
        {
            if(random==null)throw new ArgumentNullException(nameof(random));
            settings=settings??SpellSettings.Current;
            string error;
            if(!SpellValidator.Validate(basis,out error,settings) || !SpellValidator.Validate(donor,out error,settings))
                throw new InvalidOperationException("Invalid parameter crossover input: "+error);

            var candidate=basis.Clone();float chance=Mathf.Clamp01(settings.nodeParameterCrossoverChance);
            if(chance<=0)return candidate;
            var changes=new List<string>();
            if(random.NextDouble()<chance)
            {
                candidate.basePower=Blend(basis.basePower,donor.basePower,random);
                candidate.baseCooldown=Blend(basis.baseCooldown,donor.baseCooldown,random);
                candidate.range=Blend(basis.range,donor.range,random);
                changes.Add(sourceLabel+"：基礎威力・再使用時間・射程を混合");
            }

            var basisPaths=Paths(basis);var donorPaths=Paths(donor);var used=new HashSet<int>();
            foreach(var entry in basisPaths)
            {
                var node=entry.Value;SpellNode partner;
                // Prefer the same sequence of typed ports; IDs and serialized list order are irrelevant.
                if(!donorPaths.TryGetValue(entry.Key,out partner) || !Compatible(node,partner) || used.Contains(partner.id))
                {
                    var options=donor.nodes.FindAll(n=>Compatible(node,n) && !used.Contains(n.id));
                    partner=options.Count==0?null:options[random.Next(options.Count)];
                }
                if(partner==null)continue;
                used.Add(partner.id);
                if(random.NextDouble()>=chance)continue;
                candidate.Find(node.id).parameters=Mix(node.parameters,partner.parameters,random);
                changes.Add(sourceLabel+"：ノード内交叉 "+node.kind+" "+node.id+" ← "+partner.id);
            }
            // Commit all parameter changes only after validation; neither source tree is ever modified.
            if(!SpellValidator.Validate(candidate,out error,settings))return basis.Clone();
            if(history!=null)history.AddRange(changes);
            return candidate;
        }

        private static bool Compatible(SpellNode a,SpellNode b)
        {return a.kind==b.kind && a.input==b.input;}

        private static Dictionary<string,SpellNode> Paths(SpellProgram program)
        {
            var result=new Dictionary<string,SpellNode>();
            Collect(program,program.Find(program.root),"root",result);
            return result;
        }
        private static void Collect(SpellProgram program,SpellNode node,string path,Dictionary<string,SpellNode> paths)
        {
            paths.Add(path,node);
            // Fixed port order keeps pairing and RNG consumption independent of edge serialization order.
            for(int port=0;port<= (int)SpellPort.False;port++)
            {
                var edge=node.edges.Find(e=>(int)e.port==port);
                if(edge!=null)Collect(program,program.Find(edge.child),path+"/"+port,paths);
            }
        }

        private static float Blend(float a,float b,System.Random random)
        {return Mathf.Lerp(a,b,(float)random.NextDouble());}
        private static T Pick<T>(T a,T b,System.Random random)
        {return random.Next(2)==0?a:b;}
        private static SpellParameters Mix(SpellParameters a,SpellParameters b,System.Random random)
        {
            return new SpellParameters
            {
                count=Mathf.RoundToInt(Blend(a.count,b.count,random)),
                speed=Blend(a.speed,b.speed,random),angle=Blend(a.angle,b.angle,random),
                seconds=Blend(a.seconds,b.seconds,random),radius=Blend(a.radius,b.radius,random),
                homing=Blend(a.homing,b.homing,random),multiplier=Blend(a.multiplier,b.multiplier,random),
                probability=Blend(a.probability,b.probability,random),threshold=Blend(a.threshold,b.threshold,random),
                element=Pick(a.element,b.element,random),status=Pick(a.status,b.status,random),
                trajectory=Pick(a.trajectory,b.trajectory,random),target=Pick(a.target,b.target,random),
                condition=Pick(a.condition,b.condition,random),centerOnCaster=Pick(a.centerOnCaster,b.centerOnCaster,random),
                buff=Pick(a.buff,b.buff,random)
            };
        }
    }
}
