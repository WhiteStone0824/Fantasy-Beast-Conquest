using System;
using System.Collections.Generic;
using UnityEngine;
namespace FusionAnyMonster.Spells
{
    public static class SpellGenetics
    {
        public static SpellProgram Express(SpellGenotype genes,out string report,SpellSettings settings=null)
        {
            settings=settings??SpellSettings.Current;
            if(genes==null || genes.version!=1 || genes.algorithmVersion!=1)throw new InvalidOperationException("Unsupported expression algorithm");
            int expressionNodes=genes.expressionMaxNodes==0?24:genes.expressionMaxNodes,expressionDepth=genes.expressionMaxDepth==0?6:genes.expressionMaxDepth;
            if(expressionNodes<1 || expressionNodes>128 || expressionDepth<1 || expressionDepth>12)throw new InvalidOperationException("Invalid expression limits");
            string error;
            if(!SpellValidator.Validate(genes.chromosomeA,out error,settings,expressionNodes,expressionDepth) || !SpellValidator.Validate(genes.chromosomeB,out error,settings,expressionNodes,expressionDepth))throw new InvalidOperationException("Invalid chromosomes: "+error);
            bool useA=genes.chromosomeA.regulation.dominance>=genes.chromosomeB.regulation.dominance;
            var basis=useA?genes.chromosomeA:genes.chromosomeB;var donor=useA?genes.chromosomeB:genes.chromosomeA;
            report=useA?"基礎：染色体A":"基礎：染色体B";
            if(!basis.regulation.splice)return basis.Clone();
            var recipients=basis.nodes.FindAll(n=>!SpellValidator.IsTrigger(n.kind));
            if(recipients.Count==0)return basis.Clone();
            var recipient=recipients[(int)((uint)basis.regulation.recipientIndex%(uint)recipients.Count)];
            var donors=donor.nodes.FindAll(n=>n.input==recipient.input && !SpellValidator.IsTrigger(n.kind));
            if(donors.Count==0)return basis.Clone();
            var selected=donors[(int)((uint)basis.regulation.donorIndex%(uint)donors.Count)];
            SpellProgram result=TryReplace(basis,recipient.id,donor,selected.id);
            if(result==null){report+="／組み込み取消：部分木の生成上限";return basis.Clone();}
            if(SpellValidator.Validate(result,out error,settings,expressionNodes,expressionDepth)){report+="／部分木 "+recipient.id+" ← "+selected.id;return result;}
            report+="／組み込み取消："+error;return basis.Clone();
        }
        public static SpellProgram Replace(SpellProgram basis,int recipient,SpellProgram donor,int donorRoot)
        {
            SpellProgram result=basis.Clone();result.nodes.Clear();
            result.root=CopyTree(basis,basis.root,result,recipient,donor,donorRoot);return result;
        }
        private static SpellProgram TryReplace(SpellProgram basis,int recipient,SpellProgram donor,int donorRoot)
        {try{return Replace(basis,recipient,donor,donorRoot);}catch(InvalidOperationException){return null;}}
        private static int CopyTree(SpellProgram source,int id,SpellProgram result,int replace,SpellProgram donor,int donorRoot)
        {
            if(result.nodes.Count>=128)throw new InvalidOperationException("Subtree exceeds the hard safety limit");
            if(id==replace && donor!=null)return CopyTree(donor,donorRoot,result,int.MinValue,null,0);
            SpellNode old=source.Find(id);var node=JsonUtility.FromJson<SpellNode>(JsonUtility.ToJson(old));node.id=result.nodes.Count;node.edges=new List<SpellEdge>();result.nodes.Add(node);
            foreach(var e in old.edges)node.edges.Add(new SpellEdge(e.port,CopyTree(source,e.child,result,replace,donor,donorRoot)));
            return node.id;
        }
        public static SpellProgram Gamete(SpellGenotype parent,System.Random rng,SpellSettings settings,List<string> history)
        {
            bool first=rng.Next(2)==0;var basis=first?parent.chromosomeA:parent.chromosomeB;var donor=first?parent.chromosomeB:parent.chromosomeA;
            var result=basis.Clone();
            if(rng.NextDouble()<settings.crossoverChance)
            {
                var options=basis.nodes.FindAll(n=>!SpellValidator.IsTrigger(n.kind));
                if(options.Count>0)
                {
                    var recipient=options[rng.Next(options.Count)];var choices=donor.nodes.FindAll(n=>!SpellValidator.IsTrigger(n.kind) && n.input==recipient.input);
                    if(choices.Count>0){var selected=choices[rng.Next(choices.Count)];var crossed=TryReplace(basis,recipient.id,donor,selected.id);string error;if(SpellValidator.Validate(crossed,out error,settings)){result=crossed;history.Add("部分木交叉："+recipient.id+" ← "+selected.id);}}
                }
            }
            return Mutate(result,rng,settings,history);
        }
        public static SpellGenotype Cross(SpellGenotype a,SpellGenotype b,int seed,SpellSettings settings=null)
        {
            settings=settings??SpellSettings.Current;var rng=new System.Random(seed);var child=new SpellGenotype();
            child.expressionMaxNodes=settings.maxNodes;child.expressionMaxDepth=settings.maxDepth;
            string validation;
            if(a==null || b==null || !SpellValidator.Validate(a.chromosomeA,out validation,settings) || !SpellValidator.Validate(a.chromosomeB,out validation,settings) || !SpellValidator.Validate(b.chromosomeA,out validation,settings) || !SpellValidator.Validate(b.chromosomeB,out validation,settings))throw new InvalidOperationException("Invalid parental spell program");
            child.chromosomeA=Gamete(a,rng,settings,child.history);child.chromosomeB=Gamete(b,rng,settings,child.history);
            // Both hybrids read the original gametes, so one child's changes cannot feed the other.
            var gameteA=child.chromosomeA;var gameteB=child.chromosomeB;
            child.chromosomeA=SpellParameterCrossover.Cross(gameteA,gameteB,rng,settings,child.history,"親A ← 親B");
            child.chromosomeB=SpellParameterCrossover.Cross(gameteB,gameteA,rng,settings,child.history,"親B ← 親A");
            child.history.Insert(0,"親Aの配偶子／親Bの配偶子・seed="+seed);return child;
        }
        public static SpellProgram Mutate(SpellProgram original,System.Random rng,SpellSettings settings,List<string> history)
        {
            var result=original.Clone();
            if(rng.NextDouble()<settings.parameterMutationChance)
            {
                bool accepted=false;
                for(int attempt=0;attempt<3 && !accepted;attempt++)
                {
                var candidate=result.Clone();var n=candidate.nodes[rng.Next(candidate.nodes.Count)];
                var choices=ApplicableParameters(n);choices.Add(12);choices.Add(13);
                string before=JsonUtility.ToJson(candidate);int choice=choices[rng.Next(choices.Count)];
                switch(choice)
                {
                    case 0:n.parameters.count=Mathf.Clamp(n.parameters.count+rng.Next(-2,4),1,48);break;
                    case 1:n.parameters.speed=Mathf.Clamp(n.parameters.speed*(.7f+(float)rng.NextDouble()),.1f,20);break;
                    case 2:n.parameters.radius=Mathf.Clamp(n.parameters.radius*(.7f+(float)rng.NextDouble()),.02f,20);break;
                    case 3:n.parameters.multiplier=Mathf.Clamp(n.parameters.multiplier*(.7f+(float)rng.NextDouble()),.05f,5);break;
                    case 4:n.parameters.element=(AttributeElement)rng.Next(7);break;
                    case 5:n.parameters.status=(DebuffType)rng.Next(8);break;
                    case 6:n.parameters.trajectory=(SpellTrajectory)rng.Next(5);break;
                    case 7:n.parameters.target=(SpellTarget)rng.Next(6);break;
                    case 8:n.parameters.seconds=Mathf.Clamp(n.parameters.seconds*(.5f+(float)rng.NextDouble()),.05f,20);break;
                    case 9:n.parameters.probability=(float)rng.NextDouble();break;
                    case 10:n.parameters.homing=(float)rng.NextDouble();break;
                    case 11:n.parameters.angle=-180+(float)rng.NextDouble()*360;break;
                    case 12:candidate.basePower=Mathf.Clamp(candidate.basePower*(.75f+(float)rng.NextDouble()*.5f),1,500);break;
                    default:candidate.baseCooldown=Mathf.Clamp(candidate.baseCooldown*(.75f+(float)rng.NextDouble()*.5f),.1f,60);break;
                }
                if(before==JsonUtility.ToJson(candidate))continue;
                candidate.regulation.dominance=(float)rng.NextDouble();candidate.regulation.recipientIndex=rng.Next();candidate.regulation.donorIndex=rng.Next();candidate.regulation.splice=rng.Next(2)==0;
                string error;if(SpellValidator.Validate(candidate,out error,settings)){result=candidate;history.Add("パラメータ変異成功：ノード"+n.id+" 項目"+choice);accepted=true;}
                }
                if(!accepted && history!=null)history.Add("パラメータ変異試行：変更なし");
            }
            if(rng.NextDouble()<settings.nodeMutationChance)
            {
                var candidate=result.Clone();var n=candidate.nodes[rng.Next(candidate.nodes.Count)];
                var kinds=(SpellNodeKind[])Enum.GetValues(typeof(SpellNodeKind));n.kind=kinds[rng.Next(kinds.Length)];
                Accept(ref result,candidate,settings,history,"ノード置換："+n.id+" → "+n.kind);
            }
            if(rng.NextDouble()<settings.structureMutationChance && result.nodes.Count>1)
            {
                var candidate=result.Clone();var parent=candidate.nodes[rng.Next(candidate.nodes.Count)];
                int operation=rng.Next(4);
                if(operation==3 && !SpellValidator.IsTrigger(parent.kind))
                {
                    var donor=SpellProgramBuilder.Random(rng.Next()).chromosomeA;
                    var compatible=donor.nodes.FindAll(n=>n.input==parent.input && !SpellValidator.IsTrigger(n.kind));
                    if(compatible.Count>0)candidate=TryReplace(candidate,parent.id,donor,compatible[rng.Next(compatible.Count)].id);
                }
                else if(operation==0 && parent.edges.Count>0)
                {
                    var edge=parent.edges[rng.Next(parent.edges.Count)];var child=candidate.Find(edge.child);
                    if(child.input==parent.input && !SpellValidator.IsTrigger(parent.kind))candidate=Replace(candidate,parent.id,candidate,child.id);
                    else{parent.edges.Remove(edge);candidate=Replace(candidate,int.MinValue,null,0);}
                }
                else
                {
                    SpellPort port=parent.kind==SpellNodeKind.SpawnProjectile || parent.kind==SpellNodeKind.SplitProjectile || parent.kind==SpellNodeKind.SpawnArea?SpellPort.OnHit:SpellPort.Next;
                    SpellContext context;
                    if(SpellValidator.Output(parent,port,out context))
                    {
                        var edge=parent.edges.Find(e=>e.port==port);
                        var node=new SpellNode{id=candidate.nodes.Count,input=context,kind=context==SpellContext.Hit?(rng.Next(2)==0?SpellNodeKind.ApplyStatus:SpellNodeKind.SplitProjectile):SpellNodeKind.Delay};
                        node.parameters.status=(DebuffType)rng.Next(8);node.parameters.element=(AttributeElement)rng.Next(7);node.parameters.count=rng.Next(1,4);node.parameters.trajectory=(SpellTrajectory)rng.Next(5);
                        if(edge!=null){node.edges.Add(new SpellEdge(SpellPort.Next,edge.child));edge.child=node.id;}else parent.edges.Add(new SpellEdge(port,node.id));
                        candidate.nodes.Add(node);
                        if(operation==2 && node.kind==SpellNodeKind.SplitProjectile)
                        {
                            var effect=new SpellNode{id=candidate.nodes.Count,input=SpellContext.Hit,kind=SpellNodeKind.ApplyStatus};effect.parameters.status=(DebuffType)rng.Next(8);
                            candidate.nodes.Add(effect);node.edges.Add(new SpellEdge(SpellPort.OnHit,effect.id));
                        }
                    }
                }
                Accept(ref result,candidate,settings,history,"構造変異：追加／削除／部分木置換");
            }
            return result;
        }
        private static void Accept(ref SpellProgram result,SpellProgram candidate,SpellSettings settings,List<string> history,string message)
        {string error;if(SpellValidator.Validate(candidate,out error,settings)){result=candidate;history.Add(message);}}
        private static List<int> ApplicableParameters(SpellNode node)
        {
            var c=new List<int>();var k=node.kind;
            bool projectile=k==SpellNodeKind.SpawnProjectile || k==SpellNodeKind.SplitProjectile;
            bool spawn=projectile || k==SpellNodeKind.SpawnArea;
            if(projectile){c.Add(0);c.Add(1);c.Add(6);c.Add(11);if(node.parameters.trajectory==SpellTrajectory.Homing)c.Add(10);}
            if(spawn || k==SpellNodeKind.Explosion || k==SpellNodeKind.EnemyNearTrigger || k==SpellNodeKind.Conditional && node.parameters.condition==SpellCondition.EnemyNearby)c.Add(2);
            if(spawn || k==SpellNodeKind.Damage || k==SpellNodeKind.Heal || k==SpellNodeKind.Explosion || k==SpellNodeKind.Knockback || k==SpellNodeKind.LifeSteal || k==SpellNodeKind.ManaSteal || k==SpellNodeKind.ApplyBuff)c.Add(3);
            if(spawn || k==SpellNodeKind.Damage || k==SpellNodeKind.Explosion || k==SpellNodeKind.ApplyStatus)c.Add(4);
            if(k==SpellNodeKind.ApplyStatus)c.Add(5);
            if(spawn)c.Add(7);
            if(spawn || k==SpellNodeKind.Delay || k==SpellNodeKind.ApplyStatus || k==SpellNodeKind.ApplyBuff || k==SpellNodeKind.PeriodicTrigger)c.Add(8);
            if(k==SpellNodeKind.SplitProjectile || k==SpellNodeKind.Explosion || k==SpellNodeKind.ApplyStatus || k==SpellNodeKind.Knockback || k==SpellNodeKind.LifeSteal || k==SpellNodeKind.ManaSteal || k==SpellNodeKind.RandomBranch)c.Add(9);
            return c;
        }
    }
}
