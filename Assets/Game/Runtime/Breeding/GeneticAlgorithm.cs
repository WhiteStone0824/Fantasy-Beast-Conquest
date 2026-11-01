using System;
namespace FusionAnyMonster
{
    public static class GeneticAlgorithm
    {
        public static MagicGenome MutateToSpeciesDefault(MagicGenome inherited,SpeciesDefinition species,int slot,Random rng,float chance)
        {
            if(inherited==null || species==null || rng==null || chance<=0 || rng.NextDouble()>=Math.Min(1f,chance))return inherited;
            var result=Spells.SpeciesDefaultMagicCatalog.Copy(species,slot);
            if(result.programGenes.history==null)result.programGenes.history=new System.Collections.Generic.List<string>();
            if(inherited.programGenes!=null && inherited.programGenes.history!=null)result.programGenes.history.AddRange(inherited.programGenes.history);
            result.programGenes.history.Add("突然変異：子の種族の標準魔法（スロット"+(slot+1)+"）");
            return result;
        }
        // Correct incompatible recipient choices in newly bred genes; existing owned genomes are untouched.
        public static MagicGenome ImproveOffspring(MagicGenome genome,SpeciesDefinition species,int slot)
        {
            if(genome==null || !genome.HasProgram)return genome;
            var copy=genome.Clone();bool changed=false;
            changed|=RepairTargets(copy.programGenes.chromosomeA);
            changed|=RepairTargets(copy.programGenes.chromosomeB);
            if(copy.programGenes.history==null)copy.programGenes.history=new System.Collections.Generic.List<string>();
            if(changed)copy.programGenes.history.Add("対象の突然変異を効果に合わせて調整");
            string report;
            var phenotype=Spells.SpellGenetics.Express(copy.programGenes,out report);
            var semantic=Spells.SpellSemanticAnalyzer.Analyze(phenotype);
            if(!semantic.Acceptable)
            {
                var fallback=Spells.SpeciesDefaultMagicCatalog.Copy(species,slot);
                if(fallback.programGenes.history==null)fallback.programGenes.history=new System.Collections.Generic.List<string>();
                fallback.programGenes.history.AddRange(copy.programGenes.history);
                fallback.programGenes.history.Add("有効な効果がないため種族の標準魔法に変化");
                return fallback;
            }
            return Spells.SpellSlotStorage.Prepare(copy,0);
        }
        private static bool RepairTargets(Spells.SpellProgram program)
        {
            bool changed=false,rootSet=false;
            foreach(var node in program.nodes)
            {
                if(node.kind!=Spells.SpellNodeKind.SpawnProjectile && node.kind!=Spells.SpellNodeKind.SplitProjectile && node.kind!=Spells.SpellNodeKind.SpawnArea)continue;
                int support=0,hostile=0;CountEffects(program,node,ref support,ref hostile);
                // A mixed chain cannot hit allies and enemies through one impact; preserve its
                // existing target rather than pretending the majority makes every effect valid.
                if(support==0 && hostile==0 || support>0 && hostile>0)continue;
                bool wantSupport=support>0;
                bool targetSupport=IsSupport(node.parameters.target);
                if(wantSupport!=targetSupport){node.parameters.target=wantSupport?Spells.SpellTarget.LowestHealthAlly:Spells.SpellTarget.NearestEnemy;changed=true;}
                if(!rootSet && node.input==Spells.SpellContext.Cast)
                {
                    if(IsSupport(program.target)!=wantSupport){program.target=wantSupport?Spells.SpellTarget.LowestHealthAlly:Spells.SpellTarget.NearestEnemy;changed=true;}
                    rootSet=true;
                }
            }
            return changed;
        }
        private static bool IsSupport(Spells.SpellTarget target)
        {return target==Spells.SpellTarget.Self || target==Spells.SpellTarget.NearestAlly || target==Spells.SpellTarget.LowestHealthAlly;}
        private static void CountEffects(Spells.SpellProgram program,Spells.SpellNode node,ref int support,ref int hostile)
        {
            foreach(var edge in node.edges)
            {
                var child=program.Find(edge.child);if(child==null)continue;
                if(child.kind==Spells.SpellNodeKind.SpawnProjectile || child.kind==Spells.SpellNodeKind.SplitProjectile || child.kind==Spells.SpellNodeKind.SpawnArea)continue;
                if(child.kind==Spells.SpellNodeKind.Heal){if(child.parameters.multiplier>0)support++;}
                else if(child.kind==Spells.SpellNodeKind.ApplyBuff)support++;
                else if(child.kind==Spells.SpellNodeKind.Damage){if(child.parameters.multiplier>0)hostile++;}
                else if(child.kind==Spells.SpellNodeKind.Explosion || child.kind==Spells.SpellNodeKind.ApplyStatus || child.kind==Spells.SpellNodeKind.Knockback || child.kind==Spells.SpellNodeKind.ManaSteal){if(child.parameters.probability>0)hostile++;}
                CountEffects(program,child,ref support,ref hostile);
            }
        }
        public static MagicGenome Cross(MagicGenome a,MagicGenome b,SpeciesRank childRank,BreedingRuleDatabase rules,Random rng)
        {
            if(a==null || b==null || !a.HasProgram || !b.HasProgram)throw new InvalidOperationException("配合にはGP魔法の遺伝子が必要です。");
            rng=rng??new Random(0);
            return Spells.SpellSlotStorage.Prepare(new MagicGenome {programGenes=Spells.SpellGenetics.Cross(a.programGenes,b.programGenes,rng.Next())},0);
        }
        public static void Mutate(MagicGenome g,BreedingRuleDatabase rules,Random rng)
        {
            if(g==null || !g.HasProgram)throw new InvalidOperationException("Invalid GP magic data.");
            rng=rng??new Random(0);var genes=g.programGenes.Clone();
            genes.chromosomeA=Spells.SpellGenetics.Mutate(genes.chromosomeA,rng,Spells.SpellSettings.Current,genes.history);
            genes.chromosomeB=Spells.SpellGenetics.Mutate(genes.chromosomeB,rng,Spells.SpellSettings.Current,genes.history);
            g.programGenes=genes;string report;g.expressedProgram=Spells.SpellGenetics.Express(genes,out report);
        }
    }
}
