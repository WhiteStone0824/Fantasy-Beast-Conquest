using System;
using System.Collections.Generic;
using UnityEngine;

namespace FusionAnyMonster
{
    [Serializable] public sealed class BreedingResult
    {
        public UnitInstance child;
        public SpeciesDefinition species;
        public SpeciesDefinition[] speciesCandidates;
        public SpeciesRank predictedRank;
        public string summary;
        public bool isSpecial;
    }

    public sealed class BreedingSystem
    {
        private readonly SpeciesDatabase species;
        private readonly BreedingRuleDatabase rules;
        private readonly RankBreedingRule rankRule;
        private readonly PersonalityType[] personalities;
        private readonly Func<string,string> ancestorSpecies;
        public BreedingSystem(SpeciesDatabase speciesDatabase,BreedingRuleDatabase breedingRules,RankBreedingRule rankBreedingRule,PersonalityType[] personalityOptions=null,Func<string,string> ancestorSpeciesLookup=null)
        { species=speciesDatabase;rules=breedingRules;rankRule=rankBreedingRule;ancestorSpecies=ancestorSpeciesLookup;personalities=personalityOptions==null||personalityOptions.Length==0?(PersonalityType[])Enum.GetValues(typeof(PersonalityType)):personalityOptions; }

        public static int CreateRandomSeed() { return BitConverter.ToInt32(Guid.NewGuid().ToByteArray(),0); }

        public BreedingResult Breed(UnitInstance parentA,UnitInstance parentB,int seed)
        {
            if(parentA==null||parentB==null)throw new ArgumentNullException("Both parents are required.");
            if(string.IsNullOrWhiteSpace(parentA.id)||string.IsNullOrWhiteSpace(parentB.id)||parentA.id==parentB.id)throw new ArgumentException("Two different identified parent units are required.");
            SpeciesDefinition a=species==null?null:species.GetById(parentA.speciesId), b=species==null?null:species.GetById(parentB.speciesId);
            if(a==null||b==null)throw new InvalidOperationException("Both parent species must exist in the SpeciesDatabase.");
            var rng=new System.Random(seed);
            var special=a.speciesId==SpecialBreedingRecipe.DefaultResultId?a:b.speciesId==SpecialBreedingRecipe.DefaultResultId?b:ResolveSpecial(parentA,parentB);
            SpeciesRank childRank;
            SpeciesDefinition[] candidates;
            if(special!=null){childRank=special.rank;candidates=new[]{special};}
            else {SpeciesFamily family=rules==null?FamilyBreedingCatalog.Resolve(a.family,b.family):rules.ResolveFamily(a.family,b.family);childRank=ResolveChildRank(a,b,rng);candidates=ResolveCandidates(a,b,family,childRank);}
            if(candidates==null||candidates.Length==0)throw new InvalidOperationException("No species are available for breeding.");
            SpeciesDefinition childSpecies=candidates[rng.Next(candidates.Length)];
            var child=new UnitInstance{id=Guid.NewGuid().ToString("N"),speciesId=childSpecies.speciesId,level=1,experience=0,iv=CrossIV(parentA.iv,parentB.iv,rules,rng),personality=InheritPersonality(parentA.personality,parentB.personality,rules,rng),lineage=new LineageRecord{fatherId=parentA.id,motherId=parentB.id,generation=Math.Max(parentA.lineage==null?0:parentA.lineage.generation,parentB.lineage==null?0:parentB.lineage.generation)+1,breedingSummary=a.displayName+" × "+b.displayName+" → "+childSpecies.displayName+" (seed "+seed+")"},magicGenomes=new List<MagicGenome>(),mutationHistory=new List<string>()};
            int slots=RankRules.MagicSlots(childSpecies.rank);var poolA=Spells.SpellSlotStorage.Read(parentA);var poolB=Spells.SpellSlotStorage.Read(parentB);
            for(int i=0;i<Spells.SpellSlotStorage.SlotCount;i++)
            {
                MagicGenome ga=poolA[i],gb=poolB[i];
                MagicGenome g=GeneticAlgorithm.Cross(ga,gb,childSpecies.rank,rules,rng);
                g=GeneticAlgorithm.MutateToSpeciesDefault(g,childSpecies,i,rng,Spells.SpellSettings.Current.speciesDefaultMutationChance);
                child.magicGeneSlots.Add(g);if(i<slots)child.magicGenomes.Add(g.Clone());child.mutationHistory.AddRange(g.programGenes.history);
            }
            return new BreedingResult{child=child,species=childSpecies,speciesCandidates=candidates,predictedRank=childRank,summary=child.lineage.breedingSummary,isSpecial=special!=null};
        }

