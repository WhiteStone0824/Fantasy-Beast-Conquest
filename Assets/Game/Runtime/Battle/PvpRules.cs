using System;

namespace FusionAnyMonster.Battle
{
    public static class PvpRules
    {
        public const float TimeLimitSeconds=300f;
        public const float MaximumHealingFraction=.30f;
        public const float MaximumControlSeconds=2f;
        public const float ControlImmunitySeconds=1f;
        public static float LimitHealing(float amount,float maximumHp)
        { return Math.Min(Math.Max(0f,amount),Math.Max(0f,maximumHp)*MaximumHealingFraction); }
        public static bool IsHardControl(DebuffType type)
        { return type==DebuffType.Freeze || type==DebuffType.Sleep; }
        // Result is always from the host's point of view.
        public static string Resolve(int hostAlive,int guestAlive,double hostDamage,double guestDamage)
        {
            if(hostAlive>guestAlive)return "VICTORY";
            if(hostAlive<guestAlive)return "DEFEAT";
            if(hostDamage>guestDamage)return "VICTORY";
            if(hostDamage<guestDamage)return "DEFEAT";
            return "DRAW";
        }
    }
}
