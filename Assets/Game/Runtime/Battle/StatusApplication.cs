using UnityEngine;
namespace FusionAnyMonster
{
    public static class StatusApplication
    {
        public static float Chance(float chance,float elementResistance,float statusResistance)
        {return Mathf.Clamp01(chance)* (1-Mathf.Clamp01(elementResistance)) * (1-Mathf.Clamp01(statusResistance));}
        public static bool Roll(float chance) {return chance>=1 || chance>0 && Random.value<chance;}
    }
}
