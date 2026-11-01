using System;
using UnityEngine;

namespace FusionAnyMonster
{
    [Serializable] public sealed class FamilyPairRule
    {
        public SpeciesFamily first, second, result;
    }
    [Serializable] public sealed class SpecialBreedingRecipe
    {
        public const string DefaultResultId = "special_four_lineages";
        public string resultSpeciesId;
        public string[] grandparentSpeciesIds = new string[0];
        public static SpecialBreedingRecipe[] CreateDefaults()
        {
            return new[]{new SpecialBreedingRecipe {resultSpeciesId=DefaultResultId,grandparentSpeciesIds=new[]{"species_F22_R6_B","species_F28_R6_B","species_F26_R6_B","species_F24_R6_B"}}};
        }
    }
}