        private SpeciesDefinition ResolveSpecial(UnitInstance a,UnitInstance b)
        {
            if(ancestorSpecies==null || rules==null || rules.specialRecipes==null || a.lineage==null || b.lineage==null)return null;
            var ids=new[]{a.lineage.fatherId,a.lineage.motherId,b.lineage.fatherId,b.lineage.motherId};
            var unique=new HashSet<string>(StringComparer.Ordinal);var found=new List<string>();
            foreach(string id in ids)
            {
                if(string.IsNullOrWhiteSpace(id) || !unique.Add(id))return null;
                string speciesId=ancestorSpecies(id);if(string.IsNullOrWhiteSpace(speciesId))return null;found.Add(speciesId);
            }
            foreach(var recipe in rules.specialRecipes)
            {
                if(recipe==null || string.IsNullOrWhiteSpace(recipe.resultSpeciesId) || recipe.grandparentSpeciesIds==null || recipe.grandparentSpeciesIds.Length!=4)continue;
                var remaining=new List<string>(found);bool matches=true;
                foreach(string required in recipe.grandparentSpeciesIds)if(string.IsNullOrWhiteSpace(required) || !remaining.Remove(required)){matches=false;break;}
                if(!matches)continue;
                var result=species.GetById(recipe.resultSpeciesId);
                if(result==null)throw new InvalidOperationException("特殊配合の種族データが見つかりません: "+recipe.resultSpeciesId);
                return result;
            }
            return null;
        }

        public SpeciesDefinition[] PreviewCandidates(UnitInstance parentA,UnitInstance parentB,int seed,out SpeciesRank rank)
        {
            rank=SpeciesRank.F;if(parentA==null||parentB==null||species==null)return new SpeciesDefinition[0];var a=species.GetById(parentA.speciesId);var b=species.GetById(parentB.speciesId);if(a==null||b==null)return new SpeciesDefinition[0];var special=a.speciesId==SpecialBreedingRecipe.DefaultResultId?a:b.speciesId==SpecialBreedingRecipe.DefaultResultId?b:ResolveSpecial(parentA,parentB);if(special!=null){rank=special.rank;return new[]{special};}var rng=new System.Random(seed);rank=ResolveChildRank(a,b,rng);var family=rules==null?FamilyBreedingCatalog.Resolve(a.family,b.family):rules.ResolveFamily(a.family,b.family);return ResolveCandidates(a,b,family,rank);
        }

        private SpeciesRank ResolveChildRank(SpeciesDefinition a,SpeciesDefinition b,System.Random rng)
        {
            SpeciesRank promoted;
            if(RankBreedingRule.TryResolveGuaranteedPromotion(a,b,out promoted))return promoted;
            return rankRule!=null?rankRule.Resolve(a.rank,b.rank,rng):ResolveAverageRank(a.rank,b.rank,rng,rules);
        }
        private SpeciesDefinition[] ResolveCandidates(SpeciesDefinition a,SpeciesDefinition b,SpeciesFamily family,SpeciesRank rank)
        {
            SpeciesDefinition[] candidates=species.FindCandidates(family,rank);
            SpeciesRank promoted;
            // A guaranteed promotion must not silently produce a lower-rank species in a sparse catalog.
            if(candidates.Length>0 || RankBreedingRule.TryResolveGuaranteedPromotion(a,b,out promoted))return candidates;
            candidates=NearestCandidates(species.FindFamily(family),rank);
            return candidates;
        }

        private PersonalityType InheritPersonality(PersonalityType a,PersonalityType b,BreedingRuleDatabase r,System.Random rng)
        { float chance=r==null?.7f:r.personalityInheritanceChance;if(rng.NextDouble()<chance)return rng.Next(2)==0?a:b;return personalities[rng.Next(personalities.Length)]; }
        private static IndividualValues CrossIV(IndividualValues a,IndividualValues b,BreedingRuleDatabase rules,System.Random rng)
        { return IndividualValueInheritance.Inherit(a,b,rng,rules==null?IndividualValueInheritance.DefaultStandardDeviation:rules.ivStandardDeviation); }
        private static SpeciesRank ResolveAverageRank(SpeciesRank a,SpeciesRank b,System.Random rng,BreedingRuleDatabase rules)
        { int rank=Mathf.Clamp(Mathf.FloorToInt(((int)a+(int)b)*.5f),0,6);if(rules!=null){double roll=rng.NextDouble();if(roll<rules.rankBonusChance)rank=Math.Min(6,rank+1);else if(roll<rules.rankBonusChance+rules.rankPenaltyChance)rank=Math.Max(0,rank-1);}return (SpeciesRank)rank; }
        private static SpeciesDefinition[] NearestCandidates(SpeciesDefinition[] candidates,SpeciesRank rank)
        { if(candidates==null||candidates.Length==0)return new SpeciesDefinition[0];int best=99;for(int i=0;i<candidates.Length;i++)best=Math.Min(best,Math.Abs((int)candidates[i].rank-(int)rank));var result=new List<SpeciesDefinition>();for(int i=0;i<candidates.Length;i++)if(Math.Abs((int)candidates[i].rank-(int)rank)==best)result.Add(candidates[i]);return result.ToArray(); }
    }
}
