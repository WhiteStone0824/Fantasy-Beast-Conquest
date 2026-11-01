using System;
using FusionAnyMonster.Save;
using UnityEngine;

namespace FusionAnyMonster.Battle
{
    public enum LimitedStageKind { None, Weekday, DailyBoss, DailyExperience, ChallengeBoss, DailySuperExperience }
    public static class LimitedStageRules
    {
        public const double EggDropChance=.10;
        public const double NormalEnemyEggDropChance=.01;
        public const int WeekdayLimit=5;
        public const int DailyLimit=1;
        public static int RequiredRank(LimitedStageKind kind) { return kind==LimitedStageKind.ChallengeBoss?200:kind==LimitedStageKind.DailySuperExperience?100:0; }
        private static readonly SpeciesFamily[] Families={SpeciesFamily.Chaos,SpeciesFamily.Astral,SpeciesFamily.Dragon,SpeciesFamily.DivineBeast,SpeciesFamily.Angel,SpeciesFamily.Demon,SpeciesFamily.Abyssal};
        private static readonly string[] Names={"混沌系","星界系","竜系","幻獣系","天使系","悪魔系","深淵系"};
        public static SpeciesFamily Family(DayOfWeek day) { return Families[(int)day]; }
        public static string FamilyName(DayOfWeek day) { return Names[(int)day]; }
        public static string DateKey(DateTime now) { return now.ToString("yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture); }
        public static int Used(GameSaveData save,LimitedStageKind kind,DateTime now)
        {
            if(save==null || save.limitedStageDate!=DateKey(now))return 0;
            if(kind==LimitedStageKind.Weekday)return Mathf.Clamp(save.weekdayStageAttempts,0,WeekdayLimit);
            if(kind==LimitedStageKind.ChallengeBoss || kind==LimitedStageKind.None)return 0;
            if(save.limitedStageCountersVersion==0 && (kind==LimitedStageKind.DailyBoss || kind==LimitedStageKind.DailyExperience))return Mathf.Clamp(save.dailyStageAttempts,0,DailyLimit);
            return Mathf.Clamp(kind==LimitedStageKind.DailyBoss?save.dailyBossAttempts:kind==LimitedStageKind.DailyExperience?save.dailyExperienceAttempts:save.dailySuperExperienceAttempts,0,DailyLimit);
        }
        public static int Remaining(GameSaveData save,LimitedStageKind kind,DateTime now)
        { if(kind==LimitedStageKind.None)return 0;if(kind==LimitedStageKind.ChallengeBoss)return int.MaxValue;return (kind==LimitedStageKind.Weekday?WeekdayLimit:DailyLimit)-Used(save,kind,now); }
        public static bool Consume(GameSaveData save,LimitedStageKind kind,DateTime now)
        {
            if(save==null || kind==LimitedStageKind.None || Remaining(save,kind,now)<=0)return false;
            if(kind==LimitedStageKind.ChallengeBoss)return true;
            if(save.limitedStageDate!=DateKey(now)){save.limitedStageDate=DateKey(now);save.weekdayStageAttempts=save.dailyStageAttempts=save.dailyBossAttempts=save.dailyExperienceAttempts=save.dailySuperExperienceAttempts=0;save.limitedStageCountersVersion=1;}
            if(save.limitedStageCountersVersion==0){save.dailyBossAttempts=save.dailyExperienceAttempts=Mathf.Clamp(save.dailyStageAttempts,0,DailyLimit);save.limitedStageCountersVersion=1;}
            if(kind==LimitedStageKind.Weekday)save.weekdayStageAttempts++;else if(kind==LimitedStageKind.DailyBoss)save.dailyBossAttempts++;else if(kind==LimitedStageKind.DailyExperience)save.dailyExperienceAttempts++;else if(kind==LimitedStageKind.DailySuperExperience)save.dailySuperExperienceAttempts++;
            return true;
        }
        public static bool DropsEgg(System.Random random,bool boss=true) { return random!=null && random.NextDouble()<(boss?EggDropChance:NormalEnemyEggDropChance); }
        public static SpeciesDefinition ChooseSpecies(SpeciesDatabase db,SpeciesFamily family,int rank)
        {
            if(db==null || db.species==null)return null;
            SpeciesDefinition chosen=null;int count=0;
            foreach(var species in db.species)if(species!=null && species.family==family && (int)species.rank==rank)
            { count++;if(UnityEngine.Random.Range(0,count)==0)chosen=species; }
            return chosen;
        }
    }
}
