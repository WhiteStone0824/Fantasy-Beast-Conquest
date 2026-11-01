using UnityEngine;

namespace FusionAnyMonster
{
    [CreateAssetMenu(menuName="Fusion Any Monster/Breeding Rule Database")]
    public sealed class BreedingRuleDatabase : ScriptableObject
    {
        public FamilyPairRule[] familyRules = new FamilyPairRule[0];
        [Tooltip("祖父母4体の種族が一致した場合に適用する特別な配合。順序は問いません。")]
        public SpecialBreedingRecipe[] specialRecipes = SpecialBreedingRecipe.CreateDefaults();
        [Range(0,1)] public float rankBonusChance = .12f;
        [Range(0,1)] public float rankPenaltyChance = .04f;
        [Tooltip("各能力の個体値を、両親の平均を中心とする正規分布から決定します。0なら平均値に固定します。")]
        [Range(0,IndividualValues.MaxValue)] public float ivStandardDeviation = IndividualValueInheritance.DefaultStandardDeviation;
        [Range(0,1)] public float personalityInheritanceChance = .7f;
        public SpeciesFamily ResolveFamily(SpeciesFamily a, SpeciesFamily b)
        {
            if (familyRules != null)
                for (int i = 0; i < familyRules.Length; i++) { var rule = familyRules[i]; if (rule!=null && ((rule.first == a && rule.second == b) || (rule.first == b && rule.second == a))) return rule.result; }
            return FamilyBreedingCatalog.Resolve(a,b);
        }
    }
}
