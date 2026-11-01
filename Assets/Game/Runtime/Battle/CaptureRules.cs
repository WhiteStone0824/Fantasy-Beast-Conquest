using UnityEngine;

namespace FusionAnyMonster.Battle
{
    [CreateAssetMenu(menuName="Fusion Any Monster/Capture Rules")]
    public sealed class CaptureRules : ScriptableObject
    {
        [Range(.01f,1)] public float healthThreshold=.35f;
        [Range(0,1)] public float minimumChance=.4f, maximumChance=.95f;
        [Range(0,1)] public float bossChanceMultiplier=.5f;
        [Range(1,20)] public int maximumCapturesPerBattle=3;
        private static CaptureRules current;
        public static CaptureRules Current
        {get{if(current==null){current=Resources.Load<CaptureRules>("CaptureRules");if(current==null)current=CreateInstance<CaptureRules>();}return current;}}
        public float Chance(float healthRatio,bool boss)
        {
            float threshold=Mathf.Clamp(healthThreshold,.01f,1);
            if(float.IsNaN(healthRatio) || healthRatio<=0 || healthRatio>threshold)return 0;
            float low=Mathf.Clamp01(minimumChance),high=Mathf.Max(low,Mathf.Clamp01(maximumChance));
            return Mathf.Clamp01(Mathf.Lerp(low,high,1-healthRatio/threshold)*(boss?Mathf.Clamp01(bossChanceMultiplier):1));
        }
    }
}
