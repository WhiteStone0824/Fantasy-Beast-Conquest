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
