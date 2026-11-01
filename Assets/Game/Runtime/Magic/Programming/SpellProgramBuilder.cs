using UnityEngine;
namespace FusionAnyMonster.Spells
{
    /// <summary>Constructs typed GP trees directly, without an intermediate skill format.</summary>
    public static class SpellProgramBuilder
    {
        public static SpellNode Add(SpellProgram p,SpellNodeKind kind,SpellContext input)
        {int id=0;foreach(var node in p.nodes)id=Mathf.Max(id,node.id+1);var n=new SpellNode{id=id,kind=kind,input=input};p.nodes.Add(n);return n;}
        public static SpellGenotype Projectile(AttributeElement element=AttributeElement.Fire,float power=20,int count=1,
            SpellTrajectory trajectory=SpellTrajectory.Straight,SpellNodeKind effect=SpellNodeKind.Damage,
            SpellTarget target=SpellTarget.NearestEnemy,bool area=false,bool selfCentered=false)
        {
            var p=new SpellProgram{basePower=power,target=target};p.regulation.splice=false;
            var root=Add(p,SpellNodeKind.PeriodicTrigger,SpellContext.Cast);p.root=root.id;
            var spawn=Add(p,area?SpellNodeKind.SpawnArea:SpellNodeKind.SpawnProjectile,SpellContext.Cast);
            root.edges.Add(new SpellEdge(SpellPort.Next,spawn.id));
            spawn.parameters.element=element;spawn.parameters.target=target;spawn.parameters.count=count;
            spawn.parameters.trajectory=trajectory;spawn.parameters.seconds=4;spawn.parameters.homing=.7f;
            spawn.parameters.angle=count>1?30:0;spawn.parameters.centerOnCaster=selfCentered;
            if(area)spawn.parameters.radius=2.5f;
            var hit=Add(p,effect,SpellContext.Hit);hit.parameters.element=element;
            spawn.edges.Add(new SpellEdge(SpellPort.OnHit,hit.id));
            return new SpellGenotype{chromosomeA=p,chromosomeB=p.Clone()};
        }
        public static SpellGenotype Random(int seed)
        {
            var rng=new System.Random(seed);
            var g=Projectile((AttributeElement)rng.Next(7),10+rng.Next(30),1+rng.Next(4));
            g.chromosomeA.regulation.splice=true;
            foreach(var p in new[]{g.chromosomeA,g.chromosomeB})
            {
                p.regulation.dominance=(float)rng.NextDouble();var spawn=p.nodes[1];
                spawn.parameters.trajectory=(SpellTrajectory)rng.Next(5);spawn.parameters.count=rng.Next(1,5);
                var extra=Add(p,rng.Next(2)==0?SpellNodeKind.Explosion:SpellNodeKind.ApplyStatus,SpellContext.Hit);
                extra.parameters.status=(DebuffType)rng.Next(8);extra.parameters.element=spawn.parameters.element;
                extra.parameters.radius=1.5f;extra.parameters.multiplier=.2f;
                p.nodes[2].edges.Add(new SpellEdge(SpellPort.Next,extra.id));
            }
            return g;
        }
    }
}
