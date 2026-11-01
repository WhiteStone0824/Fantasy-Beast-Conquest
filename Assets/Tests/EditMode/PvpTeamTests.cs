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
    }
}
