using UnityEngine;
using FusionAnyMonster.Save;

namespace FusionAnyMonster.Battle
{
    public enum StageDifficulty { Easy, Normal, Hard, VeryHard, Hell }

    public static class BattleStage
    {
        public static StageDifficulty Difficulty { get; private set; } = StageDifficulty.Easy;
        public static bool BossRush { get; private set; }
        public static LimitedStageKind Limited { get; private set; }
        public static SpeciesFamily LimitedFamily { get; private set; }
        public static bool IsEnemyFamily(SpeciesFamily family)
        { return family==SpeciesFamily.Slime || family==SpeciesFamily.Beast || family==SpeciesFamily.Bird || family==SpeciesFamily.Material || family==SpeciesFamily.Insect || family==SpeciesFamily.Plant; }
        // Reservoir sampling gives each matching species an equal chance without allocating per spawn.
        public static SpeciesDefinition ChooseEnemySpecies(SpeciesDatabase database,int rank)
        {
            if(database==null || database.species==null)return null;
            SpeciesDefinition selected=null;int count=0;
            foreach(var species in database.species)
            {
                if(species==null || (int)species.rank!=rank || !IsEnemyFamily(species.family))continue;
                count++;if(Random.Range(0,count)==0)selected=species;
            }
            return selected;
        }
        // Enemy damage only in PvE. Higher stages retain progressively more pressure.
        public static float EnemyDamageMultiplier
        {
            get { switch(Difficulty) { case StageDifficulty.Easy:return .21f;case StageDifficulty.Normal:return .25f;case StageDifficulty.Hard:return .5f;case StageDifficulty.VeryHard:return .75f;default:return 1f; } }
        }
        public static float NormalEnemyHealthMultiplier
        { get { switch(Difficulty) { case StageDifficulty.Easy:return .3f;case StageDifficulty.Normal:return .5f;case StageDifficulty.Hard:return .75f;default:return 1f; } } }
        public static float BossHealthMultiplier
        { get { switch(Difficulty) { case StageDifficulty.Easy:return .4f;case StageDifficulty.Normal:return .55f;case StageDifficulty.Hard:return .7f;case StageDifficulty.VeryHard:return .85f;default:return 1f; } } }
        public static float MinimumSpawnInterval
        { get { switch(Difficulty) { case StageDifficulty.Easy:return 4f;case StageDifficulty.Normal:return 3.7f;case StageDifficulty.Hard:return 3.1f;case StageDifficulty.VeryHard:return 2.6f;default:return 2.2f; } } }
        public static long EnemyExperience(int level,bool boss)
        { int safe=Mathf.Clamp(level,1,99);return boss?100+safe*10L:15+safe*2L; }
        public static long StageEnemyExperience(int level,bool boss,LimitedStageKind kind)
        { return EnemyExperience(level,boss)*(kind==LimitedStageKind.DailySuperExperience?1000L:kind==LimitedStageKind.DailyExperience?100L:1L); }
        public static bool Endless { get { return Limited==LimitedStageKind.None && Difficulty == StageDifficulty.Hell; } }
        public static bool HideWave { get { return Limited==LimitedStageKind.Weekday || Limited==LimitedStageKind.ChallengeBoss; } }
        public static string Name { get { return Limited==LimitedStageKind.Weekday?"曜日限定ボス":Limited==LimitedStageKind.ChallengeBoss?"チャレンジボス":Limited==LimitedStageKind.DailyBoss?"一日一回・強敵":Limited==LimitedStageKind.DailyExperience?"一日一回・経験値":Limited==LimitedStageKind.DailySuperExperience?"一日一回・超経験値":(BossRush ? "ボスラッシュ・" : "常設・") + Difficulty; } }
        public static void Select(StageDifficulty difficulty, bool bossRush) { Difficulty = difficulty; BossRush = bossRush; Limited=LimitedStageKind.None; }
        public static void SelectLimited(LimitedStageKind kind,SpeciesFamily family) { Limited=kind;LimitedFamily=family;Difficulty=StageDifficulty.Hard;BossRush=false; }
        private static readonly int[] RequiredRanks={0,50,100,150,200};
        public static int RequiredPlayerRank(StageDifficulty difficulty) { int index=(int)difficulty;return index<0 || index>=RequiredRanks.Length?int.MaxValue:RequiredRanks[index]; }
        public static bool IsUnlocked(GameSaveData save, StageDifficulty difficulty, bool bossRush)
        {
            int rank = (int)difficulty;
            if (rank < 0 || rank > (int)StageDifficulty.Hell) return false;
            return PlayerRankCalculator.Calculate(save).Total>=RequiredPlayerRank(difficulty);
        }
        public static int Level(float elapsed, int bossCount)
        {
            int wave = BossRush ? bossCount : Mathf.FloorToInt(elapsed / 30f);
            if (Endless) return Mathf.Clamp((1 + wave) * 3, 1, 99);
            int maximum = 10 * ((int)Difficulty + 1);
            return Mathf.Clamp(Mathf.Clamp(maximum - 7 + wave * 2, 1, maximum) * 3, 1, 99);
        }
        public static int Rank(float elapsed, int bossCount)
        {
            if (!Endless) return (int)Difficulty;
            int wave = BossRush ? bossCount : Mathf.FloorToInt(elapsed / 30f);
            return Mathf.Clamp(wave / 4, 0, (int)SpeciesRank.B);
        }
    }
}
