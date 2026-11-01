using UnityEngine;

namespace FusionAnyMonster
{
    [CreateAssetMenu(menuName="Fusion Any Monster/Species Database")]
    public sealed class SpeciesDatabase : ScriptableObject
    {
        public SpeciesDefinition[] species = new SpeciesDefinition[0];
        public SpeciesDefinition GetById(string id) { if (string.IsNullOrEmpty(id)) return null; if(species!=null)for (int i = 0; i < species.Length; i++) if (species[i] != null && species[i].speciesId == id) return species[i]; var special=Resources.Load<SpeciesDefinition>("SpecialSpecies");return special!=null && special.speciesId==id?special:null; }
        public SpeciesDefinition[] FindCandidates(SpeciesFamily family, SpeciesRank rank)
        {
            var list = new System.Collections.Generic.List<SpeciesDefinition>();
            if (species == null) return list.ToArray();
            for (int i = 0; i < species.Length; i++) if (species[i] != null && species[i].family == family && species[i].rank == rank) list.Add(species[i]);
            return list.ToArray();
        }
        public SpeciesDefinition[] FindFamily(SpeciesFamily family)
        {
            var list = new System.Collections.Generic.List<SpeciesDefinition>();
            if (species == null) return list.ToArray();
            for (int i = 0; i < species.Length; i++) if (species[i] != null && species[i].family == family) list.Add(species[i]);
            return list.ToArray();
        }
    }
}
