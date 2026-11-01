using System;
using System.Collections.Generic;
namespace FusionAnyMonster.Spells
{
    public enum SpellViabilityStatus { InvalidStructure, NoUsefulPath, TargetConflict, Unaffordable, Situational, Viable }
    public sealed class SpellSemanticReport
    {
        public SpellViabilityStatus status;
        public int usefulPaths,conflictingPaths;
        public readonly List<string> warnings=new List<string>();
        public bool Acceptable {get{return status==SpellViabilityStatus.Viable || status==SpellViabilityStatus.Situational;}}
    }
    public static class SpellSemanticAnalyzer
    {
        private enum Side { None, Ally, Enemy }
        public static SpellSemanticReport Analyze(SpellProgram p)
        {
            var report=new SpellSemanticReport();string error;
            if(!SpellValidator.Validate(p,out error)){report.status=SpellViabilityStatus.InvalidStructure;report.warnings.Add(error);return report;}
            if(p.Find(p.root).kind==SpellNodeKind.HPThresholdTrigger && p.Find(p.root).parameters.threshold<=0)
            {report.status=SpellViabilityStatus.NoUsefulPath;report.warnings.Add("生存中はHP割合が0以下になりません");return report;}
            bool conditional=p.Find(p.root).kind!=SpellNodeKind.PeriodicTrigger;
            int unconditional=0;
            Walk(p,p.Find(p.root),Side.None,false,false,conditional,ref unconditional,report);
            if(report.usefulPaths==0)report.status=report.conflictingPaths>0?SpellViabilityStatus.TargetConflict:SpellViabilityStatus.NoUsefulPath;
            else report.status=unconditional>0?SpellViabilityStatus.Viable:SpellViabilityStatus.Situational;
            if(report.conflictingPaths>0)report.warnings.Add("対象と一致しない効果経路: "+report.conflictingPaths);
            return report;
        }
        private static bool Support(SpellTarget t){return t==SpellTarget.Self || t==SpellTarget.NearestAlly || t==SpellTarget.LowestHealthAlly;}
        private static void Walk(SpellProgram p,SpellNode n,Side side,bool damage,bool generated,bool conditional,ref int unconditional,SpellSemanticReport r)
        {
            bool generator=n.kind==SpellNodeKind.SpawnProjectile || n.kind==SpellNodeKind.SplitProjectile || n.kind==SpellNodeKind.SpawnArea;
            bool canRun=n.kind!=SpellNodeKind.SplitProjectile || n.parameters.probability>0;
            Side nextSide=generator?(Support(n.parameters.target)?Side.Ally:Side.Enemy):side;
            bool hit=n.input==SpellContext.Hit;
            bool useful=false,conflict=false;
            switch(n.kind)
            {
                case SpellNodeKind.Damage: useful=hit && side==Side.Enemy && n.parameters.multiplier>0;conflict=hit && side==Side.Ally;break;
                case SpellNodeKind.Heal: useful=hit && side==Side.Ally && n.parameters.multiplier>0;conflict=hit && side==Side.Enemy;break;
                case SpellNodeKind.ApplyBuff: useful=hit && side==Side.Ally;conflict=hit && side==Side.Enemy;break;
                case SpellNodeKind.ApplyStatus: case SpellNodeKind.Knockback: case SpellNodeKind.ManaSteal:
                    useful=hit && side==Side.Enemy && n.parameters.probability>0;conflict=hit && side==Side.Ally && n.parameters.probability>0;break;
                case SpellNodeKind.LifeSteal: useful=hit && damage && n.parameters.probability>0 && n.parameters.multiplier>0;break;
                case SpellNodeKind.Explosion: useful=generated && n.parameters.probability>0 && n.parameters.multiplier>0;break;
            }
            bool effectConditional=conditional || (n.kind==SpellNodeKind.Explosion || n.kind==SpellNodeKind.ApplyStatus || n.kind==SpellNodeKind.Knockback || n.kind==SpellNodeKind.ManaSteal || n.kind==SpellNodeKind.LifeSteal) && n.parameters.probability<1;
            if(useful){r.usefulPaths++;if(!effectConditional)unconditional++;}
            if(conflict)r.conflictingPaths++;
            bool dealt=damage || (n.kind==SpellNodeKind.Damage && useful);
            foreach(var edge in n.edges)
            {
                var child=p.Find(edge.child);if(child==null)continue;
                if(generator && edge.port==SpellPort.OnHit && !canRun)continue;
                if(n.kind==SpellNodeKind.RandomBranch && ((edge.port==SpellPort.True && n.parameters.probability<=0) || (edge.port==SpellPort.False && n.parameters.probability>=1)))continue;
                if(edge.port==SpellPort.OnKill && !(n.kind==SpellNodeKind.Damage && useful || n.kind==SpellNodeKind.Explosion && useful || generator && HasEnemyDamage(p,n,nextSide)))continue;
                bool condition=conditional || n.kind==SpellNodeKind.Conditional || n.kind==SpellNodeKind.RandomBranch || edge.port==SpellPort.OnKill || n.kind==SpellNodeKind.SplitProjectile && n.parameters.probability<1 || generator && n.parameters.target==SpellTarget.NextEnemy;
                Side childSide=generator && edge.port==SpellPort.OnHit?nextSide:generator && edge.port==SpellPort.OnExpire?Side.None:side;
                if(edge.port==SpellPort.OnKill)childSide=Side.Enemy;
                Walk(p,child,childSide,generator?false:dealt,generated || generator,condition,ref unconditional,r);
            }
        }
        private static bool HasEnemyDamage(SpellProgram p,SpellNode n,Side side)
        {
            if(n.kind==SpellNodeKind.SplitProjectile && n.parameters.probability<=0)return false;
            if(n.kind==SpellNodeKind.Damage && side==Side.Enemy && n.parameters.multiplier>0 || n.kind==SpellNodeKind.Explosion && n.parameters.multiplier>0 && n.parameters.probability>0)return true;
            bool generator=n.kind==SpellNodeKind.SpawnProjectile || n.kind==SpellNodeKind.SplitProjectile || n.kind==SpellNodeKind.SpawnArea;
            foreach(var e in n.edges){if(e.port==SpellPort.OnKill)continue;if(n.kind==SpellNodeKind.RandomBranch && (e.port==SpellPort.True && n.parameters.probability<=0 || e.port==SpellPort.False && n.parameters.probability>=1))continue;var child=p.Find(e.child);if(child==null)continue;Side childSide=generator && e.port==SpellPort.OnHit?(Support(n.parameters.target)?Side.Ally:Side.Enemy):generator && e.port==SpellPort.OnExpire?Side.None:side;if(HasEnemyDamage(p,child,childSide))return true;}return false;
        }
    }
}
