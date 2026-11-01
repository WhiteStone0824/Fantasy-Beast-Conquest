using System.Collections.Generic;
using FusionAnyMonster.Save;
using FusionAnyMonster.UI;
using NUnit.Framework;

namespace FusionAnyMonster.Tests
{
    public sealed class UnitSortTests
    {
        [Test] public void SortsRanksAndLevelsWithoutChangingSavedOrder()
        {
            var a=new UnitSaveData{id="a",rank="F",level=50};
            var b=new UnitSaveData{id="b",rank="S",level=2};
            var c=new UnitSaveData{id="c",rank="D",level=50};
            var units=new List<UnitSaveData>{a,b,c};
            CollectionAssert.AreEqual(new[]{b,c,a},UnitSort.Apply(units,UnitSortMode.RankDescending));
            CollectionAssert.AreEqual(new[]{a,c,b},UnitSort.Apply(units,UnitSortMode.RankAscending));
            CollectionAssert.AreEqual(new[]{a,c,b},UnitSort.Apply(units,UnitSortMode.LevelDescending));
            CollectionAssert.AreEqual(new[]{b,a,c},UnitSort.Apply(units,UnitSortMode.LevelAscending));
            CollectionAssert.AreEqual(new[]{a,b,c},units);
        }
        [Test] public void IgnoresNullsAndKeepsEqualItemsInOriginalOrder()
        {
            var a=new UnitSaveData{id="a",rank="F",level=1};var b=new UnitSaveData{id="b",rank="F",level=1};
            CollectionAssert.AreEqual(new[]{a,b},UnitSort.Apply(new List<UnitSaveData>{null,a,b},UnitSortMode.RankDescending));
        }
    }
}
