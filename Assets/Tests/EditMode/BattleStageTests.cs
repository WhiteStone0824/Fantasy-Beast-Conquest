using FusionAnyMonster.Battle;
using NUnit.Framework;
using FusionAnyMonster.Save;
using UnityEngine;

namespace FusionAnyMonster.Tests
{
    public class BattleStageTests
    {
        [TearDown] public void ResetStage() { BattleStage.Select(StageDifficulty.Easy, false); }
        [TestCase(StageDifficulty.Easy, 0, 30)]
        [TestCase(StageDifficulty.Normal, 1, 60)]
        [TestCase(StageDifficulty.Hard, 2, 90)]
        [TestCase(StageDifficulty.VeryHard, 3, 99)]
        public void FixedStagesUseExpectedRankAndLevelCap(StageDifficulty difficulty, int rank, int cap)
        {
            BattleStage.Select(difficulty, false);
            Assert.AreEqual(rank, BattleStage.Rank(10000, 0));
            Assert.AreEqual(cap, BattleStage.Level(10000, 0));
        }
        [Test] public void HellContinuesIncreasingAndCapsValidValues()
        {
            BattleStage.Select(StageDifficulty.Hell, false);
            Assert.IsTrue(BattleStage.Endless);
            Assert.Greater(BattleStage.Level(300, 0), BattleStage.Level(0, 0));
            Assert.AreEqual((int)SpeciesRank.B, BattleStage.Rank(10000, 0));
            Assert.AreEqual(99, BattleStage.Level(10000, 0));
        }
        [Test] public void BossRushProgressUsesBossKills()
        {
            BattleStage.Select(StageDifficulty.Hell, true);
            Assert.AreEqual(3, BattleStage.Level(3000, 0));
            Assert.AreEqual(6, BattleStage.Level(0, 1));
            Assert.AreEqual(1, BattleStage.Rank(0, 4));
            Assert.AreEqual((int)SpeciesRank.B, BattleStage.Rank(0, 1000));
        }
        [Test] public void BothModesUnlockAtTheRequiredPlayerRanks()
        {
            var save = new GameSaveData();
            Assert.IsTrue(BattleStage.IsUnlocked(save, StageDifficulty.Easy, false));
            Assert.IsTrue(BattleStage.IsUnlocked(save, StageDifficulty.Easy, true));
            Assert.IsFalse(BattleStage.IsUnlocked(save, StageDifficulty.Normal, false));
            for(int i=0;i<50;i++)save.discoveredSpeciesIds.Add("species-"+i);
            Assert.IsTrue(BattleStage.IsUnlocked(save, StageDifficulty.Normal, false));
            Assert.IsTrue(BattleStage.IsUnlocked(save, StageDifficulty.Normal, true));
            Assert.IsFalse(BattleStage.IsUnlocked(save, StageDifficulty.Hard, false));
            for(int i=50;i<200;i++)save.discoveredSpeciesIds.Add("species-"+i);
            var restored = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(save));
            Assert.IsTrue(BattleStage.IsUnlocked(restored, StageDifficulty.Hell, false));
            Assert.IsTrue(BattleStage.IsUnlocked(restored, StageDifficulty.Normal, true));
        }
        [Test] public void OlderSaveStartsAtEasyForBothModes()
        {
            var save = JsonUtility.FromJson<GameSaveData>("{\"schemaVersion\":4,\"battlesCompleted\":50}");
            Assert.IsTrue(BattleStage.IsUnlocked(save, StageDifficulty.Easy, false));
            Assert.IsTrue(BattleStage.IsUnlocked(save, StageDifficulty.Easy, true));
            Assert.IsFalse(BattleStage.IsUnlocked(save, StageDifficulty.Normal, false));
            Assert.IsFalse(BattleStage.IsUnlocked(save, StageDifficulty.Normal, true));
        }
        [Test] public void DifficultyScalingAndRewardsIncreaseWithoutChangingRankOrLevelLimits()
        {
            float lastDamage=0,lastNormalHp=0,lastBossHp=0,lastInterval=float.MaxValue;
            foreach(StageDifficulty difficulty in System.Enum.GetValues(typeof(StageDifficulty)))
            {
                BattleStage.Select(difficulty,false);
                Assert.Greater(BattleStage.EnemyDamageMultiplier,lastDamage);
                Assert.GreaterOrEqual(BattleStage.NormalEnemyHealthMultiplier,lastNormalHp);
                Assert.Greater(BattleStage.BossHealthMultiplier,lastBossHp);
                Assert.Less(BattleStage.MinimumSpawnInterval,lastInterval);
                lastDamage=BattleStage.EnemyDamageMultiplier;lastNormalHp=BattleStage.NormalEnemyHealthMultiplier;
                lastBossHp=BattleStage.BossHealthMultiplier;lastInterval=BattleStage.MinimumSpawnInterval;
            }
            Assert.Greater(BattleStage.EnemyExperience(30,false),BattleStage.EnemyExperience(9,false));
            Assert.Greater(BattleStage.EnemyExperience(9,true),BattleStage.EnemyExperience(9,false));
        }
        [Test] public void EnemySelectionUsesOnlySixFamiliesForEveryStageRank()
        {
            var db=Resources.Load<SpeciesDatabase>("SpeciesDatabase");Assert.NotNull(db);
            var previous=Random.state;
            try
            {
                Random.InitState(1234);
                for(int rank=0;rank<=6;rank++)
                {
                    var seen=new System.Collections.Generic.HashSet<SpeciesFamily>();
                    for(int i=0;i<500;i++)
                    {
                        var species=BattleStage.ChooseEnemySpecies(db,rank);
                        Assert.NotNull(species);Assert.AreEqual(rank,(int)species.rank);
                        Assert.IsTrue(BattleStage.IsEnemyFamily(species.family));seen.Add(species.family);
                    }
                    CollectionAssert.AreEquivalent(new[]{SpeciesFamily.Slime,SpeciesFamily.Beast,SpeciesFamily.Bird,SpeciesFamily.Material,SpeciesFamily.Insect,SpeciesFamily.Plant},seen);
                }
            }
            finally { Random.state=previous; }
        }
        [Test] public void NoMatchingSpeciesDoesNotFallBackToAnotherFamily()
        {
            var db=ScriptableObject.CreateInstance<SpeciesDatabase>();
            var other=ScriptableObject.CreateInstance<SpeciesDefinition>();
            try {other.family=SpeciesFamily.Dragon;other.rank=SpeciesRank.F;db.species=new[]{other};Assert.IsNull(BattleStage.ChooseEnemySpecies(db,0));}
            finally {Object.DestroyImmediate(other);Object.DestroyImmediate(db);}
        }
    }
}
