using System;
using FusionAnyMonster.Battle;
using FusionAnyMonster.Save;
using NUnit.Framework;

namespace FusionAnyMonster.Tests
{
    public sealed class LimitedStageRulesTests
    {
        private sealed class FixedRandom : Random
        {
            private readonly double value;
            public FixedRandom(double value) { this.value=value; }
            public override double NextDouble() { return value; }
        }
        [Test] public void EnemyEggDropUsesSeparateBossAndNormalThresholds()
        {
            Assert.IsTrue(LimitedStageRules.DropsEgg(new FixedRandom(.009999),false));
            Assert.IsFalse(LimitedStageRules.DropsEgg(new FixedRandom(.01),false));
            Assert.IsTrue(LimitedStageRules.DropsEgg(new FixedRandom(.099999),true));
            Assert.IsFalse(LimitedStageRules.DropsEgg(new FixedRandom(.10),true));
            Assert.IsFalse(LimitedStageRules.DropsEgg(null,false));
        }
        [Test] public void ExperienceStageAwardsOneHundredTimesTheNormalAmount()
        {
            Assert.AreEqual(15300,BattleStage.StageEnemyExperience(69,false,LimitedStageKind.DailyExperience));
            Assert.AreEqual(153000,BattleStage.StageEnemyExperience(69,false,LimitedStageKind.DailySuperExperience));
            Assert.AreEqual(153,BattleStage.StageEnemyExperience(69,false,LimitedStageKind.None));
        }
        [Test] public void AttemptsResetByLocalDateAndDailyModesAreIndependent()
        {
            var save=new GameSaveData();var today=new DateTime(2026,10,31,12,0,0);
            for(int i=0;i<5;i++)Assert.IsTrue(LimitedStageRules.Consume(save,LimitedStageKind.Weekday,today));
            Assert.IsFalse(LimitedStageRules.Consume(save,LimitedStageKind.Weekday,today));
            Assert.IsTrue(LimitedStageRules.Consume(save,LimitedStageKind.DailyBoss,today));
            Assert.IsTrue(LimitedStageRules.Consume(save,LimitedStageKind.DailyExperience,today));
            Assert.IsTrue(LimitedStageRules.Consume(save,LimitedStageKind.DailySuperExperience,today));
            Assert.IsFalse(LimitedStageRules.Consume(save,LimitedStageKind.DailyExperience,today));
            Assert.IsTrue(LimitedStageRules.Consume(save,LimitedStageKind.ChallengeBoss,today));
            Assert.IsTrue(LimitedStageRules.Consume(save,LimitedStageKind.ChallengeBoss,today));
            var tomorrow=today.AddDays(1);
            Assert.AreEqual(5,LimitedStageRules.Remaining(save,LimitedStageKind.Weekday,tomorrow));
            Assert.IsTrue(LimitedStageRules.Consume(save,LimitedStageKind.DailyExperience,tomorrow));
            Assert.AreEqual(5,LimitedStageRules.Remaining(save,LimitedStageKind.Weekday,tomorrow));
        }
        [Test] public void LegacySharedAttemptDoesNotResetExistingDailyUse()
        {
            var today=new DateTime(2026,11,1);var save=new GameSaveData{limitedStageDate=LimitedStageRules.DateKey(today),dailyStageAttempts=1};
            Assert.AreEqual(0,LimitedStageRules.Remaining(save,LimitedStageKind.DailyBoss,today));
            Assert.AreEqual(0,LimitedStageRules.Remaining(save,LimitedStageKind.DailyExperience,today));
            Assert.AreEqual(1,LimitedStageRules.Remaining(save,LimitedStageKind.DailySuperExperience,today));
        }
        [Test] public void EggDropIsSeededAndApproximatelyTenPercent()
        {
            var first=new Random(819);var second=new Random(819);int drops=0;
            for(int i=0;i<10000;i++){bool a=LimitedStageRules.DropsEgg(first);Assert.AreEqual(a,LimitedStageRules.DropsEgg(second));if(a)drops++;}
            Assert.That(drops,Is.InRange(900,1100));
        }
        [Test] public void AllWeekdaysHaveAvailableHighRankBosses()
        {
            var db=UnityEngine.Resources.Load<SpeciesDatabase>("SpeciesDatabase");Assert.NotNull(db);
            foreach(DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))Assert.NotNull(LimitedStageRules.ChooseSpecies(db,LimitedStageRules.Family(day),(int)SpeciesRank.B),day.ToString());
        }
        [TestCase(DayOfWeek.Sunday,SpeciesFamily.Chaos,"混沌系")]
        [TestCase(DayOfWeek.Monday,SpeciesFamily.Astral,"星界系")]
        [TestCase(DayOfWeek.Tuesday,SpeciesFamily.Dragon,"竜系")]
        [TestCase(DayOfWeek.Wednesday,SpeciesFamily.DivineBeast,"幻獣系")]
        [TestCase(DayOfWeek.Thursday,SpeciesFamily.Angel,"天使系")]
        [TestCase(DayOfWeek.Friday,SpeciesFamily.Demon,"悪魔系")]
        [TestCase(DayOfWeek.Saturday,SpeciesFamily.Abyssal,"深淵系")]
        public void WeekdayBossFamilyAndLabelMatch(DayOfWeek day,SpeciesFamily family,string label)
        { Assert.AreEqual(family,LimitedStageRules.Family(day));Assert.AreEqual(label,LimitedStageRules.FamilyName(day)); }
    }
}
