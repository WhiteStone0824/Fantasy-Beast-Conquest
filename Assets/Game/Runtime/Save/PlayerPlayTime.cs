using System;
using UnityEngine;

namespace FusionAnyMonster.Save
{
    // Tracks wall-clock session time independent of battle speed and scene changes.
    public static class PlayerPlayTime
    {
        private static bool started;
        private static double committed, checkpoint;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { started=false;committed=0;checkpoint=0; }
        public static void Start(GameSaveData save)
        {
            if(started || save==null || save.readOnly)return;
            started=true;committed=Safe(save.playTimeSeconds);checkpoint=Time.realtimeSinceStartupAsDouble;
        }
        public static double Current(GameSaveData save)
        {
            double stored=save==null?0:Safe(save.playTimeSeconds);
            return !started?stored:Math.Max(stored,committed)+Math.Max(0,Time.realtimeSinceStartupAsDouble-checkpoint);
        }
        internal static void Commit(double value)
        { if(started){committed=Safe(value);checkpoint=Time.realtimeSinceStartupAsDouble;} }
        internal static void Restart()
        { started=true;committed=0;checkpoint=Time.realtimeSinceStartupAsDouble; }
        private static double Safe(double value) { return double.IsNaN(value)||double.IsInfinity(value)||value<0?0:value; }
        public static string Format(double seconds)
        {
            long value=(long)Math.Min(Safe(seconds),long.MaxValue/2);
            return (value/3600)+"時間 "+((value/60)%60).ToString("00")+"分";
        }
    }
}
