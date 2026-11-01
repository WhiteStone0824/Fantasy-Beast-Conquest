using System;
using FusionAnyMonster.Save;
using UnityEditor;
using UnityEngine;

namespace FusionAnyMonster.Editor
{
    public static class FusionAnyMonsterValidation
    {
        [MenuItem("Fusion Any Monster/Validation/Run Core Checks")]
        public static void RunCoreChecks()
        {
            try
            {
                SpeciesDatabase species = Resources.Load<SpeciesDatabase>("SpeciesDatabase");
                BreedingRuleDatabase rules = Resources.Load<BreedingRuleDatabase>("BreedingRuleDatabase");
                RankBreedingRule rankRule = Resources.Load<RankBreedingRule>("RankBreedingRule");
                PersonalityDatabase personalities = Resources.Load<PersonalityDatabase>("PersonalityDatabase");
                Require(species != null && species.species.Length >= 10, "At least ten species are available.");
                Require(rules != null && rules.familyRules.Length == 465, "Every unordered family pair has a breeding rule.");
                for (int i = 0; i < species.species.Length; i++) for (int j = i + 1; j < species.species.Length; j++)
                    if (species.species[i].speciesId == species.species[j].speciesId && species.species[i].family == species.species[j].family && species.species[i].rank == species.species[j].rank)
                        Require(Mathf.Approximately(species.species[i].baseStats.hp, species.species[j].baseStats.hp), "Same species master values remain consistent.");

                var iv = new IndividualValues(); iv.Set(0, int.MaxValue); Require(iv.hp == IndividualValues.MaxValue, "IV clamps to 0..100.");
                var levelUnit = new UnitInstance { level = 98, experience = 0, iv = new IndividualValues() };
                ExperienceSystem.Grant(levelUnit, long.MaxValue); Require(levelUnit.level == 99, "Experience clamps level to 99.");
                Require(species.species[0].GetResistance(AttributeElement.Fire) != null && species.species[0].GetResistance(DebuffType.Poison) != null, "Element and debuff resistance entries resolve.");
                var personalityUnit = new UnitInstance { speciesId = species.species[0].speciesId, level = 10, iv = new IndividualValues(), personality = PersonalityType.Brave };
                UnitStats braveStats = UnitStatsCalculator.Calculate(species.species[0], personalityUnit, null, personalities);
                personalityUnit.personality = PersonalityType.Timid;
                UnitStats timidStats = UnitStatsCalculator.Calculate(species.species[0], personalityUnit, null, personalities);
                Require(!Mathf.Approximately(braveStats.physicalAttack, timidStats.physicalAttack), "Personality changes final combat stats.");

                MagicGenome first = DemoDataFactory.DemoGenome(0), second = DemoDataFactory.DemoGenome(4);
                MagicGenome crossedA = GeneticAlgorithm.Cross(first, second, SpeciesRank.C, rules, new System.Random(9191));
                MagicGenome crossedB = GeneticAlgorithm.Cross(first, second, SpeciesRank.C, rules, new System.Random(9191));
                Require(JsonUtility.ToJson(crossedA) == JsonUtility.ToJson(crossedB), "Seeded crossover is reproducible.");
                BuiltMagic built=MagicFactory.Build(crossedA);
                Require(built.program!=null,"GP program compiles.");
                MagicGenome restored;Require(MagicGenome.TryParseJson(JsonUtility.ToJson(crossedB),out restored),"GP genes survive JSON round-trip.");
                var parentA = new UnitInstance { id = "validation-a", speciesId = species.species[0].speciesId, level = 1, iv = IndividualValues.Random(new System.Random(1)), personality = PersonalityType.Brave, magicGenomes = new System.Collections.Generic.List<MagicGenome> { first } };
                var parentB = new UnitInstance { id = "validation-b", speciesId = species.species[1].speciesId, level = 1, iv = IndividualValues.Random(new System.Random(2)), personality = PersonalityType.Cautious, magicGenomes = new System.Collections.Generic.List<MagicGenome> { second } };
                var breeding = new BreedingSystem(species, rules, rankRule);
                BreedingResult childA = breeding.Breed(parentA, parentB, 42), childB = breeding.Breed(parentA, parentB, 42);
                Require(species.GetById(childA.child.speciesId) != null, "Breeding resolves an existing species.");
                Require(childA.child.speciesId == childB.child.speciesId && JsonUtility.ToJson(childA.child.iv) == JsonUtility.ToJson(childB.child.iv), "Breeding seed reproduces offspring.");
                Require(childA.child.magicGenomes.Count <= RankRules.MagicSlots(childA.species.rank), "Child magic slots respect child rank.");
                foreach (MagicGenome genome in childA.child.magicGenomes) Require(MagicFactory.Build(genome).program!=null,"Child GP program compiles.");

                Debug.Log("Fusion Any Monster validation passed: master data, personality stats, IV/level clamps, resistance entries, seeded crossover/breeding, genome bounds and slot cap.");
            }
            catch (Exception exception) { Debug.LogError("Fusion Any Monster validation failed: " + exception.Message); throw; }
        }

        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
