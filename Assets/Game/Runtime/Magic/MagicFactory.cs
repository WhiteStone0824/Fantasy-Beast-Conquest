using System;
using UnityEngine;
namespace FusionAnyMonster
{
    public static class MagicFactory
    {
        public static BuiltMagic Build(MagicGenome genome, SpeciesDefinition species=null)
        {
            if(genome==null || genome.programVersion!=1 || !genome.HasProgram || genome.programGenes==null)
                throw new InvalidOperationException("GP魔法の遺伝子がありません。");
            return Spells.ProgramMagic.Build(genome,species);
        }
    }
    public sealed class MagicGenomeAsset : ScriptableObject
    {
        public string genomeId;
        public MagicGenome genome;
    }
}
