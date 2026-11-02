using System.Collections.Generic;
using FusionAnyMonster.Battle;
using FusionAnyMonster.Save;
using NUnit.Framework;
using UnityEngine;

namespace FusionAnyMonster.Tests
{
    public sealed class PvpTeamTests
    {
        [Test] public void TeamUsesIndependentCopiesAndRejectsInvalidValues()
        {
            var db = Resources.Load<SpeciesDatabase>("SpeciesDatabase");
            var unit = WildUnitFactory.Create(db.GetById("demo_00"), 10, 12);
            unit.deployed = true;
            var save = new GameSaveData { units = new List<UnitSaveData> { unit } };
            Assert.IsTrue(PvpTeam.TryCreate(save, db, out var team, out var error), error);
            Assert.AreEqual(PlayerRankCalculator.Calculate(save).Total,team.playerRank);
            team.units[0].individualValues[0] = 0;
            Assert.AreNotSame(unit.individualValues, team.units[0].individualValues);
            team.units[0].individualValues[0] = 101;
            Assert.IsFalse(team.Validate(db, out error));
            Assert.AreNotEqual(101, unit.individualValues[0]);
        }

        [Test] public void EmptyAndDuplicateTeamsAreRejected()
        {
            var db = Resources.Load<SpeciesDatabase>("SpeciesDatabase");
            Assert.IsFalse(new PvpTeam().Validate(db, out _));
            var unit = WildUnitFactory.Create(db.GetById("demo_00"), 10, 12);
            var team = new PvpTeam { units = new List<UnitSaveData> { unit, unit } };
            Assert.IsFalse(team.Validate(db, out _));
        }
        [Test] public void PvpRejectsDifferentIndividualsOfSameSpeciesButAllowsSameFamily()
        {
            var db=Resources.Load<SpeciesDatabase>("SpeciesDatabase");
            var species=db.GetById("demo_00");var other=db.GetById("species_F00_R0_B");
            Assert.NotNull(species);Assert.NotNull(other);Assert.AreEqual(species.family,other.family);
            var a=WildUnitFactory.Create(species,10,1,"unit-a");var b=WildUnitFactory.Create(species,20,2,"unit-b");
            var c=WildUnitFactory.Create(other,10,3,"unit-c");a.deployed=b.deployed=c.deployed=true;
            var save=new GameSaveData{units=new List<UnitSaveData>{a,b}};
            Assert.IsFalse(PvpTeam.TryCreate(save,db,out _,out var error));StringAssert.Contains("同じ種族",error);
            var incoming=new PvpTeam{units=new List<UnitSaveData>{a,b}};
            Assert.IsFalse(incoming.Validate(db,out error));StringAssert.Contains("同じ種族",error);
            b.deployed=false;save.units.Add(c);
            Assert.IsTrue(PvpTeam.TryCreate(save,db,out var team,out error),error);Assert.AreEqual(2,team.units.Count);
        }
    }
}
