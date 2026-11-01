namespace FusionAnyMonster.Spells
{
    public static class SpellExamples
    {
        public static SpellGenotype Create(int example)
        {
            var p=new SpellProgram();p.regulation.splice=false;
            var root=SpellProgramBuilder.Add(p,example==2?SpellNodeKind.EnemyNearTrigger:SpellNodeKind.PeriodicTrigger,SpellContext.Cast);root.parameters.radius=8;
            var shot=SpellProgramBuilder.Add(p,SpellNodeKind.SpawnProjectile,SpellContext.Cast);root.edges.Add(new SpellEdge(SpellPort.Next,shot.id));
            shot.parameters.count=example==0?3:1;shot.parameters.trajectory=SpellTrajectory.Homing;shot.parameters.seconds=4;shot.parameters.angle=example==0?30:0;
            shot.parameters.element=example==0?AttributeElement.Fire:example==1?AttributeElement.Dark:AttributeElement.Light;
            var damage=SpellProgramBuilder.Add(p,SpellNodeKind.Damage,SpellContext.Hit);damage.parameters.element=shot.parameters.element;shot.edges.Add(new SpellEdge(SpellPort.OnHit,damage.id));
            if(example==0){var blast=SpellProgramBuilder.Add(p,SpellNodeKind.Explosion,SpellContext.Hit);blast.parameters.element=AttributeElement.Fire;blast.parameters.radius=2;blast.parameters.multiplier=.5f;damage.edges.Add(new SpellEdge(SpellPort.Next,blast.id));}
            else
            {
                var split=SpellProgramBuilder.Add(p,SpellNodeKind.SplitProjectile,SpellContext.Hit);split.parameters.count=example==1?3:1;split.parameters.target=SpellTarget.NextEnemy;split.parameters.trajectory=SpellTrajectory.Homing;split.parameters.element=shot.parameters.element;split.parameters.seconds=4;
                damage.edges.Add(new SpellEdge(SpellPort.Next,split.id));
                var status=SpellProgramBuilder.Add(p,SpellNodeKind.ApplyStatus,SpellContext.Hit);status.parameters.status=example==1?DebuffType.Poison:DebuffType.Paralysis;status.parameters.seconds=3;status.parameters.multiplier=example==1?.1f:0;status.parameters.element=shot.parameters.element;
                split.edges.Add(new SpellEdge(SpellPort.OnHit,status.id));
            }
            return new SpellGenotype{chromosomeA=p,chromosomeB=p.Clone()};
        }
    }
}
