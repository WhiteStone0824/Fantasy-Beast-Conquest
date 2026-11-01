using System;
using System.Collections.Generic;
using UnityEngine;

namespace FusionAnyMonster
{

    [Serializable] public sealed class MagicGenome
    {
        public Spells.SpellGenotype programGenes;
        public Spells.SpellProgram expressedProgram;
        public int programVersion=1;
        public string customName;
        public bool HasProgram {get{return programGenes!=null && programGenes.chromosomeA!=null && programGenes.chromosomeB!=null && programGenes.chromosomeA.nodes!=null && programGenes.chromosomeA.nodes.Count>0 && programGenes.chromosomeB.nodes!=null && programGenes.chromosomeB.nodes.Count>0;}}
        public static bool TryParseJson(string json, out MagicGenome genome)
        {
            genome = null;
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                genome = JsonUtility.FromJson<MagicGenome>(json);
                if(genome!=null)
                {
                    string error;var genes=genome.programGenes;
                    if(genome.programVersion!=1 || genes==null || genes.version!=1 || genes.algorithmVersion!=1){genome=null;return false;}
                    var phenotype=Spells.SpellGenetics.Express(genes,out error);
                    if(!Spells.SpellValidator.Validate(phenotype,out error)){genome=null;return false;}
                    genome.expressedProgram=phenotype;
                }
                return genome != null;
            }
            catch (Exception) { genome=null;return false; }
        }

        public MagicGenome Clone()
        {
            return new MagicGenome {programVersion=programVersion,customName=customName,
                programGenes=programGenes==null?null:programGenes.Clone(),
                expressedProgram=expressedProgram==null?null:expressedProgram.Clone()};
        }


    }

    [Serializable] public sealed class BuiltMagic
    {
        public bool available=true;
        [NonSerialized] public Spells.CompiledSpell program;
        public string displayName;
        public AttributeElement element;
        public Spells.SpellTrajectory trajectory;
        public float power, projectileSpeed, range, cooldown, area, duration, mpCost, complexity;
        public int projectileCount;
        public bool targetsAllies, isArea, centerOnCaster;
        public string Describe() { return displayName; }
    }
}
