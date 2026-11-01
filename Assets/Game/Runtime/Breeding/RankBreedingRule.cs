using UnityEngine;

namespace FusionAnyMonster
{
    [CreateAssetMenu(menuName="Fusion Any Monster/Rank Breeding Rule")]
    public sealed class RankBreedingRule : ScriptableObject
    {
        [Range(-1,1)] public int rankOffset;
        [Range(0,1)] public float promotionChance = .12f;
        [HideInInspector] public float demotionChance;
        public static bool TryResolveGuaranteedPromotion(SpeciesDefinition a,SpeciesDefinition b,out SpeciesRank rank)
        {
            rank=SpeciesRank.F;
            if(a==null || b==null || a.family!=b.family)return false;
            rank=(SpeciesRank)System.Math.Min((int)SpeciesRank.S,System.Math.Max((int)a.rank,(int)b.rank)+1);
            return true;
        }
        public static int RandomizedAverage(SpeciesRank a,SpeciesRank b,System.Random rng)
        {
            int sum=(int)a+(int)b;
            return sum/2+((sum%2!=0 && rng.Next(2)==1)?1:0);
        }
        public SpeciesRank Resolve(SpeciesRank a, SpeciesRank b, System.Random rng)
        {
            rng = rng ?? new System.Random(0);
            int rank = Mathf.Clamp(RandomizedAverage(a,b,rng) + rankOffset, 0, 6);
            double roll = rng.NextDouble(); if (roll < promotionChance) rank = System.Math.Min(6, rank + 1);
            return (SpeciesRank)rank;
        }
    }
}
