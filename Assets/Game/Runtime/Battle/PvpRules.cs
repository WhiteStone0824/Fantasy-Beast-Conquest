namespace FusionAnyMonster.Battle
{
    public static class PvpRules
    {
        public const float TimeLimitSeconds=300f;
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
