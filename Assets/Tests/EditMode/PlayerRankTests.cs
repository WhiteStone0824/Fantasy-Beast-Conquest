using System.Collections.Generic;
using FusionAnyMonster.Save;
using FusionAnyMonster.Spells;
using NUnit.Framework;
using UnityEngine;

namespace FusionAnyMonster.Tests
{
    public sealed class PlayerRankTests
    {
        [Test] public void RankUsesCurrentOwnershipAndDoesNotCountEquipmentTwice()
        {
            var spell=JsonUtility.ToJson(SpellSlotStorage.Prepare(new MagicGenome{programGenes=SpellProgramBuilder.Projectile()},0));
            var equipped=new EquipmentItem{id="equipped",level=40};
            var unit=new UnitSaveData{speciesId="a",rank="F",level=20,individualValues=new[]{100,100,100,100,100,100,100,100,100,100},equipment=equipped,magicGenomeJson=new[]{spell,spell}};
            var second=new UnitSaveData{speciesId="b",rank="F",level=10,individualValues=new int[10],magicGenomeJson=new[]{spell}};
            var save=new GameSaveData{discoveredSpeciesIds=new List<string>{"a","a"},units=new List<UnitSaveData>{unit,second},equipmentInventory=new List<EquipmentItem>{equipped,new EquipmentItem{id="bag",level=20}}};
            var score=PlayerRankCalculator.Calculate(save);
            Assert.AreEqual(2,score.catalogue);Assert.AreEqual(3,score.unitLevels);Assert.AreEqual(3,score.equipmentLevels);
            Assert.AreEqual(5,score.individualValues);Assert.AreEqual(1,score.magic);Assert.AreEqual(14,score.Total);
            save.units.Remove(second);
            Assert.Less(PlayerRankCalculator.Calculate(save).Total,score.Total);
        }
        [Test] public void EmptySaveHasZeroRank()
        { Assert.AreEqual(0,PlayerRankCalculator.Calculate(null).Total);Assert.AreEqual(0,PlayerRankCalculator.Calculate(new GameSaveData()).Total); }
        [Test] public void PlayTimeFormatsHoursAndMinutesAndReadsLegacySave()
        {
            var old=JsonUtility.FromJson<GameSaveData>("{\"schemaVersion\":7}");
            Assert.AreEqual(0,old.playTimeSeconds);
            Assert.AreEqual("1時間 01分",PlayerPlayTime.Format(3661));
            Assert.AreEqual("0時間 00分",PlayerPlayTime.Format(-1));
        }
    }
}
