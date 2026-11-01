using FusionAnyMonster.UI;
using NUnit.Framework;
using UnityEngine;

namespace FusionAnyMonster.Tests
{
    public sealed class StatBarScaleTests
    {
        [Test]
        public void CommonDisplayAxesCoverEverySpeciesAtMaximumLevelAndIndividualValues()
        {
            var settings=Resources.Load<StatBarScaleSettings>("StatBarScaleSettings");Assert.IsNotNull(settings);
            var database=Resources.Load<SpeciesDatabase>("SpeciesDatabase");Assert.IsNotNull(database);
            var personalities=Resources.Load<PersonalityDatabase>("PersonalityDatabase");
            var unit=new UnitInstance {level=99,iv=new IndividualValues()};
            for(int i=0;i<10;i++)unit.iv.Set(i,100);
            foreach(SpeciesDefinition species in database.species)
                foreach(PersonalityType personality in System.Enum.GetValues(typeof(PersonalityType)))
                {
                    unit.speciesId=species.speciesId;unit.personality=personality;
                    UnitStats stats=UnitStatsCalculator.Calculate(species,unit,null,personalities);
                    for(int i=0;i<10;i++)Assert.LessOrEqual(stats.Get(i),settings.GetMaximum(i),species.displayName+" / "+personality+" / stat "+i);
                }
        }
    }
}
