using UnityEngine;

namespace FusionAnyMonster
{
    [CreateAssetMenu(menuName="Fusion Any Monster/Rank Breeding Rule")]
    public sealed class RankBreedingRule : ScriptableObject
    {
        [Range(-1,1)] public int rankOffset;
        [Range(0,1)] public float promotionChance = .12f;
        [Range(0,1)] public float demotionChance = .04f;
        public static bool TryResolveGuaranteedPromotion(SpeciesDefinition a,SpeciesDefinition b,out SpeciesRank rank)
        {
            rank=SpeciesRank.F;
            if(a==null || b==null || a.rank!=b.rank || a.family!=b.family || a.speciesId==b.speciesId)return false;
            rank=(SpeciesRank)System.Math.Min((int)SpeciesRank.S,(int)a.rank+1);
            return true;
        }
        public SpeciesRank Resolve(SpeciesRank a, SpeciesRank b, System.Random rng)
        {
            rng = rng ?? new System.Random(0);
            int rank = Mathf.Clamp(Mathf.FloorToInt(((int)a + (int)b) * .5f) + rankOffset, 0, 6);
            double roll = rng.NextDouble(); if (roll < promotionChance) rank = System.Math.Min(6, rank + 1); else if (roll < promotionChance + demotionChance) rank = System.Math.Max(0, rank - 1);
            return (SpeciesRank)rank;
        }
    }
}
